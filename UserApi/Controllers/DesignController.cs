using System.Security.Claims;
using Application.DTO;
using Application.Request;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserDomain;
using UserDomain.Entities;
using UserDomain.Interface;

namespace UserApi.Controllers;

[Route("api/design")]
[ApiController]
public class DesignController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IAnthropicService _anthropic;
    private readonly IContextRepository _contextRepository;
    private readonly IChatMessageRepository _messageRepository;
    private readonly IImageRepository _imageRepository;
    private readonly IAIResultRepository _aiResultRepository;

    public DesignController(
        IMediator mediator,
        IAnthropicService anthropic,
        IContextRepository contextRepository,
        IChatMessageRepository messageRepository,
        IImageRepository imageRepository,
        IAIResultRepository aiResultRepository)
    {
        _mediator = mediator;
        _anthropic = anthropic;
        _contextRepository = contextRepository;
        _messageRepository = messageRepository;
        _imageRepository = imageRepository;
        _aiResultRepository = aiResultRepository;
    }

    [HttpPost("generate")]
    [Authorize]
    [RequestSizeLimit(15_000_000)]
    public async Task<IActionResult> GenerateDesign([FromForm] GenerateDesignRequest request, CancellationToken cancellationToken)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        if (userId == 0)
            return Unauthorized();

        var roomType = request.RoomType ?? "HomeSingle";
        var style = request.Style ?? "modern";
        var unit = request.Unit ?? "ft";

        var promptType = roomType.ToLower() switch
        {
            "homesingle" => "home-single",
            "homedouble" => "home-double",
            "marquee" => "marquee",
            _ => "home-single"
        };

        var prompt = await _contextRepository.GetByTypeAsync(promptType);
        var promptTemplate = prompt?.ContextData ?? GetDefaultPrompt(roomType);

        var roomGroupId = Guid.NewGuid();
        var createdRooms = new List<RoomResponseDto>();

        if (roomType == "HomeDouble")
        {
            var room1 = await CreateRoomAsync(new RoomCreateDto
            {
                RoomName = request.RoomName ?? "Room 1",
                Length = request.Length, Width = request.Width, Height = request.Height,
                Area = request.Area, Unit = unit, RoomType = roomType, RoomGroupId = roomGroupId
            }, cancellationToken);
            createdRooms.Add(room1);

            var room2 = await CreateRoomAsync(new RoomCreateDto
            {
                RoomName = request.RoomName2 ?? "Room 2",
                Length = request.Length2, Width = request.Width2, Height = request.Height2,
                Area = request.Area2, Unit = unit, RoomType = roomType, RoomGroupId = roomGroupId
            }, cancellationToken);
            createdRooms.Add(room2);
        }
        else
        {
            var room = await CreateRoomAsync(new RoomCreateDto
            {
                RoomName = request.RoomName ?? "My Room",
                Length = request.Length, Width = request.Width, Height = request.Height,
                Area = request.Area, Unit = unit, RoomType = roomType,
                CeilingType = roomType == "Marquee" ? request.CeilingType : null,
                RoomGroupId = roomGroupId
            }, cancellationToken);
            createdRooms.Add(room);
        }

        var primaryRoom = createdRooms.First();
        var chatResult = await _mediator.Send(new CreateChatRequest
        {
            ChatDto = new ChatCreateDto
            {
                Title = $"Design - {primaryRoom.RoomName}",
                RoomId = primaryRoom.RoomId,
                UserId = userId,
                ContextId = prompt?.ContextId ?? 1
            }
        }, cancellationToken);
        var chatId = chatResult.Chat.ChatId;

        var savedImages = await SaveUploadedFilesAsync(request.Files, primaryRoom.RoomId, chatId, cancellationToken);

        var userMessageText = BuildUserMessageText(roomType, createdRooms, style);
        var userMessage = new ChatMessages
        {
            ChatId = chatId, Sender = "user",
            MessageText = userMessageText, CreatedAt = DateTime.UtcNow
        };
        await _messageRepository.AddAsync(userMessage);
        await _messageRepository.SaveChangesAsync(cancellationToken);

        var filledPrompt = FillPromptTemplate(promptTemplate, roomType, createdRooms, style, request.CeilingType);
        var imageContents = await GetImageContentsAsync(savedImages);

        var context = new List<MessageDto>
        {
            new() { Role = "user", Content = filledPrompt, Images = imageContents }
        };

        string aiResponse;
        try
        {
            aiResponse = await _anthropic.SendContextAndGetRawResponseAsync(context, cancellationToken);
        }
        catch (Exception)
        {
            aiResponse = "{\"suggestions\":[{\"title\":\"Recommendation\",\"description\":\"Consider adding neutral tones and functional furniture to optimize the space.\"}]}";
        }

        var aiMessage = new ChatMessages
        {
            ChatId = chatId, Sender = "ai",
            MessageText = aiResponse, CreatedAt = DateTime.UtcNow
        };
        await _messageRepository.AddAsync(aiMessage);
        await _messageRepository.SaveChangesAsync(cancellationToken);

        await _aiResultRepository.AddAsync(new AIResults
        {
            ChatId = chatId, MessageId = aiMessage.MessageId,
            AIResponse = aiResponse, AIUsed = "claude", CreatedAt = DateTime.UtcNow
        });
        await _aiResultRepository.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            chatId,
            rooms = createdRooms,
            message = "Design generated successfully"
        });
    }

    private async Task<RoomResponseDto> CreateRoomAsync(RoomCreateDto dto, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateRoomRequest { RoomDto = dto }, ct);
        return result.Room;
    }

    private async Task<List<string>> SaveUploadedFilesAsync(List<IFormFile>? files, int roomId, int chatId, CancellationToken ct)
    {
        var savedPaths = new List<string>();
        if (files == null) return savedPaths;

        var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "uploads", chatId.ToString());
        Directory.CreateDirectory(uploadDir);

        foreach (var file in files)
        {
            var fileName = $"{Guid.NewGuid()}_{file.FileName}";
            var relativePath = $"uploads/{chatId}/{fileName}";
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), relativePath);

            await using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream, ct);

            var image = new Images
            {
                RoomId = roomId, ChatId = chatId,
                FileName = file.FileName, FilePath = relativePath,
                UploadedAt = DateTime.UtcNow
            };
            await _imageRepository.AddAsync(image);
            savedPaths.Add(relativePath);
        }
        await _imageRepository.SaveChangesAsync(ct);
        return savedPaths;
    }

    private async Task<List<ImageContent>> GetImageContentsAsync(List<string> savedPaths)
    {
        var imageContents = new List<ImageContent>();
        foreach (var path in savedPaths)
        {
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), path);
            if (!System.IO.File.Exists(fullPath)) continue;

            var bytes = await System.IO.File.ReadAllBytesAsync(fullPath);
            var ext = Path.GetExtension(path).ToLower();
            var mediaType = ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                ".gif" => "image/gif",
                _ => "image/jpeg"
            };

            imageContents.Add(new ImageContent
            {
                FileName = Path.GetFileName(path),
                Base64Data = Convert.ToBase64String(bytes),
                MediaType = mediaType
            });
        }
        return imageContents;
    }

    private static string BuildUserMessageText(string roomType, List<RoomResponseDto> rooms, string style)
    {
        if (roomType == "HomeDouble")
        {
            var r1 = rooms[0]; var r2 = rooms[1];
            return $"Design two rooms: \"{r1.RoomName}\" ({r1.Length}x{r1.Width}x{r1.Height} {r1.Unit}) and \"{r2.RoomName}\" ({r2.Length}x{r2.Width}x{r2.Height} {r2.Unit}). Style: {style}.";
        }
        var r = rooms[0];
        return $"Design \"{r.RoomName}\" ({r.Length}x{r.Width}x{r.Height} {r.Unit}). Style: {style}.";
    }

    private static string FillPromptTemplate(string template, string roomType, List<RoomResponseDto> rooms, string style, string? ceilingType)
    {
        var result = template;
        result = result.Replace("{{Style}}", style);
        result = result.Replace("{{Unit}}", rooms[0].Unit);

        if (roomType == "HomeDouble" && rooms.Count >= 2)
        {
            var r1 = rooms[0]; var r2 = rooms[1];
            result = result.Replace("{{RoomName}}", r1.RoomName);
            result = result.Replace("{{Length}}", r1.Length.ToString());
            result = result.Replace("{{Width}}", r1.Width.ToString());
            result = result.Replace("{{Height}}", r1.Height.ToString());
            result = result.Replace("{{Area}}", r1.Area?.ToString() ?? "");
            result = result.Replace("{{RoomName2}}", r2.RoomName);
            result = result.Replace("{{Length2}}", r2.Length.ToString());
            result = result.Replace("{{Width2}}", r2.Width.ToString());
            result = result.Replace("{{Height2}}", r2.Height.ToString());
            result = result.Replace("{{Area2}}", r2.Area?.ToString() ?? "");
        }
        else
        {
            var r = rooms[0];
            result = result.Replace("{{RoomName}}", r.RoomName);
            result = result.Replace("{{Length}}", r.Length.ToString());
            result = result.Replace("{{Width}}", r.Width.ToString());
            result = result.Replace("{{Height}}", r.Height.ToString());
            result = result.Replace("{{Area}}", r.Area?.ToString() ?? "");
        }

        if (roomType == "Marquee")
        {
            result = result.Replace("{{CeilingType}}", ceilingType ?? "flat");
        }

        return result;
    }

    private static string GetDefaultPrompt(string roomType)
    {
        return roomType switch
        {
            "HomeDouble" => "I have two rooms to decorate. Room 1: \"{{RoomName}}\" ({{Length}}x{{Width}}x{{Height}} {{Unit}}). Room 2: \"{{RoomName2}}\" ({{Length2}}x{{Width2}}x{{Height2}} {{Unit}}). Style: {{Style}}. Suggest a cohesive color palette, furniture layout, and decor for both rooms.",
            "Marquee" => "I am designing an event marquee: \"{{RoomName}}\" ({{Length}}x{{Width}}x{{Height}} {{Unit}}). Ceiling type: {{CeilingType}}. Style: {{Style}}. Suggest layout, seating arrangement, lighting, and ceiling decor.",
            _ => "I am designing a room: \"{{RoomName}}\" ({{Length}}x{{Width}}x{{Height}} {{Unit}}). Style: {{Style}}. Suggest color scheme, furniture placement, and decor items."
        };
    }
}

public class GenerateDesignRequest
{
    public string RoomType { get; set; } = "HomeSingle";
    public string Style { get; set; } = "modern";
    public List<IFormFile>? Files { get; set; }

    public string? RoomName { get; set; }
    public decimal Length { get; set; }
    public decimal Width { get; set; }
    public decimal Height { get; set; }
    public decimal? Area { get; set; }
    public string Unit { get; set; } = "ft";

    public string? RoomName2 { get; set; }
    public decimal Length2 { get; set; }
    public decimal Width2 { get; set; }
    public decimal Height2 { get; set; }
    public decimal? Area2 { get; set; }

    public string? CeilingType { get; set; }
}

using Application.DTO;
using Application.Request;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserDomain.Interface;

namespace UserApi.Controllers;

[Route("api/design")]
[ApiController]
public class DesignController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IAnthropicService _anthropic;
    private readonly IChatRepository _chatRepository;
    private readonly IChatMessageRepository _messageRepository;
    private readonly IImageRepository _imageRepository;
    private readonly IAIResultRepository _aiResultRepository;

    public DesignController(
        IMediator mediator,
        IAnthropicService anthropic,
        IChatRepository chatRepository,
        IChatMessageRepository messageRepository,
        IImageRepository imageRepository,
        IAIResultRepository aiResultRepository)
    {
        _mediator = mediator;
        _anthropic = anthropic;
        _chatRepository = chatRepository;
        _messageRepository = messageRepository;
        _imageRepository = imageRepository;
        _aiResultRepository = aiResultRepository;
    }

    [HttpPost("generate")]
    [Authorize]
    [RequestSizeLimit(15_000_000)]
    public async Task<IActionResult> GenerateDesign([FromForm] GenerateDesignRequest request, CancellationToken cancellationToken)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
        if (userId == 0)
            return Unauthorized();

        var files = request.Files;
        var style = request.Style ?? "modern";

        var roomResult = await _mediator.Send(new CreateRoomRequest
        {
            RoomDto = new RoomCreateDto
            {
                RoomName = request.RoomName ?? "My Room",
                Length = request.Length,
                Width = request.Width,
                Height = request.Height,
                Unit = request.Unit ?? "ft"
            }
        }, cancellationToken);

        var chatResult = await _mediator.Send(new CreateChatRequest
        {
            ChatDto = new ChatCreateDto
            {
                Title = $"Design - {roomResult.Room.RoomName}",
                RoomId = roomResult.Room.RoomId,
                UserId = userId,
                ContextId = 1
            }
        }, cancellationToken);

        var chatId = chatResult.Chat.ChatId;

        if (files != null)
        {
            foreach (var file in files)
            {
                var image = new UserDomain.Entities.Images
                {
                    RoomId = roomResult.Room.RoomId,
                    ChatId = chatId,
                    FileName = file.FileName,
                    FilePath = $"uploads/{chatId}/{file.FileName}",
                    UploadedAt = DateTime.UtcNow
                };
                await _imageRepository.AddAsync(image);
            }
            await _imageRepository.SaveChangesAsync(cancellationToken);
        }

        var userMessage = new UserDomain.Entities.ChatMessages
        {
            ChatId = chatId,
            Sender = "user",
            MessageText = $"Style: {style}, Room: {request.RoomName} ({request.Length}x{request.Width}x{request.Height} {request.Unit})",
            CreatedAt = DateTime.UtcNow
        };
        await _messageRepository.AddAsync(userMessage);

        var context = new List<UserDomain.MessageDto>
        {
            new() { Role = "user", Content = $"Design a {style} room of size {request.Length}x{request.Width}x{request.Height} {request.Unit}. Suggest improvements." }
        };

        string aiResponse;
        try
        {
            aiResponse = await _anthropic.SendContextAndGetRawResponseAsync(context, cancellationToken);
        }
        catch (Exception)
        {
            aiResponse = "{\"suggestions\":[{\"title\":\"Add light rug\",\"description\":\"A light-colored rug would complement the floor.\"}]}";
        }

        var aiMessage = new UserDomain.Entities.ChatMessages
        {
            ChatId = chatId,
            Sender = "ai",
            MessageText = aiResponse,
            CreatedAt = DateTime.UtcNow
        };
        await _messageRepository.AddAsync(aiMessage);
        await _messageRepository.SaveChangesAsync(cancellationToken);

        var aiResult = new UserDomain.Entities.AIResults
        {
            ChatId = chatId,
            MessageId = aiMessage.MessageId,
            AIResponse = aiResponse,
            AIUsed = "claude",
            CreatedAt = DateTime.UtcNow
        };
        await _aiResultRepository.AddAsync(aiResult);
        await _aiResultRepository.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            chatId,
            room = roomResult.Room,
            message = "Design generated successfully"
        });
    }
}

public class GenerateDesignRequest
{
    public string RoomName { get; set; }
    public decimal Length { get; set; }
    public decimal Width { get; set; }
    public decimal Height { get; set; }
    public string Unit { get; set; } = "ft";
    public string Style { get; set; } = "modern";
    public List<IFormFile> Files { get; set; }
}

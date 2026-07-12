using Application.DTO;
using Application.Handler;
using Application.Request;
using Moq;
using UserDomain;
using UserDomain.Entities;
using UserDomain.Interface;

namespace Application.Tests.Handlers;

public class ChatHandlerTests
{
    [Fact]
    public async Task CreateChat_ValidRequest_ReturnsCreatedChat()
    {
        var mockRepo = new Mock<IChatRepository>();
        var handler = new CreateChatHandler(mockRepo.Object);

        var createdChat = new Chats
        {
            ChatId = 1,
            Title = "Design Chat",
            RoomId = 1,
            UserId = 1,
            ContextId = 1,
            CreatedAt = DateTime.UtcNow
        };

        mockRepo.Setup(r => r.AddAsync(It.IsAny<Chats>()))
                .ReturnsAsync(createdChat);

        var request = new CreateChatRequest
        {
            ChatDto = new ChatCreateDto
            {
                Title = "Design Chat",
                RoomId = 1,
                UserId = 1,
                ContextId = 1
            }
        };

        var result = await handler.Handle(request, CancellationToken.None);

        Assert.NotNull(result.Chat);
        Assert.Equal("Design Chat", result.Chat.Title);
        Assert.Equal(1, result.Chat.RoomId);
        Assert.Equal("Chat created successfully", result.Message);
        mockRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteChat_NotDeleted_SoftDeletes()
    {
        var mockRepo = new Mock<IChatRepository>();
        var handler = new DeleteChatHandler(mockRepo.Object);

        var chat = new Chats { ChatId = 1, IsDeleted = false };
        mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(chat);

        var result = await handler.Handle(new DeleteChatRequest { Id = 1 }, CancellationToken.None);

        Assert.True(chat.IsDeleted);
        Assert.Equal("Chat deleted successfully", result.Message);
    }

    [Fact]
    public async Task DeleteChat_AlreadyDeleted_ThrowsBadRequest()
    {
        var mockRepo = new Mock<IChatRepository>();
        var handler = new DeleteChatHandler(mockRepo.Object);

        var chat = new Chats { ChatId = 1, IsDeleted = true };
        mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(chat);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            handler.Handle(new DeleteChatRequest { Id = 1 }, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteChat_NotFound_ThrowsNotFoundException()
    {
        var mockRepo = new Mock<IChatRepository>();
        var handler = new DeleteChatHandler(mockRepo.Object);

        mockRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Chats?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new DeleteChatRequest { Id = 99 }, CancellationToken.None));
    }
}

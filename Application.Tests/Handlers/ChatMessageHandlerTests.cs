using Application.DTO;
using Application.Handler;
using Application.Request;
using Moq;
using UserDomain;
using UserDomain.Entities;
using UserDomain.Interface;

namespace Application.Tests.Handlers;

public class ChatMessageHandlerTests
{
    [Fact]
    public async Task CreateMessage_ValidRequest_ReturnsCreatedMessage()
    {
        var mockRepo = new Mock<IChatMessageRepository>();
        var handler = new CreateChatMessageHandler(mockRepo.Object);

        var created = new ChatMessages
        {
            MessageId = 1,
            ChatId = 1,
            Sender = "user",
            MessageText = "Hello!",
            CreatedAt = DateTime.Now
        };

        mockRepo.Setup(r => r.AddAsync(It.IsAny<ChatMessages>()))
                .ReturnsAsync(created);

        var request = new CreateChatMessageRequest
        {
            ChatMessageDto = new ChatMessageCreateDto
            {
                ChatId = 1,
                Sender = "user",
                MessageText = "Hello!"
            }
        };

        var result = await handler.Handle(request, CancellationToken.None);

        Assert.NotNull(result.ChatMessage);
        Assert.Equal("Hello!", result.ChatMessage.MessageText);
        Assert.Equal("user", result.ChatMessage.Sender);
        Assert.Equal("Chat message created successfully", result.Message);
        mockRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteMessage_AlreadyDeleted_ThrowsBadRequest()
    {
        var mockRepo = new Mock<IChatMessageRepository>();
        var handler = new DeleteChatMessageHandler(mockRepo.Object);

        var msg = new ChatMessages { MessageId = 1, IsDeleted = true };
        mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(msg);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            handler.Handle(new DeleteChatMessageRequest { Id = 1 }, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteMessage_NotDeleted_SoftDeletes()
    {
        var mockRepo = new Mock<IChatMessageRepository>();
        var handler = new DeleteChatMessageHandler(mockRepo.Object);

        var msg = new ChatMessages { MessageId = 1, IsDeleted = false };
        mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(msg);

        var result = await handler.Handle(new DeleteChatMessageRequest { Id = 1 }, CancellationToken.None);

        Assert.True(msg.IsDeleted);
        Assert.Equal("Chat message deleted successfully", result.Message);
    }
}

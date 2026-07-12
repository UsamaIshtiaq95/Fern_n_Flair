using Application.DTO;
using Application.Handler;
using Application.Request;
using Moq;
using UserDomain;
using UserDomain.Entities;
using UserDomain.Interface;

namespace Application.Tests.Handlers;

public class RoomHandlerTests
{
    [Fact]
    public async Task CreateRoom_ValidRequest_ReturnsCreatedRoom()
    {
        var mockRepo = new Mock<IRoomRepository>();
        var handler = new CreateRoomHandler(mockRepo.Object);

        var createdRoom = new Rooms
        {
            RoomId = 1,
            RoomName = "Living Room",
            Length = 10,
            Width = 12,
            Height = 8,
            Unit = "ft",
            CreatedAt = DateTime.UtcNow
        };

        mockRepo.Setup(r => r.AddAsync(It.IsAny<Rooms>()))
                .ReturnsAsync(createdRoom);

        var request = new CreateRoomRequest
        {
            RoomDto = new RoomCreateDto
            {
                RoomName = "Living Room",
                Length = 10,
                Width = 12,
                Height = 8,
                Unit = "ft"
            }
        };

        var result = await handler.Handle(request, CancellationToken.None);

        Assert.NotNull(result.Room);
        Assert.Equal("Living Room", result.Room.RoomName);
        Assert.Equal(10, result.Room.Length);
        Assert.Equal(12, result.Room.Width);
        Assert.Equal(8, result.Room.Height);
        Assert.Equal("ft", result.Room.Unit);
        Assert.Equal("Room created successfully", result.Message);
        mockRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateRoom_DefaultUnitIsFt()
    {
        var mockRepo = new Mock<IRoomRepository>();
        var handler = new CreateRoomHandler(mockRepo.Object);

        mockRepo.Setup(r => r.AddAsync(It.IsAny<Rooms>()))
                .ReturnsAsync(new Rooms { RoomId = 2, Unit = "ft" });

        var request = new CreateRoomRequest
        {
            RoomDto = new RoomCreateDto
            {
                RoomName = "Bedroom",
                Length = 12,
                Width = 10,
                Height = 8
            }
        };

        var result = await handler.Handle(request, CancellationToken.None);

        Assert.Equal("ft", result.Room.Unit);
    }

    [Fact]
    public async Task GetAllRooms_WithoutPagination_ReturnsAllRooms()
    {
        var mockRepo = new Mock<IRoomRepository>();
        var handler = new GetAllRoomsHandler(mockRepo.Object);

        var rooms = new List<Rooms>
        {
            new() { RoomId = 1, RoomName = "Room 1", Length = 10, Width = 12, Height = 8, Unit = "ft", CreatedAt = DateTime.UtcNow },
            new() { RoomId = 2, RoomName = "Room 2", Length = 14, Width = 16, Height = 9, Unit = "ft", CreatedAt = DateTime.UtcNow }
        };

        mockRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(rooms);

        var result = await handler.Handle(new GetAllRoomsRequest(), CancellationToken.None);

        Assert.Equal(2, result.Rooms.Count());
    }

    [Fact]
    public async Task GetAllRooms_WithPagination_RespectsSkipTake()
    {
        var mockRepo = new Mock<IRoomRepository>();
        var handler = new GetAllRoomsHandler(mockRepo.Object);

        mockRepo.Setup(r => r.GetAllAsync(0, 10))
                .ReturnsAsync(new List<Rooms>());

        var request = new GetAllRoomsRequest { Skip = 0, Take = 10 };

        await handler.Handle(request, CancellationToken.None);

        mockRepo.Verify(r => r.GetAllAsync(0, 10), Times.Once);
        mockRepo.Verify(r => r.GetAllAsync(), Times.Never);
    }

    [Fact]
    public async Task GetRoomById_RoomExists_ReturnsRoom()
    {
        var mockRepo = new Mock<IRoomRepository>();
        var handler = new GetRoomByIdHandler(mockRepo.Object);

        var room = new Rooms
        {
            RoomId = 5,
            RoomName = "Kitchen",
            Length = 15,
            Width = 10,
            Height = 8,
            Unit = "ft",
            CreatedAt = DateTime.UtcNow
        };

        mockRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(room);

        var result = await handler.Handle(new GetRoomByIdRequest { Id = 5 }, CancellationToken.None);

        Assert.Equal("Kitchen", result.Room.RoomName);
    }

    [Fact]
    public async Task GetRoomById_RoomNotFound_ThrowsNotFoundException()
    {
        var mockRepo = new Mock<IRoomRepository>();
        var handler = new GetRoomByIdHandler(mockRepo.Object);

        mockRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Rooms?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new GetRoomByIdRequest { Id = 99 }, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteRoom_RoomExists_SoftDeletes()
    {
        var mockRepo = new Mock<IRoomRepository>();
        var handler = new DeleteRoomHandler(mockRepo.Object);

        var room = new Rooms
        {
            RoomId = 1,
            RoomName = "To Delete",
            IsDeleted = false
        };

        mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(room);

        var result = await handler.Handle(new DeleteRoomRequest { Id = 1 }, CancellationToken.None);

        Assert.True(room.IsDeleted);
        Assert.Equal("Room deleted successfully", result.Message);
        mockRepo.Verify(r => r.UpdateAsync(room), Times.Once);
        mockRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteRoom_AlreadyDeleted_ThrowsBadRequest()
    {
        var mockRepo = new Mock<IRoomRepository>();
        var handler = new DeleteRoomHandler(mockRepo.Object);

        var room = new Rooms { RoomId = 1, IsDeleted = true };
        mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(room);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            handler.Handle(new DeleteRoomRequest { Id = 1 }, CancellationToken.None));
    }
}

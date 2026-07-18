using Application.DTO;
using Application.Request;
using MediatR;
using UserDomain;
using UserDomain.Entities;
using UserDomain.Interface;

namespace Application.Handler;

public class CreateRoomHandler : IRequestHandler<CreateRoomRequest, CreateRoomResponse>
{
    private readonly IRoomRepository _roomRepository;

    public CreateRoomHandler(IRoomRepository roomRepository)
    {
        _roomRepository = roomRepository;
    }

    public async Task<CreateRoomResponse> Handle(CreateRoomRequest request, CancellationToken cancellationToken)
    {
        var room = new Rooms
        {
            RoomName = request.RoomDto.RoomName,
            Length = request.RoomDto.Length,
            Width = request.RoomDto.Width,
            Height = request.RoomDto.Height,
            Area = request.RoomDto.Area,
            Unit = request.RoomDto.Unit ?? "ft",
            RoomType = request.RoomDto.RoomType ?? "HomeSingle",
            CeilingType = request.RoomDto.CeilingType,
            RoomGroupId = request.RoomDto.RoomGroupId,
            CreatedAt = DateTime.UtcNow
        };

        var createdRoom = await _roomRepository.AddAsync(room);
        await _roomRepository.SaveChangesAsync(cancellationToken);

        return new CreateRoomResponse
        {
            Room = new RoomResponseDto
            {
                RoomId = createdRoom.RoomId,
                RoomName = createdRoom.RoomName,
                Length = createdRoom.Length,
                Width = createdRoom.Width,
                Height = createdRoom.Height,
                Area = createdRoom.Area,
                Unit = createdRoom.Unit,
                RoomType = createdRoom.RoomType,
                CeilingType = createdRoom.CeilingType,
                RoomGroupId = createdRoom.RoomGroupId,
                CreatedAt = createdRoom.CreatedAt
            },
            Message = "Room created successfully"
        };
    }
}

public class GetAllRoomsHandler : IRequestHandler<GetAllRoomsRequest, GetAllRoomsResponse>
{
    private readonly IRoomRepository _roomRepository;

    public GetAllRoomsHandler(IRoomRepository roomRepository)
    {
        _roomRepository = roomRepository;
    }

    public async Task<GetAllRoomsResponse> Handle(GetAllRoomsRequest request, CancellationToken cancellationToken)
    {
        IEnumerable<Rooms> rooms;

        if (request.Skip.HasValue && request.Take.HasValue)
            rooms = await _roomRepository.GetAllAsync(request.Skip.Value, request.Take.Value);
        else
            rooms = await _roomRepository.GetAllAsync();

        var roomDtos = rooms.Select(r => new RoomResponseDto
        {
            RoomId = r.RoomId,
            RoomName = r.RoomName,
            Length = r.Length,
            Width = r.Width,
            Height = r.Height,
            Area = r.Area,
            Unit = r.Unit,
            RoomType = r.RoomType,
            CeilingType = r.CeilingType,
            RoomGroupId = r.RoomGroupId,
            CreatedAt = r.CreatedAt
        });

        return new GetAllRoomsResponse { Rooms = roomDtos };
    }
}

public class GetRoomByIdHandler : IRequestHandler<GetRoomByIdRequest, GetRoomByIdResponse>
{
    private readonly IRoomRepository _roomRepository;

    public GetRoomByIdHandler(IRoomRepository roomRepository)
    {
        _roomRepository = roomRepository;
    }

    public async Task<GetRoomByIdResponse> Handle(GetRoomByIdRequest request, CancellationToken cancellationToken)
    {
        var room = await _roomRepository.GetByIdAsync(request.Id);

        if (room == null)
            throw new NotFoundException("Room not found");

        return new GetRoomByIdResponse
        {
            Room = new RoomResponseDto
            {
                RoomId = room.RoomId,
                RoomName = room.RoomName,
                Length = room.Length,
                Width = room.Width,
                Height = room.Height,
                Area = room.Area,
                Unit = room.Unit,
                RoomType = room.RoomType,
                CeilingType = room.CeilingType,
                RoomGroupId = room.RoomGroupId,
                CreatedAt = room.CreatedAt
            }
        };
    }
}

public class UpdateRoomHandler : IRequestHandler<UpdateRoomRequest, UpdateRoomResponse>
{
    private readonly IRoomRepository _roomRepository;

    public UpdateRoomHandler(IRoomRepository roomRepository)
    {
        _roomRepository = roomRepository;
    }

    public async Task<UpdateRoomResponse> Handle(UpdateRoomRequest request, CancellationToken cancellationToken)
    {
        var room = await _roomRepository.GetByIdAsync(request.Id);

        if (room == null)
            throw new NotFoundException("Room not found");

        room.RoomName = request.RoomDto.RoomName;
        room.Length = request.RoomDto.Length;
        room.Width = request.RoomDto.Width;
        room.Height = request.RoomDto.Height;
        room.Area = request.RoomDto.Area;
        room.Unit = request.RoomDto.Unit ?? room.Unit;
        room.CeilingType = request.RoomDto.CeilingType ?? room.CeilingType;
        room.UpdatedAt = DateTime.UtcNow;

        await _roomRepository.UpdateAsync(room);
        await _roomRepository.SaveChangesAsync(cancellationToken);

        return new UpdateRoomResponse { Message = "Room updated successfully" };
    }
}

public class DeleteRoomHandler : IRequestHandler<DeleteRoomRequest, DeleteRoomResponse>
{
    private readonly IRoomRepository _roomRepository;

    public DeleteRoomHandler(IRoomRepository roomRepository)
    {
        _roomRepository = roomRepository;
    }

    public async Task<DeleteRoomResponse> Handle(DeleteRoomRequest request, CancellationToken cancellationToken)
    {
        var room = await _roomRepository.GetByIdAsync(request.Id);

        if (room == null)
            throw new NotFoundException("Room not found");

        if (room.IsDeleted)
            throw new BadRequestException("Room already deleted");

        room.IsDeleted = true;
        room.UpdatedAt = DateTime.UtcNow;
        await _roomRepository.UpdateAsync(room);
        await _roomRepository.SaveChangesAsync(cancellationToken);

        return new DeleteRoomResponse { Message = "Room deleted successfully" };
    }
}

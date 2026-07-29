using MatchmakingEngine.Application.Interfaces.Repositories;
using MediatR;
using MatchmakingEngine.Domain.Exceptions;
using MatchmakingEngine.Domain;
namespace MatchmakingEngine.Application.Application.Commands.Friends;

public record SendFriendRequestCommand(Guid SenderId, Guid ReceiverId) : IRequest<bool>;
public class SendFriendRequestCommandHandler : IRequestHandler<SendFriendRequestCommand, bool>
{
    private readonly IFriendshipRepository _friendshipRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SendFriendRequestCommandHandler(IFriendshipRepository friendshipRepository, IUnitOfWork unitOfWork)
    {
        _friendshipRepository = friendshipRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(SendFriendRequestCommand request, CancellationToken cancellationToken)
    {
        if (request.SenderId == request.ReceiverId)
            throw new ConflictException("You cannot send a friend requests to yourself.");

        var existingFriedship = await _friendshipRepository.GetBetweenPlayersAsync(request.SenderId, request.ReceiverId, cancellationToken);
        if (existingFriedship != null)
            throw new ConflictException("A friendship or pending requests already exists between these players.");

        var friendship = new Friendship
        {
            SenderId = request.SenderId,
            ReceiverId = request.ReceiverId,
            Status = FriendshipStatus.Pending
        };

        await _friendshipRepository.AddAsync(friendship, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}

using MatchmakingEngine.Application.Interfaces.Repositories;
using MediatR;
using MatchmakingEngine.Domain.Exceptions;

namespace MatchmakingEngine.Application.Application.Commands.Friends;

public record AcceptFriendRequestCommand(Guid RequestId, Guid PlayerId) : IRequest<bool>;

public class AcceptFriendRequestCommandHandler : IRequestHandler<AcceptFriendRequestCommand, bool>
{
    private readonly IFriendshipRepository _friendshipRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AcceptFriendRequestCommandHandler(IFriendshipRepository friendshipRepository, IUnitOfWork unitOfWork)
    {
        _friendshipRepository = friendshipRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(AcceptFriendRequestCommand request, CancellationToken cancellationToken)
    {
        var friendship = await _friendshipRepository.GetByIdAsync(request.RequestId, trackChanges: true, cancellationToken);

        if (friendship == null)
            throw new NotFoundException("Friend request not found.");

        if (friendship.ReceiverId != request.PlayerId)
            throw new ConflictException("You can only accept requests sent to you.");

        if (friendship.Status != Domain.FriendshipStatus.Pending)
            throw new ConflictException("This request is no longer pending.");

        friendship.Status = Domain.FriendshipStatus.Accepted;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;

    }
}
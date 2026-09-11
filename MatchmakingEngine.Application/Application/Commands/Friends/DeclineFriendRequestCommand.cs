using MatchmakingEngine.Application.Interfaces.Repositories;
using MediatR;
using MatchmakingEngine.Domain.Exceptions;

namespace MatchmakingEngine.Application.Application.Commands.Friends;

public record DeclineFriendRequestCommand(Guid RequestId, Guid PlayerId) : IRequest<Guid?>;

public class DeclineFriendRequestCommandHandle : IRequestHandler<DeclineFriendRequestCommand, Guid?>
{
    private readonly IFriendshipRepository _friendshipRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeclineFriendRequestCommandHandle(IFriendshipRepository friendshipRepository, IUnitOfWork unitOfWork)
    {
         _friendshipRepository = friendshipRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid?> Handle(DeclineFriendRequestCommand request, CancellationToken cancellationToken)
    {
        var friendship = await _friendshipRepository.GetByIdAsync(request.RequestId, trackChanges: true, cancellationToken);

        if (friendship == null)
            throw new NotFoundException("Friend request not found.");


        if (friendship.ReceiverId != request.PlayerId)
            throw new ConflictException("You can only decline requests sent to you.");

        if (friendship.Status != Domain.FriendshipStatus.Pending)
            throw new ConflictException("This request is no longer pending.");

        friendship.Status = Domain.FriendshipStatus.Declined;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return friendship.SenderId;
    }
}
using MatchmakingEngine.Application.Interfaces.Repositories;
using MediatR;
using MatchmakingEngine.Domain.Exceptions;

namespace MatchmakingEngine.Application.Application.Commands.Friends;

public record RemoveFriendCommand(Guid FriendshipId, Guid PlayerId) : IRequest<bool>;

public class RemoveFriendCommandHandler : IRequestHandler<RemoveFriendCommand, bool>
{
    private readonly IFriendshipRepository _friendshipRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveFriendCommandHandler(IFriendshipRepository friendshipRepository, IUnitOfWork unitOfWork)
    {
        _friendshipRepository = friendshipRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(RemoveFriendCommand request, CancellationToken cancellationToken)
    {
        var friendship = await _friendshipRepository.GetByIdAsync(request.FriendshipId, trackChanges: true, cancellationToken);

        if (friendship == null)
            throw new NotFoundException("Friendship not found.");

        if (friendship.SenderId != request.PlayerId && friendship.ReceiverId != request.PlayerId)
            throw new ConflictException("You are not part of this friendship");

        _friendshipRepository.Delete(friendship);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
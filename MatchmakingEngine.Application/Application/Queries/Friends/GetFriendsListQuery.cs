
using MatchmakingEngine.Application.DTO;
using MatchmakingEngine.Application.Interfaces.Repositories;
using MediatR;

namespace MatchmakingEngine.Application.Application.Queries.Friends;

public record GetFriendsListQuery(Guid PlayerId) : IRequest<List<FriendDto>>;

public class GetFriendsListQueryHandler : IRequestHandler<GetFriendsListQuery, List<FriendDto>>
{
    private readonly IFriendshipRepository _friendshipRepository;

    public GetFriendsListQueryHandler(IFriendshipRepository friendshipRepository)
    {
        _friendshipRepository = friendshipRepository;
    }

    public async Task<List<FriendDto>> Handle(GetFriendsListQuery request, CancellationToken cancellationToken)
    {
        var friendships = await _friendshipRepository.GetFriendsAsync(request.PlayerId, cancellationToken);

        return friendships.Select(f =>
        {
            var friend = f.SenderId == request.PlayerId ? f.Receiver : f.Sender;

            return new FriendDto(
                f.Id,
                friend.Id,
                friend.Username,
                friend.Mmr,
                friend.Region
                );
        }).ToList();
    }
}
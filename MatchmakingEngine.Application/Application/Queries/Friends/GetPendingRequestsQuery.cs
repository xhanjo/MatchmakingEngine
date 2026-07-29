using MatchmakingEngine.Application.DTO;
using MatchmakingEngine.Application.Interfaces.Repositories;
using MediatR;

namespace MatchmakingEngine.Application.Application.Queries.Friends;

public record GetPendingRequestsQuery(Guid PlayerId) : IRequest<List<FriendRequestDto>>;

public class GetPendingRequestsQueryHandler : IRequestHandler<GetPendingRequestsQuery, List<FriendRequestDto>>
{
    private readonly IFriendshipRepository _friendshipRepository;

    public GetPendingRequestsQueryHandler(IFriendshipRepository friendshipRepository)
    {
        _friendshipRepository = friendshipRepository;
    }
    public async Task<List<FriendRequestDto>> Handle(GetPendingRequestsQuery request, CancellationToken cancellationToken)
    {
        var requests = await _friendshipRepository.GetPendingRequestsAsync(request.PlayerId, cancellationToken);

        return requests.Select(f => new FriendRequestDto(
            f.Id,
            f.SenderId,
            f.Sender.Username,
            f.CreatedAt
            )).ToList();
    }
}
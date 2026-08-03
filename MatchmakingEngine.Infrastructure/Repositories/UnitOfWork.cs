using MediatR;
using MatchmakingEngine.Application.Interfaces.Repositories;
using MatchmakingEngine.Data;
using MatchmakingEngine.Domain.Common;
using Microsoft.EntityFrameworkCore;
using MatchmakingEngine.Domain.Exceptions;

namespace MatchmakingEngine.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly MatchmakingDbContext _context;
    private readonly IMediator _mediator;
    public UnitOfWork(MatchmakingDbContext context, IMediator mediator)
    {
        _context = context;
        _mediator = mediator;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var domainEntities = _context.ChangeTracker
            .Entries<Entity>()
            .Where(x => x.Entity.DomainEvents != null && x.Entity.DomainEvents.Any())
            .ToList();

        var domainEvents = domainEntities
            .SelectMany(x => x.Entity.DomainEvents)
            .ToList();

        domainEntities.ForEach(entity => entity.Entity.ClearDomainEvents());

        int result;

        try
        {
            result = await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
        {
            throw new MatchmakingEngine.Domain.Exceptions.ConflictException("Concurrency conflict occured. Please try again.");
        }

        foreach (var domainEvent in domainEvents)
        {
            await _mediator.Publish(domainEvent, cancellationToken);
        }

        return result;
    }
}

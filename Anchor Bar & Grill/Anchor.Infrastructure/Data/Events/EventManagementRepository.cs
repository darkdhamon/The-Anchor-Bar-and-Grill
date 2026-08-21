using Anchor.Domain.Events;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Anchor.Infrastructure.Data.Events;

public sealed class EventManagementRepository(ApplicationDbContext dbContext) : IEventManagementRepository
{
    public async Task<EventManagementPage> GetEventsAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Events.AsNoTracking();
        var totalCount = await query.CountAsync(cancellationToken);
        var maxSortOrder = await query.Select(item => (int?)item.SortOrder).MaxAsync(cancellationToken) ?? 0;
        var promoBadges = await query
            .Where(item => item.PromoBadge != null && item.PromoBadge != "")
            .Select(item => item.PromoBadge!)
            .Distinct()
            .OrderBy(item => item)
            .ToListAsync(cancellationToken);
        var items = await query
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.StartsOn)
            .ThenBy(item => item.Title)
            .ThenBy(item => item.EventId)
            .Skip(skip)
            .Take(take)
            .Select(Projection)
            .ToListAsync(cancellationToken);

        return new EventManagementPage(items, totalCount, maxSortOrder, promoBadges);
    }

    public Task<EventRecord?> GetEventAsync(Guid eventId, CancellationToken cancellationToken = default) =>
        dbContext.Events
            .AsNoTracking()
            .Where(item => item.EventId == eventId)
            .Select(Projection)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<int?> GetEventIndexAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        var target = await dbContext.Events
            .AsNoTracking()
            .Where(item => item.EventId == eventId)
            .Select(item => new { item.SortOrder, item.StartsOn, item.Title })
            .SingleOrDefaultAsync(cancellationToken);
        if (target is null)
        {
            return null;
        }

        var precedingCount = await dbContext.Events.CountAsync(item =>
            item.SortOrder < target.SortOrder
            || (item.SortOrder == target.SortOrder && item.StartsOn < target.StartsOn)
            || (item.SortOrder == target.SortOrder && item.StartsOn == target.StartsOn && string.Compare(item.Title, target.Title) < 0), cancellationToken);
        var tiedIds = await dbContext.Events
            .Where(item => item.SortOrder == target.SortOrder && item.StartsOn == target.StartsOn && item.Title == target.Title)
            .OrderBy(item => item.EventId)
            .Select(item => item.EventId)
            .ToListAsync(cancellationToken);
        var tiedIndex = tiedIds.IndexOf(eventId);
        return tiedIndex < 0 ? null : precedingCount + tiedIndex;
    }

    public async Task<Guid?> UpsertEventAsync(SaveEventRequest request, CancellationToken cancellationToken = default)
    {
        var entity = request.EventId.HasValue
            ? await dbContext.Events.SingleOrDefaultAsync(item => item.EventId == request.EventId.Value, cancellationToken)
            : null;

        if (entity is null && request.EventId.HasValue)
        {
            return null;
        }

        if (entity is null)
        {
            entity = new EventEntity
            {
                EventId = request.EventId ?? Guid.NewGuid(),
                Revision = Guid.NewGuid()
            };

            dbContext.Events.Add(entity);
        }
        else if ((request.ExpectedRevision ?? Guid.Empty) != entity.Revision)
        {
            return null;
        }
        else
        {
            entity.Revision = Guid.NewGuid();
        }

        entity.Title = request.Title.Trim();
        entity.Summary = request.Summary.Trim();
        entity.Description = request.Description.Trim();
        entity.PromoBadge = string.IsNullOrWhiteSpace(request.PromoBadge) ? null : request.PromoBadge.Trim();
        entity.ImagePath = string.IsNullOrWhiteSpace(request.ImagePath) ? null : request.ImagePath.Trim();
        entity.TimingNotes = string.IsNullOrWhiteSpace(request.TimingNotes) ? null : request.TimingNotes.Trim();
        entity.StartsOn = request.StartsOn;
        entity.StartsAt = request.StartsAt;
        entity.EndsAt = request.EndsAt;
        entity.EndsNextDay = request.EndsNextDay;
        entity.SortOrder = request.SortOrder;
        entity.PublicationState = request.PublicationState;
        entity.RecurrencePattern = request.RecurrencePattern;
        entity.RecurrenceInterval = request.RecurrencePattern == EventRecurrencePattern.None ? 1 : request.RecurrenceInterval;
        entity.RecursOnDayOfWeek = request.RecurrencePattern == EventRecurrencePattern.None ? null : request.RecursOnDayOfWeek;
        entity.RecursOnWeekOfMonth = request.RecurrencePattern == EventRecurrencePattern.MonthlyNthWeekday ? request.RecursOnWeekOfMonth : null;
        entity.RecursUntil = request.RecurrencePattern == EventRecurrencePattern.None ? null : request.RecursUntil;

        return entity.EventId;
    }

    public async Task<bool> DeleteEventAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Events.SingleOrDefaultAsync(item => item.EventId == eventId, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        dbContext.Events.Remove(entity);
        return true;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);

    private static readonly Expression<Func<EventEntity, EventRecord>> Projection = item =>
        new EventRecord(
            item.EventId,
            item.Title,
            item.Summary,
            item.Description,
            item.PromoBadge,
            item.ImagePath,
            item.StartsOn,
            item.StartsAt,
            item.EndsAt,
            item.EndsNextDay,
            item.SortOrder,
            item.PublicationState,
            item.RecurrencePattern,
            item.RecurrenceInterval,
            item.RecursOnDayOfWeek,
            item.RecursOnWeekOfMonth,
            item.RecursUntil,
            item.TimingNotes)
        {
            Revision = item.Revision
        };
}

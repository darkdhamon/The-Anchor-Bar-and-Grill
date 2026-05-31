using Anchor.Domain.Events;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Anchor.Infrastructure.Data.Events;

public sealed class EventManagementRepository(ApplicationDbContext dbContext) : IEventManagementRepository
{
    public async Task<IReadOnlyList<EventRecord>> GetEventsAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Events
            .AsNoTracking()
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.StartsOn)
            .ThenBy(item => item.Title)
            .Select(Projection)
            .ToListAsync(cancellationToken);

    public async Task<Guid> UpsertEventAsync(SaveEventRequest request, CancellationToken cancellationToken = default)
    {
        var entity = request.EventId.HasValue
            ? await dbContext.Events.SingleOrDefaultAsync(item => item.EventId == request.EventId.Value, cancellationToken)
            : null;

        if (entity is null)
        {
            entity = new EventEntity
            {
                EventId = request.EventId ?? Guid.NewGuid()
            };

            dbContext.Events.Add(entity);
        }

        entity.Title = request.Title.Trim();
        entity.Summary = request.Summary.Trim();
        entity.Description = request.Description.Trim();
        entity.PromoBadge = string.IsNullOrWhiteSpace(request.PromoBadge) ? null : request.PromoBadge.Trim();
        entity.ImagePath = string.IsNullOrWhiteSpace(request.ImagePath) ? null : request.ImagePath.Trim();
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
            item.RecursUntil);
}

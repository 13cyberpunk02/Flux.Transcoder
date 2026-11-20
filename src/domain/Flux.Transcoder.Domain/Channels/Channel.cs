using Flux.Transcoder.Domain.Channels.Events;
using Flux.Transcoder.Domain.Common;
using Flux.Transcoder.Domain.ValueObjects;

namespace Flux.Transcoder.Domain.Channels;

public sealed class Channel : AggregateRoot
{
    public SourceUrl Source { get; private set; } = null!;
    public EncodingProfile Profile { get; private set; } = null!;
    public ChannelStatus Status { get; private set; }
    public RetentionPolicy Retention { get; private set; } = null!;

    private Channel() { }

    private Channel(Guid id, SourceUrl source, EncodingProfile profile, RetentionPolicy retention) : base(id)
    {
        Source = source;
        Profile = profile;
        Retention = retention;
        Status = ChannelStatus.Idle;
    }

    public static Channel Create(SourceUrl source, EncodingProfile profile, RetentionPolicy? retention = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(profile);

        return new Channel(Guid.NewGuid(), source, profile, retention ?? RetentionPolicy.Default);
    }

    /// <summary>Канал не транслируется — его можно запустить или удалить.</summary>
    public bool IsInactive => Status is ChannelStatus.Idle or ChannelStatus.Stopped or ChannelStatus.Failed;

    public void Start()
    {
        if (!IsInactive)
            throw new InvalidStateTransitionException($"Невозможно запустить канал из статуса {Status}.");

        Status = ChannelStatus.Starting;
        Raise(new ChannelStarted(Id));
    }

    /// <summary>
    /// Перезапуск работающего или упавшего канала. Статус возвращается в Starting,
    /// в Running его переведёт первый здоровый health-отчёт.
    /// </summary>
    public void Restart()
    {
        if (Status is not (ChannelStatus.Starting or ChannelStatus.Running
            or ChannelStatus.Degraded or ChannelStatus.Failed))
            throw new InvalidStateTransitionException($"Невозможно перезапустить канал из статуса {Status}.");

        Status = ChannelStatus.Starting;
        Raise(new ChannelStarted(Id));
    }

    public void MarkRunning()
    {
        if (Status is not (ChannelStatus.Starting or ChannelStatus.Degraded))
            throw new InvalidStateTransitionException($"Cannot mark running from status {Status}.");

        Status = ChannelStatus.Running;
        Raise(new ChannelRunning(Id));
    }

    public void MarkFailed(string reason)
    {
        if (Status is not (ChannelStatus.Starting or ChannelStatus.Running or ChannelStatus.Degraded))
            throw new InvalidStateTransitionException($"Cannot mark failed from status {Status}.");

        Status = ChannelStatus.Failed;
        Raise(new ChannelFailed(Id, reason));
    }

    public void UpdateRetention(RetentionPolicy retention)
    {
        ArgumentNullException.ThrowIfNull(retention);
        Retention = retention;
    }

    public void Stop()
    {
        if (Status is ChannelStatus.Idle or ChannelStatus.Stopped)
            throw new InvalidStateTransitionException($"Невозможно изменить статус канала со статуса {Status}.");

        Status = ChannelStatus.Stopped;
        Raise(new ChannelStopped(Id));
    }

    /// <summary>Удалять можно только неактивный канал — иначе ffmpeg останется без владельца.</summary>
    public void EnsureCanBeDeleted()
    {
        if (!IsInactive)
            throw new InvalidStateTransitionException(
                $"Нельзя удалить канал в статусе {Status}. Сначала остановите его.");
    }
}

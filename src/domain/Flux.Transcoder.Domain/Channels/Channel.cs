using Flux.Transcoder.Domain.Channels.Events;
using Flux.Transcoder.Domain.Common;
using Flux.Transcoder.Domain.ValueObjects;

namespace Flux.Transcoder.Domain.Channels;

public sealed class Channel : AggregateRoot
{
    public SourceUrl Source { get; private set; }
    public EncodingProfile Profile { get; private set; }
    public ChannelStatus Status { get; private set; }

    private Channel() { }
    
    private Channel(Guid id, SourceUrl source, EncodingProfile profile) : base(id)
    {
        Source = source;
        Profile = profile;
        Status = ChannelStatus.Idle;
    }

    public static Channel Create(SourceUrl source, EncodingProfile profile)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(profile);
        return new Channel(Guid.NewGuid(), source, profile);
    }

    public void Start()
    {
        if (Status != ChannelStatus.Idle)
            throw new DomainException($"Cannot start channel from status {Status}.");

        Status = ChannelStatus.Starting;
        Raise(new ChannelStarted(Id));
    }

    public void MarkRunning()
    {
        if (Status is not (ChannelStatus.Starting or ChannelStatus.Degraded))
            throw new DomainException($"Cannot mark running from status {Status}.");

        Status = ChannelStatus.Running;
        Raise(new ChannelRunning(Id));
    }

    public void MarkFailed(string reason)
    {
        if (Status is not (ChannelStatus.Starting or ChannelStatus.Running or ChannelStatus.Degraded))
            throw new DomainException($"Cannot mark failed from status {Status}.");

        Status = ChannelStatus.Failed;
        Raise(new ChannelFailed(Id, reason));
    }

    public void Stop()
    {
        if (Status is ChannelStatus.Idle or ChannelStatus.Stopped)
            throw new DomainException($"Cannot stop channel from status {Status}.");

        Status = ChannelStatus.Stopped;
        Raise(new ChannelStopped(Id));
    }
}
namespace Flux.Transcoder.Domain.Channels;

public enum ChannelStatus
{
    Idle,
    Starting,
    Running,
    Degraded,
    Failed,
    Stopped
}
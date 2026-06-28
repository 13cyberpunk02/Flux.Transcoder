namespace Flux.Transcoder.Application.Abstractions;

public interface ITokenGenerator
{
    string Generate(Guid channelId, TimeSpan lifetime);
}
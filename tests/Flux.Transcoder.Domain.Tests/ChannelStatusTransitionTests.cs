using FluentAssertions;
using Flux.Transcoder.Domain.Channels;
using Flux.Transcoder.Domain.Channels.Events;
using Flux.Transcoder.Domain.Common;
using Flux.Transcoder.Domain.Encoding;
using Flux.Transcoder.Domain.ValueObjects;

namespace Flux.Transcoder.Domain.Tests;

public class ChannelStatusTransitionTests
{
    private static Channel NewChannel()
    {
        var profile = new EncodingProfile("default",
            [new Rendition(1280, 720, 2500, EncoderType.X264)]);
        return Channel.Create(new SourceUrl("srt://localhost:9000"), profile);
    }

    [Fact]
    public void New_channel_is_idle()
    {
        NewChannel().Status.Should().Be(ChannelStatus.Idle);
    }

    [Fact]
    public void Start_from_idle_moves_to_starting_and_raises_event()
    {
        var ch = NewChannel();
        ch.Start();

        ch.Status.Should().Be(ChannelStatus.Starting);
        ch.DomainEvents.Should().ContainSingle(e => e is ChannelStarted);
    }

    [Fact]
    public void Start_from_non_idle_throws()
    {
        var ch = NewChannel();
        ch.Start();

        var act = () => ch.Start();
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void MarkRunning_from_starting_moves_to_running()
    {
        var ch = NewChannel();
        ch.Start();
        ch.MarkRunning();

        ch.Status.Should().Be(ChannelStatus.Running);
        ch.DomainEvents.Should().Contain(e => e is ChannelRunning);
    }

    [Fact]
    public void MarkRunning_from_idle_throws()
    {
        var ch = NewChannel();

        var act = () => ch.MarkRunning();
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Stop_from_idle_throws()
    {
        var ch = NewChannel();

        var act = () => ch.Stop();
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Stop_from_running_moves_to_stopped()
    {
        var ch = NewChannel();
        ch.Start();
        ch.MarkRunning();
        ch.Stop();

        ch.Status.Should().Be(ChannelStatus.Stopped);
        ch.DomainEvents.Should().Contain(e => e is ChannelStopped);
    }

    [Fact]
    public void MarkFailed_from_running_moves_to_failed()
    {
        var ch = NewChannel();
        ch.Start();
        ch.MarkRunning();
        ch.MarkFailed("encoder crashed");

        ch.Status.Should().Be(ChannelStatus.Failed);
        ch.DomainEvents.Should().Contain(e => e is ChannelFailed);
    }

    [Fact]
    public void Stop_after_failed_is_allowed()
    {
        var ch = NewChannel();
        ch.Start();
        ch.MarkFailed("boom");
        ch.Stop();

        ch.Status.Should().Be(ChannelStatus.Stopped);
    }

    [Fact]
    public void Start_after_stop_is_allowed()
    {
        var ch = NewChannel();
        ch.Start();
        ch.Stop();
        ch.Start();

        ch.Status.Should().Be(ChannelStatus.Starting);
    }

    [Fact]
    public void Start_after_failed_is_allowed()
    {
        var ch = NewChannel();
        ch.Start();
        ch.MarkFailed("boom");
        ch.Start();

        ch.Status.Should().Be(ChannelStatus.Starting);
    }

    [Fact]
    public void Start_from_running_throws_invalid_transition()
    {
        var ch = NewChannel();
        ch.Start();
        ch.MarkRunning();

        var act = () => ch.Start();
        act.Should().Throw<InvalidStateTransitionException>();
    }

    [Fact]
    public void Restart_from_failed_moves_to_starting_and_can_become_running()
    {
        var ch = NewChannel();
        ch.Start();
        ch.MarkFailed("boom");
        ch.Restart();
        ch.MarkRunning();

        ch.Status.Should().Be(ChannelStatus.Running);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Restart_from_idle_or_stopped_throws(bool stopFirst)
    {
        var ch = NewChannel();
        if (stopFirst) { ch.Start(); ch.Stop(); }

        var act = () => ch.Restart();
        act.Should().Throw<InvalidStateTransitionException>();
    }

    [Fact]
    public void Delete_is_allowed_only_for_inactive_channel()
    {
        var ch = NewChannel();
        ch.Invoking(c => c.EnsureCanBeDeleted()).Should().NotThrow();

        ch.Start();
        ch.Invoking(c => c.EnsureCanBeDeleted()).Should().Throw<InvalidStateTransitionException>();

        ch.Stop();
        ch.Invoking(c => c.EnsureCanBeDeleted()).Should().NotThrow();
    }

    [Theory]
    [InlineData("srt://host:9000")]
    [InlineData("rtmp://host/app")]
    [InlineData("udp://239.0.0.1:1234")]
    public void SourceUrl_accepts_valid_schemes(string url)
    {
        var act = () => new SourceUrl(url);
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("http://host")]
    [InlineData("")]
    [InlineData("just-text")]
    public void SourceUrl_rejects_invalid(string url)
    {
        var act = () => new SourceUrl(url);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void EncodingProfile_requires_at_least_one_rendition()
    {
        var act = () => new EncodingProfile("empty", []);
        act.Should().Throw<DomainException>();
    }
}
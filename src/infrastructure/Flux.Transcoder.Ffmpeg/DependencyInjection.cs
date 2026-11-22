using Flux.Transcoder.Application.Abstractions;
using Flux.Transcoder.Ffmpeg.Configuration;
using Flux.Transcoder.Ffmpeg.Encoding;
using Flux.Transcoder.Ffmpeg.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Flux.Transcoder.Ffmpeg;

public static class DependencyInjection
{
    public static IServiceCollection AddFfmpeg(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<FfmpegOptions>(configuration.GetSection(FfmpegOptions.SectionName));

        services.AddSingleton<FfmpegArgumentBuilder>(sp =>
            new FfmpegArgumentBuilder(
                sp.GetRequiredService<IOptions<FfmpegOptions>>().Value));

        services.AddSingleton<ProcessSupervisor>();
        services.AddSingleton<ProgressParser>();
        services.AddSingleton<SegmentHealthCheck>(sp =>
            new SegmentHealthCheck(sp.GetRequiredService<FfmpegArgumentBuilder>()));
        services.AddSingleton<RestartPolicy>();
        services.AddSingleton<RestartCoordinator>();
        services.AddSingleton<ChildProcessGuard>();
        services.AddSingleton<BitrateMeter>(sp =>
            new BitrateMeter(sp.GetRequiredService<FfmpegArgumentBuilder>()));
        services.AddSingleton<IStreamOrchestrator, FfmpegStreamOrchestrator>();

        return services;
    }
}
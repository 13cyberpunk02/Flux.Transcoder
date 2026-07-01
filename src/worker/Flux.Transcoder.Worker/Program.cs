using Flux.Transcoder.Contracts.Events;
using Flux.Transcoder.Contracts.Messaging;
using Flux.Transcoder.Ffmpeg;
using Flux.Transcoder.Infrastructure.Database;
using Flux.Transcoder.Infrastructure.Database.Persistence;
using Flux.Transcoder.Worker.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Wolverine;
using Wolverine.RabbitMQ;

var builder = Host.CreateApplicationBuilder(args);

// Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();
builder.Services.AddSerilog();

// Инфраструктура: DbContext, репозитории (воркер грузит Channel по id)
builder.Services.AddInfrastructure(builder.Configuration);

// FFmpeg-оркестрация: IStreamOrchestrator, ProcessSupervisor, ProgressParser и т.д.
builder.Services.AddFfmpeg(builder.Configuration);


// Wolverine: СЛУШАЕТ очередь команд
builder.UseWolverine(opts =>
{
    opts.UseRuntimeCompilation();

    var rabbit = builder.Configuration.GetConnectionString("RabbitMq")
                 ?? "amqp://dev_user:dev_pass12345@localhost:5672";

    // 1) СНАЧАЛА транспорт
    opts.UseRabbitMq(new Uri(rabbit))
        .AutoProvision();

    // 2) service location для EF
    opts.CodeGeneration.AlwaysUseServiceLocationFor<AppDbContext>();
    opts.CodeGeneration.AlwaysUseServiceLocationFor<DbContextOptions<AppDbContext>>();

    // 3) ПОТОМ маршруты
    // воркер СЛУШАЕТ команды
    opts.ListenToRabbitQueue(Queues.WorkerCommands);

    // воркер ПУБЛИКУЕТ health в свою очередь
    opts.PublishMessage<ChannelHealthReport>().ToRabbitQueue(Queues.HealthReports);
});

// Фоновые сервисы
builder.Services.AddHostedService<HealthReportingService>();
builder.Services.AddHostedService<ProcessReaperService>();

var host = builder.Build();
host.Run();
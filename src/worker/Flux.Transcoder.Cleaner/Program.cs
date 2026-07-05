
using Flux.Transcoder.Cleaner.Configuration;
using Flux.Transcoder.Cleaner.Services;
using Flux.Transcoder.Infrastructure.Database;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();
builder.Services.AddSerilog();

// доступ к БД (репозитории) — чистильщику нужно читать retention каналов
builder.Services.AddInfrastructure(builder.Configuration);

// опции чистильщика
builder.Services.Configure<CleanerOptions>(
    builder.Configuration.GetSection(CleanerOptions.SectionName));

// единственный сервис
builder.Services.AddHostedService<RetentionCleanupService>();

var host = builder.Build();
host.Run();
using Flux.Transcoder.Application;
using Flux.Transcoder.Infrastructure.Database;
using Flux.Transcoder.Infrastructure.Database.Logging;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;
using System.Text;
using Flux.Transcoder.Api.Archive;
using Flux.Transcoder.Api.Extensions;
using Flux.Transcoder.Api.Hubs;
using Flux.Transcoder.Api.Messaging;
using Flux.Transcoder.Api.Middleware;
using Flux.Transcoder.Application.Abstractions;
using Flux.Transcoder.Contracts.Commands;
using Flux.Transcoder.Contracts.Messaging;
using Flux.Transcoder.Infrastructure.Database.Persistence;
using Microsoft.EntityFrameworkCore;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.ErrorHandling;
using Wolverine.FluentValidation;
using Wolverine.Http;
using Wolverine.Postgresql;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

//Логгирование Serilog
Log.Logger = SerilogConfiguration.CreateBaseConfiguration().CreateLogger();
builder.Host.UseSerilog();


// OpenAPI для Scalar
builder.Services.AddOpenApi();
builder.Services.AddOpenApiExtension();

// Wolverine.Http — обязателен
builder.Services.AddWolverineHttp();

//Инфраструктура JWT, Database
builder.Services.AddInfrastructure(builder.Configuration);

// валидаторы из сборки Application — нужны FluentValidation-middleware
builder.Services.AddValidatorsFromAssembly(
    ApplicationWolverineExtensions.Assembly,
    ServiceLifetime.Singleton);

builder.Services.AddSignalR();
builder.Services.AddScoped<ITransactionalPublisher, OutboxPublisher>();

//Сервис которая строит архивные передачи потока.
builder.Services.AddSingleton<ArchivePlaylistBuilder>();

// Wolverine: in-process медиатор + RabbitMQ-транспорт + outbox в PostgreSQL
builder.Host.UseWolverine(options =>
{
    options.UseRuntimeCompilation();
    options.UseApplicationLayer();
    options.UseFluentValidation();

    var rabbit = builder.Configuration.GetConnectionString("RabbitMq")
                 ?? "amqp://dev_user:dev_pass12345@localhost:5672";

    // 1) СНАЧАЛА транспорт
    options.UseRabbitMq(new Uri(rabbit))
        .AutoProvision();

    // Outbox: команда пишется в одной транзакции с каналом и досылается после сбоя брокера или API.
    // Solo — API один (у SignalR нет backplane): недосланное подхватывается сразу при старте.
    options.PersistMessagesWithPostgresql(
        builder.Configuration.GetConnectionString("flux_transcoder_db")!, "wolverine");
    options.UseEntityFrameworkCoreTransactions();
    options.Durability.Mode = DurabilityMode.Solo;

    // статус пишут пользователь и health: проигравший перечитывает канал и решает заново
    options.Policies.OnException<DbUpdateConcurrencyException>().RetryTimes(3);

    // 2) service location для EF
    options.CodeGeneration.AlwaysUseServiceLocationFor<ITransactionalPublisher>();
    options.CodeGeneration.AlwaysUseServiceLocationFor<IUnitOfWork>();
    options.CodeGeneration.AlwaysUseServiceLocationFor(typeof(IRepository<>));
    options.CodeGeneration.AlwaysUseServiceLocationFor(typeof(IReadRepository<>));
    options.CodeGeneration.AlwaysUseServiceLocationFor<AppDbContext>();
    options.CodeGeneration.AlwaysUseServiceLocationFor<DbContextOptions<AppDbContext>>();

    // 3) ПОТОМ маршруты
    // API ПУБЛИКУЕТ команды воркеру
    options.PublishMessage<StartChannelCommand>().ToRabbitQueue(Queues.WorkerCommands).UseDurableOutbox();
    options.PublishMessage<StopChannelCommand>().ToRabbitQueue(Queues.WorkerCommands).UseDurableOutbox();
    options.PublishMessage<RestartChannelCommand>().ToRabbitQueue(Queues.WorkerCommands).UseDurableOutbox();
    
    options.Discovery.IncludeAssembly(typeof(ChannelHealthHandler).Assembly);
    // API СЛУШАЕТ health от воркера
    options.ListenToRabbitQueue(Queues.HealthReports);
});

// JWT-аутентификация
var jwt = builder.Configuration.GetSection("Jwt");
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwt["SigningKey"]!))
        };

        // SignalR по WebSocket/SSE не может послать заголовок Authorization и передаёт токен
        // в query (?access_token=). Без этого negotiate проходит с пользователем, а подключение —
        // анонимно, и SignalR отвечает 403 (смена пользователя) — остаётся только long polling.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) &&
                    context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

// CORS под Angular
const string AngularCors = "angular";
const string PlayerCors = "player";
builder.Services.AddCors(options =>
{
    options.AddPolicy(AngularCors, policy =>
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
    
    options.AddPolicy(PlayerCors, policy =>
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

// схема БД (включая метрики TimescaleDB) — до сидинга, иначе на чистой базе seeder падает
await using (var scope = app.Services.CreateAsyncScope())
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();

await DbSeeder.SeedAdminAsync(app.Services);
app.MapHub<ChannelsHub>("/hubs/channels");

// pipeline
app.UseSerilogRequestLogging();
app.UseExceptionHandling();
app.UseCors(AngularCors);
app.UseAuthentication();
app.UseAuthorization();

app.MapWolverineEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithOpenApiRoutePattern("/openapi/{documentName}.json");
    });
}

app.Run();

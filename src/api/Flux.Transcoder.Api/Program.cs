using Flux.Transcoder.Application;
using Flux.Transcoder.Infrastructure.Database;
using Flux.Transcoder.Infrastructure.Database.Logging;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;
using System.Text;
using Flux.Transcoder.Api.Extensions;
using Flux.Transcoder.Api.Hubs;
using Flux.Transcoder.Api.Middleware;
using Flux.Transcoder.Application.Abstractions;
using Flux.Transcoder.Contracts.Commands;
using Flux.Transcoder.Contracts.Messaging;
using Flux.Transcoder.Infrastructure.Database.Persistence;
using Microsoft.EntityFrameworkCore;
using Wolverine;
using Wolverine.FluentValidation;
using Wolverine.Http;
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

// Wolverine: in-process медиатор + RabbitMQ-транспорт (только публикация)
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

    // 2) service location для EF
    options.CodeGeneration.AlwaysUseServiceLocationFor<IUnitOfWork>();
    options.CodeGeneration.AlwaysUseServiceLocationFor(typeof(IRepository<>));
    options.CodeGeneration.AlwaysUseServiceLocationFor(typeof(IReadRepository<>));
    options.CodeGeneration.AlwaysUseServiceLocationFor<AppDbContext>();
    options.CodeGeneration.AlwaysUseServiceLocationFor<DbContextOptions<AppDbContext>>();

    // 3) ПОТОМ маршруты
    // API ПУБЛИКУЕТ команды воркеру
    options.PublishMessage<StartChannelCommand>().ToRabbitQueue(Queues.WorkerCommands);
    options.PublishMessage<StopChannelCommand>().ToRabbitQueue(Queues.WorkerCommands);
    options.PublishMessage<RestartChannelCommand>().ToRabbitQueue(Queues.WorkerCommands);
    
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
    });
builder.Services.AddAuthorization();

// CORS под Angular
const string AngularCors = "angular";
builder.Services.AddCors(options =>
{
    options.AddPolicy(AngularCors, policy =>
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

var app = builder.Build();

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

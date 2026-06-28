using Flux.Transcoder.Application;
using Flux.Transcoder.Infrastructure.Database;
using Flux.Transcoder.Infrastructure.Database.Logging;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;
using System.Text;
using Flux.Transcoder.Api.Messaging;
using Flux.Transcoder.Contracts.Commands;
using JasperFx.CodeGeneration;
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

//Инфраструктура JWT, Database
builder.Services.AddInfrastructure(builder.Configuration);

// валидаторы из сборки Application — нужны FluentValidation-middleware
builder.Services.AddValidatorsFromAssembly(ApplicationWolverineExtensions.Assembly);

builder.Services.AddWolverineHttp();
builder.Host.UseWolverine(options =>
{
    options.UseRuntimeCompilation();
    options.UseApplicationLayer();
    
    options.UseFluentValidation();
    var rabbit = builder.Configuration.GetConnectionString("RabbitMq")
                 ?? "amqp://dev_user:dev_pass12345@localhost:5672";
    
    options.UseRabbitMq(new Uri(rabbit))
        .AutoProvision();

    options.PublishMessage<StartChannelCommand>().ToRabbitQueue(Queues.WorkerCommands);
    options.PublishMessage<StopChannelCommand>().ToRabbitQueue(Queues.WorkerCommands);
    options.PublishMessage<RestartChannelCommand>().ToRabbitQueue(Queues.WorkerCommands);
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

// pipeline
app.UseSerilogRequestLogging();
//app.UseExceptionHandling();      // наш middleware из шага 6.4
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

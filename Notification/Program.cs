using System.Security.Claims;
using System.Text;
using Application.Messaging;
using Application.Realtime;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;
using Notification.Auth;
using Notification.Consumers;
using Notification.Hubs;
using Notification.Realtime;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddSingleton<IUserIdProvider, NameIdentifierUserIdProvider>();
builder.Services.AddSingleton<IRealtimeNotifier, HubRealtimeNotifier>();
builder.Services.AddScoped<SignalRNotificationHandler>();

var rabbit = builder.Configuration.GetSection(RabbitMqOptions.SectionName).Get<RabbitMqOptions>() ?? new RabbitMqOptions();
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<NotificationFanoutConsumer>();
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbit.Host, (ushort)rabbit.Port, rabbit.VirtualHost, host =>
        {
            host.Username(rabbit.UserName);
            host.Password(rabbit.Password);
        });
        cfg.ReceiveEndpoint("taskflow-notifications", endpoint =>
        {
            endpoint.PrefetchCount = 16;
            endpoint.UseMessageRetry(retry => retry.Exponential(
                3,
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(8),
                TimeSpan.FromSeconds(2)));
            endpoint.ConfigureConsumer<NotificationFanoutConsumer>(context);
        });
    });
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
            ClockSkew = TimeSpan.Zero,
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.NameIdentifier
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    context.Token = accessToken;
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/health", () => Results.Ok()).AllowAnonymous();
app.MapHub<NotificationHub>("/hubs/notifications");
app.Run();

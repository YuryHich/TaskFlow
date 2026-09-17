using System.Net;
using System.Security.Claims;
using System.Text;
using API.Auth;
using API.ExceptionHandling;
using API.Filters;
using API.Realtime;
using Application.Auth;
using Application.Auth.Authorization;
using Application.Caching;
using Application.DTOs;
using Application.Events;
using Application.Interfaces;
using Application.Mappings;
using Application.Realtime;
using Application.Services;
using Application.Validators;
using Domain.Models;
using Domain.Repositories;
using FluentValidation;
using Infrastructure.Authentication;
using Infrastructure.Caching;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Mapster;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using API.Hubs;
using Infrastructure.Messaging;


var builder = WebApplication.CreateBuilder(args);

MappingConfig.ConfigureMappings();
TypeAdapterConfig.GlobalSettings.Scan(typeof(MappingConfig).Assembly);

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddMapster();
builder.Services.AddValidatorsFromAssemblyContaining<CreateProjectValidator>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<ITagService, TagService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IProjectRepository, EfProjectRepository>();
builder.Services.AddScoped<ITaskRepository, EfTaskRepository>();
builder.Services.AddScoped<ICommentRepository, EfCommentRepository>();
builder.Services.AddScoped<ITagRepository, EfTagRepository>();
builder.Services.AddScoped<IUserRepository, EfUserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, EfRefreshTokenRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IProjectAudience, ProjectAudience>();
builder.Services.AddScoped<IAppEventPublisher, AppEventPublisher>();
builder.Services.AddTaskFlowMessaging(builder.Configuration, builder.Environment);
builder.Services.AddTaskFlowCache(builder.Configuration, builder.Environment);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAppEventHandler<ProjectCreatedEvent>, CacheInvalidationHandler>();
builder.Services.AddScoped<IAppEventHandler<ProjectUpdatedEvent>, CacheInvalidationHandler>();
builder.Services.AddScoped<IAppEventHandler<ProjectDeletedEvent>, CacheInvalidationHandler>();
builder.Services.AddScoped<IAppEventHandler<TaskCreatedEvent>, CacheInvalidationHandler>();
builder.Services.AddScoped<IAppEventHandler<TaskUpdatedEvent>, CacheInvalidationHandler>();
builder.Services.AddScoped<IAppEventHandler<TaskDeletedEvent>, CacheInvalidationHandler>();
builder.Services.AddScoped<IAppEventHandler<TagCatalogChangedEvent>, CacheInvalidationHandler>();
builder.Services.AddScoped<SignalRNotificationHandler>();
builder.Services.AddScoped<IAppEventHandler<ProjectCreatedEvent>>(sp => sp.GetRequiredService<SignalRNotificationHandler>());
builder.Services.AddScoped<IAppEventHandler<ProjectUpdatedEvent>>(sp => sp.GetRequiredService<SignalRNotificationHandler>());
builder.Services.AddScoped<IAppEventHandler<ProjectDeletedEvent>>(sp => sp.GetRequiredService<SignalRNotificationHandler>());
builder.Services.AddScoped<IAppEventHandler<TaskCreatedEvent>>(sp => sp.GetRequiredService<SignalRNotificationHandler>());
builder.Services.AddScoped<IAppEventHandler<TaskUpdatedEvent>>(sp => sp.GetRequiredService<SignalRNotificationHandler>());
builder.Services.AddScoped<IAppEventHandler<TaskDeletedEvent>>(sp => sp.GetRequiredService<SignalRNotificationHandler>());
builder.Services.AddScoped<IAppEventHandler<CommentAddedEvent>>(sp => sp.GetRequiredService<SignalRNotificationHandler>());
builder.Services.AddScoped<IAppEventHandler<CommentUpdatedEvent>>(sp => sp.GetRequiredService<SignalRNotificationHandler>());
builder.Services.AddScoped<IAppEventHandler<CommentDeletedEvent>>(sp => sp.GetRequiredService<SignalRNotificationHandler>());
builder.Services.AddScoped<BusBridgeHandler>();
builder.Services.AddScoped<IAppEventHandler<ProjectCreatedEvent>>(sp => sp.GetRequiredService<BusBridgeHandler>());
builder.Services.AddScoped<IAppEventHandler<ProjectUpdatedEvent>>(sp => sp.GetRequiredService<BusBridgeHandler>());
builder.Services.AddScoped<IAppEventHandler<ProjectDeletedEvent>>(sp => sp.GetRequiredService<BusBridgeHandler>());
builder.Services.AddScoped<IAppEventHandler<TaskCreatedEvent>>(sp => sp.GetRequiredService<BusBridgeHandler>());
builder.Services.AddScoped<IAppEventHandler<TaskUpdatedEvent>>(sp => sp.GetRequiredService<BusBridgeHandler>());
builder.Services.AddScoped<IAppEventHandler<TaskDeletedEvent>>(sp => sp.GetRequiredService<BusBridgeHandler>());
builder.Services.AddScoped<IAppEventHandler<CommentAddedEvent>>(sp => sp.GetRequiredService<BusBridgeHandler>());
builder.Services.AddScoped<IAppEventHandler<CommentUpdatedEvent>>(sp => sp.GetRequiredService<BusBridgeHandler>());
builder.Services.AddScoped<IAppEventHandler<CommentDeletedEvent>>(sp => sp.GetRequiredService<BusBridgeHandler>());
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<IAuthorizationHandler, ProjectOwnerHandler>();
builder.Services.AddScoped<IAuthorizationHandler, ProjectAccessHandler>();
builder.Services.AddScoped<IAuthorizationHandler, TaskAccessHandler>();
builder.Services.AddSignalR();
builder.Services.AddSingleton<IUserIdProvider, NameIdentifierUserIdProvider>();
builder.Services.AddSingleton<IRealtimeNotifier, HubRealtimeNotifier>();
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddControllers(options =>
    options.Filters.Add<ValidationActionFilter>());
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:5173")
            .WithMethods("GET", "POST", "PUT", "DELETE")
            .AllowAnyHeader());
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
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
        });
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
    .RequireAuthenticatedUser()
    .Build();

    options.AddPolicy(AuthorizationPolicies.CanManageProjects, policy =>
        policy.RequireRole(nameof(UserRole.Admin), nameof(UserRole.Manager)));
    options.AddPolicy(AuthorizationPolicies.CanDeleteProjects, policy =>
        policy.RequireRole(nameof(UserRole.Admin), nameof(UserRole.Manager)));
    options.AddPolicy(AuthorizationPolicies.ProjectOwner, policy =>
        policy.Requirements.Add(new ProjectOwnerRequirement()));
    options.AddPolicy(AuthorizationPolicies.ProjectAccess, policy =>
        policy.Requirements.Add(new ProjectAccessRequirement()));
    options.AddPolicy(AuthorizationPolicies.TaskAccess, policy =>
        policy.Requirements.Add(new TaskAccessRequirement()));
});

var app = builder.Build();

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");


app.Run();

public partial class Program { }

using Mapster;
using Application.DTOs;
using Application.Interfaces;
using Application.Mappings;
using Application.Services;
using Application.Validators;
using Domain.Repositories;
using FluentValidation;
using Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

MappingConfig.ConfigureMappings();
TypeAdapterConfig.GlobalSettings.Scan(typeof(MappingConfig).Assembly);

builder.Services.AddMapster();
builder.Services.AddValidatorsFromAssemblyContaining<CreateProjectValidator>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<ITagService, TagService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddSingleton<IProjectRepository, InMemoryProjectRepository>();
builder.Services.AddSingleton<ITaskRepository, InMemoryTaskRepository>();
builder.Services.AddSingleton<ICommentRepository, InMemoryCommentRepository>();
builder.Services.AddSingleton<ITagRepository, InMemoryTagRepository>();
builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();



app.Run();



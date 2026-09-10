using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs;
using Domain.Models;
using Mapster;

namespace Application.Mappings;

    public class MappingConfig
    {
        public static void ConfigureMappings()
        {
            TypeAdapterConfig<Project, ProjectDto>.NewConfig()
                .Map(dest => dest.Id, src => src.Id)
                .Map(dest => dest.Name, src => src.Name)
                .Map(dest => dest.Description, src => src.Description)
                .Map(dest => dest.OwnerId, src => src.OwnerId)
                .Map(dest => dest.CreatedAt, src => src.CreatedAt);

            TypeAdapterConfig<CreateProjectDto, Project>.NewConfig()
                .Ignore(dest => dest.Id)
                .Ignore(dest => dest.CreatedAt)
                .Ignore(dest => dest.OwnerId)
                .Ignore(dest => dest.Owner)    
                .Ignore(dest => dest.Tasks);

            TypeAdapterConfig<UpdateProjectDto, Project>.NewConfig()
                .Ignore(dest => dest.Id)
                .Ignore(dest => dest.CreatedAt)
                .Ignore(dest => dest.OwnerId)
                .Ignore(dest => dest.Owner)
                .Ignore(dest => dest.Tasks);

                // ============ WORK TASK ============
            TypeAdapterConfig<WorkTask, TaskDto>.NewConfig()
                .Map(dest => dest.Id, src => src.Id)
                .Map(dest => dest.ProjectId, src => src.ProjectId)
                .Map(dest => dest.AssigneeIds, src => src.Assignees.Select(assignee => assignee.Id).ToList())
                .Map(dest => dest.Title, src => src.Title)
                .Map(dest => dest.Description, src => src.Description)
                .Map(dest => dest.Status, src => src.Status)
                .Map(dest => dest.Priority, src => src.Priority)
                .Map(dest => dest.CreatedAt, src => src.CreatedAt)
                .Map(dest => dest.Deadline, src => src.Deadline);

            TypeAdapterConfig<CreateTaskDto, WorkTask>.NewConfig()
                .Ignore(dest => dest.Id)
                .Ignore(dest => dest.CreatedAt)
                .Ignore(dest => dest.Project)
                .Ignore(dest => dest.Assignees)
                .Ignore(dest => dest.Comments)
                .Ignore(dest => dest.Tags);

            TypeAdapterConfig<UpdateTaskDto, WorkTask>.NewConfig()
                .Ignore(dest => dest.Id)
                .Ignore(dest => dest.ProjectId)
                .Ignore(dest => dest.CreatedAt)
                .Ignore(dest => dest.Project)
                .Ignore(dest => dest.Assignees)
                .Ignore(dest => dest.Comments)
                .Ignore(dest => dest.Tags);

            // ============ USER ============
            TypeAdapterConfig<User, UserDto>.NewConfig()
                .Map(dest => dest.Id, src => src.Id)
                .Map(dest => dest.Email, src => src.Email)
                .Map(dest => dest.Username, src => src.Username)
                .Map(dest => dest.CreatedAt, src => src.CreatedAt);

            TypeAdapterConfig<RegisterRequest, User>.NewConfig()
                .Ignore(dest => dest.Id)
                .Ignore(dest => dest.CreatedAt)
                .Ignore(dest => dest.PasswordHash)
                .Ignore(dest => dest.Role)
                .Ignore(dest => dest.OwnedProjects)
                .Ignore(dest => dest.AssignedTasks)
                .Ignore(dest => dest.Comments)
                .Ignore(dest => dest.RefreshTokens);

            TypeAdapterConfig<UpdateUserDto, User>.NewConfig()
                .Ignore(dest => dest.Id)
                .Ignore(dest => dest.CreatedAt)
                .Ignore(dest => dest.PasswordHash)
                .Ignore(dest => dest.Role)
                .Ignore(dest => dest.OwnedProjects)
                .Ignore(dest => dest.AssignedTasks)
                .Ignore(dest => dest.Comments)
                .Ignore(dest => dest.RefreshTokens);

            // ============ COMMENT ============
            TypeAdapterConfig<Comment, CommentDto>.NewConfig()
                .Map(dest => dest.Id, src => src.Id)
                .Map(dest => dest.TaskId, src => src.TaskId)
                .Map(dest => dest.AuthorId, src => src.AuthorId)
                .Map(dest => dest.Content, src => src.Content)
                .Map(dest => dest.CreatedAt, src => src.CreatedAt);

            TypeAdapterConfig<CreateCommentDto, Comment>.NewConfig()
                .Ignore(dest => dest.Id)
                .Ignore(dest => dest.CreatedAt)
                .Ignore(dest => dest.Task)
                .Ignore(dest => dest.Author);

            TypeAdapterConfig<UpdateCommentDto, Comment>.NewConfig()
                .Ignore(dest => dest.Id)
                .Ignore(dest => dest.TaskId)
                .Ignore(dest => dest.AuthorId)
                .Ignore(dest => dest.CreatedAt)
                .Ignore(dest => dest.Task)
                .Ignore(dest => dest.Author);

            // ============ TAG ============
            TypeAdapterConfig<Tag, TagDto>.NewConfig()
                .Map(dest => dest.Id, src => src.Id)
                .Map(dest => dest.Name, src => src.Name);

            TypeAdapterConfig<CreateTagDto, Tag>.NewConfig()
                .Ignore(dest => dest.Id)
                .Ignore(dest => dest.Tasks);

            TypeAdapterConfig<UpdateTagDto, Tag>.NewConfig()
                .Ignore(dest => dest.Id)
                .Ignore(dest => dest.Tasks);

        }

    }

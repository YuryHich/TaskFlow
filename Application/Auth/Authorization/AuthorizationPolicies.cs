namespace Application.Auth.Authorization;

public static class AuthorizationPolicies
{
    public const string CanManageProjects = "CanManageProjects";
    public const string CanDeleteProjects = "CanDeleteProjects";

    /// <summary>
    /// Resource-based: call <c>IAuthorizationService.AuthorizeAsync(user, project, ProjectOwner)</c>
    /// (or <c>AuthorizeProjectOwnerAsync</c>). Do not put this on <c>[Authorize(Policy = ...)]</c> —
    /// the attribute does not pass a <c>Project</c>, so the handler never sees the resource.
    /// </summary>
    public const string ProjectOwner = "ProjectOwner";

    /// <summary>
    /// Resource-based read: owner, assignee of any task in the project, or Admin/Manager.
    /// Call via <c>AuthorizeProjectAccessAsync</c>. Do not use as <c>[Authorize(Policy = ...)]</c>.
    /// </summary>
    public const string ProjectAccess = "ProjectAccess";

    /// <summary>
    /// Resource-based: call <c>IAuthorizationService.AuthorizeAsync(user, task, TaskAccess)</c>
    /// (or <c>AuthorizeTaskAccessAsync</c>). Do not use as <c>[Authorize(Policy = ...)]</c>.
    /// Same audience as <see cref="ProjectAccess"/> for the task's project.
    /// </summary>
    public const string TaskAccess = "TaskAccess";
}

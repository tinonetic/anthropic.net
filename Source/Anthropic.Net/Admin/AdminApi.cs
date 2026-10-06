namespace Anthropic.Net.Admin;

using System.Net.Http;
using System.Text.Json.Serialization;
using Anthropic.Net.ManagedAgents;
using Anthropic.Net.Models.Models;

/// <summary>
/// An Admin API object (user, invite, workspace, member, API key, rate limit, service account, federation issuer/rule, external key).
/// Fields not modelled here are available through <see cref="ApiObject.AdditionalProperties"/>.
/// </summary>
public class AdminObject : ApiResource
{
    /// <summary>Gets or sets the email (users, invites).</summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>Gets or sets the organization role (user, claude_code_user, developer, billing, admin).</summary>
    [JsonPropertyName("role")]
    public string? Role { get; set; }

    /// <summary>Gets or sets the status (API keys: active / inactive / archived; invites: pending / ...).</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>Gets or sets the workspace id.</summary>
    [JsonPropertyName("workspace_id")]
    public string? WorkspaceId { get; set; }

    /// <summary>Gets or sets the user id (workspace members).</summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    /// <summary>Gets or sets the workspace role (workspace_user, workspace_developer, workspace_admin, workspace_billing).</summary>
    [JsonPropertyName("workspace_role")]
    public string? WorkspaceRole { get; set; }
}

/// <summary>
/// Admin API (<c>client.Admin</c>): organization management under <c>/v1/organizations</c>. Needs an admin credential
/// (Admin API key <c>sk-ant-admin...</c>, or an <c>org:admin</c> OAuth token for service accounts and federation).
/// Regular API keys are rejected, and admin credentials do not work on the Messages API.
/// </summary>
public sealed class AdminApi
{
    private readonly ApiTransport _t;

    internal AdminApi(ApiTransport t)
    {
        _t = t;
        Users = new UsersApi(this);
        Invites = new InvitesApi(this);
        Workspaces = new WorkspacesApi(this);
        ApiKeys = new ApiKeysApi(this);
        ServiceAccounts = new ServiceAccountsApi(this);
        Federation = new FederationApi(this);
        ExternalKeys = new ExternalKeysApi(this);
    }

    /// <summary>Gets the members API.</summary>
    public UsersApi Users { get; }

    /// <summary>Gets the invites API.</summary>
    public InvitesApi Invites { get; }

    /// <summary>Gets the workspaces API (members, rate limits).</summary>
    public WorkspacesApi Workspaces { get; }

    /// <summary>Gets the API keys API.</summary>
    public ApiKeysApi ApiKeys { get; }

    /// <summary>Gets the service accounts API (org:admin token required).</summary>
    public ServiceAccountsApi ServiceAccounts { get; }

    /// <summary>Gets the workload identity federation API (org:admin token required).</summary>
    public FederationApi Federation { get; }

    /// <summary>Gets the CMEK external keys API.</summary>
    public ExternalKeysApi ExternalKeys { get; }

    /// <summary>Gets organization info.</summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The organization.</returns>
    public Task<AdminObject> GetOrganizationAsync(CancellationToken ct = default) => Send<AdminObject>(HttpMethod.Get, "/v1/organizations/me", null, ct);

    /// <summary>Lists organization-wide rate limits.</summary>
    /// <param name="model">Filter by model.</param>
    /// <param name="groupType">Filter by group type.</param>
    /// <param name="limit">Page size.</param>
    /// <param name="afterId">Cursor.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A page.</returns>
    public Task<Page<AdminObject>> ListRateLimitsAsync(string? model = null, string? groupType = null, int? limit = null, string? afterId = null, CancellationToken ct = default)
        => Send<Page<AdminObject>>(HttpMethod.Get, "/v1/organizations/rate_limits" + ApiTransport.Query(("model", model), ("group_type", groupType), ("limit", limit), ("after_id", afterId)), null, ct);

    internal Task<T> Send<T>(HttpMethod method, string path, object? body, CancellationToken ct)
        => _t.RequestAsync<T>(method, path, body, _t.AdminBetas, ct);

    internal Task Send(HttpMethod method, string path, object? body, CancellationToken ct)
        => _t.RequestAsync(method, path, body, _t.AdminBetas, ct);

    internal Task<Page<AdminObject>> List(string path, int? limit, string? afterId, string? beforeId, CancellationToken ct, params (string, object?)[] extra)
        => Send<Page<AdminObject>>(HttpMethod.Get, path + ApiTransport.Query([.. extra, ("limit", limit), ("after_id", afterId), ("before_id", beforeId)]), null, ct);
}

/// <summary>Organization members.</summary>
public sealed class UsersApi
{
    private readonly AdminApi _a;

    internal UsersApi(AdminApi a) => _a = a;

    /// <summary>Lists members.</summary>
    /// <param name="limit">Page size.</param>
    /// <param name="afterId">Cursor.</param>
    /// <param name="beforeId">Cursor.</param>
    /// <param name="email">Filter by email.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A page.</returns>
    public Task<Page<AdminObject>> ListAsync(int? limit = null, string? afterId = null, string? beforeId = null, string? email = null, CancellationToken ct = default)
        => _a.List("/v1/organizations/users", limit, afterId, beforeId, ct, ("email", email));

    /// <summary>Changes a member's role.</summary>
    /// <param name="userId">The user id.</param>
    /// <param name="role">The new organization role.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The user.</returns>
    public Task<AdminObject> UpdateAsync(string userId, string role, CancellationToken ct = default)
        => _a.Send<AdminObject>(HttpMethod.Post, $"/v1/organizations/users/{Uri.EscapeDataString(userId)}", new { role }, ct);

    /// <summary>Removes a member.</summary>
    /// <param name="userId">The user id.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task.</returns>
    public Task RemoveAsync(string userId, CancellationToken ct = default)
        => _a.Send(HttpMethod.Delete, $"/v1/organizations/users/{Uri.EscapeDataString(userId)}", null, ct);
}

/// <summary>Organization invites.</summary>
public sealed class InvitesApi
{
    private readonly AdminApi _a;

    internal InvitesApi(AdminApi a) => _a = a;

    /// <summary>Invites someone.</summary>
    /// <param name="email">The email.</param>
    /// <param name="role">The organization role.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The invite.</returns>
    public Task<AdminObject> CreateAsync(string email, string role, CancellationToken ct = default)
        => _a.Send<AdminObject>(HttpMethod.Post, "/v1/organizations/invites", new { email, role }, ct);

    /// <summary>Lists invites.</summary>
    /// <param name="limit">Page size.</param>
    /// <param name="afterId">Cursor.</param>
    /// <param name="beforeId">Cursor.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A page.</returns>
    public Task<Page<AdminObject>> ListAsync(int? limit = null, string? afterId = null, string? beforeId = null, CancellationToken ct = default)
        => _a.List("/v1/organizations/invites", limit, afterId, beforeId, ct);

    /// <summary>Deletes an invite.</summary>
    /// <param name="inviteId">The invite id.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task.</returns>
    public Task DeleteAsync(string inviteId, CancellationToken ct = default)
        => _a.Send(HttpMethod.Delete, $"/v1/organizations/invites/{Uri.EscapeDataString(inviteId)}", null, ct);
}

/// <summary>Workspaces.</summary>
public sealed class WorkspacesApi
{
    private readonly AdminApi _a;

    internal WorkspacesApi(AdminApi a)
    {
        _a = a;
        Members = new WorkspaceMembersApi(a);
    }

    /// <summary>Gets the workspace members API.</summary>
    public WorkspaceMembersApi Members { get; }

    /// <summary>Creates a workspace.</summary>
    /// <param name="name">The name.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The workspace.</returns>
    public Task<AdminObject> CreateAsync(string name, CancellationToken ct = default)
        => _a.Send<AdminObject>(HttpMethod.Post, "/v1/organizations/workspaces", new { name }, ct);

    /// <summary>Gets a workspace.</summary>
    /// <param name="workspaceId">The id.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The workspace.</returns>
    public Task<AdminObject> GetAsync(string workspaceId, CancellationToken ct = default)
        => _a.Send<AdminObject>(HttpMethod.Get, $"/v1/organizations/workspaces/{Uri.EscapeDataString(workspaceId)}", null, ct);

    /// <summary>Lists workspaces.</summary>
    /// <param name="includeArchived">Include archived workspaces.</param>
    /// <param name="limit">Page size.</param>
    /// <param name="afterId">Cursor.</param>
    /// <param name="beforeId">Cursor.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A page.</returns>
    public Task<Page<AdminObject>> ListAsync(bool? includeArchived = null, int? limit = null, string? afterId = null, string? beforeId = null, CancellationToken ct = default)
        => _a.List("/v1/organizations/workspaces", limit, afterId, beforeId, ct, ("include_archived", includeArchived));

    /// <summary>Updates a workspace (e.g. <c>new { name = "x" }</c> or <c>new { external_key_id = "ekey_..." }</c> to attach a CMEK key).</summary>
    /// <param name="workspaceId">The id.</param>
    /// <param name="changes">The fields to change.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The workspace.</returns>
    public Task<AdminObject> UpdateAsync(string workspaceId, object changes, CancellationToken ct = default)
        => _a.Send<AdminObject>(HttpMethod.Post, $"/v1/organizations/workspaces/{Uri.EscapeDataString(workspaceId)}", changes, ct);

    /// <summary>Archives a workspace.</summary>
    /// <param name="workspaceId">The id.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The workspace.</returns>
    public Task<AdminObject> ArchiveAsync(string workspaceId, CancellationToken ct = default)
        => _a.Send<AdminObject>(HttpMethod.Post, $"/v1/organizations/workspaces/{Uri.EscapeDataString(workspaceId)}/archive", "{}", ct);

    /// <summary>Lists rate limits for a workspace.</summary>
    /// <param name="workspaceId">The workspace id.</param>
    /// <param name="limit">Page size.</param>
    /// <param name="afterId">Cursor.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A page.</returns>
    public Task<Page<AdminObject>> ListRateLimitsAsync(string workspaceId, int? limit = null, string? afterId = null, CancellationToken ct = default)
        => _a.List($"/v1/organizations/workspaces/{Uri.EscapeDataString(workspaceId)}/rate_limits", limit, afterId, null, ct);
}

/// <summary>Workspace members.</summary>
public sealed class WorkspaceMembersApi
{
    private readonly AdminApi _a;

    internal WorkspaceMembersApi(AdminApi a) => _a = a;

    /// <summary>Adds a member to a workspace.</summary>
    /// <param name="workspaceId">The workspace id.</param>
    /// <param name="userId">The user id.</param>
    /// <param name="workspaceRole">workspace_user, workspace_developer, workspace_admin or workspace_billing.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The member.</returns>
    public Task<AdminObject> AddAsync(string workspaceId, string userId, string workspaceRole, CancellationToken ct = default)
        => _a.Send<AdminObject>(HttpMethod.Post, Base(workspaceId), new { user_id = userId, workspace_role = workspaceRole }, ct);

    /// <summary>Lists workspace members.</summary>
    /// <param name="workspaceId">The workspace id.</param>
    /// <param name="limit">Page size.</param>
    /// <param name="afterId">Cursor.</param>
    /// <param name="beforeId">Cursor.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A page.</returns>
    public Task<Page<AdminObject>> ListAsync(string workspaceId, int? limit = null, string? afterId = null, string? beforeId = null, CancellationToken ct = default)
        => _a.List(Base(workspaceId), limit, afterId, beforeId, ct);

    /// <summary>Changes a member's workspace role.</summary>
    /// <param name="workspaceId">The workspace id.</param>
    /// <param name="userId">The user id.</param>
    /// <param name="workspaceRole">The new role.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The member.</returns>
    public Task<AdminObject> UpdateAsync(string workspaceId, string userId, string workspaceRole, CancellationToken ct = default)
        => _a.Send<AdminObject>(HttpMethod.Post, Base(workspaceId) + "/" + Uri.EscapeDataString(userId), new { workspace_role = workspaceRole }, ct);

    /// <summary>Removes a member from a workspace.</summary>
    /// <param name="workspaceId">The workspace id.</param>
    /// <param name="userId">The user id.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task.</returns>
    public Task RemoveAsync(string workspaceId, string userId, CancellationToken ct = default)
        => _a.Send(HttpMethod.Delete, Base(workspaceId) + "/" + Uri.EscapeDataString(userId), null, ct);

    private static string Base(string workspaceId) => $"/v1/organizations/workspaces/{Uri.EscapeDataString(workspaceId)}/members";
}

/// <summary>API keys (list and update only; keys are created in the Console).</summary>
public sealed class ApiKeysApi
{
    private readonly AdminApi _a;

    internal ApiKeysApi(AdminApi a) => _a = a;

    /// <summary>Lists API keys.</summary>
    /// <param name="status">Filter: active, inactive, archived.</param>
    /// <param name="workspaceId">Filter by workspace.</param>
    /// <param name="createdByUserId">Filter by creator.</param>
    /// <param name="limit">Page size.</param>
    /// <param name="afterId">Cursor.</param>
    /// <param name="beforeId">Cursor.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A page.</returns>
    public Task<Page<AdminObject>> ListAsync(string? status = null, string? workspaceId = null, string? createdByUserId = null, int? limit = null, string? afterId = null, string? beforeId = null, CancellationToken ct = default)
        => _a.List("/v1/organizations/api_keys", limit, afterId, beforeId, ct, ("status", status), ("workspace_id", workspaceId), ("created_by_user_id", createdByUserId));

    /// <summary>Renames or (de)activates an API key.</summary>
    /// <param name="apiKeyId">The key id.</param>
    /// <param name="status">active, inactive or archived (null to keep).</param>
    /// <param name="name">The new name (null to keep).</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The key.</returns>
    public Task<AdminObject> UpdateAsync(string apiKeyId, string? status = null, string? name = null, CancellationToken ct = default)
        => _a.Send<AdminObject>(HttpMethod.Post, $"/v1/organizations/api_keys/{Uri.EscapeDataString(apiKeyId)}", new { status, name }, ct);
}

/// <summary>Service accounts (OAuth-only: needs an org:admin bearer token).</summary>
public sealed class ServiceAccountsApi
{
    private readonly AdminApi _a;

    internal ServiceAccountsApi(AdminApi a) => _a = a;

    /// <summary>Creates a service account.</summary>
    /// <param name="name">The name.</param>
    /// <param name="organizationRole">The organization role.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The service account.</returns>
    public Task<AdminObject> CreateAsync(string name, string organizationRole, CancellationToken ct = default)
        => _a.Send<AdminObject>(HttpMethod.Post, "/v1/organizations/service_accounts", new { name, organization_role = organizationRole }, ct);

    /// <summary>Lists service accounts.</summary>
    /// <param name="limit">Page size.</param>
    /// <param name="afterId">Cursor.</param>
    /// <param name="beforeId">Cursor.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A page.</returns>
    public Task<Page<AdminObject>> ListAsync(int? limit = null, string? afterId = null, string? beforeId = null, CancellationToken ct = default)
        => _a.List("/v1/organizations/service_accounts", limit, afterId, beforeId, ct);

    /// <summary>Archives a service account.</summary>
    /// <param name="serviceAccountId">The id.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The service account.</returns>
    public Task<AdminObject> ArchiveAsync(string serviceAccountId, CancellationToken ct = default)
        => _a.Send<AdminObject>(HttpMethod.Post, $"/v1/organizations/service_accounts/{Uri.EscapeDataString(serviceAccountId)}/archive", "{}", ct);
}

/// <summary>Workload identity federation (OAuth-only).</summary>
public sealed class FederationApi
{
    internal FederationApi(AdminApi a)
    {
        Issuers = new FederationResourceApi(a, "/v1/organizations/federation_issuers");
        Rules = new FederationResourceApi(a, "/v1/organizations/federation_rules");
    }

    /// <summary>Gets the issuers API (name, issuer_url, jwks).</summary>
    public FederationResourceApi Issuers { get; }

    /// <summary>Gets the rules API (name, issuer_id, match, target, workspace_id, oauth_scope, token_lifetime_seconds).</summary>
    public FederationResourceApi Rules { get; }
}

/// <summary>Create / list / archive for a federation resource.</summary>
public sealed class FederationResourceApi
{
    private readonly AdminApi _a;
    private readonly string _base;

    internal FederationResourceApi(AdminApi a, string basePath)
    {
        _a = a;
        _base = basePath;
    }

    /// <summary>Creates the resource.</summary>
    /// <param name="body">The body, e.g. <c>new { name, issuer_url, jwks = new { type = "discovery" } }</c>.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The resource.</returns>
    public Task<AdminObject> CreateAsync(object body, CancellationToken ct = default) => _a.Send<AdminObject>(HttpMethod.Post, _base, body, ct);

    /// <summary>Lists the resources.</summary>
    /// <param name="limit">Page size.</param>
    /// <param name="afterId">Cursor.</param>
    /// <param name="beforeId">Cursor.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A page.</returns>
    public Task<Page<AdminObject>> ListAsync(int? limit = null, string? afterId = null, string? beforeId = null, CancellationToken ct = default)
        => _a.List(_base, limit, afterId, beforeId, ct);

    /// <summary>Archives the resource.</summary>
    /// <param name="id">The id.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The resource.</returns>
    public Task<AdminObject> ArchiveAsync(string id, CancellationToken ct = default)
        => _a.Send<AdminObject>(HttpMethod.Post, $"{_base}/{Uri.EscapeDataString(id)}/archive", "{}", ct);
}

/// <summary>Customer-managed encryption keys (CMEK).</summary>
public sealed class ExternalKeysApi
{
    private readonly AdminApi _a;

    internal ExternalKeysApi(AdminApi a) => _a = a;

    /// <summary>Registers an external key.</summary>
    /// <param name="displayName">The display name.</param>
    /// <param name="geo">The geography, e.g. "us".</param>
    /// <param name="providerConfig">Provider config, e.g. <c>new { type = "aws", kms_arn = "arn:aws:kms:..." }</c>.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The key.</returns>
    public Task<AdminObject> CreateAsync(string displayName, string geo, object providerConfig, CancellationToken ct = default)
        => _a.Send<AdminObject>(HttpMethod.Post, "/v1/organizations/external_keys", new { display_name = displayName, geo, provider_config = providerConfig }, ct);

    /// <summary>Validates a registered key before attaching it to a workspace.</summary>
    /// <param name="externalKeyId">The key id.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The validation result.</returns>
    public Task<AdminObject> ValidateAsync(string externalKeyId, CancellationToken ct = default)
        => _a.Send<AdminObject>(HttpMethod.Post, $"/v1/organizations/external_keys/{Uri.EscapeDataString(externalKeyId)}/validate", "{}", ct);
}

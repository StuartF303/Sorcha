namespace Sorcha.Admin.Models.Authentication;

/// <summary>
/// Serializable user info for transferring authentication state from server to client.
/// Shared between server and WASM client for PersistentComponentState serialization.
/// </summary>
public class UserInfo
{
    public required string UserId { get; set; }
    public required string UserName { get; set; }
    public required string Email { get; set; }
    public string[] Roles { get; set; } = Array.Empty<string>();
    public Dictionary<string, string> AdditionalClaims { get; set; } = new();
}

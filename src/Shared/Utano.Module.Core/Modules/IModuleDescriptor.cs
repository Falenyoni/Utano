namespace Utano.Module.Core.Modules;

public interface IModuleDescriptor
{
    string FeatureKey { get; }
    string Plan { get; }  // "free" | "professional" | "enterprise"
    IReadOnlyList<string> AllPermissions { get; }
    IReadOnlyList<string> GetPermissionsForRole(string roleName);
}

namespace CastoPet.Core.Product;

public sealed record CastoPetProductIdentity(
    string ApplicationId,
    string DisplayName,
    string DataDirectoryName,
    string InstanceName,
    string StartupValueName,
    string PackageId)
{
    public static CastoPetProductIdentity Current { get; } = new(
        ApplicationId: "CastoPet",
        DisplayName: "CastoPet",
        DataDirectoryName: "CastoPet",
        InstanceName: "CastoPet",
        StartupValueName: "CastoPet",
        PackageId: "CastoPet");
}

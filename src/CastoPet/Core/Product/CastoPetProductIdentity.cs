namespace CastoPet.Core.Product;

public sealed record CastoPetProductIdentity(
    CastoPetEdition Edition,
    string ApplicationId,
    string DisplayName,
    string DataDirectoryName,
    string InstanceName,
    string StartupValueName,
    string PackageId,
    bool UpdatesEnabled)
{
    public static CastoPetProductIdentity Stable { get; } = new(
        CastoPetEdition.Stable,
        ApplicationId: "CastoPet",
        DisplayName: "CastoPet",
        DataDirectoryName: "CastoPet",
        InstanceName: "CastoPet",
        StartupValueName: "CastoPet",
        PackageId: "CastoPet",
        UpdatesEnabled: true);

    public static CastoPetProductIdentity Preview { get; } = new(
        CastoPetEdition.Preview,
        ApplicationId: "CastoPet.Preview",
        DisplayName: "CastoPet Preview",
        DataDirectoryName: "CastoPet-Preview",
        InstanceName: "CastoPet.Preview",
        StartupValueName: "CastoPet Preview",
        PackageId: "CastoPet.Preview",
        UpdatesEnabled: false);

    public static CastoPetProductIdentity Current =>
        CastoPetFeatureProfile.Current.Edition == CastoPetEdition.Stable
            ? Stable
            : Preview;
}

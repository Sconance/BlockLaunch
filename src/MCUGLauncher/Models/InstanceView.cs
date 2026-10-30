
namespace BlockLaunch;

public record InstanceView(string Key, string Name, string Group, string Detail, string Icon, int JavaMajor = 0)
{
    internal static string VersionSummary(string profile, PackRequirements requirements, bool knownOfficial = false)
    {
        var minecraft = requirements.Minecraft;
        var numeric = minecraft.TrimStart('a', 'b').Split('-')[0];
        var snapshot = System.Text.RegularExpressions.Regex.IsMatch(minecraft, @"^\d{2}w\d{2}[a-z]+$");
        if (!knownOfficial && minecraft == profile && !Version.TryParse(numeric, out _) && !snapshot)
            minecraft = "版本未知";
        return (minecraft + " · " + requirements.Loader + " " + requirements.LoaderVersion).TrimEnd();
    }
}

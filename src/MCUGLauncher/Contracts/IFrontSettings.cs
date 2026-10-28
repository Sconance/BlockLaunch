namespace BlockLaunch.Contracts;

public sealed record FrontSettingsSnapshot(string Root, string Java, string Ram, string Width, string Height);

public interface IFrontSettings
{
    Task<FrontSettingsSnapshot> Read(CancellationToken token);
    Task<FrontSettingsSnapshot> Save(FrontSettingsSnapshot input, CancellationToken token);
}

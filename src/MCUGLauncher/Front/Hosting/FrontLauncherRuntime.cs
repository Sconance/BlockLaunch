namespace BlockLaunch.Front;

public sealed class FrontLauncherRuntime(string? directory = null, XboxAuthNet.OAuth.CodeFlow.IWebUI? microsoftBrowser = null)
    : BlockLaunch.LauncherRuntime(directory, microsoftBrowser)
{
}

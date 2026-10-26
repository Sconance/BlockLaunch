namespace BlockLaunch;
// Reserved provider boundary: never scrape pages or silently send an unlicensed key.
public sealed class CurseForgeProvider : IResourceProvider
{
    static Exception Missing() => new NotSupportedException("CurseForge 需要官方 API Key，当前未启用。请使用 Modrinth 或手动下载后导入。");
    public Task<SearchPage> Search(string query, string game, string loader, string type, int offset = 0) => Task.FromException<SearchPage>(Missing());
    public Task<List<ContentVersion>> Versions(string project, string game, string loader, string type) => Task.FromException<List<ContentVersion>>(Missing());
    public Task<InstallPlan> Resolve(ContentVersion version, string game, string loader) => Task.FromException<InstallPlan>(Missing());
}

namespace BlockLaunch;

public record BookmarkView(ResourceBookmark Item)
{
    public override string ToString() => Item.Project.Title + "\n" + KindName(Item.Kind) + "  ·  " + string.Join(" / ", Item.Labels);

    public static string KindName(string kind) => kind switch
    {
        "mod" => "模组",
        "modpack" => "整合包",
        "resourcepack" => "资源包",
        "shader" => "光影",
        "datapack" => "数据包",
        _ => kind
    };
}

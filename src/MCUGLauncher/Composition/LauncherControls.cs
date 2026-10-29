namespace BlockLaunch;

/// <summary>Typed references to independently declared page controls.</summary>
internal sealed class LauncherControls
{
    internal global::System.Windows.Controls.TextBlock LocalCount
    {
        get;
    }
    internal global::System.Windows.Controls.ComboBox Versions
    {
        get;
    }
    internal global::System.Windows.Controls.CheckBox Snapshots
    {
        get;
    }
    internal global::System.Windows.Controls.Button LaunchGameButton
    {
        get;
    }
    internal global::System.Windows.Controls.Button StopGameButton
    {
        get;
    }
    internal global::System.Windows.Controls.TextBlock InstalledVersionsHint
    {
        get;
    }
    internal global::System.Windows.Controls.Image SkinAvatar
    {
        get;
    }
    internal global::System.Windows.Controls.TextBlock AvatarPlayerName
    {
        get;
    }
    internal global::System.Windows.Controls.TextBlock AccountStatus
    {
        get;
    }
    internal global::System.Windows.Controls.TextBlock AvatarCaption
    {
        get;
    }
    internal global::System.Windows.Controls.ComboBox AccountMode
    {
        get;
    }
    internal global::System.Windows.Controls.TextBox PlayerName
    {
        get;
    }
    internal global::System.Windows.Controls.WrapPanel InstallActions
    {
        get;
    }
    internal global::System.Windows.Controls.TextBlock InstallTarget
    {
        get;
    }
    internal global::System.Windows.Controls.CheckBox PackOptional
    {
        get;
    }
    internal global::System.Windows.Controls.WrapPanel LoaderActions
    {
        get;
    }
    internal global::System.Windows.Controls.TextBlock ContentTarget
    {
        get;
    }
    internal global::System.Windows.Controls.TextBox SearchQuery
    {
        get;
    }
    internal global::System.Windows.Controls.ComboBox ContentType
    {
        get;
    }
    internal global::System.Windows.Controls.ListBox SearchResults
    {
        get;
    }
    internal global::System.Windows.Controls.TextBlock SearchHint
    {
        get;
    }
    internal global::System.Windows.Controls.TextBlock ProjectTitle
    {
        get;
    }
    internal global::System.Windows.Controls.TextBlock ProjectDescription
    {
        get;
    }
    internal global::System.Windows.Controls.ComboBox ProjectVersions
    {
        get;
    }
    internal global::System.Windows.Controls.Button InstallContentButton
    {
        get;
    }
    internal global::System.Windows.Controls.TextBlock SearchSummary
    {
        get;
    }
    internal global::System.Windows.Controls.TextBlock ManageTarget
    {
        get;
    }
    internal global::System.Windows.Controls.ListBox InstalledContent
    {
        get;
    }
    internal global::System.Windows.Controls.TextBox GameRoot
    {
        get;
    }
    internal global::System.Windows.Controls.TextBox JavaPath
    {
        get;
    }
    internal global::System.Windows.Controls.TextBox Memory
    {
        get;
    }
    internal global::System.Windows.Controls.TextBox GameWidth
    {
        get;
    }
    internal global::System.Windows.Controls.TextBox GameHeight
    {
        get;
    }
    internal global::System.Windows.Controls.TextBox LogBox
    {
        get;
    }
    internal global::System.Windows.Controls.TextBox GameLogView
    {
        get;
    }
    internal global::System.Windows.Controls.ListBox TaskHistoryList
    {
        get;
    }
    internal global::System.Windows.Controls.RadioButton HomeNav
    {
        get;
    }
    internal global::System.Windows.Controls.RadioButton DiscoverNav
    {
        get;
    }
    internal global::System.Windows.Controls.RadioButton ManageNav
    {
        get;
    }
    internal global::System.Windows.Controls.TextBlock PageTitle
    {
        get;
    }
    internal global::System.Windows.Controls.TextBlock PageSubtitle
    {
        get;
    }
    internal LauncherControls(HomePageView homePageView, InstallPageView installPageView, ResourcePageView resourcePageView, ContentManagementPageView contentManagementPageView, SettingsPageView settingsPageView, LogsPageView logsPageView, TasksPageView tasksPageView, NavigationPageView navigationPageView, HeadingPageView headingPageView)
    {
        LocalCount = homePageView.LocalCount;
        Versions = homePageView.Versions;
        Snapshots = homePageView.Snapshots;
        LaunchGameButton = homePageView.LaunchGameButton;
        StopGameButton = homePageView.StopGameButton;
        InstalledVersionsHint = homePageView.InstalledVersionsHint;
        SkinAvatar = homePageView.SkinAvatar;
        AvatarPlayerName = homePageView.AvatarPlayerName;
        AccountStatus = homePageView.AccountStatus;
        AvatarCaption = homePageView.AvatarCaption;
        AccountMode = homePageView.AccountMode;
        PlayerName = homePageView.PlayerName;
        InstallActions = installPageView.InstallActions;
        InstallTarget = installPageView.InstallTarget;
        PackOptional = installPageView.PackOptional;
        LoaderActions = installPageView.LoaderActions;
        ContentTarget = resourcePageView.ContentTarget;
        SearchQuery = resourcePageView.SearchQuery;
        ContentType = resourcePageView.ContentType;
        SearchResults = resourcePageView.SearchResults;
        SearchHint = resourcePageView.SearchHint;
        ProjectTitle = resourcePageView.ProjectTitle;
        ProjectDescription = resourcePageView.ProjectDescription;
        ProjectVersions = resourcePageView.ProjectVersions;
        InstallContentButton = resourcePageView.InstallContentButton;
        SearchSummary = resourcePageView.SearchSummary;
        ManageTarget = contentManagementPageView.ManageTarget;
        InstalledContent = contentManagementPageView.InstalledContent;
        GameRoot = settingsPageView.GameRoot;
        JavaPath = settingsPageView.JavaPath;
        Memory = settingsPageView.Memory;
        GameWidth = settingsPageView.GameWidth;
        GameHeight = settingsPageView.GameHeight;
        LogBox = logsPageView.LogBox;
        GameLogView = logsPageView.GameLogView;
        TaskHistoryList = tasksPageView.TaskHistoryList;
        HomeNav = navigationPageView.HomeNav;
        DiscoverNav = navigationPageView.DiscoverNav;
        ManageNav = navigationPageView.ManageNav;
        PageTitle = headingPageView.PageTitle;
        PageSubtitle = headingPageView.PageSubtitle;
    }
}

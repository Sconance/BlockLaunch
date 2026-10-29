using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BlockLaunch;

internal sealed partial class LauncherWorkspace
{
    internal void RoutePageAction(object? sender, LauncherPageAction action)
    {
        switch (action.Handler)
        {
            case "VersionChanged":
                ResourceBrowserController.VersionChanged(action.Sender, (SelectionChangedEventArgs)action.Arguments);
                break;
            case "FilterChanged":
                LaunchCoordinator.FilterChanged(action.Sender, (RoutedEventArgs)action.Arguments);
                break;
            case "RefreshClick":
                LaunchCoordinator.RefreshClick(action.Sender, (RoutedEventArgs)action.Arguments);
                break;
            case "LaunchClick":
                LaunchCoordinator.LaunchClick(action.Sender, (RoutedEventArgs)action.Arguments);
                break;
            case "StopClick":
                LaunchCoordinator.StopClick(action.Sender, (RoutedEventArgs)action.Arguments);
                break;
            case "NewInstanceClick":
                TaskRecoveryController.NewInstanceClick(action.Sender, (RoutedEventArgs)action.Arguments);
                break;
            case "RefreshAvatarClick":
                AvatarController.RefreshAvatarClick(action.Sender, (RoutedEventArgs)action.Arguments);
                break;
            case "LoginClick":
                LaunchCoordinator.LoginClick(action.Sender, (RoutedEventArgs)action.Arguments);
                break;
            case "InstallClick":
                LaunchCoordinator.InstallClick(action.Sender, (RoutedEventArgs)action.Arguments);
                break;
            case "ImportPackClick":
                ModpackController.ImportPackClick(action.Sender, (RoutedEventArgs)action.Arguments);
                break;
            case "FabricClick":
                LaunchCoordinator.FabricClick(action.Sender, (RoutedEventArgs)action.Arguments);
                break;
            case "QuiltClick":
                ResourceBrowserController.QuiltClick(action.Sender, (RoutedEventArgs)action.Arguments);
                break;
            case "LoaderJarClick":
                ResourceBrowserController.LoaderJarClick(action.Sender, (RoutedEventArgs)action.Arguments);
                break;
            case "SearchKeyDown":
                ResourceBrowserController.SearchKeyDown(action.Sender, (KeyEventArgs)action.Arguments);
                break;
            case "ContentTypeChanged":
                ResourceBrowserController.ContentTypeChanged(action.Sender, (SelectionChangedEventArgs)action.Arguments);
                break;
            case "SearchClick":
                ResourceBrowserController.SearchClick(action.Sender, (RoutedEventArgs)action.Arguments);
                break;
            case "ProjectChanged":
                ResourceBrowserController.ProjectChanged(action.Sender, (SelectionChangedEventArgs)action.Arguments);
                break;
            case "InstallContentClick":
                ResourceBrowserController.InstallContentClick(action.Sender, (RoutedEventArgs)action.Arguments);
                break;
            case "ProjectPageClick":
                ResourceBrowserController.ProjectPageClick(action.Sender, (RoutedEventArgs)action.Arguments);
                break;
            case "PreviousSearch":
                ResourceBrowserController.PreviousSearch(action.Sender, (RoutedEventArgs)action.Arguments);
                break;
            case "NextSearch":
                ResourceBrowserController.NextSearch(action.Sender, (RoutedEventArgs)action.Arguments);
                break;
            case "ImportModClick":
                LaunchCoordinator.ImportModClick(action.Sender, (RoutedEventArgs)action.Arguments);
                break;
            case "ImportResourceClick":
                LaunchCoordinator.ImportResourceClick(action.Sender, (RoutedEventArgs)action.Arguments);
                break;
            case "RefreshContentClick":
                ResourceBrowserController.RefreshContentClick(action.Sender, (RoutedEventArgs)action.Arguments);
                break;
            case "ModsFolderClick":
                LaunchCoordinator.ModsFolderClick(action.Sender, (RoutedEventArgs)action.Arguments);
                break;
            case "TrashFolderClick":
                ResourceBrowserController.TrashFolderClick(action.Sender, (RoutedEventArgs)action.Arguments);
                break;
            case "ToggleContentClick":
                ResourceBrowserController.ToggleContentClick(action.Sender, (RoutedEventArgs)action.Arguments);
                break;
            case "UpdateContentClick":
                ResourceBrowserController.UpdateContentClick(action.Sender, (RoutedEventArgs)action.Arguments);
                break;
            case "CheckAllUpdatesClick":
                ContentUpdatesController.CheckAllUpdatesClick(action.Sender, (RoutedEventArgs)action.Arguments);
                break;
            case "RestoreContentClick":
                ContentUpdatesController.RestoreContentClick(action.Sender, (RoutedEventArgs)action.Arguments);
                break;
            case "RemoveContentClick":
                ResourceBrowserController.RemoveContentClick(action.Sender, (RoutedEventArgs)action.Arguments);
                break;
            case "SaveClick":
                LaunchCoordinator.SaveClick(action.Sender, (RoutedEventArgs)action.Arguments);
                break;
            case "NetworkClick":
                NetworkController.NetworkClick(action.Sender, (RoutedEventArgs)action.Arguments);
                break;
            case "NoticesClick":
                ModpackController.NoticesClick(action.Sender, (RoutedEventArgs)action.Arguments);
                break;
            case "ReadGameLogClick":
                TaskRecoveryController.ReadGameLogClick(action.Sender, (RoutedEventArgs)action.Arguments);
                break;
            case "LogsFolderClick":
                TaskRecoveryController.LogsFolderClick(action.Sender, (RoutedEventArgs)action.Arguments);
                break;
            case "PauseDownloadClick":
                DownloadQueueController.PauseDownloadClick(action.Sender, (RoutedEventArgs)action.Arguments);
                break;
            case "ResumeDownloadClick":
                DownloadQueueController.ResumeDownloadClick(action.Sender, (RoutedEventArgs)action.Arguments);
                break;
            case "CancelDownloadClick":
                DownloadQueueController.CancelDownloadClick(action.Sender, (RoutedEventArgs)action.Arguments);
                break;
            case "RefreshOperationsClick":
                TaskRecoveryController.RefreshOperationsClick(action.Sender, (RoutedEventArgs)action.Arguments);
                break;
            case "LocateOperationClick":
                TaskRecoveryController.LocateOperationClick(action.Sender, (RoutedEventArgs)action.Arguments);
                break;
            case "Navigate":
                ResourceBrowserController.Navigate(action.Sender, (RoutedEventArgs)action.Arguments);
                break;
            default:
                throw new InvalidOperationException("Unknown page action: " + action.Handler);
        }
    }
}

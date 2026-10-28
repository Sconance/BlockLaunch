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
                ContentController.VersionChanged(action.Sender, (SelectionChangedEventArgs)action.Arguments);
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
                StabilityController.NewInstanceClick(action.Sender, (RoutedEventArgs)action.Arguments);
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
                PackController.ImportPackClick(action.Sender, (RoutedEventArgs)action.Arguments);
                break;
            case "FabricClick":
                LaunchCoordinator.FabricClick(action.Sender, (RoutedEventArgs)action.Arguments);
                break;
            case "QuiltClick":
                ContentController.QuiltClick(action.Sender, (RoutedEventArgs)action.Arguments);
                break;
            case "LoaderJarClick":
                ContentController.LoaderJarClick(action.Sender, (RoutedEventArgs)action.Arguments);
                break;
            case "SearchKeyDown":
                ContentController.SearchKeyDown(action.Sender, (KeyEventArgs)action.Arguments);
                break;
            case "ContentTypeChanged":
                ContentController.ContentTypeChanged(action.Sender, (SelectionChangedEventArgs)action.Arguments);
                break;
            case "SearchClick":
                ContentController.SearchClick(action.Sender, (RoutedEventArgs)action.Arguments);
                break;
            case "ProjectChanged":
                ContentController.ProjectChanged(action.Sender, (SelectionChangedEventArgs)action.Arguments);
                break;
            case "InstallContentClick":
                ContentController.InstallContentClick(action.Sender, (RoutedEventArgs)action.Arguments);
                break;
            case "ProjectPageClick":
                ContentController.ProjectPageClick(action.Sender, (RoutedEventArgs)action.Arguments);
                break;
            case "PreviousSearch":
                ContentController.PreviousSearch(action.Sender, (RoutedEventArgs)action.Arguments);
                break;
            case "NextSearch":
                ContentController.NextSearch(action.Sender, (RoutedEventArgs)action.Arguments);
                break;
            case "ImportModClick":
                LaunchCoordinator.ImportModClick(action.Sender, (RoutedEventArgs)action.Arguments);
                break;
            case "ImportResourceClick":
                LaunchCoordinator.ImportResourceClick(action.Sender, (RoutedEventArgs)action.Arguments);
                break;
            case "RefreshContentClick":
                ContentController.RefreshContentClick(action.Sender, (RoutedEventArgs)action.Arguments);
                break;
            case "ModsFolderClick":
                LaunchCoordinator.ModsFolderClick(action.Sender, (RoutedEventArgs)action.Arguments);
                break;
            case "TrashFolderClick":
                ContentController.TrashFolderClick(action.Sender, (RoutedEventArgs)action.Arguments);
                break;
            case "ToggleContentClick":
                ContentController.ToggleContentClick(action.Sender, (RoutedEventArgs)action.Arguments);
                break;
            case "UpdateContentClick":
                ContentController.UpdateContentClick(action.Sender, (RoutedEventArgs)action.Arguments);
                break;
            case "CheckAllUpdatesClick":
                UpdatesController.CheckAllUpdatesClick(action.Sender, (RoutedEventArgs)action.Arguments);
                break;
            case "RestoreContentClick":
                UpdatesController.RestoreContentClick(action.Sender, (RoutedEventArgs)action.Arguments);
                break;
            case "RemoveContentClick":
                ContentController.RemoveContentClick(action.Sender, (RoutedEventArgs)action.Arguments);
                break;
            case "SaveClick":
                LaunchCoordinator.SaveClick(action.Sender, (RoutedEventArgs)action.Arguments);
                break;
            case "NetworkClick":
                NetworkController.NetworkClick(action.Sender, (RoutedEventArgs)action.Arguments);
                break;
            case "NoticesClick":
                PackController.NoticesClick(action.Sender, (RoutedEventArgs)action.Arguments);
                break;
            case "ReadGameLogClick":
                StabilityController.ReadGameLogClick(action.Sender, (RoutedEventArgs)action.Arguments);
                break;
            case "LogsFolderClick":
                StabilityController.LogsFolderClick(action.Sender, (RoutedEventArgs)action.Arguments);
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
                StabilityController.RefreshOperationsClick(action.Sender, (RoutedEventArgs)action.Arguments);
                break;
            case "LocateOperationClick":
                StabilityController.LocateOperationClick(action.Sender, (RoutedEventArgs)action.Arguments);
                break;
            case "Navigate":
                ContentController.Navigate(action.Sender, (RoutedEventArgs)action.Arguments);
                break;
            default:
                throw new InvalidOperationException("Unknown page action: " + action.Handler);
        }
    }
}

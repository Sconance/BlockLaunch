using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using XboxAuthNet.OAuth.CodeFlow;

namespace BlockLaunch;

/// <summary>Uses the library's OAuth URL checker and token exchange; only the browser surface changes.</summary>
public sealed class EmbeddedMicrosoftOAuthUI(MainWindow host, string profileDirectory) : IWebUI
{
    public static bool IsMicrosoftAuthorization(Uri uri) => MicrosoftAuthorizationPolicy.IsAuthorization(uri);
    public Task<CodeFlowAuthorizationResult> DisplayDialogAndInterceptUri(Uri uri, ICodeFlowUrlChecker checker, CancellationToken token) => host.Dispatcher.CheckAccess() ? Open(uri, checker, token) : host.Dispatcher.InvokeAsync(() => Open(uri, checker, token)).Task.Unwrap();
    public async Task DisplayDialogAndNavigateUri(Uri uri, CancellationToken token) => await DisplayDialogAndInterceptUri(uri, new NoCode(), token);

    sealed class NoCode : ICodeFlowUrlChecker
    {
        public CodeFlowAuthorizationResult GetAuthCodeResult(Uri uri) => new();
    }

    async Task<CodeFlowAuthorizationResult> Open(Uri uri, ICodeFlowUrlChecker checker, CancellationToken token)
    {
        if (!IsMicrosoftAuthorization(uri))
            throw new AccountLoginException("授权地址不是微软官方 HTTPS 登录地址。");
        token.ThrowIfCancellationRequested();
        var completion = new TaskCompletionSource<CodeFlowAuthorizationResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var grid = new Grid();
        grid.RowDefinitions.Add(new()
        {
            Height = GridLength.Auto
        });
        grid.RowDefinitions.Add(new());
        var state = new TextBlock
        {
            Text = "微软官方登录 · 请在下方页面完成授权",
            Margin = new Thickness(8, 4, 8, 10),
            TextWrapping = TextWrapping.Wrap
        };
        grid.Children.Add(state);
        using var view = new WebView2();
        Grid.SetRow(view, 1);
        grid.Children.Add(view);
        var page = new InlineDialog
        {
            Owner = host,
            Title = "微软正版登录",
            Content = grid
        };
        page.Closed += (_, _) => completion.TrySetCanceled();
        using var cancel = token.Register(() => host.Dispatcher.BeginInvoke(() =>
        {
            completion.TrySetCanceled(token);
            page.ForceClose();
        }));
        try
        {
            page.Show();
            Directory.CreateDirectory(profileDirectory);
            var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: profileDirectory);
            await view.EnsureCoreWebView2Async(environment);
            token.ThrowIfCancellationRequested();
            view.CoreWebView2.Settings.AreDevToolsEnabled = false;
            view.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            view.CoreWebView2.Settings.IsStatusBarEnabled = false;
            view.CoreWebView2.PermissionRequested += (_, e) => e.State = CoreWebView2PermissionState.Deny;
            view.CoreWebView2.NewWindowRequested += (_, e) =>
            {
                e.Handled = true;
                if (Uri.TryCreate(e.Uri, UriKind.Absolute, out var next) && next.Scheme == "https")
                    view.CoreWebView2.Navigate(next.AbsoluteUri);
            };
            view.CoreWebView2.NavigationStarting += (_, e) =>
            {
                if (!Uri.TryCreate(e.Uri, UriKind.Absolute, out var next))
                {
                    e.Cancel = true;
                    return;
                }

                var result = checker.GetAuthCodeResult(next);
                if (!result.IsEmpty)
                {
                    e.Cancel = true;
                    completion.TrySetResult(result);
                    return;
                }

                if (next.Scheme != "https")
                    e.Cancel = true;
            };
            view.CoreWebView2.Navigate(uri.AbsoluteUri);
            return await completion.Task;
        }
        finally
        {
            page.Close();
        }
    }
}

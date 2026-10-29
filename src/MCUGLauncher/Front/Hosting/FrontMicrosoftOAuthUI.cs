using System.IO;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using XboxAuthNet.OAuth.CodeFlow;

namespace BlockLaunch.Front;

public sealed class FrontMicrosoftOAuthUI(Func<Window?> owner, string profileDirectory) : IWebUI
{
    public Task<CodeFlowAuthorizationResult> DisplayDialogAndInterceptUri(Uri uri, ICodeFlowUrlChecker checker, CancellationToken token)
    {
        var dispatcher = Application.Current.Dispatcher;
        return dispatcher.CheckAccess() ? Open(uri, checker, token) : dispatcher.InvokeAsync(() => Open(uri, checker, token)).Task.Unwrap();
    }

    public async Task DisplayDialogAndNavigateUri(Uri uri, CancellationToken token) => await DisplayDialogAndInterceptUri(uri, new NoCode(), token);

    sealed class NoCode : ICodeFlowUrlChecker
    {
        public CodeFlowAuthorizationResult GetAuthCodeResult(Uri uri) => new();
    }

    async Task<CodeFlowAuthorizationResult> Open(Uri uri, ICodeFlowUrlChecker checker, CancellationToken token)
    {
        if (!MicrosoftAuthorizationPolicy.IsAuthorization(uri))
            throw new AccountLoginException("授权地址不是微软官方 HTTPS 登录地址。");
        token.ThrowIfCancellationRequested();
        var completion = new TaskCompletionSource<CodeFlowAuthorizationResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var browser = new WebView2();
        var dialog = new Window
        {
            Owner = owner(), Title = "微软正版登录", Width = 720, Height = 740, Content = browser,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        var closed = false;
        dialog.Closed += (_, _) => { closed = true; completion.TrySetCanceled(); };
        using var cancellation = token.Register(() => dialog.Dispatcher.BeginInvoke(() =>
        {
            completion.TrySetCanceled(token);
            if (!closed)
                dialog.Close();
        }));
        try
        {
            dialog.Show();
            Directory.CreateDirectory(profileDirectory);
            var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: profileDirectory);
            if (closed)
                throw new OperationCanceledException();
            await browser.EnsureCoreWebView2Async(environment);
            token.ThrowIfCancellationRequested();
            if (closed)
                throw new OperationCanceledException();
            browser.CoreWebView2.Settings.AreDevToolsEnabled = false;
            browser.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            browser.CoreWebView2.Settings.IsStatusBarEnabled = false;
            browser.CoreWebView2.PermissionRequested += (_, args) => args.State = CoreWebView2PermissionState.Deny;
            browser.CoreWebView2.NewWindowRequested += (_, args) =>
            {
                args.Handled = true;
                if (Uri.TryCreate(args.Uri, UriKind.Absolute, out var next) && next.Scheme == "https")
                    browser.CoreWebView2.Navigate(next.AbsoluteUri);
            };
            browser.CoreWebView2.NavigationStarting += (_, args) =>
            {
                if (!Uri.TryCreate(args.Uri, UriKind.Absolute, out var next))
                {
                    args.Cancel = true;
                    return;
                }
                var result = checker.GetAuthCodeResult(next);
                if (!result.IsEmpty)
                {
                    args.Cancel = true;
                    completion.TrySetResult(result);
                }
                else if (next.Scheme != "https")
                    args.Cancel = true;
            };
            browser.CoreWebView2.Navigate(uri.AbsoluteUri);
            return await completion.Task;
        }
        finally
        {
            if (!closed)
                dialog.Close();
        }
    }
}

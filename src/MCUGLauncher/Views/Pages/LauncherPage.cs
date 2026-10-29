using System.Windows;
using System.Windows.Controls;

namespace BlockLaunch;

public sealed class LauncherPageAction(string handler, object sender, RoutedEventArgs arguments) : EventArgs
{
    public string Handler { get; } = handler;
    public object Sender { get; } = sender;
    public RoutedEventArgs Arguments { get; } = arguments;
}

/// <summary>A standalone markup view with an explicit action boundary.</summary>
public class LauncherPage : UserControl
{
    public event EventHandler<LauncherPageAction>? PageAction;
    protected void RaisePageAction(string handler, object sender, RoutedEventArgs arguments) => PageAction?.Invoke(this, new(handler, sender, arguments));

    // Preserve the existing visual tree while page controllers progressively take over layout ownership.
    // Named controls remain owned by the markup view and are supplied through typed bindings.
    internal ContentControl? PageHost
    {
        get; private set;
    }

    internal void Materialize()
    {
        var parent = Parent;
        var content = (FrameworkElement)Content;
        Content = null;
        if (parent is ContentControl host)
        {
            PageHost = host;
            host.Content = content;
        }
        else if (parent is Panel panel)
        {
            var index = panel.Children.IndexOf(this);
            panel.Children.RemoveAt(index);
            panel.Children.Insert(index, content);
        }
        else
            throw new InvalidOperationException("The page must have a content or panel host.");
    }
}

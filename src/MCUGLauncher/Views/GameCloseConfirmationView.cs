using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace BlockLaunch;

/// <summary>Small confirmation surface inside the owner window; it creates no native dialog.</summary>
internal sealed class GameCloseConfirmationView(Window owner)
{
    TaskCompletionSource<bool>? pending;

    internal Task<bool> Confirm(RunningGameRow game)
    {
        if (pending != null)
            return Task.FromResult(false);
        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        pending = completion;
        var root = (Grid)owner.Content;
        var previous = Keyboard.FocusedElement;
        var overlay = new Grid { Background = new SolidColorBrush(Color.FromArgb(70, 0, 0, 0)), Tag = "GameCloseConfirmation" };
        var body = new StackPanel();
        body.Children.Add(FeatureUiElements.Heading("确认关闭游戏？"));
        body.Children.Add(FeatureUiElements.Hint(game.Version + (game.PlayerName.Length > 0 ? " · " + game.PlayerName : "")));
        body.Children.Add(FeatureUiElements.Hint("未保存的内容可能丢失，请先在游戏中保存。只会向所选游戏发送关闭请求。"));
        var buttons = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right };
        var cancel = FeatureUiElements.ActionButton("取消", (_, _) => Finish(false));
        var close = FeatureUiElements.ActionButton("确认关闭", (_, _) => Finish(true));
        buttons.Children.Add(cancel);
        buttons.Children.Add(close);
        body.Children.Add(buttons);
        var card = new Border
        {
            Child = body,
            Width = 390,
            MaxWidth = Math.Max(280, owner.ActualWidth - 40),
            Padding = new Thickness(20),
            CornerRadius = new CornerRadius(10),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        card.SetResourceReference(Border.BackgroundProperty, "ThemeSurfaceBrush");
        overlay.Children.Add(card);
        KeyboardNavigation.SetTabNavigation(overlay, KeyboardNavigationMode.Cycle);
        Grid.SetRowSpan(overlay, Math.Max(1, root.RowDefinitions.Count));
        Grid.SetColumnSpan(overlay, Math.Max(1, root.ColumnDefinitions.Count));
        Panel.SetZIndex(overlay, 1000);
        root.Children.Add(overlay);
        owner.PreviewKeyDown += KeyDown;
        owner.Closed += Closed;
        cancel.Focus();
        return completion.Task;

        void Finish(bool approved)
        {
            if (completion.Task.IsCompleted)
                return;
            root.Children.Remove(overlay);
            owner.PreviewKeyDown -= KeyDown;
            owner.Closed -= Closed;
            pending = null;
            previous?.Focus();
            completion.TrySetResult(approved);
        }
        void KeyDown(object sender, KeyEventArgs args)
        {
            if (args.Key == Key.Escape)
            {
                args.Handled = true;
                Finish(false);
            }
        }
        void Closed(object? sender, EventArgs args) => Finish(false);
    }
}

using System.Windows;
using System.Windows.Controls;

namespace BlockLaunch;
internal sealed class HomeAccountController
{
    readonly AccountPanelControls controls;
    readonly AccountPanelActions actions;
    readonly Func<(bool Microsoft, bool Mua)> getAccountState;
    readonly Action<Border> configureWelcome;

    internal HomeAccountController(AccountPanelControls controls, AccountPanelActions actions,
        Func<(bool Microsoft, bool Mua)> getAccountState, Action<Border> configureWelcome)
    {
        this.controls = controls;
        this.actions = actions;
        this.getAccountState = getAccountState;
        this.configureWelcome = configureWelcome;
    }

    internal AccountPanelView? accountPanel;
    internal void BuildHomeAccountPanel()
    {
        // Reuse the named XAML controls: existing bindings and authentication handlers stay attached.
        var originalPanel = (StackPanel)controls.Mode.Parent;
        var card = (Border)originalPanel.Parent;
        var avatar = controls.Avatar;
        var originalControls = new UIElement[]
        {
            controls.Mode,
            controls.PlayerName,
            controls.DisplayName,
            controls.Status,
            avatar
        };
        foreach (var child in originalControls)
        {
            originalPanel.Children.Remove(child);
        }

        card.Child = null;
        card.Padding = new Thickness(16, 12, 16, 14);
        card.CornerRadius = new CornerRadius(10);
        accountPanel = new AccountPanelView(controls, actions);
        card.Child = accountPanel;
        configureWelcome(card);
        UpdateAccountPanel();
    }

    internal void UpdateAccountPanel()
    {
        var state = getAccountState();
        accountPanel?.UpdateState(state.Microsoft, state.Mua);
    }
}

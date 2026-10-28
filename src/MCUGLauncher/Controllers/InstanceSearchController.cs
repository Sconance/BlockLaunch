using System.ComponentModel;
using System.Windows.Controls;

namespace BlockLaunch;

internal sealed class InstanceSearchController(
    ListBox instanceList,
    TextBox searchQuery,
    TextBlock searchSummary)
{
    public void Apply()
    {
        var view = instanceList.ItemsSource as ICollectionView;
        if (view == null)
            return;
        var terms = searchQuery.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        view.Filter = item => item is InstanceView instance && terms.All(t => (instance.Name + " " + instance.Key + " " + instance.Detail).Contains(t, StringComparison.OrdinalIgnoreCase));
        searchSummary.Text = $"显示 {view.Cast<object>().Count()} 个已安装版本";
    }
}

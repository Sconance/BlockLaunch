using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BlockLaunch;

public partial class MainWindow
{
    async Task LoadResourcePresentation(ContentProject project, CancellationToken token)
    {
        try
        {
            var presentation = await resourceManagement.ReadPresentation(project.Id, token);
            token.ThrowIfCancellationRequested();
            foreach (var url in presentation.Gallery)
            {
                if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.DecodePixelWidth = 320;
                    bitmap.UriSource = uri;
                    bitmap.EndInit();
                    resourceGallery.Children.Add(new Border
                    {
                        Width = 150,
                        Height = 92,
                        CornerRadius = new CornerRadius(7),
                        ClipToBounds = true,
                        Margin = new Thickness(0, 0, 7, 7),
                        Child = new Image
                        {
                            Source = bitmap,
                            Stretch = Stretch.UniformToFill,
                            ToolTip = project.Title
                        }
                    });
                }
            }
            resourceBody.Text = presentation.Body;
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Log("项目介绍暂不可用，仍可选择版本和下载：" + ex.Message);
        }
    }
}

using System.Text.Json;
using System.Text.RegularExpressions;
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
            using var document = JsonDocument.Parse(await http.GetStringAsync(
                "https://api.modrinth.com/v2/project/" + Uri.EscapeDataString(project.Id), token));
            token.ThrowIfCancellationRequested();
            var root = document.RootElement;
            if (root.TryGetProperty("gallery", out var gallery))
            {
                foreach (var entry in gallery.EnumerateArray().Take(3))
                {
                    if (!entry.TryGetProperty("url", out var url)
                        || !Uri.TryCreate(url.GetString(), UriKind.Absolute, out var uri)
                        || uri.Scheme != Uri.UriSchemeHttps
                        || uri.Host != "cdn.modrinth.com")
                        continue;
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
                            ToolTip = entry.TryGetProperty("title", out var title) ? title.GetString() : project.Title
                        }
                    });
                }
            }
            if (root.TryGetProperty("body", out var body))
            {
                var text = body.GetString() ?? "";
                text = text[..Math.Min(text.Length, 12000)];
                text = Regex.Replace(text, @"!\[[^\]]*\]\([^)]*\)", "");
                text = Regex.Replace(text, @"\[([^\]]+)\]\([^)]*\)", "$1");
                text = Regex.Replace(text, "<[^>]*>", "");
                text = Regex.Replace(text, @"(?m)^#{1,6}\s+", "");
                resourceBody.Text = text.Replace("**", "").Replace("__", "").Replace("```", "").Trim();
            }
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

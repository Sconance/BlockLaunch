using System.Windows.Media;

namespace BlockLaunch;

/// <summary>Category artwork can be replaced independently of navigation and download behavior.</summary>
internal sealed record DownloadCategoryIcon(string Glyph, ImageSource? Image = null);

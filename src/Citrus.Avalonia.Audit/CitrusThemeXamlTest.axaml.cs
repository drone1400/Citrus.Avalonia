using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace Citrus.Avalonia.Audit;

/// <summary>
/// Compiled XAML that configures a <see cref="CitrusTheme"/> the way the README documents it
/// </summary>
public partial class CitrusThemeXamlTest : Styles {
    public CitrusThemeXamlTest() {
        AvaloniaXamlLoader.Load(this);
    }
}

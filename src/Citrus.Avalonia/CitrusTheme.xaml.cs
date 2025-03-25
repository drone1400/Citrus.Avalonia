using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using Avalonia.Themes.Simple;
namespace Citrus.Avalonia;

public class CitrusTheme : Styles {
    
    /// <summary>
    /// helper class for managing theme palette data
    /// </summary>
    public class CitrusPalette {
        
        /// <summary>
        /// Palette Key
        /// </summary>
        public string Key { get; }
        
        /// <summary>
        /// URI for palette definition xaml
        /// </summary>
        public Uri Uri { get; }
        public CitrusPalette(string key, Uri uri) {
            this.Key = key;
            this.Uri = uri;
        }
    }

    private class CitrusPaletteData {
        public string Key { get; }
        public Uri Uri { get; }
        public ResourceInclude ResourceInclude { get; }

        public CitrusPaletteData(string key, Uri uri) {
            this.Key = key;
            this.Uri = uri;
            this.ResourceInclude = new ResourceInclude(uri) { Source = uri, };
        }
    }
    
    private readonly Uri _uriSimpleTheme = new Uri("avares://Avalonia.Themes.Simple/SimpleTheme.xaml");
    private readonly Uri _uriControls = new Uri("avares://Citrus.Avalonia/CitrusControls.xaml");
    // default palettes
    private readonly Uri _uriCandyPalette = new Uri("avares://Citrus.Avalonia/Palette/CandyPalette.xaml");
    private readonly Uri _uriCitrusPalette = new Uri("avares://Citrus.Avalonia/Palette/CitrusPalette.xaml");
    private readonly Uri _uriMagmaPalette = new Uri("avares://Citrus.Avalonia/Palette/MagmaPalette.xaml");
    private readonly Uri _uriRustPalette = new Uri("avares://Citrus.Avalonia/Palette/RustPalette.xaml");
    private readonly Uri _uriSeaPalette = new Uri("avares://Citrus.Avalonia/Palette/SeaPalette.xaml");
    
    

    private readonly Dictionary<string, CitrusPaletteData> _palettes = new Dictionary<string, CitrusPaletteData>();

    private string _currentThemeKey = "";
    
    /// <summary>
    /// StyledProperty that defines the currently selected color palette 
    /// </summary>
    public static readonly StyledProperty<string> ColorPaletteProperty = 
        AvaloniaProperty.Register<CitrusTheme, string>(nameof(ColorPalette), defaultValue: "Citrus");
    
    /// <summary>
    /// The currently selected color palette, set will only update the property value if the provided value is a registered theme Key.
    /// </summary>
    public string ColorPalette {
        get => this.GetValue(CitrusTheme.ColorPaletteProperty);
        set {
            if (this.SelectPalette(value)) {
                this.SetValue(CitrusTheme.ColorPaletteProperty, value);
            }
        }
    }
    
    public CitrusTheme(IServiceProvider? sp = null) {
        //this.Add(new StyleInclude(this._uriSimpleTheme) { Source = this._uriSimpleTheme});
        
        this.Add(new StyleInclude(this._uriControls) { Source = this._uriControls});
        
        AvaloniaXamlLoader.Load(sp, this);

        this.RegisterPalette("Candy", this._uriCandyPalette);
        this.RegisterPalette("Citrus", this._uriCitrusPalette);
        this.RegisterPalette("Magma", this._uriMagmaPalette);
        this.RegisterPalette("Rust", this._uriRustPalette);
        this.RegisterPalette("Sea", this._uriSeaPalette);

        this.SelectPalette("Citrus");
    }

    
    /// <summary>
    /// Allows you to register a new custom color palette using a given URI
    /// </summary>
    /// <param name="key">Color Palette Key</param>
    /// <param name="uri">Uri to the avares xaml file</param>
    /// <returns>true if successful, false if key already exists or some other error occurred...</returns>
    public bool RegisterPalette(string key, Uri uri) {
        if (this._palettes.ContainsKey(key)) return false;
        
        // TODO validate URI somehow maybe?...
        this._palettes.Add(key, new CitrusPaletteData(key, uri));
        return true;
    }

    /// <summary>
    /// Gets a list of all the currently registered Palettes
    /// </summary>
    /// <returns>List of <see cref="CitrusPalette"/></returns>
    public List<CitrusPalette> GetRegisteredPalettes() {
        List<CitrusPalette> palettes = new List<CitrusPalette>();
        foreach (var palette in this._palettes) {
            palettes.Add(new CitrusPalette(palette.Key, palette.Value.Uri));
        }
        return palettes;
    }

    private bool SelectPalette(string key) {
        // check if same as current
        if (key == this._currentThemeKey) return true;
        
        if (this._palettes.TryGetValue(key, out CitrusPaletteData? data) == false) return false;

        // remove existing palette if possible
        if (this._palettes.TryGetValue(this._currentThemeKey, out CitrusPaletteData? currentData)) {
            this.Resources.MergedDictionaries.Remove(currentData.ResourceInclude);
        }
        
        // add new palette
        this.Resources.MergedDictionaries.Insert(0, data.ResourceInclude);
        this._currentThemeKey = key;
        return true;
    }
    
}

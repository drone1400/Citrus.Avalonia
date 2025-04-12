using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;

namespace Citrus.Avalonia;

public class CitrusTheme : Styles {
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

        this.RegisterPalette(new CitrusPaletteData(CitrusDefaultPalettes.Citrus.ToString(), this._uriCitrusPalette));
        this.RegisterPalette(new CitrusPaletteData(CitrusDefaultPalettes.Candy.ToString(), this._uriCandyPalette));
        this.RegisterPalette(new CitrusPaletteData(CitrusDefaultPalettes.Magma.ToString(), this._uriMagmaPalette));
        this.RegisterPalette(new CitrusPaletteData(CitrusDefaultPalettes.Rust.ToString(), this._uriRustPalette));
        this.RegisterPalette(new CitrusPaletteData(CitrusDefaultPalettes.Sea.ToString(), this._uriSeaPalette));

        this.SelectPalette("Citrus");
    }

    
    /// <summary>
    /// Allows you to register a new custom color palette using a given URI
    /// </summary>
    /// <param name="key">Color Palette Key</param>
    /// <param name="resource">ResourceInclude of the palette definition</param>
    /// <returns>true if successful, false if key already exists or some other error occurred...</returns>
    public bool RegisterPalette(string key, ResourceInclude resource) {
        if (this._palettes.ContainsKey(key)) return false;
        
        this._palettes.Add(key, new CitrusPaletteData(key, resource));
        return true;
    }
    
    public bool RegisterPalette(CitrusPaletteData resourceData) {
        if (this._palettes.ContainsKey(resourceData.Key)) return false;
        
        this._palettes.Add(resourceData.Key, resourceData);
        return true;
    }

    /// <summary>
    /// Gets a list of all the currently registered Palettes
    /// </summary>
    /// <returns>List of <see cref="CitrusPalette"/></returns>
    public List<CitrusPaletteData> GetRegisteredPalettes() {
        List<CitrusPaletteData> palettes = new List<CitrusPaletteData>();
        foreach (var palette in this._palettes) {
            palettes.Add(palette.Value);
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

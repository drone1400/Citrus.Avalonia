using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;

namespace Citrus.Avalonia;

public class CitrusTheme : Styles {
    private readonly Uri _uriControls = new Uri("avares://Citrus.Avalonia/CitrusControls.xaml");
    
    // default palettes
    private readonly Uri _uriCandyPalette = new Uri("avares://Citrus.Avalonia/Palette/CandyPalette.xaml");
    private readonly Uri _uriCitrusPalette = new Uri("avares://Citrus.Avalonia/Palette/CitrusPalette.xaml");
    private readonly Uri _uriMagmaPalette = new Uri("avares://Citrus.Avalonia/Palette/MagmaPalette.xaml");
    private readonly Uri _uriRustPalette = new Uri("avares://Citrus.Avalonia/Palette/RustPalette.xaml");
    private readonly Uri _uriSeaPalette = new Uri("avares://Citrus.Avalonia/Palette/SeaPalette.xaml");
    
    

    private readonly Dictionary<ThemeVariant, CitrusThemeVariantData> _palettes = new Dictionary<ThemeVariant, CitrusThemeVariantData>();
    
    /// <summary>
    /// StyledProperty that defines the desired Default theme variant to use 
    /// </summary>
    public static readonly StyledProperty<IThemeVariantProvider> DesiredDefaultThemeVariantProperty = 
        AvaloniaProperty.Register<CitrusTheme, IThemeVariantProvider>(nameof(DesiredDefaultThemeVariant));
    
    /// <summary>
    /// The desired Default theme variant
    /// </summary>
    public IThemeVariantProvider DesiredDefaultThemeVariant {
        get => this.GetValue(CitrusTheme.DesiredDefaultThemeVariantProperty);
        set {
            if (this.TrySetDefaultThemeVariant(value)) {
                this.SetValue(CitrusTheme.DesiredDefaultThemeVariantProperty, value);
            }
        }
    }
    
    /// <summary>
    /// StyledProperty that defines the desired Light theme variant to use 
    /// </summary>
    public static readonly StyledProperty<IThemeVariantProvider> DesiredLightThemeVariantProperty = 
        AvaloniaProperty.Register<CitrusTheme, IThemeVariantProvider>(nameof(DesiredLightThemeVariant));
    
    /// <summary>
    /// The desired Light theme variant
    /// </summary>
    public IThemeVariantProvider DesiredLightThemeVariant {
        get => this.GetValue(CitrusTheme.DesiredLightThemeVariantProperty);
        set {
            if (this.TrySetLightThemeVariant(value)) {
                this.SetValue(CitrusTheme.DesiredLightThemeVariantProperty, value);
            }
        }
    }
    
    /// <summary>
    /// StyledProperty that defines the desired Dark theme variant to use 
    /// </summary>
    public static readonly StyledProperty<IThemeVariantProvider> DesiredDarkThemeVariantProperty = 
        AvaloniaProperty.Register<CitrusTheme, IThemeVariantProvider>(nameof(DesiredDarkThemeVariant));
    
    /// <summary>
    /// The desired Dark theme variant
    /// </summary>
    public IThemeVariantProvider DesiredDarkThemeVariant {
        get => this.GetValue(CitrusTheme.DesiredDarkThemeVariantProperty);
        set {
            if (this.TrySetDarkThemeVariant(value)) {
                this.SetValue(CitrusTheme.DesiredDarkThemeVariantProperty, value);
            }
        }
    }
    
    public CitrusTheme(IServiceProvider? sp = null) {
        
        this.Add(new StyleInclude(this._uriControls) { Source = this._uriControls});
        
        AvaloniaXamlLoader.Load(sp, this);

        this.RegisterThemeVariant(new CitrusThemeVariantData(CitrusDefaultPalettes.Citrus.ToString(), this._uriCitrusPalette));
        this.RegisterThemeVariant(new CitrusThemeVariantData(CitrusDefaultPalettes.Candy.ToString(), this._uriCandyPalette));
        this.RegisterThemeVariant(new CitrusThemeVariantData(CitrusDefaultPalettes.Magma.ToString(), this._uriMagmaPalette));
        this.RegisterThemeVariant(new CitrusThemeVariantData(CitrusDefaultPalettes.Rust.ToString(), this._uriRustPalette));
        this.RegisterThemeVariant(new CitrusThemeVariantData(CitrusDefaultPalettes.Sea.ToString(), this._uriSeaPalette));

        this.TrySetDefaultThemeVariant(new ResourceInclude(this._uriCitrusPalette) { Source = this._uriCitrusPalette } );
        this.TrySetLightThemeVariant(new ResourceInclude(this._uriCitrusPalette) { Source = this._uriCitrusPalette });
        this.TrySetDarkThemeVariant(new ResourceInclude(this._uriSeaPalette) { Source = this._uriSeaPalette });
    }

    
    public bool RegisterThemeVariant(CitrusThemeVariantData resourceData) {
        if (this._palettes.ContainsKey(resourceData.Variant)) return false;
        
        if (this.Resources.ThemeDictionaries.ContainsKey(resourceData.Variant)) {
            return false;
        }
        
        this.Resources.ThemeDictionaries.Add(resourceData.Variant, resourceData.VariantProvider);
        this._palettes.Add(resourceData.Variant, resourceData);
        return true;
    }

    /// <summary>
    /// Gets a list of all the currently registered Palettes
    /// </summary>
    /// <returns>List of <see cref="CitrusThemeVariantData"/></returns>
    public List<ThemeVariant> GetRegisteredThemeVariants() {
        List<ThemeVariant> palettes = new List<ThemeVariant>();
        foreach (var palette in this._palettes) {
            palettes.Add(palette.Key);
        }
        return palettes;
    }

    private bool TrySetThemeVariant(IThemeVariantProvider? currentValue, IThemeVariantProvider newProvider, ThemeVariant targetThemeVariant) {
        // check if same key as current?
        if (currentValue?.Key != null && currentValue.Key == newProvider.Key) return true;

        // remove existing theme variant if possible
        if (this.Resources.ThemeDictionaries.ContainsKey(targetThemeVariant)) {
            this.Resources.ThemeDictionaries.Remove(targetThemeVariant);
        }

        // add new theme variant
        this.Resources.ThemeDictionaries.Add(targetThemeVariant, newProvider);
        return true;
    }
    
    private bool TrySetDefaultThemeVariant(IThemeVariantProvider variantProvider) => this.TrySetThemeVariant(this.DesiredDefaultThemeVariant, variantProvider, ThemeVariant.Default);
    private bool TrySetLightThemeVariant(IThemeVariantProvider variantProvider) => this.TrySetThemeVariant(this.DesiredLightThemeVariant, variantProvider, ThemeVariant.Light);
    private bool TrySetDarkThemeVariant(IThemeVariantProvider variantProvider) => this.TrySetThemeVariant(this.DesiredDarkThemeVariant, variantProvider, ThemeVariant.Dark);
    
}

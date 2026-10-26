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
    private readonly Uri _uriResources = new Uri("avares://Citrus.Avalonia/CitrusResources.xaml");
    
    // default palettes
    private readonly Uri _uriCandyPalette = new Uri("avares://Citrus.Avalonia/Palette/CandyPalette.xaml");
    private readonly Uri _uriCitrusPalette = new Uri("avares://Citrus.Avalonia/Palette/CitrusPalette.xaml");
    private readonly Uri _uriDarkGrapefruitPalette = new Uri("avares://Citrus.Avalonia/Palette/DarkGrapefruitPalette.xaml");
    private readonly Uri _uriDarkLimePalette = new Uri("avares://Citrus.Avalonia/Palette/DarkLimePalette.xaml");
    private readonly Uri _uriDarkOrangePalette = new Uri("avares://Citrus.Avalonia/Palette/DarkOrangePalette.xaml");
    private readonly Uri _uriGrapefruitPalette = new Uri("avares://Citrus.Avalonia/Palette/GrapefruitPalette.xaml");
    private readonly Uri _uriLimePalette = new Uri("avares://Citrus.Avalonia/Palette/LimePalette.xaml");
    private readonly Uri _uriMagmaPalette = new Uri("avares://Citrus.Avalonia/Palette/MagmaPalette.xaml");
    private readonly Uri _uriOrangePalette = new Uri("avares://Citrus.Avalonia/Palette/OrangePalette.xaml");
    private readonly Uri _uriRustPalette = new Uri("avares://Citrus.Avalonia/Palette/RustPalette.xaml");
    private readonly Uri _uriSeaPalette = new Uri("avares://Citrus.Avalonia/Palette/SeaPalette.xaml");
    
    

    private readonly Dictionary<ThemeVariant, CitrusThemeVariantData> _palettes = new Dictionary<ThemeVariant, CitrusThemeVariantData>();
    
    /// <summary>
    /// StyledProperty that defines the desired Default theme variant to use 
    /// </summary>
    public static readonly StyledProperty<IThemeVariantProvider> DesiredDefaultThemeVariantProperty = 
        AvaloniaProperty.Register<CitrusTheme, IThemeVariantProvider>(nameof(DesiredDefaultThemeVariant),
            coerce: CitrusTheme.CoerceToDefaultPalette(ThemeVariant.Default));
    
    /// <summary>
    /// The desired Default theme variant, setting it to null restores the Citrus palette
    /// </summary>
    public IThemeVariantProvider DesiredDefaultThemeVariant {
        get => this.GetValue(CitrusTheme.DesiredDefaultThemeVariantProperty);
        set => this.SetValue(CitrusTheme.DesiredDefaultThemeVariantProperty, value);
    }
    
    /// <summary>
    /// StyledProperty that defines the desired Light theme variant to use 
    /// </summary>
    public static readonly StyledProperty<IThemeVariantProvider> DesiredLightThemeVariantProperty = 
        AvaloniaProperty.Register<CitrusTheme, IThemeVariantProvider>(nameof(DesiredLightThemeVariant),
            coerce: CitrusTheme.CoerceToDefaultPalette(ThemeVariant.Light));
    
    /// <summary>
    /// The desired Light theme variant, setting it to null restores the Citrus palette
    /// </summary>
    public IThemeVariantProvider DesiredLightThemeVariant {
        get => this.GetValue(CitrusTheme.DesiredLightThemeVariantProperty);
        set => this.SetValue(CitrusTheme.DesiredLightThemeVariantProperty, value);
    }
    
    /// <summary>
    /// StyledProperty that defines the desired Dark theme variant to use 
    /// </summary>
    public static readonly StyledProperty<IThemeVariantProvider> DesiredDarkThemeVariantProperty = 
        AvaloniaProperty.Register<CitrusTheme, IThemeVariantProvider>(nameof(DesiredDarkThemeVariant),
            coerce: CitrusTheme.CoerceToDefaultPalette(ThemeVariant.Dark));
    
    /// <summary>
    /// The desired Dark theme variant, setting it to null restores the Sea palette
    /// </summary>
    public IThemeVariantProvider DesiredDarkThemeVariant {
        get => this.GetValue(CitrusTheme.DesiredDarkThemeVariantProperty);
        set => this.SetValue(CitrusTheme.DesiredDarkThemeVariantProperty, value);
    }
    
    public CitrusTheme(IServiceProvider? sp = null) {
        
        this.Resources.MergedDictionaries.Add(new ResourceInclude(this._uriResources) { Source = this._uriResources});
        this.Add(new StyleInclude(this._uriControls) { Source = this._uriControls});
        
        
        AvaloniaXamlLoader.Load(sp, this);

        this.RegisterThemeVariant(new CitrusThemeVariantData(CitrusDefaultPalettes.Citrus.ToString(), this._uriCitrusPalette));
        this.RegisterThemeVariant(new CitrusThemeVariantData(CitrusDefaultPalettes.Candy.ToString(), this._uriCandyPalette));
        this.RegisterThemeVariant(new CitrusThemeVariantData(CitrusDefaultPalettes.Magma.ToString(), this._uriMagmaPalette));
        this.RegisterThemeVariant(new CitrusThemeVariantData(CitrusDefaultPalettes.Rust.ToString(), this._uriRustPalette));
        this.RegisterThemeVariant(new CitrusThemeVariantData(CitrusDefaultPalettes.Sea.ToString(), this._uriSeaPalette));
        this.RegisterThemeVariant(new CitrusThemeVariantData(CitrusDefaultPalettes.Lime.ToString(), this._uriLimePalette));
        this.RegisterThemeVariant(new CitrusThemeVariantData(CitrusDefaultPalettes.Grapefruit.ToString(), this._uriGrapefruitPalette));
        this.RegisterThemeVariant(new CitrusThemeVariantData(CitrusDefaultPalettes.Orange.ToString(), this._uriOrangePalette));
        this.RegisterThemeVariant(new CitrusThemeVariantData(CitrusDefaultPalettes.DarkLime.ToString(), this._uriDarkLimePalette));
        this.RegisterThemeVariant(new CitrusThemeVariantData(CitrusDefaultPalettes.DarkGrapefruit.ToString(), this._uriDarkGrapefruitPalette));
        this.RegisterThemeVariant(new CitrusThemeVariantData(CitrusDefaultPalettes.DarkOrange.ToString(), this._uriDarkOrangePalette));

        this.DesiredDefaultThemeVariant = this.CreateDefaultPalette(ThemeVariant.Default);
        this.DesiredLightThemeVariant = this.CreateDefaultPalette(ThemeVariant.Light);
        this.DesiredDarkThemeVariant = this.CreateDefaultPalette(ThemeVariant.Dark);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change) {
        base.OnPropertyChanged(change);

        if (change.Property == CitrusTheme.DesiredDefaultThemeVariantProperty) {
            this.SetThemeDictionary(ThemeVariant.Default, change.GetNewValue<IThemeVariantProvider?>());
        } else if (change.Property == CitrusTheme.DesiredLightThemeVariantProperty) {
            this.SetThemeDictionary(ThemeVariant.Light, change.GetNewValue<IThemeVariantProvider?>());
        } else if (change.Property == CitrusTheme.DesiredDarkThemeVariantProperty) {
            this.SetThemeDictionary(ThemeVariant.Dark, change.GetNewValue<IThemeVariantProvider?>());
        }
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
    /// <returns>List of the registered <see cref="ThemeVariant"/> keys</returns>
    public List<ThemeVariant> GetRegisteredThemeVariants() {
        List<ThemeVariant> palettes = new List<ThemeVariant>();
        foreach (var palette in this._palettes) {
            palettes.Add(palette.Key);
        }
        return palettes;
    }

    private void SetThemeDictionary(ThemeVariant targetThemeVariant, IThemeVariantProvider? provider) {
        // replace the existing theme variant
        this.Resources.ThemeDictionaries.Remove(targetThemeVariant);
        if (provider != null) {
            this.Resources.ThemeDictionaries.Add(targetThemeVariant, provider);
        }
    }
    
    /// <summary>
    /// Creates a new instance of the built-in palette for a theme variant: Sea for Dark, Citrus otherwise.
    /// Every theme variant needs its own instance, a provider can only have one parent dictionary.
    /// </summary>
    private IThemeVariantProvider CreateDefaultPalette(ThemeVariant targetThemeVariant) {
        Uri uri = targetThemeVariant == ThemeVariant.Dark ? this._uriSeaPalette : this._uriCitrusPalette;
        return new ResourceInclude(uri) { Source = uri };
    }

    private static Func<AvaloniaObject, IThemeVariantProvider, IThemeVariantProvider> CoerceToDefaultPalette(ThemeVariant targetThemeVariant) {
        return (theme, value) => value ?? ((CitrusTheme)theme).CreateDefaultPalette(targetThemeVariant);
    }
    
}

using System;
using System.Collections.Generic;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Themes.Simple;
using Citrus.Avalonia.Sandbox.ViewModels;
using Citrus.Avalonia.Sandbox.Views;

namespace Citrus.Avalonia.Sandbox
{
    public class App : Application
    {
        private readonly Styles _styles = new Styles();
        private CitrusTheme? _citrusTheme = null;
        private SimpleTheme? _simpleTheme = null;
        private FluentTheme? _fluentTheme = null;

        private StyleInclude? _resDataGridFluent = null;
        private StyleInclude? _resDataGridSimple = null;
        private StyleInclude? _resDataGridCitrus = null;
        
        private object? _currentThemeObj = null;
        
        private IList<ThemeVariant>? _themePalettes = null;
        private int _selectedPaletteIndex = 0;
        
        private MainWindowViewModel? _mainWindowViewModel = null;

        private readonly string _baseAppDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? string.Empty;

        public override void Initialize() {
            this.Styles.Add(this._styles);
            AvaloniaXamlLoader.Load(this);
            
            this._citrusTheme = new CitrusTheme();
            this._fluentTheme = new FluentTheme();
            this._simpleTheme = new SimpleTheme();

            Uri uriDataGridFluent = new Uri("avares://Avalonia.Controls.DataGrid/Themes/Fluent.xaml");
            Uri uriDataGridSimple = new Uri("avares://Avalonia.Controls.DataGrid/Themes/Simple.xaml");
            Uri uriDataGridCitrus = new Uri("avares://Citrus.Avalonia.DataGrid/CitrusDataGrid.xaml");
            
            this._resDataGridFluent = new StyleInclude(uriDataGridFluent) { Source = uriDataGridFluent };
            this._resDataGridSimple = new StyleInclude(uriDataGridSimple) { Source = uriDataGridSimple };
            this._resDataGridCitrus = new StyleInclude(uriDataGridCitrus) { Source = uriDataGridCitrus };
            
            this._styles.Add(this._citrusTheme);
            this._styles.Add(this._resDataGridCitrus);
            this._currentThemeObj = this._citrusTheme;
            
            // register our custom theme variant, in this case there is only one
            this._citrusTheme.RegisterThemeVariant(new CitrusThemeVariantData("CustomPalette", "avares://Citrus.Avalonia.Sandbox/Palette/CustomPalette.xaml"));

            // register additional custom theme variants from local files
            try {
                DirectoryInfo dinfo = new DirectoryInfo(Path.Combine(this._baseAppDirectory, "CustomPalettes"));
                FileInfo[] files = dinfo.GetFiles();

                foreach (FileInfo file in files) {
                    string ext = file.Extension.ToLowerInvariant();
                    if (ext != ".xaml" && ext != ".axaml")
                        continue;
                    try {
                        string name = file.Name.Substring(0, file.Name.Length - ext.Length);
                        using FileStream fileStream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read); 
                        object obj = AvaloniaRuntimeXamlLoader.Load(fileStream);
                        if (obj is not ResourceDictionary resDic)
                            continue;
                        
                        CitrusThemeVariantData themeVariantData = new CitrusThemeVariantData(name, resDic);
                        
                        this._citrusTheme.RegisterThemeVariant(themeVariantData);
                    } catch (Exception) {
                        Console.WriteLine($"Error reading ResourceDictionary from file {file.FullName}");
                    }
                }
            } catch (Exception) {
                Console.WriteLine($"Error reading custom theme palettes");
            }

            this._themePalettes = this._citrusTheme.GetRegisteredThemeVariants();
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (this.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
            {
                this._mainWindowViewModel = new MainWindowViewModel();
                this._mainWindowViewModel.Title = $"Sandbox - Citrus Theme - {this._citrusTheme?.DesiredDefaultThemeVariant}";
                
                desktopLifetime.MainWindow = new MainWindow() {
                    DataContext = this._mainWindowViewModel,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                };
            }
            
            base.OnFrameworkInitializationCompleted();
        }

        public void SetThemeVariant(ThemeVariant? variant) {
            if (variant == null)  {
                this.SetValue(Application.RequestedThemeVariantProperty, ThemeVariant.Default);
            } else {
                this.SetValue(Application.RequestedThemeVariantProperty, variant);
            }

            ThemeVariant actualVariant = this.GetValue(Application.ActualThemeVariantProperty);
            
            if (this._mainWindowViewModel == null) return;
            this._mainWindowViewModel.Title = $"Sandbox - Citrus Theme - {actualVariant.Key}";
        }

        public IList<ThemeVariant> GetThemeVariants() {
            return this._themePalettes ?? new List<ThemeVariant>();
        }

        public void SetDesiredDarkThemeSea() {
            if (this._citrusTheme == null) return;
            Uri uriSeaPalette = new Uri("avares://Citrus.Avalonia/Palette/SeaPalette.xaml");
            this._citrusTheme.DesiredDarkThemeVariant = new ResourceInclude(uriSeaPalette) { Source = uriSeaPalette };
        }
        
        public void SetDesiredDarkThemeRust() {
            if (this._citrusTheme == null) return;
            Uri uriRustPalette = new Uri("avares://Citrus.Avalonia/Palette/RustPalette.xaml");
            this._citrusTheme.DesiredDarkThemeVariant = new ResourceInclude(uriRustPalette) { Source = uriRustPalette };
        }
        
        public void LoadNextCitrusThemeVariant() {
            if (this._citrusTheme == null || 
                this._themePalettes == null || 
                ReferenceEquals(this._currentThemeObj, this._citrusTheme) == false)
                return;
            
            this._selectedPaletteIndex++;
            if (this._selectedPaletteIndex >= this._themePalettes.Count)
                this._selectedPaletteIndex = 0;
            this.SetThemeVariant(this._themePalettes[this._selectedPaletteIndex]);
        }

        public void LoadNextTheme() {
            if (this._mainWindowViewModel == null) return;
            
            ThemeVariant actualVariant = this.GetValue(Application.ActualThemeVariantProperty);
            
            if (ReferenceEquals(this._currentThemeObj, this._citrusTheme) && this._fluentTheme != null && this._resDataGridFluent != null) {
                this._styles[0] = this._fluentTheme;
                this._styles[1] = this._resDataGridFluent;
                this._currentThemeObj = this._fluentTheme;
                if (actualVariant.Key.ToString() != "Light" && actualVariant.Key.ToString() != "Dark") {
                    this.SetThemeVariant(ThemeVariant.Default);
                }
                this._mainWindowViewModel.Title = $"Sandbox - Fluent Theme - {actualVariant.Key}";
            } else if (ReferenceEquals(this._currentThemeObj, this._fluentTheme) && this._simpleTheme != null && this._resDataGridSimple != null) {
                this._styles[0] = this._simpleTheme;
                this._styles[1] = this._resDataGridSimple;
                this._currentThemeObj = this._simpleTheme;
                if (actualVariant.Key.ToString() != "Light" && actualVariant.Key.ToString() != "Dark") {
                    this.SetThemeVariant(ThemeVariant.Default);
                }
                this._mainWindowViewModel.Title = $"Sandbox - Simple Theme - {actualVariant.Key}";
            } else if (this._citrusTheme != null && this._resDataGridCitrus != null) {
                this._styles[0] = this._citrusTheme;
                this._styles[1] = this._resDataGridCitrus;
                this._currentThemeObj = this._citrusTheme;
                this._mainWindowViewModel.Title = $"Sandbox - Citrus Theme - {actualVariant.Key}";
            }

            if (this.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime) {
                Window? oldWindow = desktopLifetime.MainWindow;
                desktopLifetime.MainWindow = new MainWindow() {
                    DataContext = this._mainWindowViewModel,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                };
                desktopLifetime.MainWindow.Show();
                oldWindow?.Close();
            }
        }
    }
}

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

            // register our custom theme palettes, in this case there is only one
            this._citrusTheme.RegisterPalette(new CitrusPaletteData("CustomPalette", "avares://Citrus.Avalonia.Sandbox/Palette/CustomPalette.xaml"));

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
                        
                        CitrusPaletteData paletteData = new CitrusPaletteData(name, resDic);
                        
                        this._citrusTheme.RegisterPalette(paletteData);
                    } catch (Exception) {

                    }
                }
            } catch (Exception) {
                
            }
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (this.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
            {
                this._mainWindowViewModel = new MainWindowViewModel();
                this._mainWindowViewModel.Title = $"Sandbox - Citrus Theme - {this._citrusTheme?.ColorPalette}";
                
                desktopLifetime.MainWindow = new MainWindow() { DataContext = this._mainWindowViewModel };
            }
            
            base.OnFrameworkInitializationCompleted();
        }

        public void SetCitrusThemePalette(string paletteKey) {
            if (this._citrusTheme == null) return;
            this._citrusTheme.ColorPalette = paletteKey;
            
            if (this._mainWindowViewModel == null) return;
            this._mainWindowViewModel.Title = $"Sandbox - Citrus Theme - {this._citrusTheme.ColorPalette}";
        }

        public void LoadNextTheme() {
            if (Application.Current is not App app) return;
            if (this._mainWindowViewModel == null) return;
            
            if (ReferenceEquals(this._currentThemeObj, this._citrusTheme) && this._fluentTheme != null && this._resDataGridFluent != null) {
                this._styles[0] = this._fluentTheme;
                this._styles[1] = this._resDataGridFluent;
                this._currentThemeObj = this._fluentTheme;
                this._mainWindowViewModel.Title = "Sandbox - Fluent Theme";
            } else if (ReferenceEquals(this._currentThemeObj, this._fluentTheme) && this._simpleTheme != null && this._resDataGridSimple != null) {
                this._styles[0] = this._simpleTheme;
                this._styles[1] = this._resDataGridSimple;
                this._currentThemeObj = this._simpleTheme;
                this._mainWindowViewModel.Title = "Sandbox - Simple Theme";
            } else if (this._citrusTheme != null && this._resDataGridCitrus != null) {
                this._styles[0] = this._citrusTheme;
                this._styles[1] = this._resDataGridCitrus;
                this._currentThemeObj = this._citrusTheme;
                this._mainWindowViewModel.Title = $"Sandbox - Citrus Theme - {this._citrusTheme.ColorPalette}";
            }

            if (app.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime) {
                Window? oldWindow = desktopLifetime.MainWindow;
                desktopLifetime.MainWindow = new MainWindow() { DataContext = this._mainWindowViewModel };
                desktopLifetime.MainWindow.Show();
                oldWindow?.Close();
            }
        }

        public IList<string> GetPaletteNames() {
            IList<string> paletteNames = new List<string>();
            if (this._citrusTheme == null) 
                return paletteNames;
            var palettes = this._citrusTheme.GetRegisteredPalettes();
            foreach (var palette in palettes) {
                paletteNames.Add(palette.Key);
            }
            return paletteNames;
        }
    }
}

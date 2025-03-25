using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Themes.Simple;

namespace Citrus.Avalonia.Sandbox
{
    public enum AppPalettes {
        // default theme palettes
        Citrus, Sea, Rust, Candy, Magma, 
        // custom palette 
        Custom
    }
    
    public class App : Application
    {
        private readonly Styles _styles = new Styles();
        private CitrusTheme? _citrusTheme;
        private SimpleTheme? _simpleTheme;
        private FluentTheme? _fluentTheme;
        
        private object? _currentThemeObj = null;
        
        private MainWindowViewModel? _mainWindowViewModel = null;

        public override void Initialize() {
            this.Styles.Add(this._styles);
            AvaloniaXamlLoader.Load(this);
            
            this._citrusTheme = new CitrusTheme();
            this._fluentTheme = new FluentTheme();
            this._simpleTheme = new SimpleTheme();
            
            this._styles.Add(this._citrusTheme);
            this._currentThemeObj = this._citrusTheme;
            this._mainWindowViewModel = new MainWindowViewModel();
            this._mainWindowViewModel.Title = "Sandbox - Citrus Theme";

            // register our custom theme palettes, in this case there is only one
            this._citrusTheme.RegisterPalette("Custom",
                new Uri("avares://Citrus.Avalonia.Sandbox/Palette/CustomPalette.xaml"));
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (this.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
            {
                desktopLifetime.MainWindow = new MainWindow() { DataContext = this._mainWindowViewModel };
            }
            
            base.OnFrameworkInitializationCompleted();
        }

        public void SetCitrusThemePalette(string paletteKey) {
            if (this._citrusTheme == null) return;
            this._citrusTheme.ColorPalette = paletteKey;
        }

        public void LoadNextTheme() {
            if (Application.Current is not App app) return;
            if (this._mainWindowViewModel == null) return;
            
            if (ReferenceEquals(this._currentThemeObj, this._citrusTheme) && this._fluentTheme != null) {
                this._styles[0] = this._fluentTheme;
                this._currentThemeObj = this._fluentTheme;
                this._mainWindowViewModel.Title = "Sandbox - Fluent Theme";
            } else if (ReferenceEquals(this._currentThemeObj, this._fluentTheme) && this._simpleTheme != null) {
                this._styles[0] = this._simpleTheme;
                this._currentThemeObj = this._simpleTheme;
                this._mainWindowViewModel.Title = "Sandbox - Simple Theme";
            } else if (this._citrusTheme != null) {
                this._styles[0] = this._citrusTheme;
                this._currentThemeObj = this._citrusTheme;
                this._mainWindowViewModel.Title = "Sandbox - Citrus Theme";
            }

            if (app.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime) {
                Window? oldWindow = desktopLifetime.MainWindow;
                desktopLifetime.MainWindow = new MainWindow() { DataContext = this._mainWindowViewModel };
                desktopLifetime.MainWindow.Show();
                oldWindow?.Close();
            }
        }
    }
}

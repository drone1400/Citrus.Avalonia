using System;
using System.Reactive;
using Avalonia;
using Avalonia.Themes.Fluent;
using Avalonia.Themes.Simple;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ReactiveUI.Validation.Extensions;
using ReactiveUI.Validation.Helpers;

namespace Citrus.Avalonia.Sandbox
{
    public sealed class MainWindowViewModel : ReactiveValidationObject
    {
        public MainWindowViewModel()
        {
            // This is ReactiveUI.Validation magic, that supports
            // INotifyDataErrorInfo <TextBox /> error messages.
            this.ValidationRule(x => x.Message,
                message => !string.IsNullOrWhiteSpace(message),
                "This text is not happy to be empty!");
        }

        public string Title {
            get => this._title;
            set => this.RaiseAndSetIfChanged(ref this._title, value);
        }
        private string _title = "Replace me";

        public string Message {
            get => this._message; 
            set => this.RaiseAndSetIfChanged(ref this._message, value);
        }
        private string _message = "";

        public AppPalettes SelectedThemePalette {
            get => this._selectedThemePalette;
            set {
                this.RaiseAndSetIfChanged(ref this._selectedThemePalette, value);
                
                if (Application.Current is not App app) return;
                app.SetCitrusThemePalette(value.ToString());
            } 
        }
        private AppPalettes _selectedThemePalette = AppPalettes.Citrus;
        
        // Each time a user clicks 'Switch theme', we load next theme.
        public ReactiveCommand<Unit, Unit> ChangeThemePalette => this._changeThemePalette ??= ReactiveCommand.Create(() => {
            this.SelectedThemePalette = this.SelectedThemePalette switch {
                AppPalettes.Citrus => AppPalettes.Sea,
                AppPalettes.Sea => AppPalettes.Rust,
                AppPalettes.Rust => AppPalettes.Candy,
                AppPalettes.Candy => AppPalettes.Magma,
                AppPalettes.Magma => AppPalettes.Custom,
                _ => AppPalettes.Citrus,
            };
        });
        private ReactiveCommand<Unit, Unit>? _changeThemePalette = null;
        
        public ReactiveCommand<Unit, Unit> ChangeTheme => this._changeTheme ??= ReactiveCommand.Create(() => {
            if (Application.Current is not App app) return;
            
            app.LoadNextTheme();
        });
        private ReactiveCommand<Unit, Unit>? _changeTheme = null;
    }
}
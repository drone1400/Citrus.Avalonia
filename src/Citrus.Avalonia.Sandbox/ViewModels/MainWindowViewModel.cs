using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reactive;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Styling;
using ReactiveUI;
using ReactiveUI.Validation.Extensions;
using ReactiveUI.Validation.Helpers;

namespace Citrus.Avalonia.Sandbox.ViewModels
{
    public sealed class MainWindowViewModel : ReactiveValidationObject
    {
        public Dock TabStripPlacement {
            get => this._tabStripPlacement; 
            set => this.RaiseAndSetIfChanged(ref this._tabStripPlacement, value);
        }
        private Dock _tabStripPlacement = Dock.Top;

        public class MyComparer : IComparer {
            public int Compare(object? x, object? y) {
                if (x is string x1 && y is string y1)
                {
                    return string.CompareOrdinal(x1, y1);
                }
                return 0;
            }
        }

        public List<string> AutoCompleteList { get; } = new List<string>() {
            "Citrus",
            "Candy",
            "Magma",
            "Rust",
            "Sea"
        };
        
        public MainWindowViewModel()
        {
            // This is ReactiveUI.Validation magic, that supports
            // INotifyDataErrorInfo <TextBox /> error messages.
            this.ValidationRule(x => x.Message,
                message => !string.IsNullOrWhiteSpace(message),
                "This text is not happy to be empty!");

            this.SomeCollectionViewData = new DataGridCollectionView(this.SomeData);
            this.SomeCollectionViewData.GroupDescriptions.Add(new DataGridPathGroupDescription("Category"));
            this.SomeCollectionViewData.SortDescriptions.Add(new DataGridComparerSortDescription(new MyComparer(), ListSortDirection.Ascending));
        }

        public IEnumerable<SampleDataViewModel> SomeData { get; } = new List<SampleDataViewModel>() {
            new SampleDataViewModel("Entry 1", "Something", 1, true, 5.0),
            new SampleDataViewModel("Entry 2", "Something",  2, true, 7.5),
            new SampleDataViewModel("Entry 3", "Something",  3, true, 10.0),
            new SampleDataViewModel("Entry 4", "Other",  4, false, 15.5),
            new SampleDataViewModel("Entry 5", "Other",  5, false, 19.33),
        };

        public DataGridCollectionView SomeCollectionViewData { get; }

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
        
        // Each time a user clicks 'Switch theme', we load next theme.
        public ReactiveCommand<Unit, Unit> CommandSwitchNextRegisteredThemeVariant => this._commandSwitchNextRegisteredThemeVariant ??= ReactiveCommand.Create(() => {
            if (Application.Current is not App app) return;
            app.LoadNextCitrusThemeVariant();
        });
        private ReactiveCommand<Unit, Unit>? _commandSwitchNextRegisteredThemeVariant = null;

        public ReactiveCommand<Unit, Unit> CommandSwitchLightThemeVariant => this._commandSwitchLightThemeVariant ??= ReactiveCommand.Create(() => {
            if (Application.Current is not App app) return;
            app.SetThemeVariant(ThemeVariant.Light);
        });
        private ReactiveCommand<Unit, Unit>? _commandSwitchLightThemeVariant = null;
        
        public ReactiveCommand<Unit, Unit> CommandSwitchDarkThemeVariant => this._commandSwitchDarkThemeVariant ??= ReactiveCommand.Create(() => {
            if (Application.Current is not App app) return;
            app.SetThemeVariant(ThemeVariant.Dark);
        });
        private ReactiveCommand<Unit, Unit>? _commandSwitchDarkThemeVariant = null;
        
        public ReactiveCommand<Unit, Unit> CommandSwitchDefaultThemeVariant => this._commandSwitchDefaultThemeVariant ??= ReactiveCommand.Create(() => {
            if (Application.Current is not App app) return;
            app.SetThemeVariant(ThemeVariant.Default);
        });
        private ReactiveCommand<Unit, Unit>? _commandSwitchDefaultThemeVariant = null;
        
        public ReactiveCommand<Unit, Unit> CommandSetDefaultDarkThemeRust => this._commandSetDefaultDarkThemeRust ??= ReactiveCommand.Create(() => {
            if (Application.Current is not App app) return;
            app.SetDesiredDarkThemeRust();
        });
        private ReactiveCommand<Unit, Unit>? _commandSetDefaultDarkThemeRust = null;
        
        public ReactiveCommand<Unit, Unit> CommandSetDefaultDarkThemeSea => this._commandSetDefaultDarkThemeSea ??= ReactiveCommand.Create(() => {
            if (Application.Current is not App app) return;
            app.SetDesiredDarkThemeSea();
        });
        private ReactiveCommand<Unit, Unit>? _commandSetDefaultDarkThemeSea = null;
        
        public ReactiveCommand<Unit, Unit> ChangeTheme => this._changeTheme ??= ReactiveCommand.Create(() => {
            if (Application.Current is not App app) return;
            
            app.LoadNextTheme();
        });
        private ReactiveCommand<Unit, Unit>? _changeTheme = null;
        
        public ReactiveCommand<Unit, Unit> ChangeTabStripPlacementTop => this._changeTabStripPlacementTop ??= ReactiveCommand.Create(() => {
            this.TabStripPlacement = Dock.Top;
        });
        private ReactiveCommand<Unit, Unit>? _changeTabStripPlacementTop = null;
        
        public ReactiveCommand<Unit, Unit> ChangeTabStripPlacementBottom => this._changeTabStripPlacementBottom ??= ReactiveCommand.Create(() => {
            this.TabStripPlacement = Dock.Bottom;
        });
        private ReactiveCommand<Unit, Unit>? _changeTabStripPlacementBottom = null;
        
        public ReactiveCommand<Unit, Unit> ChangeTabStripPlacementLeft => this._changeTabStripPlacementLeft ??= ReactiveCommand.Create(() => {
            this.TabStripPlacement = Dock.Left;
        });
        private ReactiveCommand<Unit, Unit>? _changeTabStripPlacementLeft = null;
        
        public ReactiveCommand<Unit, Unit> ChangeTabStripPlacementRight => this._changeTabStripPlacementRight ??= ReactiveCommand.Create(() => {
            this.TabStripPlacement = Dock.Right;
        });
        private ReactiveCommand<Unit, Unit>? _changeTabStripPlacementRight = null;
    }
}
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reactive;
using Avalonia;
using Avalonia.Collections;
using ReactiveUI;
using ReactiveUI.Validation.Extensions;
using ReactiveUI.Validation.Helpers;

namespace Citrus.Avalonia.Sandbox.ViewModels
{
    public sealed class MainWindowViewModel : ReactiveValidationObject
    {
        public class MyComparer : IComparer {
            public int Compare(object? x, object? y) {
                if (x is string x1 && y is string y1)
                {
                    return string.CompareOrdinal(x1, y1);
                }
                return 0;
            }
        }
        
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
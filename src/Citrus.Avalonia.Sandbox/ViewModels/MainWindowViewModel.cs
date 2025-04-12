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
        
        private IList<string> _themePalettes;
        
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

            this._themePalettes = (Application.Current as App)!.GetPaletteNames();
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

        public string SelectedThemePalette {
            get => this._selectedThemePalette;
            private set {
                this.RaiseAndSetIfChanged(ref this._selectedThemePalette, value);
                
                if (Application.Current is not App app) return;
                app.SetCitrusThemePalette(value);
            } 
        }
        private string _selectedThemePalette = "Citrus";
        private int _selectedPaletteIndex = -1;
        
        // Each time a user clicks 'Switch theme', we load next theme.
        public ReactiveCommand<Unit, Unit> ChangeThemePalette => this._changeThemePalette ??= ReactiveCommand.Create(() => {
            if (this._selectedPaletteIndex < 0 ||
                this._selectedPaletteIndex >= this._themePalettes.Count) {
                for (int i = 0; i < this._themePalettes.Count; i++) {
                    if (this._themePalettes[i] == this._selectedThemePalette) {
                        this._selectedPaletteIndex = i;
                        break;
                    }
                }
                if (this._selectedPaletteIndex < 0)
                    this._selectedPaletteIndex = 0;
            }

            this._selectedPaletteIndex++;
            if (this._selectedPaletteIndex >= this._themePalettes.Count)
                this._selectedPaletteIndex = 0;
            this.SelectedThemePalette = this._themePalettes[this._selectedPaletteIndex];
        });
        private ReactiveCommand<Unit, Unit>? _changeThemePalette = null;
        
        public ReactiveCommand<Unit, Unit> ChangeTheme => this._changeTheme ??= ReactiveCommand.Create(() => {
            if (Application.Current is not App app) return;
            
            app.LoadNextTheme();
        });
        private ReactiveCommand<Unit, Unit>? _changeTheme = null;
    }
}
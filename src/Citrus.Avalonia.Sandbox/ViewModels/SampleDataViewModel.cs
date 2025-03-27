using System;
using ReactiveUI;
namespace Citrus.Avalonia.Sandbox.ViewModels {
    public class SampleDataViewModel : ReactiveObject  {
        
        
        public string Name {
            get => this._name; 
            set => this.RaiseAndSetIfChanged(ref this._name, value);
        }
        private string _name = "Placeholder";

        public string Category {
            get => this._category;
            set => this.RaiseAndSetIfChanged(ref this._category, value);
        }
        private string _category = "";
        
        public int IntValue {
            get => this._intValue; 
            set => this.RaiseAndSetIfChanged(ref this._intValue, value);
        }
        private int _intValue = 0;

        public bool BoolValue {
            get => this._boolValue; 
            set => this.RaiseAndSetIfChanged(ref this._boolValue, value);
        }
        private bool _boolValue = false;

        public double DoubleValue {
            get => this._doubleValue; 
            set => this.RaiseAndSetIfChanged(ref this._doubleValue, value);
        }
        private double _doubleValue = Double.NaN;

        public SampleDataViewModel() {
            
        }

        public SampleDataViewModel(string name, string category, int intValue, bool boolValue, double doubleValue) {
            this.Name = name;
            this.Category = category;
            this.IntValue = intValue;
            this.BoolValue = boolValue;
            this.DoubleValue = doubleValue;
        }
    }
}

using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Styling;
namespace Citrus.Avalonia {
    public class CitrusPaletteData {
        public string Key { get; }
        public IResourceProvider ResourceInclude { get; }

        public CitrusPaletteData(string key, Uri uri) {
            this.Key = key;
            this.ResourceInclude = new ResourceInclude(uri) { Source = uri, };
        }
        
        public CitrusPaletteData(string key, string uriString) {
            Uri uri = new Uri(uriString);
            this.Key = key;
            this.ResourceInclude = new ResourceInclude(uri) { Source = uri, };
        }

        public CitrusPaletteData(string key, IResourceProvider resourceInclude) {
            this.Key = key;
            this.ResourceInclude = resourceInclude;
        }
    }
}

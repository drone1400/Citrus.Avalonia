using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
namespace Citrus.Avalonia {
    public class CitrusThemeVariantData {
        public ThemeVariant Variant { get; }
        public IThemeVariantProvider VariantProvider { get; }

        public CitrusThemeVariantData(string key, Uri uri) {
            this.Variant = new ThemeVariant(key, null);
            this.VariantProvider = new ResourceInclude(uri) { Source = uri, };
        }
        
        public CitrusThemeVariantData(string key, string uriString) {
            Uri uri = new Uri(uriString);
            this.Variant = new ThemeVariant(key, null);
            this.VariantProvider = new ResourceInclude(uri) { Source = uri, };
        }

        public CitrusThemeVariantData(string key, IThemeVariantProvider provider) {
            this.Variant = provider.Key ?? new ThemeVariant(key, null);
            this.VariantProvider = provider;
        }
        
        public CitrusThemeVariantData(ThemeVariant themeVariant, IThemeVariantProvider provider) {
            this.Variant = themeVariant;
            this.VariantProvider = provider;
        }
    }
}

using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Citrus.Avalonia.Sandbox.Views {
    public partial class MoreControls : UserControl {
        private readonly ObservableCollection<string> _refreshItems = new ObservableCollection<string> { "Item 1", "Item 2", "Item 3" };
        private int _refreshCount = 0;

        public MoreControls() {
            InitializeComponent();
            this.refreshList.ItemsSource = this._refreshItems;
        }

        private void OnCarouselPrevious(object? sender, RoutedEventArgs e) {
            this.carousel.Previous();
        }

        private void OnCarouselNext(object? sender, RoutedEventArgs e) {
            this.carousel.Next();
        }

        private void OnRefresh(object? sender, RoutedEventArgs e) {
            this.refreshContainer.RequestRefresh();
        }

        private async void OnRefreshRequested(object? sender, RefreshRequestedEventArgs e) {
            // keep the refresh visualizer spinning for a moment, like a real refresh would
            RefreshCompletionDeferral deferral = e.GetDeferral();
            await Task.Delay(1000);
            this._refreshCount++;
            this._refreshItems.Insert(0, $"Refreshed item {this._refreshCount}");
            deferral.Complete();
        }
    }
}

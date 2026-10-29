using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Interactivity;

namespace Citrus.Avalonia.Sandbox.Views {
    public partial class MoreControls : UserControl {
        private readonly ObservableCollection<string> _refreshItems = new ObservableCollection<string> { "Item 1", "Item 2", "Item 3" };
        private int _refreshCount = 0;
        private WindowNotificationManager? _notificationManager = null;

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

        private void OnShowNotification(object? sender, RoutedEventArgs e) {
            if (sender is not Button { Tag: string tag }) return;
            NotificationType type = Enum.Parse<NotificationType>(tag);
            // created on first use, the window's adorner layer only exists once the window is shown
            this._notificationManager ??= new WindowNotificationManager(TopLevel.GetTopLevel(this)) {
                Position = NotificationPosition.BottomRight,
                MaxItems = 3,
            };
            this._notificationManager.Show(new Notification(type.ToString(), $"This is a notification of type {type}", type));
        }
    }
}

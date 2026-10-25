using Avalonia;
using Avalonia.Markup.Xaml;
using ReactiveUI.Avalonia;
using Citrus.Avalonia.Sandbox.ViewModels;
using ReactiveUI;
namespace Citrus.Avalonia.Sandbox.Views
{
    public class MainWindow : ReactiveWindow<MainWindowViewModel>
    {
        public MainWindow()
        {
            AvaloniaXamlLoader.Load(this);
            this.WhenActivated(disposables => { });
            
            #if DEBUG
                this.AttachDevTools();
            #endif
        }
    }
}

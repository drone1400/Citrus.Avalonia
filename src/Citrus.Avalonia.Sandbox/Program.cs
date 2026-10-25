using Avalonia;
using Avalonia.Dialogs;
using ReactiveUI.Avalonia;

namespace Citrus.Avalonia.Sandbox
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        public static AppBuilder BuildAvaloniaApp()
        {
            return AppBuilder
                .Configure<App>()
                .UsePlatformDetect()
                .With(new X11PlatformOptions {EnableMultiTouch = true, UseDBusMenu = true})
                .With(new Win32PlatformOptions {RenderingMode = new Win32RenderingMode[] { Win32RenderingMode.AngleEgl }})
                .UseSkia()
                .UseReactiveUI(_ => { })
                .UseManagedSystemDialogs();
        }
    }
}

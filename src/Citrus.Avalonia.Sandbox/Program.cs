using System.Runtime.Versioning;
using Avalonia;
using Avalonia.Dialogs;
using ReactiveUI.Avalonia;

namespace Citrus.Avalonia.Sandbox
{
    // UseManagedSystemDialogs is only supported on the desktop platforms
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
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

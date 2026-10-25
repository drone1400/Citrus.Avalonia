using System;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Headless;
using Avalonia.Logging;
using Avalonia.Threading;
using Citrus.Avalonia.Sandbox;
using ReactiveUI.Avalonia;

namespace Citrus.Avalonia.Audit;

/// <summary>
/// Headless audit of the Citrus themes, rendering the Sandbox application.
/// <para>Usage: <c>Citrus.Avalonia.Audit [output directory] [--src &lt;src folder&gt;]</c></para>
/// <para>Usage: <c>Citrus.Avalonia.Audit --compare &lt;screenshots A&gt; &lt;screenshots B&gt;</c></para>
/// The exit code is the number of failed checks (or of differing screenshots), so 0 means everything passed.
/// </summary>
public static class Program {
    private const int MaxExitCode = 100;

    [STAThread]
    public static int Main(string[] args) {
        if (args.Length > 0 && args[0] == "--compare") {
            if (args.Length != 3) {
                Console.WriteLine("Usage: Citrus.Avalonia.Audit --compare <screenshots A> <screenshots B>");
                return 1;
            }
            return Math.Min(CompareScreenshots(args[1], args[2]), MaxExitCode);
        }

        string outputDirectory = Path.Combine("artifacts", "audit");
        string? sourceDirectory = null;
        for (int i = 0; i < args.Length; i++) {
            if (args[i] == "--src" && i + 1 < args.Length) {
                sourceDirectory = args[++i];
            } else {
                outputDirectory = args[i];
            }
        }
        sourceDirectory ??= FindSourceDirectory();
        Directory.CreateDirectory(outputDirectory);

        AuditLogSink sink = new AuditLogSink();
        Logger.Sink = sink;
        AppBuilder.Configure<App>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
            .UseReactiveUI(_ => { })
            .SetupWithoutStarting();

        AuditReport report = new AuditReport();
        Dispatcher.UIThread.UnhandledException += (_, e) => {
            report.Fail($"Unhandled exception during '{sink.Phase}': {e.Exception}");
            e.Handled = true;
        };

        try {
            new SandboxAudit(report, sink, outputDirectory, sourceDirectory).Run();
        } catch (Exception ex) {
            report.Fail($"Audit aborted during '{sink.Phase}': {ex}");
        }

        report.Info($"{report.FailureCount} failed check(s), screenshots in {Path.GetFullPath(outputDirectory)}");
        report.Save(Path.Combine(outputDirectory, "report.txt"));
        return Math.Min(report.FailureCount, MaxExitCode);
    }

    /// <summary>
    /// Finds the src folder by walking up from the executable to the folder that contains the solution
    /// </summary>
    private static string? FindSourceDirectory() {
        DirectoryInfo? directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Citrus.Avalonia.sln"))) {
            directory = directory.Parent;
        }
        return directory?.FullName;
    }

    /// <summary>
    /// Byte-compares the screenshots of two audit runs, returns the number of files that differ or exist only on one side
    /// </summary>
    private static int CompareScreenshots(string directoryA, string directoryB) {
        string[] names = Directory.EnumerateFiles(directoryA, "*.png")
            .Concat(Directory.EnumerateFiles(directoryB, "*.png"))
            .Select(Path.GetFileName)
            .OfType<string>()
            .Distinct()
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();
        int differences = 0;
        foreach (string name in names) {
            string fileA = Path.Combine(directoryA, name);
            string fileB = Path.Combine(directoryB, name);
            string? problem = null;
            if (!File.Exists(fileA)) {
                problem = "only in B";
            } else if (!File.Exists(fileB)) {
                problem = "only in A";
            } else if (!File.ReadAllBytes(fileA).AsSpan().SequenceEqual(File.ReadAllBytes(fileB))) {
                problem = "different";
            }
            if (problem == null) continue;
            differences++;
            Console.WriteLine($"{problem}: {name}");
        }
        Console.WriteLine($"{names.Length - differences} identical, {differences} different");
        return differences;
    }
}

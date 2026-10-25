using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Logging;

namespace Citrus.Avalonia.Audit;

/// <summary>
/// Collects the audit results, every failure counts towards the process exit code
/// </summary>
internal sealed class AuditReport {
    private readonly List<string> _lines = new List<string>();

    public int FailureCount { get; private set; }

    public void Info(string message) => this.Write(message);

    public void Pass(string message) => this.Write("PASS " + message);

    public void Fail(string message) {
        this.FailureCount++;
        this.Write("FAIL " + message);
    }

    public void Check(bool condition, string message) {
        if (condition) {
            this.Pass(message);
        } else {
            this.Fail(message);
        }
    }

    public void Save(string path) => File.WriteAllLines(path, this._lines);

    private void Write(string line) {
        this._lines.Add(line);
        Console.WriteLine(line);
    }
}

/// <summary>
/// Avalonia log sink that records every Warning or Error, tagged with the audit phase that produced it
/// </summary>
internal sealed class AuditLogSink : ILogSink {
    private readonly List<string> _events = new List<string>();

    public string Phase { get; set; } = "startup";

    public IReadOnlyList<string> Events => this._events;

    public bool IsEnabled(LogEventLevel level, string area) => level >= LogEventLevel.Warning;

    public void Log(LogEventLevel level, string area, object? source, string messageTemplate) {
        this.Add(level, area, source, messageTemplate, Array.Empty<object?>());
    }

    public void Log(LogEventLevel level, string area, object? source, string messageTemplate, params object?[] propertyValues) {
        this.Add(level, area, source, messageTemplate, propertyValues);
    }

    private void Add(LogEventLevel level, string area, object? source, string messageTemplate, object?[] propertyValues) {
        int index = 0;
        string message = Regex.Replace(messageTemplate, @"\{[^}]+\}", match =>
            index < propertyValues.Length
                ? Convert.ToString(propertyValues[index++], CultureInfo.InvariantCulture) ?? "null"
                : match.Value);
        string sourceName = source is StyledElement element
            ? $"{element.GetType().Name}#{element.Name}"
            : source?.GetType().Name ?? "-";
        this._events.Add($"[{this.Phase}] {level} {area} {sourceName}: {message}");
    }
}

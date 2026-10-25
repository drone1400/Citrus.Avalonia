using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Chrome;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Dialogs;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Citrus.Avalonia.Sandbox;
using Citrus.Avalonia.Sandbox.ViewModels;
using Citrus.Avalonia.Sandbox.Views;

namespace Citrus.Avalonia.Audit;

/// <summary>
/// Renders the Sandbox headlessly and checks the Citrus themes for missing templates, missing resources,
/// broken popups and other regressions. Screenshots of everything it renders are saved for visual review.
/// </summary>
internal sealed class SandboxAudit {
    private static readonly Regex ResourceUseRegex =
        new Regex(@"\{(?:DynamicResource|StaticResource)\s+([A-Za-z0-9_.]+)\s*\}|ResourceKey=""([A-Za-z0-9_.]+)""");
    private static readonly Regex ResourceKeyRegex = new Regex(@"x:Key=""([^""{][^""]*)""");
    private static readonly Regex XmlCommentRegex = new Regex(@"<!--.*?-->", RegexOptions.Singleline);

    private static readonly Color RustBackground = Color.Parse("#000000");
    private static readonly Color SeaBackground = Color.Parse("#001446");

    private readonly AuditReport _report;
    private readonly AuditLogSink _sink;
    private readonly string _outputDirectory;
    private readonly string? _sourceDirectory;
    private readonly List<Popup> _openedPopups = new List<Popup>();

    public SandboxAudit(AuditReport report, AuditLogSink sink, string outputDirectory, string? sourceDirectory) {
        this._report = report;
        this._sink = sink;
        this._outputDirectory = outputDirectory;
        this._sourceDirectory = sourceDirectory;
    }

    public void Run() {
        App app = (App)Application.Current!;
        Popup.IsOpenProperty.Changed.AddClassHandler<Popup>((popup, _) => {
            if (popup.IsOpen) this._openedPopups.Add(popup);
        });

        this.CheckCitrusThemeApi();

        this._sink.Phase = "main-window";
        MainWindow window = new MainWindow { DataContext = new MainWindowViewModel() };
        window.Show();
        Pump();

        List<ThemeVariant> variants = app.GetThemeVariants()
            .Concat(new[] { ThemeVariant.Light, ThemeVariant.Dark, ThemeVariant.Default })
            .ToList();
        this._report.Info("Theme variants: " + string.Join(", ", variants.Select(v => v.Key)));

        this.CheckControlThemesExist(window);
        this.CheckResourceKeys(window, variants);
        this.CheckTabs(window);
        this.CheckThemeVariants(app, window, variants);
        this.CheckRuntimePaletteSwitch(app, window);
        this.CheckEditableComboBox();
        this.CheckAccessKeys();
        this.CheckLogEvents();

        window.Close();
    }

    // ---------------------------------------------------------------- CitrusTheme API

    private void CheckCitrusThemeApi() {
        this._sink.Phase = "citrus-theme-api";

        CitrusTheme fromXaml = new CitrusThemeXamlTest().OfType<CitrusTheme>().First();
        this._report.Check(GetThemeBackground(fromXaml, ThemeVariant.Dark) == RustBackground,
            "CitrusTheme: DesiredDarkThemeVariant set from XAML (README example) is applied");

        CitrusTheme theme = new CitrusTheme();
        this._report.Check(
            theme.DesiredDefaultThemeVariant != null &&
            theme.DesiredLightThemeVariant != null &&
            theme.DesiredDarkThemeVariant != null,
            "CitrusTheme: Desired*ThemeVariant getters return the default palettes after construction");

        Uri rustUri = new Uri("avares://Citrus.Avalonia/Palette/RustPalette.xaml");
        theme.SetValue(CitrusTheme.DesiredDarkThemeVariantProperty, new ResourceInclude(rustUri) { Source = rustUri });
        this._report.Check(GetThemeBackground(theme, ThemeVariant.Dark) == RustBackground,
            "CitrusTheme: DesiredDarkThemeVariant set through SetValue is applied");

        try {
            theme.DesiredDarkThemeVariant = null!;
            this._report.Check(GetThemeBackground(theme, ThemeVariant.Dark) == SeaBackground,
                "CitrusTheme: setting DesiredDarkThemeVariant to null restores the default Sea palette");
        } catch (Exception ex) {
            this._report.Fail($"CitrusTheme: setting DesiredDarkThemeVariant to null throws {ex.GetType().Name}");
        }
    }

    private static Color? GetThemeBackground(CitrusTheme theme, ThemeVariant variant) {
        if (theme.Resources.ThemeDictionaries.TryGetValue(variant, out IThemeVariantProvider? provider) &&
            provider.TryGetResource("ThemeBackgroundBrush", null, out object? value) &&
            value is ISolidColorBrush brush) {
            return brush.Color;
        }
        return null;
    }

    // ---------------------------------------------------------------- control themes and resources

    private void CheckControlThemesExist(Window window) {
        Type[] required = {
            typeof(PopupRoot),
            typeof(OverlayPopupHost),
            typeof(TitleBar),
            typeof(CaptionButtons),
            typeof(ManagedFileChooser),
        };
        foreach (Type type in required) {
            bool found = window.TryFindResource(type, out object? theme) && theme is ControlTheme;
            this._report.Check(found, $"ControlTheme exists for {type.Name}");
        }
    }

    /// <summary>
    /// Every resource key the Citrus XAML references, but does not define itself, must resolve in every theme variant
    /// </summary>
    private void CheckResourceKeys(Window window, IReadOnlyList<ThemeVariant> variants) {
        if (this._sourceDirectory == null) {
            this._report.Fail("Resource keys: source directory not found, pass --src <path to src folder>");
            return;
        }

        HashSet<string> definedKeys = new HashSet<string>();
        Dictionary<string, List<string>> usages = new Dictionary<string, List<string>>();
        foreach (string project in new[] { "Citrus.Avalonia", "Citrus.Avalonia.DataGrid" }) {
            string projectDirectory = Path.Combine(this._sourceDirectory, project);
            foreach (string file in Directory.EnumerateFiles(projectDirectory, "*.xaml", SearchOption.AllDirectories)) {
                string relative = Path.GetRelativePath(this._sourceDirectory, file).Replace('\\', '/');
                if (relative.Contains("/obj/") || relative.Contains("/bin/")) continue;

                // blank out XML comments but keep their line breaks, so line numbers stay correct
                string text = XmlCommentRegex.Replace(File.ReadAllText(file), m => Regex.Replace(m.Value, @"[^\n]", " "));
                string[] lines = text.Split('\n');
                bool isPalette = relative.Contains("/Palette/");
                for (int i = 0; i < lines.Length; i++) {
                    if (!isPalette) {
                        foreach (Match match in ResourceKeyRegex.Matches(lines[i])) {
                            definedKeys.Add(match.Groups[1].Value);
                        }
                    }
                    foreach (Match match in ResourceUseRegex.Matches(lines[i])) {
                        string key = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
                        if (!usages.TryGetValue(key, out List<string>? places)) {
                            places = new List<string>();
                            usages.Add(key, places);
                        }
                        places.Add($"{relative}:{i + 1}");
                    }
                }
            }
        }

        int checkedKeys = 0;
        int missingKeys = 0;
        foreach (KeyValuePair<string, List<string>> usage in usages.OrderBy(u => u.Key, StringComparer.Ordinal)) {
            if (definedKeys.Contains(usage.Key)) continue;
            checkedKeys++;
            List<string> missingIn = variants
                .Where(variant => !window.TryFindResource(usage.Key, variant, out _))
                .Select(variant => variant.Key.ToString() ?? "?")
                .ToList();
            if (missingIn.Count == 0) continue;
            missingKeys++;
            string where = missingIn.Count == variants.Count ? "any theme variant" : string.Join(", ", missingIn);
            this._report.Fail($"Resource '{usage.Key}' does not resolve in {where} (used at {string.Join(", ", usage.Value.Take(3))})");
        }
        if (missingKeys == 0) {
            this._report.Pass($"Resource keys: all {checkedKeys} externally provided keys resolve in every theme variant");
        }
    }

    // ---------------------------------------------------------------- Sandbox tabs

    private void CheckTabs(Window window) {
        TabControl tabs = window.GetVisualDescendants().OfType<TabControl>().First();
        List<TabItem> tabItems = tabs.Items.OfType<TabItem>().ToList();
        Dictionary<string, List<string>> untemplated = new Dictionary<string, List<string>>();

        for (int i = 0; i < tabItems.Count; i++) {
            string header = tabItems[i].Header?.ToString() ?? $"Tab {i}";
            this._sink.Phase = "tab: " + header;
            tabs.SelectedIndex = i;
            Pump();

            foreach (TemplatedControl control in window.GetVisualDescendants().OfType<TemplatedControl>()) {
                if (control.Template != null) continue;
                string typeName = control.GetType().Name;
                if (!untemplated.TryGetValue(typeName, out List<string>? tabNames)) {
                    tabNames = new List<string>();
                    untemplated.Add(typeName, tabNames);
                }
                if (!tabNames.Contains(header)) tabNames.Add(header);
            }

            this.SaveScreenshot(window, $"tab{i:00}_{FileNamePart(header)}");
            this.CheckTabPopups(window, header);
            if (header == "Data Grid") this.CheckDataGridEditing(window);
        }
        tabs.SelectedIndex = 0;
        Pump();

        foreach (KeyValuePair<string, List<string>> entry in untemplated) {
            this._report.Fail($"{entry.Key} has no template (tabs: {string.Join(", ", entry.Value)})");
        }
        if (untemplated.Count == 0) {
            this._report.Pass($"Every templated control in all {tabItems.Count} tabs has a template");
        }
    }

    private void CheckTabPopups(Window window, string tab) {
        List<Visual> visuals = window.GetVisualDescendants().ToList();
        string name = FileNamePart(tab);

        if (tab == "Common Controls" && visuals.OfType<Menu>().FirstOrDefault() is { } menu) {
            this.CheckPopup(window, tab, "Menu", () => {
                MenuItem root = (MenuItem)menu.Items[0]!;
                root.IsSubMenuOpen = true;
                Pump();
                ((MenuItem)root.Items[0]!).IsSubMenuOpen = true;
            });
        }
        if (visuals.OfType<ComboBox>().FirstOrDefault() is { } comboBox) {
            this.CheckPopup(window, tab, "ComboBox", () => comboBox.IsDropDownOpen = true);
        }
        if (visuals.OfType<AutoCompleteBox>().FirstOrDefault() is { } autoCompleteBox) {
            this.CheckPopup(window, tab, "AutoCompleteBox", () => {
                autoCompleteBox.Text = "C";
                Pump();
                autoCompleteBox.IsDropDownOpen = true;
            });
        }
        if (visuals.OfType<CalendarDatePicker>().FirstOrDefault() is { } calendarDatePicker) {
            this.CheckPopup(window, tab, "CalendarDatePicker", () => calendarDatePicker.IsDropDownOpen = true);
        }
        if (visuals.OfType<Control>().FirstOrDefault(c => ToolTip.GetTip(c) != null) is { } toolTipOwner) {
            this.CheckPopup(window, tab, "ToolTip", () => ToolTip.SetIsOpen(toolTipOwner, true));
        }
        if (visuals.OfType<Control>().FirstOrDefault(c => c.ContextMenu != null) is { } contextMenuOwner) {
            this.CheckPopup(window, tab, "ContextMenu", () => contextMenuOwner.ContextMenu!.Open(contextMenuOwner));
        }
        if (visuals.OfType<TextBox>().FirstOrDefault(t => t.ContextFlyout is MenuFlyout) is { } textBox) {
            this.CheckPopup(window, tab, "TextBoxContextFlyout", () => {
                MenuFlyout flyout = (MenuFlyout)textBox.ContextFlyout!;
                flyout.ShowAt(textBox);
                Pump();
                List<string> headers = flyout.Items.OfType<MenuItem>().Select(m => m.Header?.ToString() ?? "").ToList();
                this._report.Check(headers.All(h => h.Length > 0),
                    $"[{tab}] TextBox context menu items have text: {string.Join(" | ", headers.Select(h => $"'{h}'"))}");
            });
        }

        List<DatePicker> datePickers = visuals.OfType<DatePicker>().ToList();
        List<TimePicker> timePickers = visuals.OfType<TimePicker>().ToList();
        if (datePickers.Count > 0 || timePickers.Count > 0) {
            this.CheckPickerTexts(tab, datePickers.Cast<TemplatedControl>().Concat(timePickers));
        }
        // open a picker that already has a value, so the presenter does not depend on the current date/time
        if (datePickers.FirstOrDefault(p => p.SelectedDate != null && p.IsEffectivelyEnabled) is { } datePicker) {
            this.CheckPopup(window, tab, "DatePicker", () => {
                ClickFlyoutButton(datePicker);
                this.CheckPresenterButtons(tab, "DatePicker", this._openedPopups.Select(p => p.Child).OfType<DatePickerPresenter>().FirstOrDefault());
            });
        }
        if (timePickers.FirstOrDefault(p => p.SelectedTime != null && p.IsEffectivelyEnabled) is { } timePicker) {
            this.CheckPopup(window, tab, "TimePicker", () => {
                ClickFlyoutButton(timePicker);
                this.CheckPresenterButtons(tab, "TimePicker", this._openedPopups.Select(p => p.Child).OfType<TimePickerPresenter>().FirstOrDefault());
            });
        }
    }

    /// <summary>
    /// Runs <paramref name="open"/>, then checks that every popup it opened is hosted, templated and has content
    /// </summary>
    private void CheckPopup(Window window, string tab, string what, Action open) {
        this._sink.Phase = $"popup: {tab} / {what}";
        this._openedPopups.Clear();
        try {
            open();
            Pump();
            if (this._openedPopups.Count == 0) {
                this._report.Fail($"[{tab}] {what}: no popup opened");
            }
            foreach (Popup popup in this._openedPopups.Distinct()) {
                bool hosted = popup.Host is TemplatedControl host && host.Template != null;
                bool hasContent = popup.Child is Control child && child.Bounds.Width > 0 && child.Bounds.Height > 0;
                this._report.Check(hosted && hasContent,
                    $"[{tab}] {what} popup is hosted and rendered (host {popup.Host?.GetType().Name ?? "none"}, content {popup.Child?.GetType().Name ?? "none"})");
            }
            this.SaveScreenshot(window, $"popup_{FileNamePart(tab)}_{what}");
        } catch (Exception ex) {
            this._report.Fail($"[{tab}] {what}: {ex.GetType().Name}: {ex.Message}");
        } finally {
            foreach (Popup popup in this._openedPopups) {
                popup.IsOpen = false;
            }
            this._openedPopups.Clear();
            Pump();
        }
    }

    private void CheckPickerTexts(string tab, IEnumerable<TemplatedControl> pickers) {
        List<string> empty = new List<string>();
        int count = 0;
        foreach (TemplatedControl picker in pickers) {
            foreach (TextBlock textBlock in picker.GetVisualDescendants().OfType<TextBlock>()) {
                if (textBlock.Name == null || !textBlock.Name.StartsWith("PART_") || !textBlock.IsEffectivelyVisible) continue;
                count++;
                if (string.IsNullOrEmpty(textBlock.Text)) empty.Add($"{picker.GetType().Name}.{textBlock.Name}");
            }
        }
        this._report.Check(empty.Count == 0,
            $"[{tab}] Date/TimePicker text blocks show text or placeholders ({count} checked" +
            (empty.Count > 0 ? $", empty: {string.Join(", ", empty.Distinct())})" : ")"));
    }

    private static void ClickFlyoutButton(TemplatedControl picker) {
        Button button = picker.GetVisualDescendants().OfType<Button>().First(b => b.Name == "PART_FlyoutButton");
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Pump();
    }

    /// <summary>
    /// The up/down buttons of the picker presenters must be hit-testable over their whole area, not only over the arrow glyph
    /// </summary>
    private void CheckPresenterButtons(string tab, string what, Control? presenter) {
        if (presenter == null) {
            this._report.Fail($"[{tab}] {what}: presenter not found");
            return;
        }
        List<string> notHit = new List<string>();
        List<RepeatButton> buttons = presenter.GetVisualDescendants().OfType<RepeatButton>().ToList();
        foreach (RepeatButton button in buttons) {
            bool wasVisible = button.IsVisible;
            button.IsVisible = true;
            Pump();
            Point point = button.TranslatePoint(new Point(20, button.Bounds.Height / 2), presenter) ?? default;
            IInputElement? hit = presenter.InputHitTest(point);
            bool isButton = hit is Visual visual && (visual == button || button.IsVisualAncestorOf(visual));
            if (!isButton) notHit.Add(button.Name ?? "?");
            button.IsVisible = wasVisible;
        }
        this._report.Check(buttons.Count > 0 && notHit.Count == 0,
            $"[{tab}] {what} presenter up/down buttons are clickable ({buttons.Count} checked" +
            (notHit.Count > 0 ? $", not hit-testable: {string.Join(", ", notHit)})" : ")"));
    }

    private void CheckDataGridEditing(Window window) {
        this._sink.Phase = "datagrid-editing";
        DataGrid? grid = window.GetVisualDescendants().OfType<DataGrid>().FirstOrDefault(g => g.IsEffectivelyEnabled);
        if (grid == null) {
            this._report.Fail("DataGrid editing: no enabled DataGrid found");
            return;
        }
        grid.SelectedIndex = 1;
        grid.CurrentColumn = grid.Columns[0];
        Pump();
        DataGridRow? row = grid.GetVisualDescendants().OfType<DataGridRow>().FirstOrDefault(r => r.IsSelected);
        double heightBefore = row?.Bounds.Height ?? 0;

        bool editing = grid.BeginEdit();
        Pump();
        double heightEditing = row?.Bounds.Height ?? 0;
        this.SaveScreenshot(window, "datagrid_editing");
        grid.CancelEdit();
        Pump();

        this._report.Check(editing && row != null && Math.Abs(heightEditing - heightBefore) <= 2,
            $"DataGrid: row height stays the same while editing a text cell ({heightBefore} -> {heightEditing})");
    }

    // ---------------------------------------------------------------- theme variants

    private void CheckThemeVariants(App app, Window window, IReadOnlyList<ThemeVariant> variants) {
        TabControl tabs = window.GetVisualDescendants().OfType<TabControl>().First();
        int tabCount = tabs.ItemCount;
        foreach (ThemeVariant variant in variants) {
            app.SetThemeVariant(variant);
            for (int i = 0; i < tabCount; i++) {
                this._sink.Phase = $"variant: {variant.Key} / tab {i}";
                tabs.SelectedIndex = i;
                Pump();
            }
            tabs.SelectedIndex = 0;
            Pump();
            this.SaveScreenshot(window, $"variant_{FileNamePart(variant.Key.ToString() ?? "")}");
            this._report.Check(window.Background is ISolidColorBrush,
                $"Theme variant {variant.Key}: window background resolves ({(window.Background as ISolidColorBrush)?.Color})");
        }
        app.SetThemeVariant(ThemeVariant.Default);
        Pump();
    }

    private void CheckRuntimePaletteSwitch(App app, Window window) {
        this._sink.Phase = "runtime-palette-switch";
        app.SetThemeVariant(ThemeVariant.Dark);
        Pump();
        Color? before = (window.Background as ISolidColorBrush)?.Color;
        app.SetDesiredDarkThemeRust();
        Pump();
        Color? after = (window.Background as ISolidColorBrush)?.Color;
        this._report.Check(before == SeaBackground && after == RustBackground,
            $"Runtime switch of DesiredDarkThemeVariant (Sea -> Rust) updates the window ({before} -> {after})");
        app.SetDesiredDarkThemeSea();
        app.SetThemeVariant(ThemeVariant.Default);
        Pump();
    }

    // ---------------------------------------------------------------- standalone controls

    private void CheckEditableComboBox() {
        this._sink.Phase = "editable-combobox";
        ComboBox comboBox = new ComboBox {
            IsEditable = true,
            ItemsSource = new[] { "Citrus", "Candy", "Magma", "Rust", "Sea" },
            SelectedIndex = 0,
            Width = 200,
        };
        Window host = new Window { Width = 300, Height = 120, Content = comboBox };
        host.Show();
        Pump();
        TextBox? editor = comboBox.GetVisualDescendants().OfType<TextBox>().FirstOrDefault(t => t.Name == "PART_EditableTextBox");
        this._report.Check(editor != null && editor.IsEffectivelyVisible,
            "Editable ComboBox (IsEditable) shows PART_EditableTextBox");
        this.SaveScreenshot(host, "editable_combobox");
        host.Close();
    }

    /// <summary>
    /// Text content with an underscore must be turned into an <see cref="AccessText"/> (RecognizesAccessKey)
    /// </summary>
    private void CheckAccessKeys() {
        this._sink.Phase = "access-keys";
        Control[] controls = {
            new Button { Content = "_Button" },
            new ToggleButton { Content = "_ToggleButton" },
            new RepeatButton { Content = "_RepeatButton" },
            new CheckBox { Content = "_CheckBox" },
            new RadioButton { Content = "_RadioButton" },
            new Label { Content = "_Label" },
            new TabStrip { ItemsSource = new[] { new TabStripItem { Content = "_TabStripItem" } } },
            new TabControl { ItemsSource = new[] { new TabItem { Header = "_TabItem" } } },
        };
        StackPanel panel = new StackPanel();
        panel.Children.AddRange(controls);
        Window host = new Window { Width = 300, Height = 500, Content = panel };
        host.Show();
        Pump();
        List<string> missing = controls
            .Where(c => !c.GetVisualDescendants().OfType<AccessText>().Any())
            .Select(c => c.GetType().Name)
            .ToList();
        this._report.Check(missing.Count == 0,
            $"Access keys are recognized in {controls.Length} control types" +
            (missing.Count > 0 ? $" (missing in: {string.Join(", ", missing)})" : ""));
        this.SaveScreenshot(host, "access_keys");
        host.Close();
    }

    private void CheckLogEvents() {
        foreach (IGrouping<string, string> group in this._sink.Events
                     .GroupBy(e => Regex.Replace(e, @"^\[[^\]]*\] ", ""))
                     .OrderBy(g => g.Key, StringComparer.Ordinal)) {
            this._report.Fail($"Log x{group.Count()}: {group.Key} (first seen in {Regex.Match(group.First(), @"^\[([^\]]*)\]").Groups[1].Value})");
        }
        if (this._sink.Events.Count == 0) {
            this._report.Pass("No warnings or errors were logged by Avalonia");
        }
    }

    // ---------------------------------------------------------------- helpers

    private static void Pump() {
        for (int i = 0; i < 3; i++) {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        }
        Dispatcher.UIThread.RunJobs();
    }

    private void SaveScreenshot(TopLevel topLevel, string name) {
        Pump();
        topLevel.CaptureRenderedFrame()?.Save(Path.Combine(this._outputDirectory, name + ".png"));
    }

    private static string FileNamePart(string text) => Regex.Replace(text, @"\W+", "_").Trim('_');
}

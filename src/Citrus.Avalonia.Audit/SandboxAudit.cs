using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Chrome;
using Avalonia.Controls.Embedding;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Dialogs;
using Avalonia.Dialogs.Internal;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Citrus.Avalonia.Sandbox;
using Citrus.Avalonia.Sandbox.ViewModels;
using Citrus.Avalonia.Sandbox.Views;
using PathShape = Avalonia.Controls.Shapes.Path;
using Shape = Avalonia.Controls.Shapes.Shape;

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
        this.CheckThemeVariantScope(app);
        this.CheckEditableComboBox();
        this.CheckManagedFileChooser();
        this.CheckAccessKeys();
        this.CheckNotifications();
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
            typeof(EmbeddableControlRoot),
            typeof(HeaderedContentControl),
            typeof(PathIcon),
            typeof(SelectableTextBlock),
            typeof(TextSelectionHandle),
            typeof(ThemeVariantScope),
            typeof(TransitioningContentControl),
            typeof(ToggleSwitch),
            typeof(DropDownButton),
            typeof(SplitButton),
            typeof(HyperlinkButton),
            typeof(SplitView),
            typeof(Carousel),
            typeof(NotificationCard),
            typeof(WindowNotificationManager),
            typeof(RefreshContainer),
            typeof(RefreshVisualizer),
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
        Dictionary<string, List<string>> wrongDisabledOpacity = new Dictionary<string, List<string>>();

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
            CollectDisabledOpacity(window, header, wrongDisabledOpacity);
            this.CheckTabPopups(window, header);
            if (header == "Data Grid") {
                this.CheckDataGridEditing(window);
                this.CheckDataGridColumnDrag(window);
            }
        }
        tabs.SelectedIndex = 0;
        Pump();

        foreach (KeyValuePair<string, List<string>> entry in untemplated) {
            this._report.Fail($"{entry.Key} has no template (tabs: {string.Join(", ", entry.Value)})");
        }
        if (untemplated.Count == 0) {
            this._report.Pass($"Every templated control in all {tabItems.Count} tabs has a template");
        }

        foreach (KeyValuePair<string, List<string>> entry in wrongDisabledOpacity) {
            this._report.Fail($"{entry.Key} (tabs: {string.Join(", ", entry.Value)})");
        }
        if (wrongDisabledOpacity.Count == 0) {
            this._report.Pass($"Disabled controls in all {tabItems.Count} tabs draw their text and glyphs at ThemeDisabledOpacity, once");
        }
    }

    /// <summary>
    /// Text and glyphs of a disabled control must be drawn at ThemeDisabledOpacity: not at full opacity, and not faded twice
    /// by a control that fades as a whole around a part that fades itself again
    /// </summary>
    private static void CollectDisabledOpacity(Window window, string tab, Dictionary<string, List<string>> wrong) {
        double disabledOpacity = window.TryFindResource("ThemeDisabledOpacity", out object? resource) && resource is double value ? value : 0.5;
        foreach (Control content in window.GetVisualDescendants().OfType<Control>()) {
            string? text = content switch {
                TextBlock textBlock => textBlock.Text,
                TextPresenter textPresenter => textPresenter.Text,
                PathShape => "(glyph)",
                _ => null,
            };
            // watermarks are faded on purpose, with TextControlPlaceholderOpacity
            if (string.IsNullOrWhiteSpace(text) || content.IsEffectivelyEnabled || !content.IsEffectivelyVisible || content.Name == "PART_Watermark") continue;
            List<Visual> chain = content.GetSelfAndVisualAncestors().TakeWhile(v => v is not TopLevel).ToList();
            // validation messages are not part of the control
            if (IsValidationMessage(chain)) continue;
            double opacity = chain.Aggregate(1.0, (product, v) => product * v.Opacity);
            if (opacity == 0 || Math.Abs(opacity - disabledOpacity) < 0.01) continue;

            // the outermost control that is disabled itself, which is the control the Sandbox disabled
            string owner = chain.OfType<InputElement>().Last(e => !e.IsEnabled).GetType().Name;
            string how = opacity > disabledOpacity ? "not faded"
                : Math.Abs(opacity - disabledOpacity * disabledOpacity) < 0.01 ? "faded twice"
                : "faded too much";
            string problem = $"Disabled {owner} draws {(text == "(glyph)" ? "a glyph" : "its text")} at opacity {opacity:0.##}, {how}";
            if (!wrong.TryGetValue(problem, out List<string>? tabNames)) {
                tabNames = new List<string>();
                wrong.Add(problem, tabNames);
            }
            if (!tabNames.Contains(tab)) tabNames.Add(tab);
        }
    }

    private static bool IsValidationMessage(List<Visual> chain) {
        DataValidationErrors? errors = chain.OfType<DataValidationErrors>().FirstOrDefault();
        return errors != null && !(errors.Content is Visual control && chain.Contains(control));
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
            // closing the popups leaves the menu item selected, it would show highlighted in every later screenshot
            menu.Close();
            Pump();
        }
        List<Button> flyoutButtons = visuals.OfType<Button>().Where(b => b.Flyout != null && b.IsEffectivelyEnabled).ToList();
        for (int i = 0; i < flyoutButtons.Count; i++) {
            Button button = flyoutButtons[i];
            this.CheckPopup(window, tab, $"ButtonFlyout{i + 1}", () => button.Flyout!.ShowAt(button));
        }
        // SplitButton is not a Button, its flyout is opened from the secondary (arrow) part
        if (visuals.OfType<SplitButton>().FirstOrDefault(b => b.Flyout != null && b.IsEffectivelyEnabled) is { } splitButton) {
            this.CheckPopup(window, tab, "SplitButtonFlyout", () => splitButton.Flyout!.ShowAt(splitButton));
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
        // skip the buttons of hidden columns (seconds, AM/PM)
        List<RepeatButton> buttons = presenter.GetVisualDescendants().OfType<RepeatButton>()
            .Where(b => b.GetVisualParent() is Visual { IsEffectivelyVisible: true })
            .ToList();
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

    /// <summary>
    /// Drags a column header sideways and checks that the drop location indicator is drawn
    /// </summary>
    private void CheckDataGridColumnDrag(Window window) {
        this._sink.Phase = "datagrid-column-drag";
        DataGrid? grid = window.GetVisualDescendants().OfType<DataGrid>().FirstOrDefault(g => g.IsEffectivelyEnabled);
        DataGridColumnHeader? header = grid?.GetVisualDescendants().OfType<DataGridColumnHeader>()
            .FirstOrDefault(h => h.Content is string && h.IsEffectivelyVisible);
        if (grid == null || header == null) {
            this._report.Fail("DataGrid column drag: no enabled DataGrid with column headers found");
            return;
        }

        // reordering is off by default, the indicator only shows up when an app enables it
        bool canUserReorderColumns = grid.CanUserReorderColumns;
        grid.CanUserReorderColumns = true;
        Point start = header.TranslatePoint(new Point(header.Bounds.Width / 2, header.Bounds.Height / 2), window) ?? default;
        window.MouseDown(start, MouseButton.Left);
        for (int i = 1; i <= 6; i++) {
            window.MouseMove(start + new Point(i * 20, 0), RawInputModifiers.LeftMouseButton);
            Pump();
        }
        // the indicator is built from DataGrid.DropLocationIndicatorTemplate (a 2 pixel wide shape) and added to the headers presenter
        Shape? indicator = grid.GetVisualDescendants().OfType<DataGridColumnHeadersPresenter>().FirstOrDefault()?
            .GetVisualChildren().OfType<Shape>()
            .FirstOrDefault(s => s.Width == 2 && s.IsEffectivelyVisible);
        this.SaveScreenshot(window, "datagrid_column_drag");
        // the indicator's DynamicResource brush resolves on a later layout pass, read it after rendering
        IBrush? fill = indicator?.Fill;
        // drop it back where it started, so the column order does not change
        window.MouseMove(start, RawInputModifiers.LeftMouseButton);
        window.MouseUp(start, MouseButton.Left);
        // park the pointer in the bottom right corner, away from anything with a hover style
        window.MouseMove(new Point(window.Bounds.Width - 1, window.Bounds.Height - 1));
        grid.CanUserReorderColumns = canUserReorderColumns;
        Pump();

        this._report.Check(indicator != null && fill != null,
            $"DataGrid: column drop location indicator is drawn while dragging a column header ({(indicator == null ? "not found" : $"fill {fill?.ToString() ?? "null"}")})");
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
            this.CheckFrameThemeVariant(window, variant);
        }
        app.SetThemeVariant(ThemeVariant.Default);
        Pump();
    }

    /// <summary>
    /// The native window frame (title bar) follows the platform variant that TopLevel derives from ActualThemeVariant,
    /// a palette with a dark background must map to Dark there
    /// </summary>
    private void CheckFrameThemeVariant(Window window, ThemeVariant variant) {
        if (window.Background is not ISolidColorBrush background) return;
        Color color = background.Color;
        double luminance = (0.2126 * color.R + 0.7152 * color.G + 0.0722 * color.B) / 255;
        PlatformThemeVariant expected = luminance < 0.5 ? PlatformThemeVariant.Dark : PlatformThemeVariant.Light;
        // the same conversion TopLevel uses for the frame
        PlatformThemeVariant actual = (PlatformThemeVariant?)window.ActualThemeVariant ?? PlatformThemeVariant.Light;
        this._report.Check(actual == expected,
            $"Theme variant {variant.Key}: window frame is {expected} for background {color} (got {actual})");
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

    /// <summary>
    /// Plain text inside a ThemeVariantScope must use the scope's palette, not the foreground inherited from outside
    /// </summary>
    private void CheckThemeVariantScope(App app) {
        this._sink.Phase = "theme-variant-scope";
        ThemeVariant rust = app.GetThemeVariants().First(v => Equals(v.Key, CitrusDefaultPalettes.Rust.ToString()));
        TextBlock text = new TextBlock { Text = "Plain TextBlock in a Rust ThemeVariantScope" };
        Border background = new Border { Padding = new Thickness(10), Child = text };
        background.Bind(Border.BackgroundProperty, background.GetResourceObservable("ThemeBackgroundBrush"));
        ThemeVariantScope scope = new ThemeVariantScope { RequestedThemeVariant = rust, Child = background };
        Window host = new Window { Width = 400, Height = 100, Content = scope };
        host.Show();
        Pump();
        Color? expected = scope.TryFindResource("ThemeForegroundBrush", scope.ActualThemeVariant, out object? value)
            ? (value as ISolidColorBrush)?.Color
            : null;
        Color? actual = (text.Foreground as ISolidColorBrush)?.Color;
        this._report.Check(expected != null && actual == expected,
            $"ThemeVariantScope: plain text uses the scope's ThemeForegroundBrush (expected {expected}, got {actual})");
        this.SaveScreenshot(host, "theme_variant_scope");
        host.Close();
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
    /// The managed file dialog (UseManagedSystemDialogs), opened on the Palette folder so the screenshot is stable
    /// </summary>
    private void CheckManagedFileChooser() {
        this._sink.Phase = "managed-file-chooser";
        ManagedFileChooserViewModel viewModel = new ManagedFileChooserViewModel(
            new FilePickerOpenOptions { Title = "Open file" }, new ManagedFileDialogOptions());
        if (this._sourceDirectory != null) {
            viewModel.Navigate(Path.Combine(this._sourceDirectory, "Citrus.Avalonia", "Palette"));
        }
        ManagedFileChooser chooser = new ManagedFileChooser { DataContext = viewModel };
        Window host = new Window { Width = 800, Height = 500, Content = chooser };
        host.Show();
        Pump();
        int fileCount = chooser.GetVisualDescendants().OfType<ListBox>().FirstOrDefault(l => l.Name == "PART_Files")?.ItemCount ?? 0;
        this._report.Check(chooser.Template != null && fileCount > 0,
            $"ManagedFileChooser renders its file list ({fileCount} files)");
        this.SaveScreenshot(host, "managed_file_chooser");
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
            new ToggleSwitch { Content = "_ToggleSwitch" },
            new DropDownButton { Content = "_DropDownButton" },
            new SplitButton { Content = "_SplitButton" },
            new HyperlinkButton { Content = "_HyperlinkButton" },
        };
        StackPanel panel = new StackPanel();
        panel.Children.AddRange(controls);
        Window host = new Window { Width = 300, Height = 500, Content = panel };
        // a control without a theme can throw while it is laid out (ToggleSwitch requires PART_MovingKnobs)
        try {
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
        } catch (Exception ex) {
            this._report.Fail($"Access keys: {ex.GetType().Name}: {ex.Message}");
        } finally {
            host.Close();
        }
    }

    /// <summary>
    /// One notification of every type, shown through a WindowNotificationManager in the window's adorner layer
    /// </summary>
    private void CheckNotifications() {
        this._sink.Phase = "notifications";
        Window host = new Window { Width = 400, Height = 700 };
        try {
            host.Show();
            Pump();
            WindowNotificationManager manager = new WindowNotificationManager(host) {
                Position = NotificationPosition.TopLeft,
                MaxItems = 4,
            };
            Pump();
            NotificationType[] types = Enum.GetValues<NotificationType>();
            foreach (NotificationType type in types) {
                manager.Show(new Notification(type.ToString(), $"A notification of type {type}", type));
            }
            // the cards fade in with an animation, wait until it has finished so the screenshot is stable
            Settle(TimeSpan.FromMilliseconds(600));
            List<NotificationCard> cards = host.GetVisualDescendants().OfType<NotificationCard>().ToList();
            int rendered = cards.Count(c => c.Template != null && c.Bounds.Width > 0 && c.Bounds.Height > 0 && c.Background is ISolidColorBrush);
            this._report.Check(manager.Template != null && rendered == types.Length,
                $"WindowNotificationManager shows a rendered card for each of the {types.Length} notification types ({rendered} rendered)");
            this.SaveScreenshot(host, "notifications");
        } catch (Exception ex) {
            this._report.Fail($"Notifications: {ex.GetType().Name}: {ex.Message}");
        } finally {
            host.Close();
        }
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

    /// <summary>
    /// The headless render clock runs in real time, so animations and transitions need real time to finish
    /// </summary>
    private static void Settle(TimeSpan duration) {
        Stopwatch stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < duration) {
            Thread.Sleep(20);
            Pump();
        }
    }

    private void SaveScreenshot(TopLevel topLevel, string name) {
        Pump();
        topLevel.CaptureRenderedFrame()?.Save(Path.Combine(this._outputDirectory, name + ".png"));
    }

    private static string FileNamePart(string text) => Regex.Replace(text, @"\W+", "_").Trim('_');
}

### Citrus Unofficial

An Avalonia experimental theme, originally developed by Artyom V. Gorchakov.

This is a heavily modified fork for my own custom use.

It was completely reworked to use Avalonia's new ControlTheme system instead of just plain old styles.

Since I am developing it to use it in my Martridge project, I also added some missing controls like the DataGrid which I needed there. I may further add other missing controls later on...

### Getting Started

Since this is not the official distribution, the NuGet package is not available.

If you want to use this theme, the way to do it is to add this repo as a [submodule](https://git-scm.com/book/en/v2/Git-Tools-Submodules) to your existing git repository:

```sh
git submodule add https://github.com/drone1400/Citrus.Avalonia ./citrus
# Reference the /citrus/src/Citrus.Avalonia/Citrus.Avalonia.csproj project.
# Reference the /citrus/src/Citrus.Avalonia.DataGrid/Citrus.Avalonia.DataGrid.csproj project if you need to use the DataGrid control.
# The /citrus/src/Citrus.Avalonia.Sandbox/Citrus.Avalonia.Sandbox.csproj is 
# the sandbox where you can browse the markup samples.
```

#### Simple way to use the theme

```xml
<Application x:Class="Citrus.Avalonia.Sandbox.App"
             xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             RequestedThemeVariant="Default">
    <Application.Styles>
        <Styles>
            <StyleInclude Source="avares://Citrus.Avalonia/CitrusTheme.xaml" />
            <!-- Only when using the DataGrid, it must be included after the Citrus theme -->
            <StyleInclude Source="avares://Citrus.Avalonia.DataGrid/CitrusDataGrid.xaml" />
        </Styles>
    </Application.Styles>
</Application>

```

The texts used by the controls (TextBox context menu, DatePicker/TimePicker placeholders, managed file dialog) use the same resource keys as Avalonia's own themes, for example `StringTextFlyoutCutText` or `StringDatePickerDayText`. Define them in your application resources to localize them.

### Theme Variants

Now implements ThemeVariants! The default Light theme is Citrus, the default Dark theme is Sea, and the Default fallback is also Citrus.

This means you can use `RequestedThemeVariant="Default"` in your application and Citrus will use the Light or Dark variant automagically.

Built-in palettes (see `CitrusDefaultPalettes`): Citrus, Candy, Magma, Lime, Grapefruit and Orange are light, Rust, Sea, DarkLime, DarkGrapefruit and DarkOrange are dark. Each one is registered as a ThemeVariant with the same name, inheriting `ThemeVariant.Light` or `ThemeVariant.Dark`, so the native window title bar matches the palette.

### Overriding implicit ThemeVariant values

You can customize what Light, Dark or even Default variants are used through the `CitrusTheme` style's properties: `DesiredLightThemeVariant`, `DesiredDarkThemeVariant` or `DesiredDefaultThemeVariant`. Setting one of them to `null` restores the built-in palette (Citrus for Light and Default, Sea for Dark).

**NOTE:** the `IThemeVariantProvider` object use must not be included in another ThemeDictionary elsewhere, or you will get an exception: `System.InvalidOperationException: The ResourceDictionary already has a parent.`

#### Example implementation (xaml only)

```xml
<Application x:Class="Citrus.Avalonia.Sandbox.App"
             xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:avalonia="clr-namespace:Citrus.Avalonia;assembly=Citrus.Avalonia"
             RequestedThemeVariant="Default">
    <Application.Styles>
        <Styles>
            <avalonia:CitrusTheme>
                <!-- Uncomment this to override light theme -->
                <!-- <avalonia:CitrusTheme.DesiredLightThemeVariant> -->
                <!--     <ResourceInclude Source="avares://Citrus.Avalonia/Palette/CandyPalette.xaml"/> -->
                <!-- </avalonia:CitrusTheme.DesiredLightThemeVariant> -->
                <avalonia:CitrusTheme.DesiredDarkThemeVariant>
                    <ResourceInclude Source="avares://Citrus.Avalonia/Palette/RustPalette.xaml"/>
                </avalonia:CitrusTheme.DesiredDarkThemeVariant>
            </avalonia:CitrusTheme>
        </Styles>
    </Application.Styles>
</Application>
```

#### Example implementation from codebehind

```xml
<Application x:Class="Citrus.Avalonia.Sandbox.App"
             xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             RequestedThemeVariant="Default">
</Application>
```
```cs
CitrusTheme citrusTheme = new CitrusTheme();
Uri uri = new Uri("avares://Citrus.Avalonia/Palette/RustPalette.xaml");
citrusTheme.DesiredDarkThemeVariant = new ResourceInclude(uri) { Source = uri };
Application.Current.Styles.Add(citrusTheme);
```


### Using a custom ThemeVariant

Other than the `Default`, `Light`, `Dark` ThemeVariants, you can of course use a custom ThemeVariant. The `CitrusTheme` object has some helper properties to facilitate this. 

You can use the `RegisterThemeVariant` method to register a custom ThemeVariant with the `CitrusTheme` style object. This effectively adds the ThemeVariant to the `CitrusTheme` style object's ThemeDictionaries.

You can then use the `GetRegisteredThemeVariants` method to get a list of all the currently registered ThemeVariants. Afterward, set the Application's `RequestedThemeVariant` to one of the ThemeVariants from the list to use the custom ThemeVariant.

#### Example using the RegisterThemeVariant method

```cs
CitrusTheme citrusTheme = new CitrusTheme();
citrusTheme.RegisterThemeVariant(new CitrusThemeVariantData(
    "CustomPalette", "avares://Citrus.Avalonia.Sandbox/Palette/CustomPalette.xaml"));
IList<ThemeVariant> list = citrusTheme.GetRegisteredThemeVariants();
Application.Current.Styles.Add(citrusTheme);
Application.Current.SetValue(Application.RequestedThemeVariantProperty, list[list.Count-1]);
```

A custom dark palette should inherit `ThemeVariant.Dark` like the built-in dark palettes, otherwise windows keep a light native title bar:

```cs
citrusTheme.RegisterThemeVariant(new CitrusThemeVariantData(
    "CustomDarkPalette", new Uri("avares://MyApp/Palette/CustomDarkPalette.xaml"), ThemeVariant.Dark));
```


#### Example overriding the Light ThemeVariant from AvaloniaResource
```cs
CitrusTheme citrusTheme = new CitrusTheme();
Uri uri = new Uri("avares://Citrus.Avalonia.Sandbox/Palette/CustomPalette.xaml");
citrusTheme.DesiredLightThemeVariant = new ResourceInclude(uri) { Source = uri };
Application.Current.Styles.Add(citrusTheme);
```

#### Example overriding the Light ThemeVariant from local file
```cs
CitrusTheme citrusTheme = new CitrusTheme();
using FileStream fileStream = new FileStream("MyTheme.axaml", FileMode.Open, FileAccess.Read); 
object obj = AvaloniaRuntimeXamlLoader.Load(fileStream);
if (obj is ResourceDictionary dictionary) {
    citrusTheme.DesiredLightThemeVariant = dictionary;
}
Application.Current.Styles.Add(citrusTheme);
```

For more examples check out the `Citrus.Avalonia.Sandbox` project!


### Audit tool

The `Citrus.Avalonia.Audit` project renders the Sandbox headlessly (every tab, every registered theme variant, the popups, a few standalone controls) and checks the themes for missing templates, resource keys that do not resolve, empty texts, broken popups and Avalonia warnings. It saves a screenshot of everything it renders, and its exit code is the number of failed checks. CI runs it after every build.

```sh
cd src
dotnet run --project Citrus.Avalonia.Audit -c Release -- <output folder>
# byte-compare the screenshots of two runs, e.g. to check that a refactoring changed nothing
dotnet run --project Citrus.Avalonia.Audit -c Release -- --compare <output folder A> <output folder B>
```


### Technologies and Tools Used

- <a href="https://github.com/avaloniaui">AvaloniaUI</a> cross-platform XAML-based GUI framework
- <a href="https://github.com/reactiveui/ReactiveUI">ReactiveUI.Avalonia</a> for MVVM implementation in the sandbox project
- <a href="https://github.com/reactiveui/reactiveui.validation">ReactiveUI.Validation</a> for `INotifyDataErrorInfo` validations
- <a href="https://www.jetbrains.com/rider/">JetBrains Rider</a> cross-platform C# IDE

### Screenshots

These are screenshots of the `Citrus.Avalonia.Sandbox` taken by the audit tool. To update them, run the audit and copy its `tab*.png` files (except the disabled page) and the `variant_*.png` files of the built-in palettes into `doc/`.

#### Sandbox tabs

|  |  |
|--|--|
| **Common Controls**<br>![](doc/tab00_Common_Controls.png) | **Spinner Controls**<br>![](doc/tab01_Spinner_Controls.png) |
| **Text Controls**<br>![](doc/tab02_Text_Controls.png) | **Slider and Progress**<br>![](doc/tab03_Slider_and_Progress.png) |
| **Lists and Trees**<br>![](doc/tab04_Lists_and_Trees.png) | **Expanders**<br>![](doc/tab05_Expanders.png) |
| **Misc**<br>![](doc/tab06_Misc.png) | **Date/Time Controls**<br>![](doc/tab07_Date_Time_Controls.png) |
| **Data Grid**<br>![](doc/tab08_Data_Grid.png) | **Theme Variants**<br>![](doc/tab09_Theme_Variants.png) |
| **More Controls**<br>![](doc/tab10_More_Controls.png) |  |

#### Palettes

The Common Controls tab in each built-in palette.

|  |  |
|--|--|
| **Citrus** (light, the default Light)<br>![](doc/variant_Citrus.png) | **Candy** (light)<br>![](doc/variant_Candy.png) |
| **Magma** (light)<br>![](doc/variant_Magma.png) | **Lime** (light)<br>![](doc/variant_Lime.png) |
| **Grapefruit** (light)<br>![](doc/variant_Grapefruit.png) | **Orange** (light)<br>![](doc/variant_Orange.png) |
| **Rust** (dark)<br>![](doc/variant_Rust.png) | **Sea** (dark, the default Dark)<br>![](doc/variant_Sea.png) |
| **DarkLime** (dark)<br>![](doc/variant_DarkLime.png) | **DarkGrapefruit** (dark)<br>![](doc/variant_DarkGrapefruit.png) |
| **DarkOrange** (dark)<br>![](doc/variant_DarkOrange.png) |  |

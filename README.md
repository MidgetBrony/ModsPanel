# ModsPanel

ModsPanel is a shared MelonLoader settings API for BOXROOM mods. It adds a
scrollable **Mod Settings** screen inside BOXROOM's existing **Mods** tab, so
individual mods do not need to clone or resize the vanilla Settings layout.

## Player installation

Copy `ModsPanel.dll` into BOXROOM's `Mods` folder. Mods that use ModsPanel will
register their own sections automatically.

Open BOXROOM's **Mods** tab and select **Mod Settings**. Select the inner
**Mods** tab to return to BOXROOM's normal Mods screen.

## Mod developer usage

Reference `ModsPanel.dll`, then register a stable section during your mod's
initialization:

```csharp
ModsPanelApi.RegisterSection("Example.Author.MyMod", "My Mod")
    .Clear()
    .AddFolder(
        "library",
        "Library Folder",
        () => CurrentFolder,
        path => SaveFolder(path),
        () => $"{ItemCount} items found",
        () => Rescan(),
        "Select library folder")
    .AddToggle(
        "enabled",
        "Feature Enabled",
        () => Enabled,
        value => Enabled = value)
    .AddSlider(
        "volume",
        "Volume",
        () => Volume,
        value => SaveVolume(value),
        0f,
        1f,
        false,
        "0%")
    .AddDropdown(
        "quality",
        "Quality",
        () => new[] { "Low", "Medium", "High" },
        () => QualityIndex,
        index => SaveQuality(index))
    .AddText(
        "username",
        "Display Name",
        () => DisplayName,
        value => SaveDisplayName(value),
        "Enter a name")
    .AddNumber(
        "limit",
        "Item Limit",
        () => ItemLimit,
        value => SaveItemLimit((int)value),
        1,
        1000,
        true)
    .AddButton(
        "rebuild",
        "Rebuild cached data",
        "Rebuild",
        () => Rebuild());
```

Available controls in v1.1.0:

- Folder picker with path, status, Browse, and optional Refresh action
- Boolean toggle
- Single-line text input
- Number input with optional minimum, maximum, and whole-number mode
- Horizontal slider with bounds, whole-number mode, and value formatting
- Dropdown with choices supplied dynamically by the registering mod
- Action button
- Read-only label and vertical spacer

The panel owns scrolling, section ordering, BOXROOM-like colors, font reuse,
and scene rebuilding. A mod should register definitions once and keep its actual
values in its own save/configuration system.

## Build

Copy `Directory.Build.user.props.example` to `Directory.Build.user.props` and
set `GamePath`, or supply `BOXROOM_GAME_PATH`/`-p:GamePath=...`.

```powershell
dotnet build -c Release -p:DeployToGame=false
```

`Directory.Build.user.props` is ignored so local Steam paths are not committed.

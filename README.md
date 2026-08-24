# ModsPanel

ModsPanel is a shared MelonLoader UI API for BOXROOM mods. It provides both a
scrollable **Mod Settings** screen inside BOXROOM's existing **Mods** tab and
temporary BOXROOM-styled menus that can be opened during gameplay. Individual
mods no longer need to clone vanilla layouts or maintain their own IMGUI skin.

Controller prompts use the CC0 Kenney Input Prompts 1.5 pack. ModsPanel embeds
outlined Xbox, PlayStation, Nintendo Switch, and Steam Deck glyphs and selects
the family from the controller Unity reports at runtime. The supplied license is
kept at `Assets/Kenney-Input-Prompts-License.txt`.

## Player installation

Copy `ModsPanel.dll` into BOXROOM's `Mods` folder. Mods that use ModsPanel will
register their own sections automatically.

Open BOXROOM's **Mods** tab and select **Mod Settings**. Select the inner
**Mods** tab to return to BOXROOM's normal Mods screen.

## Mod developer usage: settings

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

Available settings controls:

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

## Mod developer usage: general menus

Create a menu once, populate it with the current choices, and call `Show()`
from your interaction patch. Menus automatically release the cursor, close on
Escape, restore the previous cursor state, scale from a 1920x1080 reference
layout, scroll when needed, and focus the first selectable control.

```csharp
private static bool createTwoWayLink = true;

private static void OpenRoomPicker()
{
    ModMenu menu = ModsUi.CreateMenu(
            "Example.Author.MultiRoom",
            "Link Door",
            "Choose the room containing the destination door.")
        .AddHeading("Choose a destination")
        .AddToggle(
            "Create a two-way link",
            () => createTwoWayLink,
            value => createTwoWayLink = value);

    foreach (RoomInfo room in AvailableRooms)
    {
        RoomInfo selectedRoom = room;
        menu.AddButton(
            selectedRoom.Name,
            () => LoadDestination(selectedRoom),
            $"Slot {selectedRoom.Slot}");
    }

    menu.Closed = () => CancelPendingLink();
    menu.Show();
}
```

General menus support headings, wrapped labels, action buttons with optional
detail text, toggles, spacers, custom eyebrow/title/subtitle/close text, an
`Closed` callback, explicit `Close()`, and global `ModsUi.CloseMenu()` /
`ModsUi.IsMenuOpen` access. Use `ModsUi.ShowToast(message)` for transient
feedback that does not interrupt gameplay.

## Build

Copy `Directory.Build.user.props.example` to `Directory.Build.user.props` and
set `GamePath`, or supply `BOXROOM_GAME_PATH`/`-p:GamePath=...`.

```powershell
dotnet build -c Release -p:DeployToGame=false
```

`Directory.Build.user.props` is ignored so local Steam paths are not committed.

using System;
using System.Collections.Generic;

namespace ModsPanel
{
    /// <summary>
    /// Entry point for temporary, BOXROOM-styled mod menus. Unlike a settings
    /// section, a ModMenu can be opened from gameplay and closed when its task is
    /// complete.
    /// </summary>
    public static class ModsUi
    {
        public static ModMenu CreateMenu(string ownerId, string title, string subtitle = "")
        {
            if (string.IsNullOrWhiteSpace(ownerId))
                throw new ArgumentException("A stable owner ID is required.", nameof(ownerId));

            return new ModMenu(ownerId, title ?? ownerId, subtitle ?? string.Empty);
        }

        public static bool IsMenuOpen => ModMenuRuntime.HasOpenMenu;

        public static void CloseMenu() => ModMenuRuntime.Ensure().CloseMenu();
    }

    /// <summary>A reusable modal menu definition owned by another mod.</summary>
    public sealed class ModMenu
    {
        private readonly List<ModMenuItem> items = new List<ModMenuItem>();

        internal ModMenu(string ownerId, string title, string subtitle)
        {
            OwnerId = ownerId;
            Title = title;
            Subtitle = subtitle;
        }

        public string OwnerId { get; }
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string Eyebrow { get; set; } = "MOD MENU";
        public string CloseText { get; set; } = "CLOSE";
        public Action Closed { get; set; }
        internal IReadOnlyList<ModMenuItem> Items => items;

        public ModMenu Clear()
        {
            items.Clear();
            RefreshIfOpen();
            return this;
        }

        public ModMenu AddHeading(string text)
        {
            items.Add(new ModMenuHeading(text));
            RefreshIfOpen();
            return this;
        }

        public ModMenu AddLabel(string text)
        {
            items.Add(new ModMenuLabel(text));
            RefreshIfOpen();
            return this;
        }

        public ModMenu AddButton(string text, Action pressed, string detail = "")
        {
            items.Add(new ModMenuButton(text, detail, pressed));
            RefreshIfOpen();
            return this;
        }

        public ModMenu AddToggle(string text, Func<bool> getValue, Action<bool> setValue)
        {
            items.Add(new ModMenuToggle(text, getValue, setValue));
            RefreshIfOpen();
            return this;
        }

        public ModMenu AddSpacer(float height = 24f)
        {
            items.Add(new ModMenuSpacer(Math.Max(0f, height)));
            RefreshIfOpen();
            return this;
        }

        public void Show() => ModMenuRuntime.Ensure().ShowMenu(this);
        public void Close() => ModMenuRuntime.Ensure().CloseMenu(this);

        private void RefreshIfOpen()
        {
            if (ModMenuRuntime.IsOpen(this))
                ModMenuRuntime.Ensure().ShowMenu(this);
        }
    }

    internal abstract class ModMenuItem { }

    internal sealed class ModMenuHeading : ModMenuItem
    {
        internal ModMenuHeading(string text) { Text = text ?? string.Empty; }
        internal string Text { get; }
    }

    internal sealed class ModMenuLabel : ModMenuItem
    {
        internal ModMenuLabel(string text) { Text = text ?? string.Empty; }
        internal string Text { get; }
    }

    internal sealed class ModMenuButton : ModMenuItem
    {
        internal ModMenuButton(string text, string detail, Action pressed)
        {
            Text = text ?? string.Empty;
            Detail = detail ?? string.Empty;
            Pressed = pressed ?? (() => { });
        }

        internal string Text { get; }
        internal string Detail { get; }
        internal Action Pressed { get; }
    }

    internal sealed class ModMenuToggle : ModMenuItem
    {
        internal ModMenuToggle(string text, Func<bool> getValue, Action<bool> setValue)
        {
            Text = text ?? string.Empty;
            GetValue = getValue ?? (() => false);
            SetValue = setValue ?? (_ => { });
        }

        internal string Text { get; }
        internal Func<bool> GetValue { get; }
        internal Action<bool> SetValue { get; }
    }

    internal sealed class ModMenuSpacer : ModMenuItem
    {
        internal ModMenuSpacer(float height) { Height = height; }
        internal float Height { get; }
    }
}

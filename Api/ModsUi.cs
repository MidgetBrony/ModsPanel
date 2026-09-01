using System;
using System.Collections.Generic;
using UnityEngine;

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

        /// <summary>Shows a short BOXROOM-styled notification without opening a menu.</summary>
        public static void ShowToast(string message, float seconds = 4f) =>
            ModMenuRuntime.Ensure().ShowToast(message, seconds);

        /// <summary>Returns a controller-family glyph for an Input System gamepad path.</summary>
        public static Sprite GetControllerGlyph(string controlPath) => ControllerGlyphs.ForPath(controlPath);
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
        public bool IsOpen => ModMenuRuntime.IsOpen(this);
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

        /// <summary>Adds a non-interactive image preview to the menu.</summary>
        public ModMenu AddImage(Func<Texture> getTexture, float preferredHeight = 430f)
        {
            items.Add(new ModMenuImage(getTexture, preferredHeight));
            RefreshIfOpen();
            return this;
        }

        public ModMenu AddToggle(string text, Func<bool> getValue, Action<bool> setValue)
        {
            items.Add(new ModMenuToggle(text, getValue, setValue));
            RefreshIfOpen();
            return this;
        }

        /// <summary>Adds a controller-friendly slider to a temporary gameplay menu.</summary>
        public ModMenu AddSlider(string text, Func<float> getValue, Action<float> setValue,
            float minimum, float maximum, bool wholeNumbers = false,
            Func<float, string> formatValue = null)
        {
            items.Add(new ModMenuSlider(text, getValue, setValue, minimum, maximum,
                wholeNumbers, formatValue));
            RefreshIfOpen();
            return this;
        }

        /// <summary>Adds a dropdown backed by a selected option index.</summary>
        public ModMenu AddDropdown(string text, Func<IReadOnlyList<string>> getOptions,
            Func<int> getSelectedIndex, Action<int> setSelectedIndex)
        {
            items.Add(new ModMenuDropdown(text, getOptions, getSelectedIndex, setSelectedIndex));
            RefreshIfOpen();
            return this;
        }

        public ModMenu AddTextInput(string label, Func<string> getValue,
            Action<string> setValue, string placeholder = "")
        {
            items.Add(new ModMenuTextInput(label, getValue, setValue, placeholder));
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

    internal sealed class ModMenuImage : ModMenuItem
    {
        internal ModMenuImage(Func<Texture> getTexture, float preferredHeight)
        {
            GetTexture = getTexture ?? (() => null);
            PreferredHeight = Math.Max(80f, preferredHeight);
        }

        internal Func<Texture> GetTexture { get; }
        internal float PreferredHeight { get; }
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

    internal sealed class ModMenuSlider : ModMenuItem
    {
        internal ModMenuSlider(string text, Func<float> getValue, Action<float> setValue,
            float minimum, float maximum, bool wholeNumbers, Func<float, string> formatValue)
        {
            Text = text ?? string.Empty;
            GetValue = getValue ?? (() => minimum);
            SetValue = setValue ?? (_ => { });
            Minimum = minimum;
            Maximum = Math.Max(minimum, maximum);
            WholeNumbers = wholeNumbers;
            FormatValue = formatValue ?? (value => value.ToString(wholeNumbers ? "0" : "0.##"));
        }

        internal string Text { get; }
        internal Func<float> GetValue { get; }
        internal Action<float> SetValue { get; }
        internal float Minimum { get; }
        internal float Maximum { get; }
        internal bool WholeNumbers { get; }
        internal Func<float, string> FormatValue { get; }
    }

    internal sealed class ModMenuDropdown : ModMenuItem
    {
        internal ModMenuDropdown(string text, Func<IReadOnlyList<string>> getOptions,
            Func<int> getSelectedIndex, Action<int> setSelectedIndex)
        {
            Text = text ?? string.Empty;
            GetOptions = getOptions ?? (() => Array.Empty<string>());
            GetSelectedIndex = getSelectedIndex ?? (() => 0);
            SetSelectedIndex = setSelectedIndex ?? (_ => { });
        }
        internal string Text { get; }
        internal Func<IReadOnlyList<string>> GetOptions { get; }
        internal Func<int> GetSelectedIndex { get; }
        internal Action<int> SetSelectedIndex { get; }
    }

    internal sealed class ModMenuSpacer : ModMenuItem
    {
        internal ModMenuSpacer(float height) { Height = height; }
        internal float Height { get; }
    }

    internal sealed class ModMenuTextInput : ModMenuItem
    {
        internal ModMenuTextInput(string label, Func<string> getValue,
            Action<string> setValue, string placeholder)
        {
            Label = label ?? string.Empty;
            GetValue = getValue ?? (() => string.Empty);
            SetValue = setValue ?? (_ => { });
            Placeholder = placeholder ?? string.Empty;
        }
        internal string Label { get; }
        internal Func<string> GetValue { get; }
        internal Action<string> SetValue { get; }
        internal string Placeholder { get; }
    }
}

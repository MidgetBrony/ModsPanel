using System;
using System.Collections.Generic;
using System.Linq;

namespace ModsPanel
{
    /// <summary>
    /// Public entry point used by BOXROOM mods to add settings to the shared,
    /// scrollable Mod Settings screen.
    /// </summary>
    public static class ModsPanelApi
    {
        private static readonly List<ModSection> Sections = new List<ModSection>();

        /// <summary>
        /// Registers or retrieves a mod-owned section. The stable owner ID prevents
        /// duplicate panels when a scene is reloaded or a mod initializes twice.
        /// </summary>
        public static ModSection RegisterSection(
            string ownerId,
            string title,
            int order = 0)
        {
            if (string.IsNullOrWhiteSpace(ownerId))
                throw new ArgumentException("A stable owner ID is required.", nameof(ownerId));

            ModSection existing = Sections.FirstOrDefault(
                section => string.Equals(section.OwnerId, ownerId, StringComparison.Ordinal));

            if (existing != null)
            {
                existing.Title = title ?? ownerId;
                existing.Order = order;
                NotifyChanged();
                return existing;
            }

            var created = new ModSection(ownerId, title ?? ownerId, order);
            Sections.Add(created);
            NotifyChanged();
            return created;
        }

        /// <summary>Removes a section previously registered by the calling mod.</summary>
        public static void UnregisterSection(string ownerId)
        {
            Sections.RemoveAll(section =>
                string.Equals(section.OwnerId, ownerId, StringComparison.Ordinal));
            NotifyChanged();
        }

        internal static IReadOnlyList<ModSection> Snapshot() => Sections
            .OrderBy(section => section.Order)
            .ThenBy(section => section.Title, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        internal static void NotifyChanged()
        {
            if (ModsPanelRuntime.Instance != null)
                ModsPanelRuntime.Instance.RequestRebuild();
        }
    }

    /// <summary>A named group of controls owned by one mod.</summary>
    public sealed class ModSection
    {
        private readonly List<ModControl> controls = new List<ModControl>();

        internal ModSection(string ownerId, string title, int order)
        {
            OwnerId = ownerId;
            Title = title;
            Order = order;
        }

        public string OwnerId { get; }
        public string Title { get; internal set; }
        public int Order { get; internal set; }
        internal IReadOnlyList<ModControl> Controls => controls;

        /// <summary>
        /// Removes all existing controls. This is useful when a mod re-registers
        /// its section after a configuration or version change.
        /// </summary>
        public ModSection Clear()
        {
            controls.Clear();
            ModsPanelApi.NotifyChanged();
            return this;
        }

        public ModSection AddFolder(
            string id,
            string label,
            Func<string> getValue,
            Action<string> setValue,
            Func<string> getStatus = null,
            Action refresh = null,
            string browseTitle = null)
        {
            controls.Add(new FolderControl(
                id, label, getValue, setValue, getStatus, refresh, browseTitle));
            ModsPanelApi.NotifyChanged();
            return this;
        }

        public ModSection AddButton(string id, string label, string buttonText, Action pressed)
        {
            controls.Add(new ButtonControl(id, label, buttonText, pressed));
            ModsPanelApi.NotifyChanged();
            return this;
        }

        public ModSection AddToggle(
            string id,
            string label,
            Func<bool> getValue,
            Action<bool> setValue)
        {
            controls.Add(new ToggleControl(id, label, getValue, setValue));
            ModsPanelApi.NotifyChanged();
            return this;
        }

        /// <summary>Adds an editable single-line text setting.</summary>
        public ModSection AddText(
            string id,
            string label,
            Func<string> getValue,
            Action<string> setValue,
            string placeholder = "")
        {
            controls.Add(new TextControl(id, label, getValue, setValue, placeholder));
            ModsPanelApi.NotifyChanged();
            return this;
        }

        /// <summary>Adds an editable numeric setting with optional bounds.</summary>
        public ModSection AddNumber(
            string id,
            string label,
            Func<float> getValue,
            Action<float> setValue,
            float minimum = float.MinValue,
            float maximum = float.MaxValue,
            bool wholeNumbers = false)
        {
            controls.Add(new NumberControl(
                id, label, getValue, setValue, minimum, maximum, wholeNumbers));
            ModsPanelApi.NotifyChanged();
            return this;
        }

        /// <summary>Adds a horizontal slider and its current numeric value.</summary>
        public ModSection AddSlider(
            string id,
            string label,
            Func<float> getValue,
            Action<float> setValue,
            float minimum,
            float maximum,
            bool wholeNumbers = false,
            string valueFormat = "0.##")
        {
            controls.Add(new SliderControl(
                id, label, getValue, setValue, minimum, maximum, wholeNumbers, valueFormat));
            ModsPanelApi.NotifyChanged();
            return this;
        }

        /// <summary>Adds a dropdown whose selected value is represented by an index.</summary>
        public ModSection AddDropdown(
            string id,
            string label,
            Func<IReadOnlyList<string>> getOptions,
            Func<int> getSelectedIndex,
            Action<int> setSelectedIndex)
        {
            controls.Add(new DropdownControl(
                id, label, getOptions, getSelectedIndex, setSelectedIndex));
            ModsPanelApi.NotifyChanged();
            return this;
        }

        /// <summary>Adds non-editable explanatory or status text.</summary>
        public ModSection AddLabel(string id, string text)
        {
            controls.Add(new LabelControl(id, text));
            ModsPanelApi.NotifyChanged();
            return this;
        }

        /// <summary>Adds vertical space between controls.</summary>
        public ModSection AddSpacer(string id, float height = 24f)
        {
            controls.Add(new SpacerControl(id, height));
            ModsPanelApi.NotifyChanged();
            return this;
        }
    }

    internal abstract class ModControl
    {
        protected ModControl(string id, string label)
        {
            Id = id ?? string.Empty;
            Label = label ?? string.Empty;
        }

        internal string Id { get; }
        internal string Label { get; }
    }

    internal sealed class FolderControl : ModControl
    {
        internal FolderControl(
            string id,
            string label,
            Func<string> getValue,
            Action<string> setValue,
            Func<string> getStatus,
            Action refresh,
            string browseTitle) : base(id, label)
        {
            GetValue = getValue ?? (() => string.Empty);
            SetValue = setValue ?? (_ => { });
            GetStatus = getStatus;
            Refresh = refresh;
            BrowseTitle = string.IsNullOrWhiteSpace(browseTitle)
                ? $"Select {label}"
                : browseTitle;
        }

        internal Func<string> GetValue { get; }
        internal Action<string> SetValue { get; }
        internal Func<string> GetStatus { get; }
        internal Action Refresh { get; }
        internal string BrowseTitle { get; }
    }

    internal sealed class ButtonControl : ModControl
    {
        internal ButtonControl(string id, string label, string buttonText, Action pressed)
            : base(id, label)
        {
            ButtonText = buttonText ?? "Run";
            Pressed = pressed ?? (() => { });
        }

        internal string ButtonText { get; }
        internal Action Pressed { get; }
    }

    internal sealed class ToggleControl : ModControl
    {
        internal ToggleControl(
            string id,
            string label,
            Func<bool> getValue,
            Action<bool> setValue) : base(id, label)
        {
            GetValue = getValue ?? (() => false);
            SetValue = setValue ?? (_ => { });
        }

        internal Func<bool> GetValue { get; }
        internal Action<bool> SetValue { get; }
    }

    internal sealed class TextControl : ModControl
    {
        internal TextControl(string id, string label, Func<string> getValue,
            Action<string> setValue, string placeholder) : base(id, label)
        {
            GetValue = getValue ?? (() => string.Empty);
            SetValue = setValue ?? (_ => { });
            Placeholder = placeholder ?? string.Empty;
        }

        internal Func<string> GetValue { get; }
        internal Action<string> SetValue { get; }
        internal string Placeholder { get; }
    }

    internal class NumberControl : ModControl
    {
        internal NumberControl(string id, string label, Func<float> getValue,
            Action<float> setValue, float minimum, float maximum, bool wholeNumbers)
            : base(id, label)
        {
            if (maximum < minimum) throw new ArgumentOutOfRangeException(nameof(maximum));
            GetValue = getValue ?? (() => 0f);
            SetValue = setValue ?? (_ => { });
            Minimum = minimum;
            Maximum = maximum;
            WholeNumbers = wholeNumbers;
        }

        internal Func<float> GetValue { get; }
        internal Action<float> SetValue { get; }
        internal float Minimum { get; }
        internal float Maximum { get; }
        internal bool WholeNumbers { get; }
    }

    internal sealed class SliderControl : NumberControl
    {
        internal SliderControl(string id, string label, Func<float> getValue,
            Action<float> setValue, float minimum, float maximum, bool wholeNumbers,
            string valueFormat)
            : base(id, label, getValue, setValue, minimum, maximum, wholeNumbers)
        {
            ValueFormat = string.IsNullOrWhiteSpace(valueFormat) ? "0.##" : valueFormat;
        }

        internal string ValueFormat { get; }
    }

    internal sealed class DropdownControl : ModControl
    {
        internal DropdownControl(string id, string label,
            Func<IReadOnlyList<string>> getOptions, Func<int> getSelectedIndex,
            Action<int> setSelectedIndex) : base(id, label)
        {
            GetOptions = getOptions ?? (() => Array.Empty<string>());
            GetSelectedIndex = getSelectedIndex ?? (() => 0);
            SetSelectedIndex = setSelectedIndex ?? (_ => { });
        }

        internal Func<IReadOnlyList<string>> GetOptions { get; }
        internal Func<int> GetSelectedIndex { get; }
        internal Action<int> SetSelectedIndex { get; }
    }

    internal sealed class LabelControl : ModControl
    {
        internal LabelControl(string id, string text) : base(id, text) { }
    }

    internal sealed class SpacerControl : ModControl
    {
        internal SpacerControl(string id, float height) : base(id, string.Empty)
        {
            Height = Math.Max(0f, height);
        }

        internal float Height { get; }
    }
}

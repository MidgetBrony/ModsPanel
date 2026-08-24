using MelonLoader;
using SFB;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;
using SteamShelf;
using SteamShelf.Input;

namespace ModsPanel
{
    /// <summary>
    /// Builds the shared screen at runtime so ModsPanel does not require an asset
    /// bundle or a copied BOXROOM prefab. Only the existing ModsTab is used as the
    /// attachment point; registered mod controls live in a self-sizing ScrollRect.
    /// </summary>
    internal sealed class ModsPanelRuntime : MonoBehaviour
    {
        private static readonly Color Teal = new Color32(56, 116, 125, 255);
        private static readonly Color TealDark = new Color32(39, 83, 89, 255);
        private static readonly Color Paper = new Color32(240, 240, 237, 250);
        private static readonly Color Ink = new Color32(62, 135, 151, 255);
        private static readonly Color Field = new Color32(242, 242, 239, 255);
        private static readonly Color NativeToggleTeal = new Color32(59, 120, 129, 255);
        private static readonly Color NativeHeadingTeal = new Color32(56, 111, 118, 255);

        private readonly Dictionary<GameObject, bool> nativeChildStates =
            new Dictionary<GameObject, bool>();

        private RectTransform modsTab;
        private GameObject overlay;
        private Button modsTabButton;
        private Button settingsTabButton;
        private bool wasModsTabVisible;
        private int settingsFocusFrames;
        private RectTransform content;
        private TMP_Text fontTemplate;
        private bool installRequested;
        private bool rebuildRequested;
        private float nextInstallAttempt;

        internal static ModsPanelRuntime Instance { get; private set; }

        internal static ModsPanelRuntime Ensure()
        {
            if (Instance != null) return Instance;

            var host = new GameObject("ModsPanel Runtime");
            DontDestroyOnLoad(host);
            Instance = host.AddComponent<ModsPanelRuntime>();
            return Instance;
        }

        internal void RequestInstall()
        {
            installRequested = true;
            nextInstallAttempt = 0f;
        }

        internal void RequestRebuild()
        {
            rebuildRequested = true;
        }

        private void Update()
        {
            if (modsTab == null && installRequested && Time.unscaledTime >= nextInstallAttempt)
            {
                nextInstallAttempt = Time.unscaledTime + 1f;
                TryInstall();
            }

            if (rebuildRequested && content != null)
            {
                rebuildRequested = false;
                RebuildControls();
            }

            bool modsTabVisible = modsTab != null && modsTab.gameObject.activeInHierarchy;
            if (modsTabVisible && !wasModsTabVisible)
                FocusModsTabEntry();
            wasModsTabVisible = modsTabVisible;

            if (modsTabVisible)
            {
                if (settingsFocusFrames > 0)
                {
                    settingsFocusFrames--;
                    if (settingsFocusFrames == 0) FocusFirstSetting();
                }
            }
        }

        private void FocusModsTabEntry()
        {
            if (modsTabButton == null || EventSystem.current == null) return;
            GameObject selected = EventSystem.current.currentSelectedGameObject;
            if (selected == null || !selected.activeInHierarchy || !selected.transform.IsChildOf(modsTab))
                EventSystem.current.SetSelectedGameObject(modsTabButton.gameObject);
        }

        private void TryInstall()
        {
            foreach (RectTransform candidate in Resources.FindObjectsOfTypeAll<RectTransform>())
            {
                if (candidate == null ||
                    candidate.name != "ModsTab" ||
                    !candidate.gameObject.scene.IsValid())
                {
                    continue;
                }

                if (candidate.Find("ModsPanel Subtabs") != null)
                {
                    modsTab = candidate;
                    installRequested = false;
                    return;
                }

                Install(candidate);
                installRequested = false;
                return;
            }
        }

        private void Install(RectTransform target)
        {
            modsTab = target;
            fontTemplate = target.GetComponentInChildren<TMP_Text>(true);

            BuildSubtabs(target);

            overlay = CreateRect("ModsPanel Overlay", target).gameObject;
            // The native Mods page begins immediately below the narrow white strip
            // at the top of ModsTab. Start our alternate page at the same point.
            Stretch((RectTransform)overlay.transform, 18f, 18f, 58f, 18f);
            // Keep BOXROOM's own textured settings sheet visible. A nearly
            // transparent graphic still gives the overlay a raycast surface
            // without painting a flat rectangle over the native background.
            overlay.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.001f);
            Canvas overlayCanvas = overlay.AddComponent<Canvas>();
            overlayCanvas.overrideSorting = true;
            overlayCanvas.sortingOrder = 50;
            overlay.AddComponent<GraphicRaycaster>();

            BuildScrollArea((RectTransform)overlay.transform);
            overlay.SetActive(false);
            RebuildControls();
        }

        /// <summary>
        /// Adds a second level of navigation inside BOXROOM's existing Mods tab.
        /// The original Mods page remains the default and is never reparented or
        /// rebuilt; Mod Settings is simply a sibling page selected by this strip.
        /// </summary>
        private void BuildSubtabs(RectTransform parent)
        {
            RectTransform tabs = CreateRect("ModsPanel Subtabs", parent);
            tabs.anchorMin = new Vector2(0f, 1f);
            tabs.anchorMax = new Vector2(1f, 1f);
            tabs.pivot = new Vector2(0.5f, 1f);
            // ModsTab itself begins below BOXROOM's main navigation. A positive
            // local Y places these buttons in the unused white strip instead of
            // across the native "Share Current Room Template" title bar.
            tabs.anchoredPosition = new Vector2(0f, 32f);
            tabs.sizeDelta = new Vector2(0f, 46f);
            Canvas tabsCanvas = tabs.gameObject.AddComponent<Canvas>();
            tabsCanvas.overrideSorting = true;
            tabsCanvas.sortingOrder = 60;
            tabs.gameObject.AddComponent<GraphicRaycaster>();

            Button modsButton = CreateButton("Mods Tab", tabs, "Mods", ClosePanel);
            modsTabButton = modsButton;
            RectTransform modsRect = (RectTransform)modsButton.transform;
            modsRect.anchorMin = new Vector2(0f, 0.5f);
            modsRect.anchorMax = new Vector2(0f, 0.5f);
            modsRect.pivot = new Vector2(0f, 0.5f);
            modsRect.anchoredPosition = new Vector2(22f, 0f);
            modsRect.sizeDelta = new Vector2(205f, 42f);

            Button settingsButton = CreateButton(
                "Mod Settings Tab",
                tabs,
                "Mod Settings",
                OpenPanel);
            settingsTabButton = settingsButton;
            RectTransform settingsRect = (RectTransform)settingsButton.transform;
            settingsRect.anchorMin = new Vector2(0f, 0.5f);
            settingsRect.anchorMax = new Vector2(0f, 0.5f);
            settingsRect.pivot = new Vector2(0f, 0.5f);
            settingsRect.anchoredPosition = new Vector2(239f, 0f);
            settingsRect.sizeDelta = new Vector2(230f, 42f);

            BuildControllerLegend(parent);

            Navigation modsNavigation = modsButton.navigation;
            modsNavigation.mode = Navigation.Mode.Explicit;
            modsNavigation.selectOnRight = settingsButton;
            modsButton.navigation = modsNavigation;
            Navigation settingsNavigation = settingsButton.navigation;
            settingsNavigation.mode = Navigation.Mode.Explicit;
            settingsNavigation.selectOnLeft = modsButton;
            settingsButton.navigation = settingsNavigation;
        }

        private void BuildControllerLegend(RectTransform parent)
        {
            RectTransform legend = CreateRect("Controller Legend", parent);
            legend.anchorMin = new Vector2(0f, 0f);
            legend.anchorMax = new Vector2(1f, 0f);
            legend.pivot = new Vector2(0.5f, 0f);
            legend.anchoredPosition = new Vector2(0f, 10f);
            legend.sizeDelta = new Vector2(0f, 48f);

            float x = 22f;
            AddLegendItem(legend, FindLoadedSprite("Gamepad_Dpad_Left"), "", ref x);
            AddLegendItem(legend, FindLoadedSprite("Gamepad_Dpad_Right"), "CHOOSE PAGE", ref x);
            Sprite confirm = Singleton<InputManager>.HasInstance()
                ? Singleton<InputManager>.Instance.GetInputIconForAction("Confirm") : null;
            Sprite back = Singleton<InputManager>.HasInstance()
                ? Singleton<InputManager>.Instance.GetInputIconForAction("Back") : null;
            AddLegendItem(legend, confirm, "SELECT", ref x);
            AddLegendItem(legend, back, "BACK", ref x);
        }

        private void AddLegendItem(RectTransform parent, Sprite sprite, string caption, ref float x)
        {
            if (sprite != null)
            {
                RectTransform iconRect = CreateRect("Glyph", parent);
                iconRect.anchorMin = iconRect.anchorMax = new Vector2(0f, 0.5f);
                iconRect.pivot = new Vector2(0f, 0.5f);
                iconRect.anchoredPosition = new Vector2(x, 0f);
                iconRect.sizeDelta = new Vector2(38f, 38f);
                Image icon = iconRect.gameObject.AddComponent<Image>();
                icon.sprite = sprite;
                icon.preserveAspect = true;
                x += 44f;
            }
            if (!string.IsNullOrEmpty(caption))
            {
                TMP_Text label = CreateText("Legend Label", parent, caption, 22f, Ink);
                RectTransform labelRect = label.rectTransform;
                labelRect.anchorMin = labelRect.anchorMax = new Vector2(0f, 0.5f);
                labelRect.pivot = new Vector2(0f, 0.5f);
                labelRect.anchoredPosition = new Vector2(x, 0f);
                labelRect.sizeDelta = new Vector2(170f, 42f);
                label.alignment = TextAlignmentOptions.MidlineLeft;
                x += 180f;
            }
        }

        private void BuildScrollArea(RectTransform parent)
        {
            RectTransform scrollRoot = CreateRect("Settings Scroll", parent);
            Stretch(scrollRoot, 38f, 38f, 16f, 20f);
            ScrollRect scroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 36f;

            RectTransform viewport = CreateRect("Viewport", scrollRoot);
            Stretch(viewport, 0f, 28f, 0f, 0f);
            viewport.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.001f);
            viewport.gameObject.AddComponent<RectMask2D>();

            content = CreateRect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 10, 24);
            layout.spacing = 24f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Scrollbar scrollbar = BuildScrollbar(scrollRoot);
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            scroll.verticalScrollbarSpacing = 6f;
        }

        private Scrollbar BuildScrollbar(RectTransform parent)
        {
            RectTransform barRect = CreateRect("Scrollbar Vertical", parent);
            barRect.anchorMin = new Vector2(1f, 0f);
            barRect.anchorMax = new Vector2(1f, 1f);
            barRect.pivot = new Vector2(1f, 0.5f);
            barRect.anchoredPosition = Vector2.zero;
            barRect.sizeDelta = new Vector2(20f, 0f);
            barRect.gameObject.AddComponent<Image>().color = new Color(TealDark.r, TealDark.g, TealDark.b, 0.18f);

            Scrollbar bar = barRect.gameObject.AddComponent<Scrollbar>();
            bar.direction = Scrollbar.Direction.BottomToTop;

            RectTransform sliding = CreateRect("Sliding Area", barRect);
            Stretch(sliding, 3f, 3f, 3f, 3f);
            RectTransform handle = CreateRect("Handle", sliding);
            Stretch(handle, 0f, 0f, 0f, 0f);
            Image handleImage = handle.gameObject.AddComponent<Image>();
            handleImage.color = Teal;
            bar.handleRect = handle;
            bar.targetGraphic = handleImage;
            return bar;
        }

        private void OpenPanel()
        {
            if (overlay != null && overlay.activeSelf)
                return;

            nativeChildStates.Clear();
            foreach (Transform child in modsTab)
            {
                GameObject childObject = child.gameObject;
                if (childObject == overlay || childObject.name == "ModsPanel Subtabs")
                    continue;

                nativeChildStates[childObject] = childObject.activeSelf;
                childObject.SetActive(false);
            }

            overlay.SetActive(true);
            overlay.transform.SetAsLastSibling();
            RebuildControls();
            Canvas.ForceUpdateCanvases();
            settingsFocusFrames = 2;
        }

        private void ClosePanel()
        {
            if (overlay != null)
                overlay.SetActive(false);

            foreach (KeyValuePair<GameObject, bool> state in nativeChildStates)
            {
                if (state.Key != null)
                    state.Key.SetActive(state.Value);
            }
            nativeChildStates.Clear();
            if (modsTabButton != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(modsTabButton.gameObject);
        }

        private void FocusFirstSetting()
        {
            if (content == null || EventSystem.current == null) return;
            Selectable first = content.GetComponentsInChildren<Selectable>(false)
                .FirstOrDefault(item => item != null && item.IsInteractable());
            if (first != null) EventSystem.current.SetSelectedGameObject(first.gameObject);
        }

        private void RebuildControls()
        {
            if (content == null) return;

            for (int index = content.childCount - 1; index >= 0; index--)
                Destroy(content.GetChild(index).gameObject);

            IReadOnlyList<ModSection> sections = ModsPanelApi.Snapshot();
            if (sections.Count == 0)
            {
                TMP_Text empty = CreateText(
                    "Empty",
                    content,
                    "No mods have registered settings yet.",
                    34f,
                    Ink);
                empty.alignment = TextAlignmentOptions.Center;
                empty.gameObject.AddComponent<LayoutElement>().preferredHeight = 130f;
                return;
            }

            foreach (ModSection section in sections)
                BuildSection(section);

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        }

        private void BuildSection(ModSection section)
        {
            RectTransform card = CreateRect($"Section {section.OwnerId}", content);
            Image frame = card.gameObject.AddComponent<Image>();
            frame.color = Teal;
            frame.sprite = FindLoadedSprite("SquareRounded_Border");
            frame.type = Image.Type.Sliced;
            frame.pixelsPerUnitMultiplier = 1f;

            float preferredHeight = 90f;
            foreach (ModControl control in section.Controls)
            {
                preferredHeight += GetControlHeight(control);
            }
            card.gameObject.AddComponent<LayoutElement>().preferredHeight = preferredHeight;

            var cardLayout = card.gameObject.AddComponent<VerticalLayoutGroup>();
            cardLayout.padding = new RectOffset(0, 0, 0, 18);
            cardLayout.spacing = 0f;
            cardLayout.childControlWidth = true;
            cardLayout.childControlHeight = true;
            cardLayout.childForceExpandWidth = true;
            cardLayout.childForceExpandHeight = false;

            // Keep background and text on separate objects. Unity UI allows only
            // one primary Graphic per object; putting Image beside TMP_Text caused
            // the heading background to disappear and destabilized preferred-size
            // calculation for the rows below it.
            RectTransform headingRoot = CreateRect("Heading", card);
            Image headingImage = headingRoot.gameObject.AddComponent<Image>();
            headingImage.color = NativeHeadingTeal;
            headingImage.sprite = FindLoadedSprite("SquareRounded_TopOnly");
            headingImage.type = Image.Type.Sliced;
            headingImage.pixelsPerUnitMultiplier = 1.5f;
            headingRoot.gameObject.AddComponent<LayoutElement>().preferredHeight = 72f;
            TMP_Text heading = CreateText("Text", headingRoot, section.Title, 42f, Color.white);
            Stretch(heading.rectTransform, 24f, 12f, 0f, 0f);
            heading.alignment = TextAlignmentOptions.MidlineLeft;

            foreach (ModControl control in section.Controls)
            {
                try
                {
                    if (control is FolderControl folder) BuildFolderControl(card, folder);
                    else if (control is ButtonControl button) BuildButtonControl(card, button);
                    else if (control is ToggleControl toggle) BuildToggleControl(card, toggle);
                    else if (control is SliderControl slider) BuildSliderControl(card, slider);
                    else if (control is NumberControl number) BuildNumberControl(card, number);
                    else if (control is TextControl text) BuildTextControl(card, text);
                    else if (control is DropdownControl dropdown) BuildDropdownControl(card, dropdown);
                    else if (control is LabelControl label) BuildLabelControl(card, label);
                    else if (control is SpacerControl spacer) CreateRow(card, spacer.Id, spacer.Height);
                }
                catch (Exception exception)
                {
                    MelonLogger.Error($"ModsPanel could not build {section.OwnerId}/{control.Id}: {exception}");
                }
            }
        }

        private void BuildFolderControl(RectTransform parent, FolderControl control)
        {
            RectTransform row = CreateRow(parent, control.Id, 154f);

            TMP_Text label = CreateText("Label", row, control.Label, 38f, Ink);
            PlaceTop(label.rectTransform, 0f, 0.2f, 16f, 58f, 18f, 8f);
            label.alignment = TextAlignmentOptions.MidlineLeft;

            Image field = CreateRect("Path", row).gameObject.AddComponent<Image>();
            field.color = Teal;
            field.sprite = FindLoadedSprite("SquareRounded_Border");
            field.type = Image.Type.Sliced;
            field.pixelsPerUnitMultiplier = 3.5f;
            PlaceTop(field.rectTransform, 0.2f, 0.76f, 14f, 64f, 8f, 8f);

            TMP_Text pathText = CreateText(
                "Value",
                field.rectTransform,
                SafeInvoke(control.GetValue, string.Empty),
                40f,
                new Color32(91, 116, 122, 255));
            Stretch(pathText.rectTransform, 14f, 14f, 4f, 4f);
            pathText.alignment = TextAlignmentOptions.MidlineLeft;
            pathText.enableAutoSizing = true;
            pathText.fontSizeMin = 22f;
            pathText.fontSizeMax = 40f;
            pathText.overflowMode = TextOverflowModes.Ellipsis;

            TMP_Text status = CreateText(
                "Status",
                row,
                control.GetStatus == null ? string.Empty : SafeInvoke(control.GetStatus, string.Empty),
                38f,
                Ink);
            PlaceTop(status.rectTransform, 0.2f, 0.61f, 84f, 48f, 8f, 8f);
            status.alignment = TextAlignmentOptions.MidlineLeft;

            Button browse = CreateButton("Browse", row, "Browse", () =>
            {
                string current = SafeInvoke(control.GetValue, string.Empty);
                string[] selected = StandaloneFileBrowser.OpenFolderPanel(
                    control.BrowseTitle,
                    current ?? string.Empty,
                    false);

                if (selected == null || selected.Length == 0 || string.IsNullOrWhiteSpace(selected[0]))
                    return;

                control.SetValue(selected[0]);
                pathText.text = SafeInvoke(control.GetValue, selected[0]);
                status.text = control.GetStatus == null
                    ? string.Empty
                    : SafeInvoke(control.GetStatus, string.Empty);
            });
            PlaceTop((RectTransform)browse.transform, 0.8f, 1f, 14f, 64f, 8f, 28f);

            if (control.Refresh != null)
            {
                Button refresh = CreateButton("Refresh", row, "Refresh", () =>
                {
                    control.Refresh();
                    pathText.text = SafeInvoke(control.GetValue, string.Empty);
                    status.text = control.GetStatus == null
                        ? string.Empty
                        : SafeInvoke(control.GetStatus, string.Empty);
                });
                PlaceTop((RectTransform)refresh.transform, 0.62f, 0.79f, 84f, 54f, 8f, 8f);
            }
        }

        private void BuildButtonControl(RectTransform parent, ButtonControl control)
        {
            RectTransform row = CreateRow(parent, control.Id, 92f);
            TMP_Text label = CreateText("Label", row, control.Label, 38f, Ink);
            PlaceTop(label.rectTransform, 0f, 0.7f, 10f, 64f, 18f, 8f);
            label.alignment = TextAlignmentOptions.MidlineLeft;

            Button button = CreateButton("Action", row, control.ButtonText, control.Pressed);
            PlaceTop((RectTransform)button.transform, 0.72f, 1f, 12f, 58f, 8f, 28f);
        }

        private void BuildToggleControl(RectTransform parent, ToggleControl control)
        {
            RectTransform row = CreateRow(parent, control.Id, 92f);
            TMP_Text label = CreateText("Label", row, control.Label, 38f, Ink);
            PlaceTop(label.rectTransform, 0f, 0.82f, 10f, 64f, 18f, 8f);
            label.alignment = TextAlignmentOptions.MidlineLeft;

            RectTransform toggleRect = CreateRect("Toggle", row);
            toggleRect.anchorMin = new Vector2(1f, 0.5f);
            toggleRect.anchorMax = new Vector2(1f, 0.5f);
            toggleRect.pivot = new Vector2(0.5f, 0.5f);
            toggleRect.anchoredPosition = new Vector2(-58f, 0f);
            toggleRect.sizeDelta = new Vector2(62.0468f, 62.0468f);

            // Match BOXROOM's SettingsBoolDisplay hierarchy. The toolbar-frame
            // sprite supplies the rounded/sliced border and the native tick is
            // inset by 9.7172 pixels on every side.
            RectTransform backgroundRect = CreateRect("Background", toggleRect);
            Stretch(backgroundRect, 0f, 0f, 0f, 0f);
            Image background = backgroundRect.gameObject.AddComponent<Image>();
            background.color = NativeToggleTeal;
            background.sprite = FindLoadedSprite("UI_toolbarFrame");
            background.type = Image.Type.Sliced;
            background.pixelsPerUnitMultiplier = 2f;

            RectTransform checkRect = CreateRect("Checkmark", backgroundRect);
            Stretch(checkRect, 9.7172f, 9.7172f, 9.7172f, 9.7172f);
            Image check = checkRect.gameObject.AddComponent<Image>();
            check.color = Color.white;
            check.sprite = FindLoadedSprite("UI_WhiteTick");

            Toggle toggle = toggleRect.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = background;
            toggle.graphic = check;
            toggle.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = toggle.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.5377358f, 0.5377358f, 0.5377358f, 1f);
            colors.pressedColor = new Color(0.2924528f, 0.2924528f, 0.2924528f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.1981132f, 0.1981132f, 0.1981132f, 0.5019608f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.1f;
            toggle.colors = colors;
            toggle.isOn = SafeInvoke(control.GetValue, false);
            toggle.onValueChanged.AddListener(new UnityAction<bool>(control.SetValue));
        }

        private static Sprite FindLoadedSprite(string spriteName)
        {
            foreach (Sprite sprite in Resources.FindObjectsOfTypeAll<Sprite>())
            {
                if (sprite != null && sprite.name == spriteName)
                    return sprite;
            }

            MelonLogger.Warning($"ModsPanel could not find BOXROOM sprite '{spriteName}'.");
            return null;
        }

        private void BuildTextControl(RectTransform parent, TextControl control)
        {
            RectTransform row = CreateRow(parent, control.Id, 92f);
            BuildRowLabel(row, control.Label, 0.38f);
            TMP_InputField input = CreateInputField(row, SafeInvoke(control.GetValue, string.Empty),
                control.Placeholder, TMP_InputField.ContentType.Standard);
            PlaceTop((RectTransform)input.transform, 0.4f, 1f, 12f, 58f, 8f, 18f);
            input.onEndEdit.AddListener(new UnityAction<string>(value => SafeInvoke(() => control.SetValue(value))));
        }

        private void BuildNumberControl(RectTransform parent, NumberControl control)
        {
            RectTransform row = CreateRow(parent, control.Id, 92f);
            BuildRowLabel(row, control.Label, 0.68f);
            float value = Mathf.Clamp(SafeInvoke(control.GetValue, 0f), control.Minimum, control.Maximum);
            TMP_InputField input = CreateInputField(row, FormatNumber(value, control.WholeNumbers),
                string.Empty, control.WholeNumbers
                    ? TMP_InputField.ContentType.IntegerNumber
                    : TMP_InputField.ContentType.DecimalNumber);
            PlaceTop((RectTransform)input.transform, 0.7f, 1f, 12f, 58f, 8f, 18f);
            input.onEndEdit.AddListener(new UnityAction<string>(text =>
            {
                if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
                {
                    input.text = FormatNumber(SafeInvoke(control.GetValue, value), control.WholeNumbers);
                    return;
                }

                parsed = Mathf.Clamp(parsed, control.Minimum, control.Maximum);
                if (control.WholeNumbers) parsed = Mathf.Round(parsed);
                SafeInvoke(() => control.SetValue(parsed));
                input.text = FormatNumber(parsed, control.WholeNumbers);
            }));
        }

        private void BuildSliderControl(RectTransform parent, SliderControl control)
        {
            RectTransform row = CreateRow(parent, control.Id, 92f);
            BuildRowLabel(row, control.Label, 0.36f);

            TMP_Text valueText = CreateText("Value", row, string.Empty, 27f, Ink);
            PlaceTop(valueText.rectTransform, 0.36f, 0.48f, 12f, 58f, 5f, 5f);
            valueText.alignment = TextAlignmentOptions.Center;

            RectTransform sliderRect = CreateRect("Slider", row);
            PlaceTop(sliderRect, 0.49f, 1f, 22f, 38f, 8f, 18f);
            Slider slider = BuildSlider(sliderRect);
            slider.minValue = control.Minimum;
            slider.maxValue = control.Maximum;
            slider.wholeNumbers = control.WholeNumbers;
            slider.value = Mathf.Clamp(SafeInvoke(control.GetValue, control.Minimum),
                control.Minimum, control.Maximum);
            valueText.text = slider.value.ToString(control.ValueFormat, CultureInfo.InvariantCulture);
            slider.onValueChanged.AddListener(new UnityAction<float>(value =>
            {
                valueText.text = value.ToString(control.ValueFormat, CultureInfo.InvariantCulture);
                SafeInvoke(() => control.SetValue(value));
            }));
        }

        private void BuildDropdownControl(RectTransform parent, DropdownControl control)
        {
            RectTransform row = CreateRow(parent, control.Id, 92f);
            BuildRowLabel(row, control.Label, 0.48f);
            RectTransform root = CreateRect("Dropdown", row);
            PlaceTop(root, 0.5f, 1f, 12f, 58f, 8f, 18f);
            TMP_Dropdown dropdown = BuildDropdown(root);
            IReadOnlyList<string> options = SafeInvoke(control.GetOptions, Array.Empty<string>());
            dropdown.ClearOptions();
            dropdown.AddOptions(new List<string>(options ?? Array.Empty<string>()));
            dropdown.value = Mathf.Clamp(SafeInvoke(control.GetSelectedIndex, 0), 0,
                Math.Max(0, dropdown.options.Count - 1));
            dropdown.RefreshShownValue();
            dropdown.onValueChanged.AddListener(
                new UnityAction<int>(value => SafeInvoke(() => control.SetSelectedIndex(value))));
        }

        private void BuildLabelControl(RectTransform parent, LabelControl control)
        {
            RectTransform row = CreateRow(parent, control.Id, 68f);
            TMP_Text text = CreateText("Text", row, control.Label, 31f, new Color32(38, 61, 66, 255));
            Stretch(text.rectTransform, 18f, 18f, 4f, 4f);
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.textWrappingMode = TextWrappingModes.Normal;
        }

        private void BuildRowLabel(RectTransform row, string label, float anchorMaxX)
        {
            TMP_Text text = CreateText("Label", row, label, 38f, Ink);
            PlaceTop(text.rectTransform, 0f, anchorMaxX, 10f, 64f, 18f, 8f);
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.enableAutoSizing = true;
            text.fontSizeMin = 20f;
            text.fontSizeMax = 31f;
        }

        private TMP_InputField CreateInputField(Transform parent, string value,
            string placeholder, TMP_InputField.ContentType contentType)
        {
            RectTransform root = CreateRect("Input", parent);
            Image image = root.gameObject.AddComponent<Image>();
            image.color = Teal;
            image.sprite = FindLoadedSprite("SquareRounded_Border");
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 3.5f;

            TMP_Text text = CreateText("Text Area", root, value, 38f, Ink);
            Stretch(text.rectTransform, 14f, 14f, 4f, 4f);
            text.alignment = TextAlignmentOptions.MidlineLeft;

            TMP_Text hint = CreateText("Placeholder", root, placeholder, 38f,
                new Color(Ink.r, Ink.g, Ink.b, 0.45f));
            Stretch(hint.rectTransform, 14f, 14f, 4f, 4f);
            hint.fontStyle = FontStyles.Italic;
            hint.alignment = TextAlignmentOptions.MidlineLeft;

            TMP_InputField input = root.gameObject.AddComponent<TMP_InputField>();
            input.textViewport = root;
            input.textComponent = text;
            input.placeholder = hint;
            input.contentType = contentType;
            input.text = value ?? string.Empty;
            return input;
        }

        private Slider BuildSlider(RectTransform root)
        {
            Image background = root.gameObject.AddComponent<Image>();
            background.color = TealDark;
            RectTransform fillArea = CreateRect("Fill Area", root);
            Stretch(fillArea, 4f, 4f, 4f, 4f);
            RectTransform fill = CreateRect("Fill", fillArea);
            Stretch(fill, 0f, 0f, 0f, 0f);
            fill.gameObject.AddComponent<Image>().color = Teal;
            RectTransform handleArea = CreateRect("Handle Slide Area", root);
            Stretch(handleArea, 10f, 10f, -8f, -8f);
            RectTransform handle = CreateRect("Handle", handleArea);
            handle.sizeDelta = new Vector2(24f, 52f);
            handle.gameObject.AddComponent<Image>().color = Color.white;
            Slider slider = root.gameObject.AddComponent<Slider>();
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.direction = Slider.Direction.LeftToRight;
            return slider;
        }

        private TMP_Dropdown BuildDropdown(RectTransform root)
        {
            Image dropdownImage = root.gameObject.AddComponent<Image>();
            dropdownImage.color = Teal;
            dropdownImage.sprite = FindLoadedSprite("SquareRounded_Filled");
            dropdownImage.type = Image.Type.Sliced;
            dropdownImage.pixelsPerUnitMultiplier = 3.5f;
            TMP_Text caption = CreateText("Label", root, string.Empty, 27f, Color.white);
            Stretch(caption.rectTransform, 14f, 38f, 4f, 4f);
            caption.alignment = TextAlignmentOptions.MidlineLeft;
            TMP_Text arrow = CreateText("Arrow", root, "▼", 22f, Color.white);
            arrow.rectTransform.anchorMin = new Vector2(1f, 0f);
            arrow.rectTransform.anchorMax = Vector2.one;
            arrow.rectTransform.offsetMin = new Vector2(-34f, 0f);
            arrow.rectTransform.offsetMax = Vector2.zero;
            arrow.alignment = TextAlignmentOptions.Center;

            RectTransform template = CreateRect("Template", root);
            template.anchorMin = new Vector2(0f, 0f);
            template.anchorMax = new Vector2(1f, 0f);
            template.pivot = new Vector2(0.5f, 1f);
            template.anchoredPosition = Vector2.zero;
            template.sizeDelta = new Vector2(0f, 220f);
            template.gameObject.AddComponent<Image>().color = Paper;
            ScrollRect scroll = template.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            RectTransform viewport = CreateRect("Viewport", template);
            Stretch(viewport, 0f, 0f, 0f, 0f);
            viewport.gameObject.AddComponent<RectMask2D>();
            RectTransform contentRoot = CreateRect("Content", viewport);
            contentRoot.anchorMin = new Vector2(0f, 1f);
            contentRoot.anchorMax = new Vector2(1f, 1f);
            contentRoot.pivot = new Vector2(0.5f, 1f);
            contentRoot.sizeDelta = new Vector2(0f, 46f);
            RectTransform item = CreateRect("Item", contentRoot);
            item.anchorMin = new Vector2(0f, 1f);
            item.anchorMax = new Vector2(1f, 1f);
            item.pivot = new Vector2(0.5f, 1f);
            item.sizeDelta = new Vector2(0f, 46f);
            Toggle toggle = item.gameObject.AddComponent<Toggle>();
            item.gameObject.AddComponent<Image>().color = Field;
            TMP_Text itemLabel = CreateText("Item Label", item, "Option", 25f, Ink);
            Stretch(itemLabel.rectTransform, 12f, 12f, 2f, 2f);
            itemLabel.alignment = TextAlignmentOptions.MidlineLeft;
            toggle.targetGraphic = item.GetComponent<Image>();
            scroll.viewport = viewport;
            scroll.content = contentRoot;

            TMP_Dropdown dropdown = root.gameObject.AddComponent<TMP_Dropdown>();
            dropdown.targetGraphic = root.GetComponent<Image>();
            dropdown.template = template;
            dropdown.captionText = caption;
            dropdown.itemText = itemLabel;
            template.gameObject.SetActive(false);
            return dropdown;
        }

        private static float GetControlHeight(ModControl control)
        {
            if (control is FolderControl) return 154f;
            if (control is SpacerControl spacer) return spacer.Height;
            if (control is LabelControl) return 68f;
            return 92f;
        }

        private static string FormatNumber(float value, bool wholeNumbers) =>
            value.ToString(wholeNumbers ? "0" : "0.###", CultureInfo.InvariantCulture);

        private static void SafeInvoke(Action callback)
        {
            try { callback?.Invoke(); }
            catch (Exception exception)
            {
                MelonLogger.Error($"ModsPanel setting callback failed: {exception}");
            }
        }

        private RectTransform CreateRow(RectTransform parent, string name, float height)
        {
            RectTransform row = CreateRect($"Control {name}", parent);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
            return row;
        }

        private Button CreateButton(string name, Transform parent, string text, Action pressed)
        {
            RectTransform rect = CreateRect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = Teal;
            image.sprite = FindLoadedSprite("SquareRounded_Filled");
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 3.5f;

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
            colors.selectedColor = new Color(0.55f, 1.35f, 1.35f, 1f);
            colors.pressedColor = new Color(0.72f, 0.72f, 0.72f, 1f);
            colors.colorMultiplier = 1.25f;
            button.colors = colors;
            button.onClick.AddListener(new UnityAction(pressed));

            TMP_Text label = CreateText("Text", rect, text, 36f, Color.white);
            Stretch(label.rectTransform, 6f, 6f, 3f, 3f);
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            label.fontSizeMin = 18f;
            label.fontSizeMax = 36f;
            return button;
        }

        private TMP_Text CreateText(
            string name,
            Transform parent,
            string value,
            float size,
            Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value ?? string.Empty;
            text.fontSize = size;
            text.color = color;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;

            if (fontTemplate != null)
            {
                text.font = fontTemplate.font;
                text.fontSharedMaterial = fontTemplate.fontSharedMaterial;
            }

            return text;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.layer = parent.gameObject.layer;
            RectTransform rect = (RectTransform)gameObject.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static void Stretch(
            RectTransform rect,
            float left,
            float right,
            float top,
            float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        /// <summary>
        /// Places a fixed-height control relative to the top edge of a row while
        /// allowing its width to stretch between fractional horizontal anchors.
        /// This avoids the negative offset convention that previously expanded
        /// controls outside their rows and left the section apparently empty.
        /// </summary>
        private static void PlaceTop(
            RectTransform rect,
            float anchorMinX,
            float anchorMaxX,
            float top,
            float height,
            float left,
            float right)
        {
            rect.anchorMin = new Vector2(anchorMinX, 1f);
            rect.anchorMax = new Vector2(anchorMaxX, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2((left - right) * 0.5f, -top);
            rect.sizeDelta = new Vector2(-(left + right), height);
        }

        private static T SafeInvoke<T>(Func<T> callback, T fallback)
        {
            try { return callback != null ? callback() : fallback; }
            catch (Exception exception)
            {
                MelonLogger.Error($"ModsPanel setting callback failed: {exception}");
                return fallback;
            }
        }
    }
}

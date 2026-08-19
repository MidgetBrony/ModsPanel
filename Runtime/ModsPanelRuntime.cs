using MelonLoader;
using SFB;
using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ModsPanel
{
    /// <summary>
    /// Builds the shared screen at runtime so ModsPanel does not require an asset
    /// bundle or a copied BOXROOM prefab. Only the existing ModsTab is used as the
    /// attachment point; registered mod controls live in a self-sizing ScrollRect.
    /// </summary>
    internal sealed class ModsPanelRuntime : MonoBehaviour
    {
        private static readonly Color Teal = new Color32(61, 126, 136, 255);
        private static readonly Color TealDark = new Color32(39, 83, 89, 255);
        private static readonly Color Paper = new Color32(235, 235, 231, 250);
        private static readonly Color Ink = new Color32(48, 74, 78, 255);
        private static readonly Color Field = new Color32(242, 242, 239, 255);

        private readonly Dictionary<GameObject, bool> nativeChildStates =
            new Dictionary<GameObject, bool>();

        private RectTransform modsTab;
        private GameObject overlay;
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
            overlay.AddComponent<Image>().color = Paper;
            Canvas overlayCanvas = overlay.AddComponent<Canvas>();
            overlayCanvas.overrideSorting = true;
            overlayCanvas.sortingOrder = 50;
            overlay.AddComponent<GraphicRaycaster>();

            BuildHeader((RectTransform)overlay.transform);
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
            RectTransform settingsRect = (RectTransform)settingsButton.transform;
            settingsRect.anchorMin = new Vector2(0f, 0.5f);
            settingsRect.anchorMax = new Vector2(0f, 0.5f);
            settingsRect.pivot = new Vector2(0f, 0.5f);
            settingsRect.anchoredPosition = new Vector2(239f, 0f);
            settingsRect.sizeDelta = new Vector2(230f, 42f);
        }

        private void BuildHeader(RectTransform parent)
        {
            RectTransform header = CreateRect("Header", parent);
            header.anchorMin = new Vector2(0f, 1f);
            header.anchorMax = new Vector2(1f, 1f);
            header.pivot = new Vector2(0.5f, 1f);
            header.anchoredPosition = Vector2.zero;
            header.sizeDelta = new Vector2(0f, 90f);
            header.gameObject.AddComponent<Image>().color = Teal;

            TMP_Text title = CreateText("Title", header, "Registered Mod Settings", 42f, Color.white);
            Stretch((RectTransform)title.transform, 28f, 28f, 8f, 8f);
            title.alignment = TextAlignmentOptions.MidlineLeft;
        }

        private void BuildScrollArea(RectTransform parent)
        {
            RectTransform scrollRoot = CreateRect("Settings Scroll", parent);
            Stretch(scrollRoot, 20f, 20f, 105f, 20f);
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
            layout.padding = new RectOffset(10, 10, 10, 18);
            layout.spacing = 18f;
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
            card.gameObject.AddComponent<Image>().color = new Color32(248, 248, 245, 255);

            float preferredHeight = 86f;
            foreach (ModControl control in section.Controls)
            {
                preferredHeight += GetControlHeight(control) + 10f;
            }
            card.gameObject.AddComponent<LayoutElement>().preferredHeight = preferredHeight;

            var cardLayout = card.gameObject.AddComponent<VerticalLayoutGroup>();
            cardLayout.padding = new RectOffset(0, 0, 0, 18);
            cardLayout.spacing = 10f;
            cardLayout.childControlWidth = true;
            cardLayout.childControlHeight = true;
            cardLayout.childForceExpandWidth = true;
            cardLayout.childForceExpandHeight = false;

            // Keep background and text on separate objects. Unity UI allows only
            // one primary Graphic per object; putting Image beside TMP_Text caused
            // the heading background to disappear and destabilized preferred-size
            // calculation for the rows below it.
            RectTransform headingRoot = CreateRect("Heading", card);
            headingRoot.gameObject.AddComponent<Image>().color = Teal;
            headingRoot.gameObject.AddComponent<LayoutElement>().preferredHeight = 68f;
            TMP_Text heading = CreateText("Text", headingRoot, section.Title, 38f, Color.white);
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

            TMP_Text label = CreateText("Label", row, control.Label, 31f, Ink);
            PlaceTop(label.rectTransform, 0f, 1f, 4f, 38f, 18f, 18f);
            label.alignment = TextAlignmentOptions.MidlineLeft;

            Image field = CreateRect("Path", row).gameObject.AddComponent<Image>();
            field.color = Field;
            PlaceTop(field.rectTransform, 0f, 0.56f, 44f, 52f, 18f, 8f);
            Outline outline = field.gameObject.AddComponent<Outline>();
            outline.effectColor = Teal;
            outline.effectDistance = new Vector2(2f, -2f);

            TMP_Text pathText = CreateText(
                "Value",
                field.rectTransform,
                SafeInvoke(control.GetValue, string.Empty),
                25f,
                Ink);
            Stretch(pathText.rectTransform, 14f, 14f, 4f, 4f);
            pathText.alignment = TextAlignmentOptions.MidlineLeft;
            pathText.enableAutoSizing = true;
            pathText.fontSizeMin = 15f;
            pathText.fontSizeMax = 25f;
            pathText.overflowMode = TextOverflowModes.Ellipsis;

            TMP_Text status = CreateText(
                "Status",
                row,
                control.GetStatus == null ? string.Empty : SafeInvoke(control.GetStatus, string.Empty),
                25f,
                Teal);
            PlaceTop(status.rectTransform, 0f, 1f, 100f, 36f, 18f, 18f);
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
            PlaceTop((RectTransform)browse.transform, 0.57f, 0.78f, 44f, 52f, 8f, 8f);

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
                PlaceTop((RectTransform)refresh.transform, 0.79f, 1f, 44f, 52f, 8f, 18f);
            }
        }

        private void BuildButtonControl(RectTransform parent, ButtonControl control)
        {
            RectTransform row = CreateRow(parent, control.Id, 92f);
            TMP_Text label = CreateText("Label", row, control.Label, 31f, Ink);
            PlaceTop(label.rectTransform, 0f, 0.7f, 10f, 64f, 18f, 8f);
            label.alignment = TextAlignmentOptions.MidlineLeft;

            Button button = CreateButton("Action", row, control.ButtonText, control.Pressed);
            PlaceTop((RectTransform)button.transform, 0.72f, 1f, 12f, 58f, 8f, 18f);
        }

        private void BuildToggleControl(RectTransform parent, ToggleControl control)
        {
            RectTransform row = CreateRow(parent, control.Id, 92f);
            TMP_Text label = CreateText("Label", row, control.Label, 31f, Ink);
            PlaceTop(label.rectTransform, 0f, 0.82f, 10f, 64f, 18f, 8f);
            label.alignment = TextAlignmentOptions.MidlineLeft;

            RectTransform toggleRect = CreateRect("Toggle", row);
            toggleRect.anchorMin = new Vector2(1f, 0.5f);
            toggleRect.anchorMax = new Vector2(1f, 0.5f);
            toggleRect.pivot = new Vector2(1f, 0.5f);
            toggleRect.anchoredPosition = new Vector2(-22f, 0f);
            toggleRect.sizeDelta = new Vector2(62f, 62f);
            Image background = toggleRect.gameObject.AddComponent<Image>();
            background.color = TealDark;

            RectTransform checkRect = CreateRect("Checkmark", toggleRect);
            Stretch(checkRect, 9f, 9f, 9f, 9f);
            Image check = checkRect.gameObject.AddComponent<Image>();
            check.color = Color.white;

            Toggle toggle = toggleRect.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = background;
            toggle.graphic = check;
            toggle.isOn = SafeInvoke(control.GetValue, false);
            toggle.onValueChanged.AddListener(new UnityAction<bool>(control.SetValue));
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
            TMP_Text text = CreateText("Text", row, control.Label, 27f, Ink);
            Stretch(text.rectTransform, 18f, 18f, 4f, 4f);
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.textWrappingMode = TextWrappingModes.Normal;
        }

        private void BuildRowLabel(RectTransform row, string label, float anchorMaxX)
        {
            TMP_Text text = CreateText("Label", row, label, 31f, Ink);
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
            image.color = Field;
            Outline outline = root.gameObject.AddComponent<Outline>();
            outline.effectColor = Teal;
            outline.effectDistance = new Vector2(2f, -2f);

            TMP_Text text = CreateText("Text Area", root, value, 27f, Ink);
            Stretch(text.rectTransform, 14f, 14f, 4f, 4f);
            text.alignment = TextAlignmentOptions.MidlineLeft;

            TMP_Text hint = CreateText("Placeholder", root, placeholder, 27f,
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
            root.gameObject.AddComponent<Image>().color = Teal;
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

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
            colors.pressedColor = new Color(0.72f, 0.72f, 0.72f, 1f);
            button.colors = colors;
            button.onClick.AddListener(new UnityAction(pressed));

            TMP_Text label = CreateText("Text", rect, text, 31f, Color.white);
            Stretch(label.rectTransform, 6f, 6f, 3f, 3f);
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            label.fontSizeMin = 18f;
            label.fontSizeMax = 31f;
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

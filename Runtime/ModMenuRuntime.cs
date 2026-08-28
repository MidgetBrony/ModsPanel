using MelonLoader;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;
using SteamShelf;
using SteamShelf.Input;

namespace ModsPanel
{
    /// <summary>Renders the public ModsUi modal menu definitions.</summary>
    internal sealed class ModMenuRuntime : MonoBehaviour
    {
        private static readonly Color Ink = new Color32(32, 61, 64, 255);
        private static readonly Color DeepBlue = new Color32(33, 77, 90, 255);
        private static readonly Color Blue = new Color32(60, 118, 124, 255);
        private static readonly Color Paper = new Color32(243, 233, 233, 255);
        private static readonly Color Red = new Color32(188, 86, 78, 255);

        private GameObject root;
        private GameObject toastRoot;
        private float toastUntil;
        private RectTransform content;
        private TMP_FontAsset font;
        private ModMenu openMenu;
        private CursorLockMode previousLockMode;
        private bool previousCursorVisible;
        private ScrollRect activeScroll;
        private RectTransform activeViewport;
        private GameObject lastSelected;

        internal static ModMenuRuntime Instance { get; private set; }
        internal static bool HasOpenMenu => Instance != null && Instance.openMenu != null;
        internal static bool IsOpen(ModMenu menu) => Instance != null && Instance.openMenu == menu;

        internal static ModMenuRuntime Ensure()
        {
            if (Instance != null) return Instance;
            GameObject host = new GameObject("ModsPanel Menu Runtime");
            DontDestroyOnLoad(host);
            Instance = host.AddComponent<ModMenuRuntime>();
            return Instance;
        }

        internal void ShowMenu(ModMenu menu)
        {
            if (menu == null) return;

            bool wasClosed = openMenu == null;
            openMenu = menu;
            if (wasClosed)
            {
                previousLockMode = Cursor.lockState;
                previousCursorVisible = Cursor.visible;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (Singleton<InputManager>.HasInstance())
                Singleton<InputManager>.Instance.SwapToInputMap(EInputMap.UI);
            Build(menu);
        }

        internal void CloseMenu(ModMenu expected = null)
        {
            if (openMenu == null || (expected != null && openMenu != expected)) return;
            ModMenu closing = openMenu;
            openMenu = null;
            if (root != null) Destroy(root);
            root = null;
            content = null;
            Cursor.lockState = previousLockMode;
            Cursor.visible = previousCursorVisible;
            if (Singleton<InputManager>.HasInstance())
                Singleton<InputManager>.Instance.SwapToInputMap(EInputMap.Player);
            SafeInvoke(closing.Closed);
        }

        internal void ShowToast(string message, float seconds)
        {
            if (toastRoot != null) Destroy(toastRoot);
            toastUntil = Time.realtimeSinceStartup + Mathf.Max(0.5f, seconds);

            toastRoot = new GameObject("ModsPanel Toast", typeof(RectTransform));
            toastRoot.transform.SetParent(transform, false);
            Canvas canvas = toastRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32001;
            CanvasScaler scaler = toastRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform frame = Rect("Toast Frame", toastRoot.transform);
            frame.anchorMin = frame.anchorMax = new Vector2(0.5f, 1f);
            frame.pivot = new Vector2(0.5f, 1f);
            frame.anchoredPosition = new Vector2(0f, -52f);
            frame.sizeDelta = new Vector2(810f, 82f);
            Image image = frame.gameObject.AddComponent<Image>();
            image.color = DeepBlue;
            image.sprite = FindSprite("SquareRounded_Filled");
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 3.5f;

            font = FindFont();
            TMP_Text text = TextFill(frame, (message ?? string.Empty).ToUpperInvariant(), 29f, Paper, true);
            text.alignment = TextAlignmentOptions.Center;
        }

        private void Update()
        {
            if (openMenu != null && Input.GetKeyDown(KeyCode.Escape))
                CloseMenu();

            if (toastRoot != null && Time.realtimeSinceStartup >= toastUntil)
            {
                Destroy(toastRoot);
                toastRoot = null;
            }

            if (openMenu != null && activeScroll != null && EventSystem.current != null)
            {
                GameObject selected = EventSystem.current.currentSelectedGameObject;
                if (selected != null && selected != lastSelected && selected.transform.IsChildOf(content))
                {
                    lastSelected = selected;
                    KeepVisible(selected.GetComponent<RectTransform>());
                }
            }
        }

        private void Build(ModMenu menu)
        {
            if (root != null) Destroy(root);
            font = FindFont();

            root = new GameObject($"ModsPanel Menu {menu.OwnerId}", typeof(RectTransform));
            root.transform.SetParent(transform, false);
            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;
            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>();

            RawImage backdrop = Rect("Background", root.transform).gameObject.AddComponent<RawImage>();
            Stretch(backdrop.rectTransform, 0f, 0f, 0f, 0f);
            backdrop.texture = FindTexture("PauseMenuBackgroundPattern");
            backdrop.color = new Color(0.035f, 0.075f, 0.08f, 0.92f);

            RectTransform panel = Rect("Panel", root.transform);
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(1380f, 870f);
            panel.gameObject.AddComponent<Image>().color = Paper;

            RectTransform rail = Rect("Rail", panel);
            rail.anchorMin = Vector2.zero;
            rail.anchorMax = new Vector2(0f, 1f);
            rail.pivot = new Vector2(0f, 0.5f);
            rail.sizeDelta = new Vector2(360f, 0f);
            rail.gameObject.AddComponent<Image>().color = DeepBlue;

            Text(rail, menu.Eyebrow.ToUpperInvariant(), 24f, Paper,
                new Vector2(55f, -70f), new Vector2(250f, 40f), TextAlignmentOptions.MidlineLeft);
            Text(rail, menu.Title.ToUpperInvariant(), 52f, Paper,
                new Vector2(55f, -125f), new Vector2(260f, 145f), TextAlignmentOptions.TopLeft, true);
            Text(rail, menu.Subtitle, 28f, Paper,
                new Vector2(55f, -315f), new Vector2(250f, 300f), TextAlignmentOptions.TopLeft, true);

            Button close = Button("Close", rail, menu.CloseText, Red, () => CloseMenu(menu));
            RectTransform closeRect = (RectTransform)close.transform;
            closeRect.anchorMin = closeRect.anchorMax = new Vector2(0f, 0f);
            closeRect.pivot = Vector2.zero;
            closeRect.anchoredPosition = new Vector2(55f, 70f);
            closeRect.sizeDelta = new Vector2(250f, 70f);

            RectTransform scrollRoot = Rect("Menu Scroll", panel);
            Stretch(scrollRoot, 410f, 70f, 65f, 65f);
            ScrollRect scroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia = true;
            scroll.decelerationRate = 0.12f;
            scroll.scrollSensitivity = 65f;

            RectTransform viewport = Rect("Viewport", scrollRoot);
            Stretch(viewport, 0f, 24f, 0f, 0f);
            viewport.gameObject.AddComponent<RectMask2D>();
            content = Rect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = Vector2.zero;
            VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 18f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport;
            scroll.content = content;
            activeScroll = scroll;
            activeViewport = viewport;

            RectTransform barRect = Rect("Scrollbar", scrollRoot);
            barRect.anchorMin = new Vector2(1f, 0f);
            barRect.anchorMax = Vector2.one;
            barRect.pivot = new Vector2(1f, 0.5f);
            barRect.offsetMin = new Vector2(-14f, 0f);
            barRect.offsetMax = Vector2.zero;
            Image barBackground = barRect.gameObject.AddComponent<Image>();
            barBackground.color = new Color(0.13f, 0.30f, 0.34f, 0.25f);
            RectTransform handleRect = Rect("Handle", barRect);
            Stretch(handleRect, 0f, 0f, 0f, 0f);
            Image handle = handleRect.gameObject.AddComponent<Image>();
            handle.color = Blue;
            Scrollbar scrollbar = barRect.gameObject.AddComponent<Scrollbar>();
            scrollbar.handleRect = handleRect;
            scrollbar.targetGraphic = handle;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            scroll.verticalScrollbarSpacing = -14f;

            Selectable first = null;
            foreach (ModMenuItem item in menu.Items)
            {
                Selectable selectable = BuildItem(item);
                if (first == null && selectable != null) first = selectable;
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            if (first != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(first.gameObject);
        }

        private void KeepVisible(RectTransform selected)
        {
            if (selected == null || activeViewport == null || content == null) return;
            Canvas.ForceUpdateCanvases();
            Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(activeViewport, selected);
            Rect view = activeViewport.rect;
            float targetY = content.anchoredPosition.y;
            if (bounds.min.y < view.yMin + 12f)
                targetY += view.yMin + 12f - bounds.min.y;
            else if (bounds.max.y > view.yMax - 12f)
                targetY -= bounds.max.y - (view.yMax - 12f);
            float maximum = Mathf.Max(0f, content.rect.height - activeViewport.rect.height);
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, Mathf.Clamp(targetY, 0f, maximum));
        }

        private Selectable BuildItem(ModMenuItem item)
        {
            if (item is ModMenuHeading heading)
            {
                RectTransform row = LayoutRow("Heading", 64f);
                TMP_Text text = TextFill(row, heading.Text, 42f, Ink);
                text.alignment = TextAlignmentOptions.MidlineLeft;
                return null;
            }
            if (item is ModMenuLabel label)
            {
                RectTransform row = LayoutRow("Label", 72f);
                TMP_Text text = TextFill(row, label.Text, 29f, Ink, true);
                text.alignment = TextAlignmentOptions.TopLeft;
                return null;
            }
            if (item is ModMenuSpacer spacer)
            {
                LayoutRow("Spacer", spacer.Height);
                return null;
            }
            if (item is ModMenuButton action)
            {
                Button button = Button("Action", content, action.Text, Blue, () => SafeInvoke(action.Pressed));
                button.gameObject.AddComponent<LayoutElement>().preferredHeight =
                    string.IsNullOrWhiteSpace(action.Detail) ? 78f : 96f;
                if (!string.IsNullOrWhiteSpace(action.Detail))
                {
                    TMP_Text caption = button.GetComponentInChildren<TMP_Text>();
                    caption.text = action.Text + "\n<size=25>" + action.Detail + "</size>";
                    caption.alignment = TextAlignmentOptions.MidlineLeft;
                }
                return button;
            }
            if (item is ModMenuImage preview)
            {
                RectTransform row = LayoutRow("Image Preview", preview.PreferredHeight);
                RectTransform imageRect = Rect("Preview Image", row);
                Stretch(imageRect, 0f, 0f, 0f, 0f);
                RawImage image = imageRect.gameObject.AddComponent<RawImage>();
                image.texture = SafeValue(preview.GetTexture, null);
                image.color = Color.white;
                image.raycastTarget = false;
                AspectRatioFitter ratio = imageRect.gameObject.AddComponent<AspectRatioFitter>();
                ratio.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                ratio.aspectRatio = image.texture != null && image.texture.height > 0
                    ? (float)image.texture.width / image.texture.height
                    : 2f / 3f;
                return null;
            }
            if (item is ModMenuToggle option)
            {
                RectTransform row = LayoutRow("Toggle", 82f);
                TMP_Text toggleLabel = TextFill(row, option.Text, 36f, Ink);
                toggleLabel.rectTransform.offsetMax = new Vector2(-100f, 0f);
                toggleLabel.alignment = TextAlignmentOptions.MidlineLeft;

                RectTransform toggleRect = Rect("Toggle", row);
                toggleRect.anchorMin = toggleRect.anchorMax = new Vector2(1f, 0.5f);
                toggleRect.anchoredPosition = new Vector2(-42f, 0f);
                toggleRect.sizeDelta = new Vector2(62f, 62f);
                Image background = toggleRect.gameObject.AddComponent<Image>();
                background.color = Blue;
                background.sprite = FindSprite("UI_toolbarFrame");
                background.type = Image.Type.Sliced;
                background.pixelsPerUnitMultiplier = 2f;
                RectTransform markRect = Rect("Checkmark", toggleRect);
                Stretch(markRect, 10f, 10f, 10f, 10f);
                Image mark = markRect.gameObject.AddComponent<Image>();
                mark.sprite = FindSprite("UI_WhiteTick");
                mark.color = Color.white;
                Toggle toggle = toggleRect.gameObject.AddComponent<Toggle>();
                toggle.targetGraphic = background;
                toggle.graphic = mark;
                toggle.isOn = SafeValue(option.GetValue, false);
                toggle.onValueChanged.AddListener(new UnityAction<bool>(value => SafeInvoke(() => option.SetValue(value))));
                return toggle;
            }
            if (item is ModMenuTextInput textInput)
            {
                RectTransform row = LayoutRow("Text Input", 154f);

                RectTransform labelRect = Rect("Input Label", row);
                labelRect.anchorMin = labelRect.anchorMax = new Vector2(0f, 1f);
                labelRect.pivot = new Vector2(0f, 1f);
                labelRect.anchoredPosition = Vector2.zero;
                labelRect.sizeDelta = new Vector2(820f, 46f);
                TMP_Text inputLabel = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
                inputLabel.text = textInput.Label;
                inputLabel.font = font;
                inputLabel.fontSize = 29f;
                inputLabel.color = Ink;
                inputLabel.raycastTarget = false;
                inputLabel.alignment = TextAlignmentOptions.MidlineLeft;

                RectTransform fieldRect = Rect("Input", row);
                fieldRect.anchorMin = new Vector2(0f, 0f);
                fieldRect.anchorMax = new Vector2(1f, 0f);
                fieldRect.pivot = new Vector2(0.5f, 0f);
                fieldRect.anchoredPosition = Vector2.zero;
                fieldRect.sizeDelta = new Vector2(0f, 82f);
                Image fieldImage = fieldRect.gameObject.AddComponent<Image>();
                fieldImage.color = new Color32(224, 225, 216, 255);
                fieldImage.sprite = FindSprite("SquareRounded_Border");
                fieldImage.type = Image.Type.Sliced;
                fieldImage.pixelsPerUnitMultiplier = 3.5f;

                TMP_Text value = TextFill(fieldRect, SafeValue(textInput.GetValue, string.Empty), 31f, Ink);
                Stretch(value.rectTransform, 20f, 20f, 8f, 8f);
                value.alignment = TextAlignmentOptions.MidlineLeft;
                TMP_Text placeholder = TextFill(fieldRect, textInput.Placeholder, 31f,
                    new Color(Ink.r, Ink.g, Ink.b, 0.45f));
                Stretch(placeholder.rectTransform, 20f, 20f, 8f, 8f);
                placeholder.fontStyle = FontStyles.Italic;
                placeholder.alignment = TextAlignmentOptions.MidlineLeft;
                TMP_InputField input = fieldRect.gameObject.AddComponent<TMP_InputField>();
                input.textViewport = fieldRect;
                input.textComponent = value;
                input.placeholder = placeholder;
                input.lineType = TMP_InputField.LineType.SingleLine;
                input.contentType = TMP_InputField.ContentType.Standard;
                input.text = SafeValue(textInput.GetValue, string.Empty);
                input.onValueChanged.AddListener(new UnityAction<string>(newValue =>
                    SafeInvoke(() => textInput.SetValue(newValue))));
                return input;
            }
            return null;
        }

        private RectTransform LayoutRow(string name, float height)
        {
            RectTransform row = Rect(name, content);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
            return row;
        }

        private Button Button(string name, Transform parent, string caption, Color color, Action pressed)
        {
            RectTransform rect = Rect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.sprite = FindSprite("SquareRounded_Filled");
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 3.5f;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            rect.gameObject.AddComponent<ControllerSelectionVisual>();
            button.onClick.AddListener(new UnityAction(pressed));
            TMP_Text text = TextFill(rect, caption, 34f, Paper);
            text.alignment = TextAlignmentOptions.Center;
            return button;
        }

        private TMP_Text TextFill(Transform parent, string value, float size, Color color, bool wrap = false)
        {
            RectTransform rect = Rect("Text", parent);
            Stretch(rect, 22f, 22f, 4f, 4f);
            TMP_Text text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value ?? string.Empty;
            text.font = font;
            text.fontSize = size;
            text.color = color;
            text.raycastTarget = false;
            text.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            return text;
        }

        private TMP_Text Text(Transform parent, string value, float size, Color color,
            Vector2 position, Vector2 dimensions, TextAlignmentOptions alignment, bool wrap = false)
        {
            RectTransform rect = Rect("Text", parent);
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
            TMP_Text text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value ?? string.Empty;
            text.font = font;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            return text;
        }

        private static RectTransform Rect(string name, Transform parent)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            RectTransform rect = (RectTransform)gameObject.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static void Stretch(RectTransform rect, float left, float right, float top, float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static TMP_FontAsset FindFont()
        {
            foreach (TMP_FontAsset candidate in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
                if (candidate != null && candidate.name.IndexOf("Lilita", StringComparison.OrdinalIgnoreCase) >= 0)
                    return candidate;
            return TMP_Settings.defaultFontAsset;
        }

        private static Sprite FindSprite(string name)
        {
            foreach (Sprite sprite in Resources.FindObjectsOfTypeAll<Sprite>())
                if (sprite != null && sprite.name == name) return sprite;
            return null;
        }

        private static Texture FindTexture(string name)
        {
            foreach (Texture texture in Resources.FindObjectsOfTypeAll<Texture>())
                if (texture != null && texture.name == name) return texture;
            return Texture2D.whiteTexture;
        }

        private static void SafeInvoke(Action callback)
        {
            try { callback?.Invoke(); }
            catch (Exception exception) { MelonLogger.Error($"ModsPanel menu callback failed: {exception}"); }
        }

        private static T SafeValue<T>(Func<T> callback, T fallback)
        {
            try { return callback != null ? callback() : fallback; }
            catch (Exception exception)
            {
                MelonLogger.Error($"ModsPanel menu callback failed: {exception}");
                return fallback;
            }
        }
    }
}

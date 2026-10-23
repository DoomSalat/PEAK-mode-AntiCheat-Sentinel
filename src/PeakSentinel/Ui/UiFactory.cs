using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PeakSentinel.Ui
{
    public sealed class UiFactory
    {
        private const float ReferenceWidth = 1920f;
        private const float ReferenceHeight = 1080f;
        private const float ButtonHeight = 38f;
        private const float HeaderSize = 24f;
        private const float BoxSize = 28f;
        private const float MarkSize = 16f;
        private const float BarWidth = 14f;
        private const float ScrollInset = 8f;
        private const float ScrollSensitivity = 40f;
        private const float MinScrollHeight = 80f;
        private const float SearchInset = 12f;

        private readonly CardSprites _sprites = new CardSprites();
        private readonly FaceSprites _faces = new FaceSprites();
        private readonly ShieldSprite _shield = new ShieldSprite();
        private readonly DotSprite _dot = new DotSprite();
        private readonly CloverSprite _clover = new CloverSprite();
        private readonly PlatformSprites _platforms = new PlatformSprites();

        public UiFactory(TMP_FontAsset font)
        {
            Font = font;
        }

        public TMP_FontAsset Font { get; }
        public CardSprites Sprites => _sprites;
        public FaceSprites Faces => _faces;
        public Sprite Shield => _shield.Sprite;
        public Sprite Dot => _dot.Sprite;
        public Sprite Clover => _clover.Sprite;
        public PlatformSprites Platforms => _platforms;

        public GameObject Canvas(string name, int sortingOrder)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.matchWidthOrHeight = 0.5f;
            return root;
        }

        public RectTransform Panel(Transform parent, Vector2 size, float topMargin)
        {
            RectTransform panel = Element("Panel", parent);
            panel.anchorMin = new Vector2(0.5f, 1f);
            panel.anchorMax = new Vector2(0.5f, 1f);
            panel.pivot = new Vector2(0.5f, 1f);
            panel.sizeDelta = size;
            panel.anchoredPosition = new Vector2(0f, -topMargin);
            Sliced(panel.gameObject, _sprites.Panel);
            return panel;
        }

        public RectTransform Column(Transform parent, float spacing, RectOffset padding)
        {
            RectTransform column = Element("Column", parent);
            VerticalLayoutGroup layout = column.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = padding;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return column;
        }

        public RectTransform Row(Transform parent, float spacing)
        {
            RectTransform row = Element("Row", parent);
            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return row;
        }

        public void Stretch(RectTransform element)
        {
            element.anchorMin = Vector2.zero;
            element.anchorMax = Vector2.one;
            element.offsetMin = Vector2.zero;
            element.offsetMax = Vector2.zero;
        }

        public void Flexible(Component element, float width, float height)
        {
            LayoutElement layout = LayoutOf(element);
            layout.flexibleWidth = width;
            layout.flexibleHeight = height;
        }

        public void Fixed(Component element, float width, float height)
        {
            LayoutElement layout = LayoutOf(element);

            if (width > 0f)
            {
                layout.minWidth = width;
                layout.preferredWidth = width;
                layout.flexibleWidth = 0f;
            }

            if (height > 0f)
            {
                layout.minHeight = height;
                layout.preferredHeight = height;
            }
        }

        public TextMeshProUGUI Label(Transform parent, string text, float size, Color color, bool rich)
        {
            RectTransform element = Element("Label", parent);
            TextMeshProUGUI label = element.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = Font;
            label.fontSize = size;
            label.color = color;
            label.fontStyle = FontStyles.Normal;
            label.richText = rich;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Overflow;
            label.raycastTarget = false;
            label.text = text;
            return label;
        }

        public TextMeshProUGUI Header(Transform parent, string text)
        {
            return Label(parent, text, HeaderSize, UiPalette.Text, false);
        }

        public (Button Button, TextMeshProUGUI Label) Button(Transform parent, string text, float width, float fontSize, UnityAction onClick)
        {
            RectTransform element = Element("Button", parent);
            Image image = Sliced(element.gameObject, _sprites.ButtonIdle);
            Button button = element.gameObject.AddComponent<Button>();
            SpriteSwap(button, image, _sprites.ButtonHover, _sprites.ButtonSelected);
            button.onClick.AddListener(onClick);
            Fixed(element, width, ButtonHeight);

            if (width <= 0f)
                Flexible(element, 1f, 0f);

            TextMeshProUGUI label = Label(element, text, fontSize, UiPalette.Text, false);
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            Stretch(label.rectTransform);
            return (button, label);
        }

        public Button RowButton(Transform parent, UnityAction onClick)
        {
            RectTransform element = Element("RowButton", parent);
            Image image = Sliced(element.gameObject, _sprites.Card);
            Button button = element.gameObject.AddComponent<Button>();
            SpriteSwap(button, image, _sprites.CardHover, _sprites.CardHover);
            button.onClick.AddListener(onClick);
            HorizontalLayoutGroup layout = element.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(14, 10, 8, 8);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return button;
        }

        public Toggle Toggle(Transform parent, string text, float fontSize, bool isOn, UnityAction<bool> onChange)
        {
            RectTransform root = Row(parent, 10f);
            Image box = Sliced(Element("Box", root).gameObject, _sprites.Box);
            Fixed(box, BoxSize, BoxSize);
            RectTransform markElement = Element("Mark", box.transform);
            markElement.sizeDelta = new Vector2(MarkSize, MarkSize);
            Image mark = Sliced(markElement.gameObject, _sprites.Mark);
            mark.raycastTarget = false;
            TextMeshProUGUI label = Label(root, text, fontSize, UiPalette.Text, true);
            label.raycastTarget = true;
            Flexible(label, 1f, 0f);

            Toggle toggle = root.gameObject.AddComponent<Toggle>();
            SpriteSwap(toggle, box, _sprites.CardHover, _sprites.CardHover);
            toggle.graphic = mark;
            toggle.isOn = isOn;
            toggle.onValueChanged.AddListener(onChange);
            root.gameObject.AddComponent<CanvasGroup>();
            return toggle;
        }

        public TMP_InputField Input(Transform parent, string placeholder, float fontSize, float width)
        {
            RectTransform element = Element("Input", parent);
            element.gameObject.SetActive(false);
            Image background = Sliced(element.gameObject, _sprites.Card);
            Fixed(element, width, ButtonHeight);

            if (width <= 0f)
                Flexible(element, 1f, 0f);

            RectTransform area = Element("TextArea", element);
            Stretch(area);
            area.offsetMin = new Vector2(SearchInset, 2f);
            area.offsetMax = new Vector2(-SearchInset, -2f);
            area.gameObject.AddComponent<RectMask2D>();
            TextMeshProUGUI hint = SingleLine(Label(area, placeholder, fontSize, UiPalette.Notice, false));
            TextMeshProUGUI text = SingleLine(Label(area, string.Empty, fontSize, UiPalette.Text, false));

            TMP_InputField input = element.gameObject.AddComponent<TMP_InputField>();
            input.textViewport = area;
            input.textComponent = text;
            input.placeholder = hint;
            input.fontAsset = Font;
            input.targetGraphic = background;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.richText = false;
            input.caretColor = UiPalette.Ink;
            input.customCaretColor = true;
            input.selectionColor = UiPalette.BarBackground;
            input.navigation = new Navigation { mode = Navigation.Mode.None };
            element.gameObject.SetActive(true);
            return input;
        }

        public Image Icon(Transform parent, Sprite sprite, float size)
        {
            RectTransform element = Element("Icon", parent);
            Image image = element.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            Fixed(element, size, size);
            return image;
        }

        public (ScrollRect Scroll, RectTransform Content) Scroll(Transform parent, float spacing)
        {
            RectTransform element = Element("Scroll", parent);
            Sliced(element.gameObject, _sprites.Card);
            Flexible(element, 1f, 1f);
            LayoutOf(element).minHeight = MinScrollHeight;

            RectTransform viewport = Element("Viewport", element);
            Stretch(viewport);
            viewport.offsetMin = new Vector2(ScrollInset, ScrollInset);
            viewport.offsetMax = new Vector2(-(ScrollInset * 2f + BarWidth), -ScrollInset);
            viewport.gameObject.AddComponent<RectMask2D>();

            RectTransform content = Column(viewport, spacing, new RectOffset(4, 4, 2, 2));
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = element.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = ScrollSensitivity;
            scroll.verticalScrollbar = Scrollbar(element);
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            return (scroll, content);
        }

        private Scrollbar Scrollbar(RectTransform parent)
        {
            RectTransform element = Element("Scrollbar", parent);
            element.anchorMin = new Vector2(1f, 0f);
            element.anchorMax = new Vector2(1f, 1f);
            element.pivot = new Vector2(1f, 0.5f);
            element.sizeDelta = new Vector2(BarWidth, -ScrollInset * 2f);
            element.anchoredPosition = new Vector2(-ScrollInset, 0f);
            Sliced(element.gameObject, _sprites.BarBackground);

            RectTransform area = Element("Area", element);
            Stretch(area);
            RectTransform handle = Element("Handle", area);
            Stretch(handle);
            Image handleImage = Sliced(handle.gameObject, _sprites.BarHandle);

            Scrollbar scrollbar = element.gameObject.AddComponent<Scrollbar>();
            scrollbar.direction = UnityEngine.UI.Scrollbar.Direction.BottomToTop;
            scrollbar.handleRect = handle;
            scrollbar.targetGraphic = handleImage;
            scrollbar.navigation = new Navigation { mode = Navigation.Mode.None };
            return scrollbar;
        }

        private TextMeshProUGUI SingleLine(TextMeshProUGUI label)
        {
            Stretch(label.rectTransform);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            return label;
        }

        private LayoutElement LayoutOf(Component element)
        {
            return element.TryGetComponent(out LayoutElement layout) ? layout : element.gameObject.AddComponent<LayoutElement>();
        }

        private RectTransform Element(string name, Transform parent)
        {
            GameObject element = new GameObject(name, typeof(RectTransform));
            element.transform.SetParent(parent, false);
            return (RectTransform)element.transform;
        }

        private Image Sliced(GameObject target, Sprite sprite)
        {
            Image image = target.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            return image;
        }

        private void SpriteSwap(Selectable selectable, Image image, Sprite hover, Sprite pressed)
        {
            selectable.targetGraphic = image;
            selectable.transition = Selectable.Transition.SpriteSwap;
            selectable.spriteState = new SpriteState { highlightedSprite = hover, pressedSprite = pressed, selectedSprite = image.sprite, disabledSprite = image.sprite };
            selectable.navigation = new Navigation { mode = Navigation.Mode.None };
        }
    }
}

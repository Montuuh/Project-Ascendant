using System;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectAscendant.UI
{
    // Per gap #43 + Epic 13 — in-run pause overlay (ESC). Resume / Quit to Main Menu. The run autosaves
    // at the start of every node, so quitting to the menu loses nothing past the last node checkpoint —
    // no explicit "save" action is needed. View-layer only; MapViewUI owns the callbacks.
    public sealed class PauseMenuUI : MonoBehaviour
    {
        private Font _font;
        private GameObject _root;
        private Action _onResume, _onQuitToMenu;

        public bool IsOpen => _root != null;

        public void Open(Action onResume, Action onQuitToMenu)
        {
            _onResume = onResume;
            _onQuitToMenu = onQuitToMenu;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_root != null) Destroy(_root);
            _root = new GameObject("PauseMenuCanvas");
            Canvas c = _root.AddComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = 45;
            CanvasScaler s = _root.AddComponent<CanvasScaler>(); s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; s.referenceResolution = new Vector2(1920, 1080);
            _root.AddComponent<GraphicRaycaster>();

            Image bg = Img(_root.transform, new Color(0.04f, 0.05f, 0.08f, 0.85f)); Stretch(bg.rectTransform);

            Txt(_root.transform, "PAUSED", 56, new Color(0.85f, 0.95f, 0.7f), new Vector2(0, 170), new Vector2(900, 80));

            Btn(_root.transform, new Vector2(0, 40), new Vector2(420, 64), "▶  RESUME",
                new Color(0.26f, 0.5f, 0.34f), () => { Close(); _onResume?.Invoke(); });
            Btn(_root.transform, new Vector2(0, -44), new Vector2(420, 64), "⌂  QUIT TO MAIN MENU",
                new Color(0.42f, 0.34f, 0.30f), () => { Close(); _onQuitToMenu?.Invoke(); });

            Txt(_root.transform, "Progress is autosaved at the start of every node.", 17,
                new Color(0.6f, 0.65f, 0.75f), new Vector2(0, -130), new Vector2(1000, 28));
        }

        public void Close()
        {
            if (_root != null) Destroy(_root);
            _root = null;
        }

        private Image Img(Transform parent, Color c) { GameObject g = new("Image", typeof(RectTransform)); g.transform.SetParent(parent, false); Image i = g.AddComponent<Image>(); i.color = c; return i; }
        private Text Txt(Transform parent, string text, int size, Color color, Vector2 pos, Vector2 sizeD)
        {
            GameObject g = new("Text", typeof(RectTransform)); g.transform.SetParent(parent, false);
            Text t = g.AddComponent<Text>(); t.font = _font; t.text = text; t.fontSize = size; t.color = color; t.alignment = TextAnchor.MiddleCenter; t.horizontalOverflow = HorizontalWrapMode.Overflow;
            RectTransform rt = t.rectTransform; rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f); rt.anchoredPosition = pos; rt.sizeDelta = sizeD; return t;
        }
        private Button Btn(Transform parent, Vector2 pos, Vector2 size, string label, Color color, Action onClick)
        {
            GameObject g = new("Button", typeof(RectTransform)); g.transform.SetParent(parent, false);
            Image i = g.AddComponent<Image>(); i.color = color;
            RectTransform rt = (RectTransform)g.transform; rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f); rt.anchoredPosition = pos; rt.sizeDelta = size;
            Button b = g.AddComponent<Button>(); if (onClick != null) b.onClick.AddListener(() => onClick());
            Text t = Txt(g.transform, label, 22, Color.white, Vector2.zero, size);
            RectTransform trt = t.rectTransform; trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero; return b;
        }
        private static void Stretch(RectTransform rt) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero; }
    }
}

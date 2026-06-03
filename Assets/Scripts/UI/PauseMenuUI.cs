using System;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectAscendant.UI
{
    // Per gap #43 + Epic 13 — in-run pause overlay (ESC). Resume / Team / Bag / TMs / Quit to Main
    // Menu. The between-node management actions (Team loadout, Inventory+equip, TM application) live
    // here so the map stays uncluttered. The run autosaves at the start of every node, so quitting to
    // the menu loses nothing past the last node checkpoint. View-layer only; MapViewUI owns callbacks.
    public sealed class PauseMenuUI : MonoBehaviour
    {
        private Font _font;
        private GameObject _root;
        private Action _onResume, _onTeam, _onBag, _onTMs, _onQuitToMenu;

        public bool IsOpen => _root != null;

        public void Open(Action onResume, Action onTeam, Action onBag, Action onTMs, Action onQuitToMenu)
        {
            _onResume = onResume;
            _onTeam = onTeam;
            _onBag = onBag;
            _onTMs = onTMs;
            _onQuitToMenu = onQuitToMenu;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_root != null) Destroy(_root);
            _root = new GameObject("PauseMenuCanvas");
            Canvas c = _root.AddComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = 45;
            CanvasScaler s = _root.AddComponent<CanvasScaler>(); s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; s.referenceResolution = new Vector2(1920, 1080);
            _root.AddComponent<GraphicRaycaster>();

            Image bg = Img(_root.transform, new Color(0.04f, 0.05f, 0.08f, 0.9f)); Stretch(bg.rectTransform);

            Txt(_root.transform, "PAUSED", 56, new Color(0.85f, 0.95f, 0.7f), new Vector2(0, 240), new Vector2(900, 80));

            Btn(_root.transform, new Vector2(0, 140), new Vector2(420, 60), "▶  RESUME",
                new Color(0.26f, 0.5f, 0.34f), () => { Close(); _onResume?.Invoke(); });
            Btn(_root.transform, new Vector2(0, 68), new Vector2(420, 60), "⛉  TEAM",
                new Color(0.30f, 0.42f, 0.58f), () => { Close(); _onTeam?.Invoke(); });
            Btn(_root.transform, new Vector2(0, -4), new Vector2(420, 60), "📦  BAG",
                new Color(0.36f, 0.32f, 0.44f), () => { Close(); _onBag?.Invoke(); });
            Btn(_root.transform, new Vector2(0, -76), new Vector2(420, 60), "🎒  TMs",
                new Color(0.40f, 0.36f, 0.24f), () => { Close(); _onTMs?.Invoke(); });
            Btn(_root.transform, new Vector2(0, -160), new Vector2(420, 60), "⌂  QUIT TO MAIN MENU",
                new Color(0.42f, 0.34f, 0.30f), () => { Close(); _onQuitToMenu?.Invoke(); });

            Txt(_root.transform, "Progress is autosaved at the start of every node.", 17,
                new Color(0.6f, 0.65f, 0.75f), new Vector2(0, -236), new Vector2(1000, 28));
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

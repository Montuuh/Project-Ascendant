using System;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectAscendant.UI
{
    // Per gap #43 + Epic 13 — the boot Main Menu. Continue (enabled only if a resumable save exists) /
    // New Run (confirm before abandoning an in-progress save) / Quit. View-layer only; MapViewUI owns
    // the callbacks (which drive RunLauncher.ContinueSavedRun / BeginNewRun / Application.Quit).
    // Procedural uGUI to match the project's screenshot-verifiable UI convention (gap #38 tech-debt).
    public sealed class MainMenuUI : MonoBehaviour
    {
        private Font _font;
        private GameObject _root;
        private bool _hasSave;
        private Action _onContinue, _onNewRun, _onQuit;

        public bool IsOpen => _root != null;

        public void Open(bool hasSave, Action onContinue, Action onNewRun, Action onQuit)
        {
            _hasSave = hasSave;
            _onContinue = onContinue;
            _onNewRun = onNewRun;
            _onQuit = onQuit;
            Build();
        }

        public void Close()
        {
            if (_root != null) Destroy(_root);
            _root = null;
        }

        private void Build()
        {
            if (_root != null) Destroy(_root);
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _root = new GameObject("MainMenuCanvas");
            Canvas c = _root.AddComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = 50;
            CanvasScaler s = _root.AddComponent<CanvasScaler>(); s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; s.referenceResolution = new Vector2(1920, 1080);
            _root.AddComponent<GraphicRaycaster>();

            Image bg = Img(_root.transform, new Color(0.05f, 0.07f, 0.11f, 1f)); Stretch(bg.rectTransform);

            Txt(_root.transform, "PROJECT ASCENDANT", 64, new Color(0.85f, 0.95f, 0.7f), new Vector2(0, 250), new Vector2(1400, 90));
            Txt(_root.transform, "Region 1 — Verdant Route", 26, new Color(0.7f, 0.8f, 0.9f), new Vector2(0, 185), new Vector2(1400, 36));

            // Continue — only when a resumable save exists.
            Btn(_root.transform, new Vector2(0, 60), new Vector2(420, 64),
                _hasSave ? "▶  CONTINUE" : "CONTINUE  (no save)",
                _hasSave ? new Color(0.26f, 0.5f, 0.34f) : new Color(0.22f, 0.24f, 0.28f),
                _hasSave, () => { Close(); _onContinue?.Invoke(); });

            Btn(_root.transform, new Vector2(0, -24), new Vector2(420, 64), "✦  NEW RUN",
                new Color(0.30f, 0.42f, 0.58f), true, OnNewRunClicked);

            Btn(_root.transform, new Vector2(0, -108), new Vector2(420, 64), "✕  QUIT",
                new Color(0.42f, 0.30f, 0.32f), true, () => { Close(); _onQuit?.Invoke(); });

            Txt(_root.transform, "Autosaves at the start of every node — quitting mid-run is safe.", 17,
                new Color(0.55f, 0.6f, 0.7f), new Vector2(0, -220), new Vector2(1200, 28));
        }

        private void OnNewRunClicked()
        {
            // No save to lose → start immediately. Save present → confirm before abandoning it.
            if (!_hasSave) { Close(); _onNewRun?.Invoke(); return; }
            BuildConfirm();
        }

        private void BuildConfirm()
        {
            // Dim overlay above the menu with a confirm card.
            GameObject layer = new("Confirm", typeof(RectTransform)); layer.transform.SetParent(_root.transform, false);
            Image dim = layer.AddComponent<Image>(); dim.color = new Color(0f, 0f, 0f, 0.6f); Stretch(dim.rectTransform);

            GameObject card = new("Card", typeof(RectTransform)); card.transform.SetParent(layer.transform, false);
            Image ci = card.AddComponent<Image>(); ci.color = new Color(0.14f, 0.18f, 0.24f, 1f);
            Place((RectTransform)card.transform, Vector2.zero, new Vector2(760, 280));

            Txt(card.transform, "Abandon your in-progress run?", 28, new Color(0.95f, 0.9f, 0.6f), new Vector2(0, 80), new Vector2(700, 40));
            Txt(card.transform, "Your saved run will be permanently deleted and a new one started.", 19,
                new Color(0.82f, 0.86f, 0.92f), new Vector2(0, 28), new Vector2(700, 30));

            Btn(card.transform, new Vector2(-180, -70), new Vector2(280, 60), "ABANDON & START",
                new Color(0.55f, 0.30f, 0.30f), true, () => { Close(); _onNewRun?.Invoke(); });
            Btn(card.transform, new Vector2(180, -70), new Vector2(280, 60), "CANCEL",
                new Color(0.30f, 0.40f, 0.52f), true, () => Destroy(layer));
        }

        // ── uGUI primitives (mirrors StartingRelicPanelUI) ───────────────────────
        private Image Img(Transform parent, Color c) { GameObject g = new("Image", typeof(RectTransform)); g.transform.SetParent(parent, false); Image i = g.AddComponent<Image>(); i.color = c; return i; }
        private Text Txt(Transform parent, string text, int size, Color color, Vector2 pos, Vector2 sizeD)
        {
            GameObject g = new("Text", typeof(RectTransform)); g.transform.SetParent(parent, false);
            Text t = g.AddComponent<Text>(); t.font = _font; t.text = text; t.fontSize = size; t.color = color; t.alignment = TextAnchor.MiddleCenter; t.horizontalOverflow = HorizontalWrapMode.Overflow;
            RectTransform rt = t.rectTransform; rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f); rt.anchoredPosition = pos; rt.sizeDelta = sizeD; return t;
        }
        private Button Btn(Transform parent, Vector2 pos, Vector2 size, string label, Color color, bool interactable, Action onClick)
        {
            GameObject g = new("Button", typeof(RectTransform)); g.transform.SetParent(parent, false);
            Image i = g.AddComponent<Image>(); i.color = color; Place((RectTransform)g.transform, pos, size);
            Button b = g.AddComponent<Button>(); b.interactable = interactable; if (onClick != null) b.onClick.AddListener(() => onClick());
            Text t = Txt(g.transform, label, 22, interactable ? Color.white : new Color(1, 1, 1, 0.5f), Vector2.zero, size);
            RectTransform rt = t.rectTransform; rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero; return b;
        }
        private static void Stretch(RectTransform rt) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero; }
        private static void Place(RectTransform rt, Vector2 pos, Vector2 size) { rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f); rt.anchoredPosition = pos; rt.sizeDelta = size; }
    }
}

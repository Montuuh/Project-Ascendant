using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectAscendant.Core;

namespace ProjectAscendant.UI
{
    // Per §6.8 + gap #43 — the difficulty picker shown when starting a NEW RUN (after the abandon-save
    // confirm, if any). None (baseline ×1.0) plus each VS modifier (XP multiplier + effect). onChosen
    // receives the pick (null = None); onBack returns to the Main Menu. View-layer only.
    public sealed class DifficultySelectUI : MonoBehaviour
    {
        private Font _font;
        private GameObject _root;
        private Action<DifficultyModifierSO> _onChosen;
        private Action _onBack;

        public bool IsOpen => _root != null;

        public void Open(IReadOnlyList<DifficultyModifierSO> choices, Action<DifficultyModifierSO> onChosen, Action onBack)
        {
            _onChosen = onChosen;
            _onBack = onBack;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_root != null) Destroy(_root);
            _root = new GameObject("DifficultySelectCanvas");
            Canvas c = _root.AddComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = 52;
            CanvasScaler s = _root.AddComponent<CanvasScaler>(); s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; s.referenceResolution = new Vector2(1920, 1080);
            _root.AddComponent<GraphicRaycaster>();

            Image bg = Img(_root.transform, new Color(0.05f, 0.07f, 0.11f, 1f)); Stretch(bg.rectTransform);

            Txt(_root.transform, "CHOOSE DIFFICULTY", 44, new Color(0.85f, 0.95f, 0.7f), new Vector2(0, 330), new Vector2(1300, 60));
            Txt(_root.transform, "Higher difficulty boosts Trainer XP. §6.8", 20, new Color(0.78f, 0.82f, 0.9f), new Vector2(0, 280), new Vector2(1200, 30));

            // One card per option (None + each modifier), laid out in a centered row.
            List<DifficultyModifierSO> opts = new() { null };
            if (choices != null) foreach (DifficultyModifierSO d in choices) if (d != null) opts.Add(d);

            const float cw = 360f, gap = 36f;
            float x = -((opts.Count - 1) * (cw + gap)) / 2f;
            foreach (DifficultyModifierSO d in opts)
            {
                DifficultyModifierSO pick = d;
                bool none = d == null;
                string name = none ? "NONE" : (d.DisplayName ?? d.ModifierId);
                string xpLine = none ? "×1.00 Trainer XP" : $"×{d.TrainerXPMultiplier:0.00} Trainer XP";
                string desc = none ? "Standard run — baseline rewards." : (d.Description ?? "");

                GameObject card = new("Card", typeof(RectTransform)); card.transform.SetParent(_root.transform, false);
                Image ci = card.AddComponent<Image>(); ci.color = none ? new Color(0.16f, 0.22f, 0.18f, 1f) : new Color(0.18f, 0.20f, 0.28f, 1f);
                Place((RectTransform)card.transform, new Vector2(x, 20), new Vector2(cw, 320));
                Txt(card.transform, name, 26, new Color(0.92f, 0.96f, 0.8f), new Vector2(0, 110), new Vector2(cw - 30, 36));
                Txt(card.transform, xpLine, 20, new Color(0.95f, 0.85f, 0.45f), new Vector2(0, 64), new Vector2(cw - 30, 28));
                Txt(card.transform, desc, 17, new Color(0.78f, 0.82f, 0.88f), new Vector2(0, 6), new Vector2(cw - 40, 110));
                Btn(card.transform, new Vector2(0, -110), new Vector2(cw - 80, 56), "SELECT",
                    none ? new Color(0.28f, 0.5f, 0.34f) : new Color(0.30f, 0.40f, 0.55f), () => Choose(pick));
                x += cw + gap;
            }

            Btn(_root.transform, new Vector2(0, -240), new Vector2(300, 56), "◀  BACK",
                new Color(0.42f, 0.34f, 0.36f), () => { Action back = _onBack; Close(); back?.Invoke(); });
        }

        private void Choose(DifficultyModifierSO d)
        {
            Action<DifficultyModifierSO> cb = _onChosen;
            Close();
            cb?.Invoke(d);
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
            Text t = g.AddComponent<Text>(); t.font = _font; t.text = text; t.fontSize = size; t.color = color; t.alignment = TextAnchor.MiddleCenter; t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform rt = t.rectTransform; rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f); rt.anchoredPosition = pos; rt.sizeDelta = sizeD; return t;
        }
        private Button Btn(Transform parent, Vector2 pos, Vector2 size, string label, Color color, Action onClick)
        {
            GameObject g = new("Button", typeof(RectTransform)); g.transform.SetParent(parent, false);
            Image i = g.AddComponent<Image>(); i.color = color;
            RectTransform rt = (RectTransform)g.transform; rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f); rt.anchoredPosition = pos; rt.sizeDelta = size;
            Button b = g.AddComponent<Button>(); if (onClick != null) b.onClick.AddListener(() => onClick());
            Text t = Txt(g.transform, label, 20, Color.white, Vector2.zero, size);
            RectTransform trt = t.rectTransform; trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero; return b;
        }
        private static void Stretch(RectTransform rt) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero; }
        private static void Place(RectTransform rt, Vector2 pos, Vector2 size) { rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f); rt.anchoredPosition = pos; rt.sizeDelta = size; }
    }
}

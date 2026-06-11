using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectAscendant.Core;

namespace ProjectAscendant.UI
{
    // Per §8.3.7 (CL-021 — Q10) — the choice-only Legendary relic picker, shown at a Gym victory (and,
    // post-VS, the Victory Road Summit + Black Market). Offers 1-of-N Legendaries (LegendaryPickService,
    // already excludes held + respects the max-2/run cap) → the player picks exactly 1. onChosen receives
    // the pick; the caller adds it to RunStateSO.HeldRelics. View-layer only; mirrors RegionModifierSelectUI.
    public sealed class LegendaryPickSelectUI : MonoBehaviour
    {
        private Font _font;
        private GameObject _root;
        private Action<RelicSO> _onChosen;

        public bool IsOpen => _root != null;

        public void Open(IReadOnlyList<RelicSO> offer, Action<RelicSO> onChosen)
        {
            _onChosen = onChosen;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_root != null) Destroy(_root);

            List<RelicSO> opts = new();
            if (offer != null) foreach (RelicSO r in offer) if (r != null) opts.Add(r);
            if (opts.Count == 0) { Choose(null); return; } // cap reached / nothing to offer → no-op

            _root = new GameObject("LegendaryPickCanvas");
            Canvas c = _root.AddComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = 54;
            CanvasScaler s = _root.AddComponent<CanvasScaler>(); s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; s.referenceResolution = new Vector2(1920, 1080);
            _root.AddComponent<GraphicRaycaster>();

            Image bg = Img(_root.transform, new Color(0.06f, 0.05f, 0.02f, 1f)); Stretch(bg.rectTransform);

            Txt(_root.transform, "★ LEGENDARY RELIC ★", 46, new Color(0.97f, 0.84f, 0.45f), new Vector2(0, 340), new Vector2(1300, 64));
            Txt(_root.transform, "Choose one — a powerful run-long relic (max 2 per run). §8.3.7", 20, new Color(0.85f, 0.82f, 0.7f), new Vector2(0, 286), new Vector2(1200, 30));

            const float cw = 400f, gap = 40f;
            float x = -((opts.Count - 1) * (cw + gap)) / 2f;
            foreach (RelicSO r in opts)
            {
                RelicSO pick = r;
                GameObject card = new("Card", typeof(RectTransform)); card.transform.SetParent(_root.transform, false);
                Image ci = card.AddComponent<Image>(); ci.color = new Color(0.20f, 0.17f, 0.10f, 1f);
                Place((RectTransform)card.transform, new Vector2(x, 16), new Vector2(cw, 340));
                Txt(card.transform, r.DisplayName ?? r.Id, 28, new Color(0.98f, 0.9f, 0.65f), new Vector2(0, 122), new Vector2(cw - 30, 40));
                Txt(card.transform, "LEGENDARY", 18, new Color(0.95f, 0.78f, 0.4f), new Vector2(0, 84), new Vector2(cw - 30, 26));
                Txt(card.transform, r.EffectDescription ?? "", 18, new Color(0.82f, 0.82f, 0.78f), new Vector2(0, 6), new Vector2(cw - 44, 150));
                Btn(card.transform, new Vector2(0, -120), new Vector2(cw - 80, 58), "CLAIM",
                    new Color(0.55f, 0.42f, 0.18f), () => Choose(pick));
                x += cw + gap;
            }
        }

        private void Choose(RelicSO r)
        {
            Action<RelicSO> cb = _onChosen;
            Close();
            cb?.Invoke(r);
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

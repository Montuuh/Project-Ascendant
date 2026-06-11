using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectAscendant.Core;

namespace ProjectAscendant.UI
{
    // Per §7.8.3.1 (CL-016) — the Region Modifier picker shown at the start of each Region (pre-R1 at
    // run setup; City Reflection for R2/R3). Offers 3 modifiers (RegionModifierPool.BuildOffer) → the
    // player picks exactly 1 (mandatory — no "None"). onChosen receives the pick. View-layer only;
    // mirrors DifficultySelectUI. The caller wires onChosen → RunStateSO.SetRegionModifier.
    public sealed class RegionModifierSelectUI : MonoBehaviour
    {
        private Font _font;
        private GameObject _root;
        private Action<RegionModifierSO> _onChosen;

        public bool IsOpen => _root != null;

        public void Open(IReadOnlyList<RegionModifierSO> offer, Action<RegionModifierSO> onChosen)
        {
            _onChosen = onChosen;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_root != null) Destroy(_root);
            _root = new GameObject("RegionModifierSelectCanvas");
            Canvas c = _root.AddComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = 53;
            CanvasScaler s = _root.AddComponent<CanvasScaler>(); s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; s.referenceResolution = new Vector2(1920, 1080);
            _root.AddComponent<GraphicRaycaster>();

            Image bg = Img(_root.transform, new Color(0.05f, 0.07f, 0.11f, 1f)); Stretch(bg.rectTransform);

            Txt(_root.transform, "REGION MODIFIER", 44, new Color(0.7f, 0.9f, 0.95f), new Vector2(0, 330), new Vector2(1300, 60));
            Txt(_root.transform, "Pick one — it shapes this Region only. §7.8.3.1", 20, new Color(0.78f, 0.82f, 0.9f), new Vector2(0, 280), new Vector2(1200, 30));

            List<RegionModifierSO> opts = new();
            if (offer != null) foreach (RegionModifierSO m in offer) if (m != null) opts.Add(m);
            if (opts.Count == 0) { Choose(null); return; } // nothing to offer → no-op

            const float cw = 380f, gap = 36f;
            float x = -((opts.Count - 1) * (cw + gap)) / 2f;
            foreach (RegionModifierSO m in opts)
            {
                RegionModifierSO pick = m;
                Color tierCol = m.Tier switch
                {
                    RegionModifierTier.Strong => new Color(0.95f, 0.78f, 0.45f),
                    RegionModifierTier.Medium => new Color(0.7f, 0.85f, 0.95f),
                    _ => new Color(0.75f, 0.78f, 0.82f),
                };

                GameObject card = new("Card", typeof(RectTransform)); card.transform.SetParent(_root.transform, false);
                Image ci = card.AddComponent<Image>(); ci.color = new Color(0.16f, 0.19f, 0.26f, 1f);
                Place((RectTransform)card.transform, new Vector2(x, 20), new Vector2(cw, 320));
                Txt(card.transform, m.DisplayName ?? m.ModifierId, 26, new Color(0.92f, 0.96f, 0.9f), new Vector2(0, 110), new Vector2(cw - 30, 36));
                Txt(card.transform, m.Tier.ToString(), 18, tierCol, new Vector2(0, 70), new Vector2(cw - 30, 26));
                Txt(card.transform, m.EffectDescription ?? "", 17, new Color(0.78f, 0.82f, 0.88f), new Vector2(0, 4), new Vector2(cw - 40, 120));
                Btn(card.transform, new Vector2(0, -112), new Vector2(cw - 80, 56), "SELECT",
                    new Color(0.26f, 0.42f, 0.55f), () => Choose(pick));
                x += cw + gap;
            }
        }

        private void Choose(RegionModifierSO m)
        {
            Action<RegionModifierSO> cb = _onChosen;
            Close();
            cb?.Invoke(m);
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

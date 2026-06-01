using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectAscendant.Core;

namespace ProjectAscendant.UI
{
    // Per §6.6.3 + Epic 12 Task 12.11 — the pre-run "choose 1 of 3 Starting Relics" modal. View-layer
    // only; the offer (Common/Uncommon, never Rare) is built by StartingRelicService.
    public sealed class StartingRelicPanelUI : MonoBehaviour
    {
        private Action<RelicSO> _onPicked;
        private Font _font;
        private GameObject _root;

        public bool IsOpen => _root != null;

        public void Open(IReadOnlyList<RelicSO> offer, Action<RelicSO> onPicked)
        {
            _onPicked = onPicked;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _root = new GameObject("StartingRelicCanvas");
            Canvas c = _root.AddComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = 29;
            CanvasScaler s = _root.AddComponent<CanvasScaler>(); s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; s.referenceResolution = new Vector2(1920, 1080);
            _root.AddComponent<GraphicRaycaster>();
            Image bg = Img(_root.transform, new Color(0.06f, 0.08f, 0.12f, 1f)); Stretch(bg.rectTransform);

            Txt(_root.transform, "CHOOSE A STARTING RELIC", 38, new Color(0.85f, 0.95f, 0.7f), new Vector2(0, 260), new Vector2(1300, 52));
            Txt(_root.transform, "Sets your build direction (Common / Uncommon — never Rare). §6.6.3", 20, new Color(0.8f, 0.85f, 0.9f), new Vector2(0, 210), new Vector2(1300, 30));

            float x = offer != null && offer.Count > 0 ? -((offer.Count - 1) * 520f) / 2f : 0f;
            if (offer != null)
                foreach (RelicSO r in offer)
                {
                    if (r == null) continue;
                    RelicSO pick = r;
                    GameObject card = new("Card", typeof(RectTransform)); card.transform.SetParent(_root.transform, false);
                    Image ci = card.AddComponent<Image>(); ci.color = new Color(0.16f, 0.22f, 0.28f, 1f);
                    Place((RectTransform)card.transform, new Vector2(x, 20), new Vector2(480, 240));
                    Txt(card.transform, r.DisplayName ?? r.Id, 24, new Color(0.92f, 0.96f, 0.8f), new Vector2(0, 70), new Vector2(440, 34));
                    Txt(card.transform, $"[{r.Rarity}]", 18, new Color(0.8f, 0.85f, 0.95f), new Vector2(0, 30), new Vector2(440, 26));
                    string cats = r.Categories != null && r.Categories.Count > 0 ? r.Categories[0].ToString() : "";
                    Txt(card.transform, cats, 17, new Color(0.7f, 0.75f, 0.8f), new Vector2(0, -2), new Vector2(440, 24));
                    Btn(card.transform, new Vector2(0, -70), new Vector2(300, 56), "CHOOSE", new Color(0.28f, 0.5f, 0.34f), () => Pick(pick));
                    x += 520f;
                }
            else
                Btn(_root.transform, new Vector2(0, 20), new Vector2(300, 56), "SKIP", new Color(0.42f, 0.34f, 0.36f), () => Pick(null));
        }

        private void Pick(RelicSO r)
        {
            Action<RelicSO> cb = _onPicked;
            if (_root != null) Destroy(_root);
            _root = null;
            cb?.Invoke(r);
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
            Image i = g.AddComponent<Image>(); i.color = color; Place((RectTransform)g.transform, pos, size);
            Button b = g.AddComponent<Button>(); if (onClick != null) b.onClick.AddListener(() => onClick());
            Text t = Txt(g.transform, label, 19, Color.white, Vector2.zero, size); RectTransform rt = t.rectTransform; rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero; return b;
        }
        private static void Stretch(RectTransform rt) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero; }
        private static void Place(RectTransform rt, Vector2 pos, Vector2 size) { rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f); rt.anchoredPosition = pos; rt.sizeDelta = size; }
    }
}

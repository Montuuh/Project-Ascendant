using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectAscendant.Core;
using ProjectAscendant.Map;

namespace ProjectAscendant.UI
{
    // Per §8.6 + Epic 12 Task 12.9/12.5 — the Map-View Inventory: three sections (Consumables / Held
    // Items / Relics) plus the Held-Item equip flow (pick item → pick Box Pokémon → equip; one slot per
    // Pokémon §8.4.1, the displaced item returns to inventory). View-layer only.
    //
    // ⚠ TECH-DEBT (gap #38): uGUI for bridge screenshot-verify; project mandates UI Toolkit. Sort/filter
    // + drag-drop + rich tooltips (12.9.3/12.9.4) → Epic 13.
    public sealed class InventoryPanelUI : MonoBehaviour
    {
        private RunStateSO _state;
        private Box _box;
        private Action _onClosed;
        private Font _font;
        private GameObject _root;
        private RectTransform _body;
        private HeldItemSO _equipping; // when set, the panel is in "pick a Pokémon" mode

        public bool IsOpen => _root != null;

        public void Open(RunStateSO state, Box box, Action onClosed)
        {
            _state = state; _box = box; _onClosed = onClosed; _equipping = null;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Build();
            Refresh();
        }

        private void Close()
        {
            if (_root != null) Destroy(_root);
            _root = null;
            _onClosed?.Invoke();
        }

        private void Build()
        {
            _root = new GameObject("InventoryPanelCanvas");
            Canvas canvas = _root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 27;
            CanvasScaler scaler = _root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            _root.AddComponent<GraphicRaycaster>();
            Image bg = Img(_root.transform, new Color(0.07f, 0.09f, 0.12f, 1f));
            Stretch(bg.rectTransform);
            GameObject b = new("Body", typeof(RectTransform));
            b.transform.SetParent(_root.transform, false);
            _body = (RectTransform)b.transform;
            Place(_body, Mid(), Vector2.zero, new Vector2(1700, 1000));
        }

        private void Refresh()
        {
            for (int i = _body.childCount - 1; i >= 0; i--) Destroy(_body.GetChild(i).gameObject);

            if (_equipping != null) { RenderEquipTargets(); return; }

            Txt(_body, "INVENTORY", 38, new Color(0.85f, 0.95f, 0.7f), Mid(), new Vector2(0, 440), new Vector2(1300, 52));

            // ── Consumables (left) ──────────────────────────────────────────────
            RenderColumn("CONSUMABLES", -540f, ConsumableLines());
            // ── Held Items (centre) — with EQUIP ───────────────────────────────
            RenderHeldColumn(0f);
            // ── Relics (right) ──────────────────────────────────────────────────
            RenderColumn("TRAINER RELICS", 540f, RelicLines());

            Btn(_body, Mid(), new Vector2(0, -440), new Vector2(340, 60), "CLOSE  ✕", new Color(0.42f, 0.34f, 0.36f), true, Close);
        }

        private List<string> ConsumableLines()
        {
            List<string> lines = new();
            Dictionary<string, int> counts = new();
            if (_state?.Inventory != null)
                foreach (ConsumableSO c in _state.Inventory)
                    if (c != null) { string n = c.DisplayName ?? c.name; counts[n] = counts.TryGetValue(n, out int v) ? v + 1 : 1; }
            foreach (KeyValuePair<string, int> kv in counts) lines.Add($"{kv.Key} ×{kv.Value}");
            if (lines.Count == 0) lines.Add("(none)");
            return lines;
        }

        private List<string> RelicLines()
        {
            List<string> lines = new();
            if (_state?.HeldRelics != null)
                foreach (RelicSO r in _state.HeldRelics)
                    if (r != null) lines.Add($"◆ {r.DisplayName ?? r.name}  [{r.Rarity}]");
            if (lines.Count == 0) lines.Add("(none)");
            return lines;
        }

        private void RenderColumn(string title, float x, List<string> lines)
        {
            Txt(_body, title, 24, new Color(0.8f, 0.9f, 1f), Mid(), new Vector2(x, 370), new Vector2(480, 32));
            float y = 320f;
            foreach (string line in lines)
            {
                Image row = Img(_body, new Color(0.13f, 0.16f, 0.2f, 1f));
                Place(row.rectTransform, Mid(), new Vector2(x, y), new Vector2(500, 44));
                Txt(row.transform, line, 19, new Color(0.88f, 0.9f, 0.94f), Mid(), Vector2.zero, new Vector2(480, 40));
                y -= 52f;
            }
        }

        private void RenderHeldColumn(float x)
        {
            Txt(_body, "HELD ITEMS", 24, new Color(0.8f, 0.9f, 1f), Mid(), new Vector2(x, 370), new Vector2(480, 32));
            float y = 320f;
            List<HeldItemSO> items = _state?.OwnedHeldItems;
            if (items == null || items.Count == 0)
                Txt(_body, "(none — equip from drops)", 18, new Color(0.6f, 0.65f, 0.72f), Mid(), new Vector2(x, y), new Vector2(480, 30));
            else
                for (int i = 0; i < items.Count; i++)
                {
                    HeldItemSO it = items[i];
                    if (it == null) continue;
                    Image row = Img(_body, new Color(0.15f, 0.19f, 0.16f, 1f));
                    Place(row.rectTransform, Mid(), new Vector2(x, y), new Vector2(500, 50));
                    Txt(row.transform, it.DisplayName ?? it.Id, 19, new Color(0.9f, 0.95f, 0.88f), Mid(), new Vector2(-90, 0), new Vector2(300, 44));
                    HeldItemSO captured = it;
                    Btn(_body, Mid(), new Vector2(x + 180, y), new Vector2(120, 44), "EQUIP", new Color(0.28f, 0.46f, 0.34f), true,
                        () => { _equipping = captured; Refresh(); });
                    y -= 58f;
                }

            // Show who currently wears what.
            Txt(_body, "Equipped:", 18, new Color(0.7f, 0.75f, 0.8f), Mid(), new Vector2(x, y - 8), new Vector2(480, 26)); y -= 40f;
            if (_box?.Members != null)
                foreach (PokemonInstance p in _box.Members)
                    if (p?.Species != null && p.HeldItem != null)
                    {
                        Txt(_body, $"{p.Species.DisplayName ?? p.Species.name}: {p.HeldItem.DisplayName ?? p.HeldItem.Id}", 17,
                            new Color(0.8f, 0.85f, 0.7f), Mid(), new Vector2(x, y), new Vector2(500, 24));
                        y -= 30f;
                    }
        }

        // §8.4.1 — pick a Box Pokémon to equip _equipping onto (one slot; displaced item returns).
        private void RenderEquipTargets()
        {
            Txt(_body, $"EQUIP {(_equipping.DisplayName ?? _equipping.Id)} to:", 30, new Color(0.85f, 0.95f, 0.7f), Mid(), new Vector2(0, 420), new Vector2(1300, 44));
            float y = 320f;
            List<PokemonInstance> box = _box?.Members ?? new List<PokemonInstance>();
            for (int i = 0; i < box.Count; i++)
            {
                PokemonInstance p = box[i];
                if (p?.Species == null) continue;
                string cur = p.HeldItem != null ? $"  (holds {p.HeldItem.DisplayName ?? p.HeldItem.Id})" : "";
                PokemonInstance mon = p;
                Btn(_body, Mid(), new Vector2(0, y), new Vector2(820, 52),
                    $"{p.Species.DisplayName ?? p.Species.name}  Lv{p.Level}{cur}", new Color(0.26f, 0.40f, 0.5f), true,
                    () => Equip(mon));
                y -= 64f;
            }
            Btn(_body, Mid(), new Vector2(0, y - 16f), new Vector2(320, 54), "◀ BACK", new Color(0.42f, 0.34f, 0.36f), true,
                () => { _equipping = null; Refresh(); });
        }

        private void Equip(PokemonInstance mon)
        {
            if (_equipping == null || mon == null) return;
            _state.OwnedHeldItems?.Remove(_equipping);              // out of inventory
            if (mon.HeldItem != null)                              // §8.4.1 — displaced item returns
                (_state.OwnedHeldItems ??= new List<HeldItemSO>()).Add(mon.HeldItem);
            mon.HeldItem = _equipping;                              // one slot per Pokémon
            _equipping = null;
            Refresh();
        }

        // ── uGUI primitives ───────────────────────────────────────────────────
        private Image Img(Transform parent, Color c)
        {
            GameObject go = new("Image", typeof(RectTransform)); go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>(); img.color = c; return img;
        }
        private Text Txt(Transform parent, string text, int size, Color color, Vector2 anchor, Vector2 pos, Vector2 sizeD)
        {
            GameObject go = new("Text", typeof(RectTransform)); go.transform.SetParent(parent, false);
            Text t = go.AddComponent<Text>(); t.font = _font; t.text = text; t.fontSize = size; t.color = color;
            t.alignment = TextAnchor.MiddleCenter; t.horizontalOverflow = HorizontalWrapMode.Overflow;
            Place(t.rectTransform, anchor, pos, sizeD); return t;
        }
        private Button Btn(Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, string label, Color color, bool interactable, Action onClick)
        {
            GameObject go = new("Button", typeof(RectTransform)); go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>(); img.color = interactable ? color : color * 0.5f;
            Place((RectTransform)go.transform, anchor, pos, size);
            Button btn = go.AddComponent<Button>(); btn.interactable = interactable;
            if (onClick != null) btn.onClick.AddListener(() => onClick());
            Text t = Txt(go.transform, label, 18, Color.white, Mid(), Vector2.zero, size); Stretch(t.rectTransform);
            return btn;
        }
        private static void Stretch(RectTransform rt) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero; }
        private static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size) { rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(0.5f, 0.5f); rt.anchoredPosition = pos; rt.sizeDelta = size; }
        private static Vector2 Mid() => new(0.5f, 0.5f);
    }
}

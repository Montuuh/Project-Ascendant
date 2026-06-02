using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectAscendant.Core;
using ProjectAscendant.Map;
using ProjectAscendant.Progression;

namespace ProjectAscendant.UI
{
    // Per §5.4.1 + Epic 10 Task 10.6 — the Map-View TM application flow. Three steps: pick a TM from
    // the run inventory → pick a COMPATIBLE Box Pokémon (incompatible greyed, 10.6.4) → pick the
    // CurrentMoves slot to replace. Applies via TMApplicator (replace-a-slot, Mastery exempt, TM
    // consumed). View-layer only.
    //
    // ⚠ TECH-DEBT (gap #38): uGUI so the bridge can screenshot-verify; project mandates UI Toolkit.
    public sealed class TMPanelUI : MonoBehaviour
    {
        private Box _box;
        private RunStateSO _state;
        private Action _onClosed;
        private Font _font;
        private GameObject _root;
        private RectTransform _body;

        private TMSO _tm;            // step-2/3 selection
        private PokemonInstance _mon; // step-3 selection

        public bool IsOpen => _root != null;

        public void Open(Box box, RunStateSO state, Action onClosed)
        {
            _box = box; _state = state; _onClosed = onClosed; _tm = null; _mon = null;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Build();
            RefreshBody();
        }

        private void Close()
        {
            if (_root != null) Destroy(_root);
            _root = null;
            _onClosed?.Invoke();
        }

        private void Build()
        {
            _root = new GameObject("TMPanelCanvas");
            Canvas canvas = _root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 26;
            CanvasScaler scaler = _root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            _root.AddComponent<GraphicRaycaster>();

            Image bg = Img(_root.transform, new Color(0.08f, 0.10f, 0.13f, 1f));
            Stretch(bg.rectTransform);

            GameObject bodyGO = new("Body", typeof(RectTransform));
            bodyGO.transform.SetParent(_root.transform, false);
            _body = (RectTransform)bodyGO.transform;
            Place(_body, Mid(), Vector2.zero, new Vector2(1500, 1000));
        }

        private void RefreshBody()
        {
            for (int i = _body.childCount - 1; i >= 0; i--) Destroy(_body.GetChild(i).gameObject);

            List<TMSO> tms = _state?.OwnedTMs;
            Txt(_body, "TECHNICAL MACHINES", 36, new Color(0.85f, 0.95f, 0.7f), Mid(), new Vector2(0, 430), new Vector2(1300, 52));

            if (tms == null || tms.Count == 0)
            {
                Txt(_body, "No TMs in your bag.", 24, new Color(0.8f, 0.85f, 0.9f), Mid(), new Vector2(0, 120), new Vector2(1000, 40));
                Btn(_body, Mid(), new Vector2(0, -40), new Vector2(360, 64), "CLOSE  ✕", new Color(0.42f, 0.34f, 0.36f), true, Close);
                return;
            }

            if (_tm == null) RenderTMList(tms);
            else if (_mon == null) RenderTargetList();
            else RenderSlotPicker();
        }

        // Step 1 — pick a TM.
        private void RenderTMList(List<TMSO> tms)
        {
            Txt(_body, "Choose a TM to teach", 22, new Color(0.8f, 0.85f, 0.9f), Mid(), new Vector2(0, 388), new Vector2(1200, 32));
            float y = 300f;
            for (int i = 0; i < tms.Count; i++)
            {
                TMSO tm = tms[i];
                string move = tm.MoveTeach != null ? (tm.MoveTeach.DisplayName ?? tm.MoveTeach.name) : "?";
                Btn(_body, Mid(), new Vector2(0, y), new Vector2(900, 60),
                    $"{(tm.DisplayName ?? tm.name)}   —   teaches {move}", new Color(0.26f, 0.40f, 0.5f), true,
                    () => { _tm = tm; RefreshBody(); });
                y -= 76f;
            }
            Btn(_body, Mid(), new Vector2(0, y - 20f), new Vector2(360, 60), "CLOSE  ✕", new Color(0.42f, 0.34f, 0.36f), true, Close);
        }

        // Step 2 — pick a compatible Box Pokémon (incompatible greyed).
        private void RenderTargetList()
        {
            string move = _tm.MoveTeach != null ? (_tm.MoveTeach.DisplayName ?? _tm.MoveTeach.name) : "?";
            Txt(_body, $"Teach {move} to which Pokémon?", 22, new Color(0.8f, 0.85f, 0.9f), Mid(), new Vector2(0, 388), new Vector2(1200, 32));
            List<PokemonInstance> box = _box?.Members ?? new List<PokemonInstance>();
            float y = 300f;
            for (int i = 0; i < box.Count; i++)
            {
                PokemonInstance p = box[i];
                if (p?.Species == null) continue;
                bool ok = TMApplicator.IsCompatible(_tm, p);
                bool known = p.CurrentMoves.Contains(_tm.MoveTeach);
                string name = p.Species.DisplayName ?? p.Species.name;
                string suffix = !ok ? "  (incompatible)" : known ? "  (already knows it)" : "";
                Btn(_body, Mid(), new Vector2(0, y), new Vector2(900, 56),
                    $"{name}  Lv{p.Level}{suffix}", ok && !known ? new Color(0.28f, 0.44f, 0.34f) : new Color(0.3f, 0.3f, 0.34f),
                    ok && !known, () => { _mon = p; RefreshBody(); });
                y -= 70f;
            }
            Btn(_body, Mid(), new Vector2(0, y - 20f), new Vector2(360, 60), "◀ BACK", new Color(0.42f, 0.34f, 0.36f), true,
                () => { _tm = null; RefreshBody(); });
        }

        // Step 3 — confirm. Per §5.10 the TM ADDS the move to the Pokémon's Learned Move Pool;
        // the player equips it into the active 4 later via the Move Manager (Mastery untouched).
        private void RenderSlotPicker()
        {
            string move = _tm.MoveTeach != null ? (_tm.MoveTeach.DisplayName ?? _tm.MoveTeach.name) : "?";
            string monName = _mon.Species != null ? (_mon.Species.DisplayName ?? _mon.Species.name) : "?";
            Txt(_body, $"Teach {move} to {monName}?", 22, new Color(0.8f, 0.9f, 1f), Mid(), new Vector2(0, 388), new Vector2(1200, 32));
            Txt(_body, "Added to its move pool — equip it in the Move Manager (§5.10).", 18, new Color(0.8f, 0.95f, 0.85f), Mid(), new Vector2(0, 348), new Vector2(1200, 28));
            Btn(_body, Mid(), new Vector2(0, 120), new Vector2(340, 70), $"✔ LEARN {move}",
                new Color(0.30f, 0.50f, 0.36f), true, () =>
                {
                    TMApplicator.Apply(_state, _tm, _mon);
                    Close();
                });
            Btn(_body, Mid(), new Vector2(0, -40), new Vector2(360, 60), "◀ BACK", new Color(0.42f, 0.34f, 0.36f), true,
                () => { _mon = null; RefreshBody(); });
        }

        // ── uGUI primitives ───────────────────────────────────────────────────

        private Image Img(Transform parent, Color c)
        {
            GameObject go = new("Image", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>(); img.color = c;
            return img;
        }

        private Text Txt(Transform parent, string text, int size, Color color, Vector2 anchor, Vector2 pos, Vector2 sizeD)
        {
            GameObject go = new("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Text t = go.AddComponent<Text>();
            t.font = _font; t.text = text; t.fontSize = size; t.color = color;
            t.alignment = TextAnchor.MiddleCenter; t.horizontalOverflow = HorizontalWrapMode.Overflow;
            Place(t.rectTransform, anchor, pos, sizeD);
            return t;
        }

        private Button Btn(Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, string label, Color color, bool interactable, Action onClick)
        {
            GameObject go = new("Button", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>(); img.color = interactable ? color : color * 0.5f;
            Place((RectTransform)go.transform, anchor, pos, size);
            Button btn = go.AddComponent<Button>(); btn.interactable = interactable;
            if (onClick != null) btn.onClick.AddListener(() => onClick());
            Text t = Txt(go.transform, label, 19, interactable ? Color.white : new Color(1, 1, 1, 0.55f), Mid(), Vector2.zero, size);
            Stretch(t.rectTransform);
            return btn;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        private static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
        }

        private static Vector2 Mid() => new(0.5f, 0.5f);
    }
}

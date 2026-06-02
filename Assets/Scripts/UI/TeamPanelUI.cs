using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectAscendant.Core;
using ProjectAscendant.Map;
using ProjectAscendant.Progression;

namespace ProjectAscendant.UI
{
    // Per §2.3 + Epic 9 Task 9.10 (the deferred Map-View loadout UI) — manage the Active Team from the
    // Map View between nodes. Shows the whole Box; the player toggles up to 3 Pokémon into the team,
    // picks the Lead, and Confirms through the real LoadoutManager (which enforces §2.4.1 no-fainted,
    // distinct, 1..3, and the lock-on-node-entry rule). View-layer only.
    //
    // ⚠ TECH-DEBT (gap #38): uGUI so the bridge can screenshot-verify; project mandates UI Toolkit.
    public sealed class TeamPanelUI : MonoBehaviour
    {
        private Box _box;
        private RunStateSO _state;
        private LoadoutManager _loadout;
        private EconomyConfigSO _economy; // §6.2.5 — Trauma params for the Effective-Max-HP preview
        private Action _onClosed;
        private Action<PokemonInstance> _onEvolve; // §5.3.1 — request the Branch-Selection modal
        private Font _font;
        private GameObject _root;
        private RectTransform _body;
        private MoveManagerUI _moveManager; // §5.10.2 — paid entry from Map View

        // Working selection (box indices, ordered) + the Lead's position within it.
        private readonly List<int> _selected = new();
        private int _leadPos;

        public bool IsOpen => _root != null;

        public void Open(Box box, RunStateSO state, LoadoutManager loadout, Action onClosed,
                         Action<PokemonInstance> onEvolve = null, EconomyConfigSO economy = null)
        {
            _box = box; _state = state; _loadout = loadout; _onClosed = onClosed; _onEvolve = onEvolve;
            _economy = economy;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // §5.10.2 — lazy-init the Move Manager UI if needed.
            if (_moveManager == null)
                _moveManager = gameObject.AddComponent<MoveManagerUI>();

            // Seed the working selection from the current Active Team.
            _selected.Clear();
            if (state?.ActiveTeamIndices != null)
                foreach (int i in state.ActiveTeamIndices)
                    if (i >= 0 && i < box.Members.Count && !_selected.Contains(i)) _selected.Add(i);
            _leadPos = state != null ? Mathf.Clamp(state.LeadIndex, 0, Mathf.Max(0, _selected.Count - 1)) : 0;

            Build();
            RefreshBody();
        }

        private void Close()
        {
            if (_root != null) Destroy(_root);
            _root = null;
            _onClosed?.Invoke();
        }

        // ── Selection logic ───────────────────────────────────────────────────

        private void ToggleMember(int boxIdx)
        {
            if (_selected.Contains(boxIdx))
            {
                int pos = _selected.IndexOf(boxIdx);
                _selected.Remove(boxIdx);
                if (pos < _leadPos) _leadPos--;
                _leadPos = Mathf.Clamp(_leadPos, 0, Mathf.Max(0, _selected.Count - 1));
            }
            else
            {
                if (_selected.Count >= LoadoutManager.MAX_ACTIVE_TEAM) return;
                PokemonInstance p = _box.Members[boxIdx];
                if (p == null || p.CurrentHP == 0) return; // §2.4.1 — no fainted in the team
                _selected.Add(boxIdx);
            }
            RefreshBody();
        }

        private void SetLead(int boxIdx)
        {
            int pos = _selected.IndexOf(boxIdx);
            if (pos >= 0) { _leadPos = pos; RefreshBody(); }
        }

        private void ConfirmAndClose()
        {
            if (_loadout.Confirm(_selected, _leadPos)) Close();
        }

        // ── Build / render ────────────────────────────────────────────────────

        private void Build()
        {
            _root = new GameObject("TeamPanelCanvas");
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

            Txt(_body, "ACTIVE TEAM", 38, new Color(0.85f, 0.95f, 0.7f), Mid(), new Vector2(0, 430), new Vector2(1300, 54));
            Txt(_body, $"{_selected.Count}/{LoadoutManager.MAX_ACTIVE_TEAM} selected   ·   tap ADD/REMOVE, then ★ to set the Lead",
                22, new Color(0.8f, 0.85f, 0.9f), Mid(), new Vector2(0, 388), new Vector2(1300, 34));

            int count = _box != null ? _box.Members.Count : 0;
            float y = 300f;

            // Per §2.3 + §3.3 + Bug R2-1 — render the active-team members FIRST, in LEAD-then-BENCH order.
            if (_selected.Count > 0)
            {
                // Build a reordered list: Lead first, then bench members in stable order.
                List<int> orderedActive = new();
                if (_leadPos >= 0 && _leadPos < _selected.Count) orderedActive.Add(_selected[_leadPos]);
                for (int i = 0; i < _selected.Count; i++)
                    if (i != _leadPos) orderedActive.Add(_selected[i]);

                foreach (int boxIdx in orderedActive) { BuildBoxRow(boxIdx, y); y -= 86f; }
            }

            // Then render the rest of the Box (non-selected members).
            for (int i = 0; i < count; i++)
                if (!_selected.Contains(i)) { BuildBoxRow(i, y); y -= 86f; }

            bool valid = _loadout != null && _loadout.IsValidSelection(_selected, _leadPos) && !_loadout.IsLocked;
            Btn(_body, Mid(), new Vector2(-260, y - 20f), new Vector2(380, 66), "✔ CONFIRM",
                new Color(0.28f, 0.52f, 0.36f), valid, ConfirmAndClose);
            Btn(_body, Mid(), new Vector2(260, y - 20f), new Vector2(380, 66), "CLOSE  ✕",
                new Color(0.42f, 0.34f, 0.36f), true, Close);
        }

        private void BuildBoxRow(int boxIdx, float y)
        {
            PokemonInstance p = _box.Members[boxIdx];
            bool inTeam = _selected.Contains(boxIdx);
            bool isLead = inTeam && _selected.IndexOf(boxIdx) == _leadPos;
            bool fainted = p == null || p.CurrentHP == 0;

            Color bg = isLead ? new Color(0.16f, 0.26f, 0.19f, 1f)
                     : inTeam ? new Color(0.15f, 0.20f, 0.24f, 1f)
                     : fainted ? new Color(0.18f, 0.13f, 0.14f, 1f)
                               : new Color(0.13f, 0.15f, 0.17f, 1f);
            Image row = Img(_body, bg);
            Place(row.rectTransform, Mid(), new Vector2(0, y), new Vector2(1180, 76));

            string name = p?.Species != null ? (p.Species.DisplayName ?? p.Species.name) : "—";
            // §6.2.5 — the team panel previews the Trauma-adjusted ceiling, not the base max.
            int effMax = p == null ? 0
                       : _economy != null ? PokemonVitals.EffectiveMaxHP(p, _economy)
                       : PokemonVitals.MaxHP(p);
            int hp = p?.CurrentHP ?? 0;
            int stacks = p?.TraumaStacks ?? 0;
            string st = p != null && p.PrimaryStatus != StatusCondition.None ? $"  [{p.PrimaryStatus}]" : "";
            // Per §2.3 + §3.3.1 + Bug R2-1 — label active-team members as LEAD / BENCH 1 / BENCH 2.
            string role = !inTeam ? "" : isLead ? "★ LEAD   " : RoleName(_selected.IndexOf(boxIdx), _leadPos);
            Color nameCol = fainted ? new Color(0.62f, 0.5f, 0.5f) : new Color(0.92f, 0.97f, 0.92f);
            Txt(row.transform, $"{role}{name}  Lv{p?.Level ?? 0}    HP {hp}/{effMax}{st}", 22, nameCol,
                Mid(), new Vector2(-360, 0), new Vector2(680, 50));

            // §6.2.5 — Trauma badge (⚠N) + the lost ceiling, amber, only when traumatized.
            if (stacks > 0 && !fainted)
                Txt(row.transform, $"⚠{stacks}  ·  base {PokemonVitals.MaxHP(p)}", 18,
                    new Color(1f, 0.78f, 0.32f), Mid(), new Vector2(120, -24), new Vector2(360, 26));

            // ADD / REMOVE toggle (fainted is locked out of the team — §2.4.1).
            bool full = _selected.Count >= LoadoutManager.MAX_ACTIVE_TEAM;
            string tgl = fainted ? "FAINTED" : inTeam ? "REMOVE" : "ADD";
            Color tglCol = fainted ? new Color(0.5f, 0.3f, 0.3f) : inTeam ? new Color(0.5f, 0.36f, 0.34f) : new Color(0.28f, 0.46f, 0.34f);
            bool tglOn = !fainted && (inTeam || !full);
            Btn(_body, Mid(), new Vector2(370, y), new Vector2(150, 56), tgl, tglCol, tglOn, () => ToggleMember(boxIdx));

            // ★ Lead selector (only for selected, non-lead members).
            if (inTeam && !isLead)
                Btn(_body, Mid(), new Vector2(530, y), new Vector2(120, 56), "★ LEAD",
                    new Color(0.40f, 0.40f, 0.24f), true, () => SetLead(boxIdx));

            // §5.3.1 — evolution-eligible (reached EvolveLevel with a branch) → open Branch Selection.
            if (!fainted && p != null && _onEvolve != null && LevelUpResolver.IsEvolutionEligible(p))
            {
                PokemonInstance mon = p;
                Btn(_body, Mid(), new Vector2(150, y), new Vector2(170, 56), "✦ EVOLVE",
                    new Color(0.58f, 0.42f, 0.22f), true, () => { _onEvolve(mon); Close(); });
            }

            // §5.10.2 — PAID Move Manager from Map View (50₽). Only for non-fainted Pokémon.
            // Position left of the Evolve button (or in the same spot if no Evolve button).
            if (!fainted && p != null && _economy != null)
            {
                int cost = _economy.MoveReconfigCost;
                bool affordable = _state != null && _state.PokeDollars >= cost;
                PokemonInstance mon = p;
                float xPos = _onEvolve != null && LevelUpResolver.IsEvolutionEligible(p) ? -40f : 150f;
                Btn(_body, Mid(), new Vector2(xPos, y), new Vector2(170, 56), $"MOVES ({cost}₽)",
                    new Color(0.34f, 0.48f, 0.56f), affordable, () => OpenMoveManager(mon));
            }
        }

        // §5.10.2 — open the Move Manager for a Pokémon. Deduct cost BEFORE opening (service validates).
        private void OpenMoveManager(PokemonInstance mon)
        {
            if (mon == null || _state == null || _economy == null) return;

            // Deduct the cost. If it fails (unaffordable), abort — button should have been disabled.
            if (!MoveLoadoutService.DeductReconfigCost(_state, _economy))
            {
                Debug.LogWarning("[TeamPanelUI] Move Manager cost deduction failed — button should have been disabled.");
                return;
            }

            // Close this panel and open the Move Manager. On close, re-open this panel.
            Close();
            _moveManager.Open(mon, () =>
            {
                // Re-open this panel after the Move Manager closes.
                Open(_box, _state, _loadout, _onClosed, _onEvolve, _economy);
            });
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
            Text t = Txt(go.transform, label, 19, interactable ? Color.white : new Color(1, 1, 1, 0.7f), Mid(), Vector2.zero, size);
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

        // Per §2.3 + §3.3 + Bug R2-1 — produce "BENCH 1   " or "BENCH 2   " for non-lead members.
        private static string RoleName(int posInSelected, int leadPos)
        {
            if (posInSelected == leadPos) return "★ LEAD   ";
            int benchIdx = posInSelected < leadPos ? posInSelected + 1 : posInSelected - leadPos;
            return $"BENCH {benchIdx}   ";
        }
    }
}

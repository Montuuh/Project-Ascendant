using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectAscendant.Core;
using ProjectAscendant.Progression;

namespace ProjectAscendant.UI
{
    // Per §5.10 (approved 2026-06-02, pending Notion lock) — Move Manager screen. Lets the player
    // configure which 4 moves from the Learned Move Pool are active (contributing to the Skill Deck).
    // The Mastery Move is shown as a 5th immutable slot (§4.3.9.2). All mutations go through
    // MoveLoadoutService — this UI owns no game state. Entry points:
    //   • FREE at Pokémon Center (§5.10.2).
    //   • PAID from Map View Team panel — 50₽ cost (EconomyConfigSO.MoveReconfigCost).
    //   • FREE post-evolution (if caller chooses to offer it).
    //
    // ⚠ TECH-DEBT (gap #38): uGUI so the bridge can screenshot-verify; project mandates UI Toolkit.
    public sealed class MoveManagerUI : MonoBehaviour
    {
        private PokemonInstance _pokemon;
        private Action _onClosed;
        private Font _font;
        private GameObject _root;
        private RectTransform _body;

        // Working selection: the 4 moves the player has chosen. On open, seed from CurrentMoves.
        // On Confirm, commit via MoveLoadoutService.SetActiveMoves.
        private readonly List<MoveSO> _workingActive = new(4);

        public bool IsOpen => _root != null;

        public void Open(PokemonInstance pokemon, Action onClosed)
        {
            if (pokemon == null) return;

            _pokemon = pokemon;
            _onClosed = onClosed;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Seed working selection from current active 4.
            _workingActive.Clear();
            for (int i = 0; i < pokemon.CurrentMoves.Count && i < 4; i++)
                _workingActive.Add(pokemon.CurrentMoves[i]);

            Build();
            RefreshBody();
        }

        private void Close(bool commit)
        {
            if (commit && _pokemon != null)
            {
                // §5.10.2 — commit the working selection to CurrentMoves via the service.
                MoveLoadoutService.SetActiveMoves(_pokemon, _workingActive);
            }

            if (_root != null) Destroy(_root);
            _root = null;
            _onClosed?.Invoke();
        }

        // ── Toggle logic ──────────────────────────────────────────────────────

        private void ToggleMove(MoveSO move)
        {
            if (move == null) return;

            // §4.3.9.2 — Mastery is immutable, can't be toggled.
            if (move == _pokemon.MasteryMove) return;

            // If already in the working active, remove it (if count > 4, let them trim).
            if (_workingActive.Contains(move))
            {
                _workingActive.Remove(move);
                RefreshBody();
                return;
            }

            // Add if we haven't hit the 4-move budget yet.
            if (_workingActive.Count < 4)
            {
                _workingActive.Add(move);
                RefreshBody();
            }
        }

        // ── Build / render ────────────────────────────────────────────────────

        private void Build()
        {
            _root = new GameObject("MoveManagerCanvas");
            Canvas canvas = _root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 27; // above TeamPanel (26) and NodePanel (25)
            CanvasScaler scaler = _root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            _root.AddComponent<GraphicRaycaster>();

            Image bg = Img(_root.transform, new Color(0.08f, 0.10f, 0.13f, 1f));
            Stretch(bg.rectTransform);

            GameObject bodyGO = new("Body", typeof(RectTransform));
            bodyGO.transform.SetParent(_root.transform, false);
            _body = (RectTransform)bodyGO.transform;
            Place(_body, Mid(), Vector2.zero, new Vector2(1600, 1000));
        }

        private void RefreshBody()
        {
            for (int i = _body.childCount - 1; i >= 0; i--) Destroy(_body.GetChild(i).gameObject);

            string pokeName = _pokemon?.Species != null ? (_pokemon.Species.DisplayName ?? _pokemon.Species.name) : "—";
            Txt(_body, $"MOVE MANAGER — {pokeName}", 38, new Color(0.85f, 0.95f, 0.7f), Mid(), new Vector2(0, 450), new Vector2(1400, 54));

            // Validation: are we at exactly 4?
            bool valid = _workingActive.Count == 4 && MoveLoadoutService.ValidateActiveLoadout(_pokemon, _workingActive);
            string status = _workingActive.Count == 4 && valid ? "Ready to confirm"
                          : _workingActive.Count < 4 ? $"Select {4 - _workingActive.Count} more move(s)"
                          : "Invalid selection";
            Color statusCol = valid && _workingActive.Count == 4 ? new Color(0.6f, 0.9f, 0.7f)
                            : new Color(0.9f, 0.7f, 0.4f);
            Txt(_body, status, 22, statusCol, Mid(), new Vector2(0, 406), new Vector2(1300, 34));

            float y = 356f;

            // §5.10.2 — Active 4 section (fixed, top).
            Txt(_body, "ACTIVE 4", 26, new Color(0.8f, 0.95f, 0.85f), Mid(), new Vector2(0, y), new Vector2(1300, 36));
            y -= 32f;
            Txt(_body, "(These moves contribute to your Skill Deck)", 16, new Color(0.75f, 0.8f, 0.85f), Mid(), new Vector2(0, y), new Vector2(1000, 24));
            y -= 34f;

            for (int i = 0; i < 4; i++)
            {
                MoveSO m = i < _workingActive.Count ? _workingActive[i] : null;
                RenderMoveRow(_body, Mid(), m, y, isActive: true, slotLabel: $"SLOT {i + 1}");
                y -= 56f;
            }

            y -= 6f;

            // §4.3.9.2 — Mastery Move (immutable 5th slot, fixed).
            Txt(_body, "MASTERY MOVE (Immutable, fixed 5th card)", 22, new Color(0.9f, 0.78f, 0.55f), Mid(), new Vector2(0, y), new Vector2(1300, 30));
            y -= 32f;
            RenderMoveRow(_body, Mid(), _pokemon.MasteryMove, y, isActive: false, slotLabel: "MASTERY", isMastery: true);
            y -= 56f;

            // §5.10.1 — Learned Move Pool (SCROLLABLE — the pool can grow to 8+ entries; see BuildPoolScroll).
            Txt(_body, "LEARNED MOVE POOL", 26, new Color(0.8f, 0.95f, 0.85f), Mid(), new Vector2(0, y), new Vector2(1300, 36));
            y -= 30f;
            Txt(_body, "Tap ADD / REMOVE to set your Active 4  —  scroll for more", 16, new Color(0.75f, 0.8f, 0.85f), Mid(), new Vector2(0, y), new Vector2(1100, 24));

            BuildPoolScroll(_pokemon.LearnedMoves);

            // Buttons pinned to a FIXED bottom position so a large pool never pushes them off-screen.
            Btn(_body, Mid(), new Vector2(-260, -462), new Vector2(380, 60), "✔ CONFIRM",
                new Color(0.28f, 0.52f, 0.36f), valid, () => Close(true));
            Btn(_body, Mid(), new Vector2(260, -462), new Vector2(380, 60), "CANCEL  ✕",
                new Color(0.42f, 0.34f, 0.36f), true, () => Close(false));
        }

        // §5.10.1 — scrollable Learned Move Pool. A clipped viewport + a tall content rect inside a
        // ScrollRect (vertical, clamped). The pool can grow to 8+ entries, so it must scroll at any
        // resolution. Rows live in `content` (top-anchored) and scroll; the viewport masks the overflow.
        private void BuildPoolScroll(List<MoveSO> pool)
        {
            GameObject viewportGO = new("PoolViewport", typeof(RectTransform));
            viewportGO.transform.SetParent(_body, false);
            RectTransform viewport = (RectTransform)viewportGO.transform;
            Image vpBg = viewportGO.AddComponent<Image>();
            vpBg.color = new Color(0.06f, 0.07f, 0.09f, 0.55f); // also the ScrollRect drag/raycast target
            viewportGO.AddComponent<UnityEngine.UI.RectMask2D>(); // clip rows to the box
            Place(viewport, Mid(), new Vector2(0, -245f), new Vector2(1340, 360));

            GameObject contentGO = new("PoolContent", typeof(RectTransform));
            contentGO.transform.SetParent(viewport, false);
            RectTransform content = (RectTransform)contentGO.transform;
            content.anchorMin = new Vector2(0.5f, 1f);
            content.anchorMax = new Vector2(0.5f, 1f);
            content.pivot = new Vector2(0.5f, 1f);           // top-pivot: rows fill downward from the top
            content.anchoredPosition = Vector2.zero;

            const float rowSpacing = 60f;
            int count = pool?.Count ?? 0;
            float contentHeight = Mathf.Max(360f, count * rowSpacing + 10f);
            content.sizeDelta = new Vector2(1320, contentHeight);

            UnityEngine.UI.ScrollRect sr = viewportGO.AddComponent<UnityEngine.UI.ScrollRect>();
            sr.viewport = viewport;
            sr.content = content;
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
            sr.scrollSensitivity = 30f;

            for (int i = 0; i < count; i++)
            {
                MoveSO m = pool[i];
                float rowY = -(i * rowSpacing) - rowSpacing / 2f; // from content top
                RenderMoveRow(content, new Vector2(0.5f, 1f), m, rowY, isActive: false, slotLabel: null,
                    isInPool: true, isActiveInPool: _workingActive.Contains(m));
            }
        }

        // Renders a move row under `parent` at the given anchor + y. The toggle button is parented to
        // the ROW (not _body) so pool rows scroll as a unit inside the ScrollRect content.
        private void RenderMoveRow(Transform parent, Vector2 anchor, MoveSO move, float y, bool isActive, string slotLabel, bool isMastery = false, bool isInPool = false, bool isActiveInPool = false)
        {
            Color rowBg;
            if (isMastery)
                rowBg = new Color(0.24f, 0.20f, 0.16f, 1f); // amber-ish for Mastery
            else if (isActiveInPool && isActive)
                rowBg = new Color(0.18f, 0.26f, 0.20f, 1f); // green-tinted for active from pool
            else if (isActive)
                rowBg = new Color(0.16f, 0.22f, 0.26f, 1f); // neutral active
            else
                rowBg = new Color(0.13f, 0.15f, 0.17f, 1f); // pool default

            Image row = Img(parent, rowBg);
            Place(row.rectTransform, anchor, new Vector2(0, y), new Vector2(1280, 52));

            // If this is a placeholder slot (no move yet), just show the slot label.
            if (move == null)
            {
                string label = slotLabel ?? "EMPTY";
                Txt(row.transform, label, 20, new Color(0.6f, 0.65f, 0.7f), Mid(), new Vector2(0, 0), new Vector2(1200, 46));
                return;
            }

            // Move info: name, type, AP, power/role.
            string name = move.DisplayName ?? move.name;
            string type = move.Type.ToString();
            int ap = move.APCost;
            string role = move.Role.ToString();
            int pwr = move.BasePower;
            string info = $"{name}   ·   {type}   ·   {ap} AP   ·   {role} {pwr}";

            if (slotLabel != null && !isMastery)
                info = $"{slotLabel}: {info}";

            Color txtCol = isMastery ? new Color(1f, 0.92f, 0.7f)
                         : isActiveInPool ? new Color(0.7f, 0.95f, 0.75f)
                         : new Color(0.92f, 0.97f, 0.92f);
            Txt(row.transform, info, 19, txtCol, Mid(), new Vector2(-60, 0), new Vector2(1080, 46));

            // Toggle button for pool rows (parented to the row so it scrolls with it).
            if (isInPool && !isMastery)
            {
                string btnLabel = isActiveInPool ? "REMOVE" : "ADD";
                Color btnCol = isActiveInPool ? new Color(0.5f, 0.36f, 0.34f) : new Color(0.28f, 0.46f, 0.34f);
                bool canToggle = isActiveInPool || _workingActive.Count < 4;
                Btn(row.transform, Mid(), new Vector2(540, 0), new Vector2(140, 44), btnLabel, btnCol, canToggle, () => ToggleMove(move));
            }
            else if (isMastery)
            {
                // §4.3.9.2 — show LOCKED badge.
                Txt(row.transform, "🔒 LOCKED", 18, new Color(0.8f, 0.75f, 0.7f), Mid(), new Vector2(520, 0), new Vector2(160, 46));
            }
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
    }
}

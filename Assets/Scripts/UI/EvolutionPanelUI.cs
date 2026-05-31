using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using ProjectAscendant.Core;
using ProjectAscendant.Progression;

namespace ProjectAscendant.UI
{
    // Per §5.3.3 + Epic 10 Task 10.3/10.4 — the Branch-Selection modal. Opened from the Team panel
    // when a Pokémon is evolution-eligible. Shows each authored branch (VS = Vanguard) with its
    // archetype, the move upgrades/additions it applies, and the ability it grants; Specialist/Support
    // appear greyed "Coming Soon" (10.3.6). Choosing a branch arms a "this cannot be undone" confirm
    // (10.3.5); confirming runs EvolutionExecutor and closes. View-layer only.
    //
    // ⚠ TECH-DEBT (gap #38): uGUI so the bridge can screenshot-verify; project mandates UI Toolkit.
    public sealed class EvolutionPanelUI : MonoBehaviour
    {
        private PokemonInstance _mon;
        private Action _onClosed;
        private Font _font;
        private GameObject _root;
        private RectTransform _body;
        private EvolutionBranchSO _pending; // armed branch awaiting "cannot be undone" confirm

        public bool IsOpen => _root != null;

        public void Open(PokemonInstance mon, Action onClosed)
        {
            _mon = mon; _onClosed = onClosed; _pending = null;
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

        private void Confirm()
        {
            if (_pending != null) EvolutionExecutor.Evolve(_mon, _pending);
            Close();
        }

        // ── Build / render ────────────────────────────────────────────────────

        private void Build()
        {
            _root = new GameObject("EvolutionPanelCanvas");
            Canvas canvas = _root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 27; // above the Team panel (26)
            CanvasScaler scaler = _root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            _root.AddComponent<GraphicRaycaster>();

            Image bg = Img(_root.transform, new Color(0.07f, 0.08f, 0.12f, 1f));
            Stretch(bg.rectTransform);

            GameObject bodyGO = new("Body", typeof(RectTransform));
            bodyGO.transform.SetParent(_root.transform, false);
            _body = (RectTransform)bodyGO.transform;
            Place(_body, Mid(), Vector2.zero, new Vector2(1500, 1000));
        }

        private void RefreshBody()
        {
            for (int i = _body.childCount - 1; i >= 0; i--) Destroy(_body.GetChild(i).gameObject);

            string name = _mon?.Species != null ? (_mon.Species.DisplayName ?? _mon.Species.name) : "—";
            Txt(_body, $"EVOLVE  {name}", 38, new Color(0.85f, 0.95f, 0.7f), Mid(), new Vector2(0, 430), new Vector2(1300, 54));
            Txt(_body, "Evolution permanently rewrites this Pokémon's cards and identity.", 22,
                new Color(0.8f, 0.85f, 0.9f), Mid(), new Vector2(0, 388), new Vector2(1300, 32));

            if (_pending != null) { RenderConfirm(); return; }

            List<EvolutionBranchSO> branches = _mon?.Species != null ? _mon.Species.Branches : null;
            float y = 300f;
            if (branches != null)
                for (int i = 0; i < branches.Count; i++)
                    if (branches[i] != null) { RenderBranchCard(branches[i], y); y -= 240f; }

            // §10.3.6 — Specialist/Support present but greyed "Coming Soon" in the VS.
            RenderLockedCard("SPECIALIST", y); y -= 96f;
            RenderLockedCard("SUPPORT", y);

            Btn(_body, Mid(), new Vector2(0, -440), new Vector2(360, 60), "LATER  ✕",
                new Color(0.42f, 0.34f, 0.36f), true, Close);
        }

        private void RenderBranchCard(EvolutionBranchSO branch, float y)
        {
            string evolved = branch.EvolvedSpecies != null
                ? (branch.EvolvedSpecies.DisplayName ?? branch.EvolvedSpecies.name) : "?";
            Image card = Img(_body, new Color(0.16f, 0.24f, 0.19f, 1f));
            Place(card.rectTransform, Mid(), new Vector2(-200, y), new Vector2(900, 220));

            string label = !string.IsNullOrEmpty(branch.DisplayName) ? branch.DisplayName : branch.Archetype.ToString();
            Txt(card.transform, $"▶ {branch.Archetype}  —  {label}   →   {evolved}", 24,
                new Color(0.9f, 0.97f, 0.9f), Mid(), new Vector2(0, 78), new Vector2(840, 32));
            Txt(card.transform, MoveDiff(branch), 19, new Color(0.82f, 0.9f, 0.82f), Mid(), new Vector2(0, 6), new Vector2(840, 110));

            string ab = branch.GrantedAbility != null ? branch.GrantedAbility.name : "—";
            Txt(card.transform, $"Ability: {ab}", 19, new Color(0.75f, 0.85f, 0.95f), Mid(), new Vector2(0, -70), new Vector2(840, 28));

            EvolutionBranchSO b = branch;
            Btn(_body, Mid(), new Vector2(420, y), new Vector2(240, 90), "CHOOSE\nTHIS PATH",
                new Color(0.28f, 0.5f, 0.34f), true, () => { _pending = b; RefreshBody(); });
        }

        private string MoveDiff(EvolutionBranchSO branch)
        {
            StringBuilder sb = new();
            if (branch.MoveUpgrades != null)
                for (int i = 0; i < branch.MoveUpgrades.Count; i++)
                {
                    MoveUpgradePair up = branch.MoveUpgrades[i];
                    if (up.OldMove == null || up.NewMove == null) continue;
                    sb.AppendLine($"↑ {MoveName(up.OldMove)}  →  {MoveName(up.NewMove)}");
                }
            if (branch.NewMoves != null)
                for (int i = 0; i < branch.NewMoves.Count; i++)
                    if (branch.NewMoves[i] != null) sb.AppendLine($"+ {MoveName(branch.NewMoves[i])}  (new)");
            if (sb.Length == 0) sb.Append("Moves retained.");
            return sb.ToString().TrimEnd();
        }

        private static string MoveName(MoveSO m) => m == null ? "?" : (m.DisplayName ?? m.name);

        private void RenderLockedCard(string archetype, float y)
        {
            Image card = Img(_body, new Color(0.14f, 0.14f, 0.16f, 1f));
            Place(card.rectTransform, Mid(), new Vector2(-200, y), new Vector2(900, 76));
            Txt(card.transform, $"{archetype}  —  Coming Soon", 22, new Color(0.55f, 0.55f, 0.6f), Mid(), Vector2.zero, new Vector2(840, 30));
            Btn(_body, Mid(), new Vector2(420, y), new Vector2(240, 60), "LOCKED",
                new Color(0.3f, 0.3f, 0.34f), false, null);
        }

        private void RenderConfirm()
        {
            string evolved = _pending.EvolvedSpecies != null
                ? (_pending.EvolvedSpecies.DisplayName ?? _pending.EvolvedSpecies.name) : "?";
            Txt(_body, $"Evolve into {evolved}?", 30, new Color(0.9f, 0.97f, 0.9f), Mid(), new Vector2(0, 80), new Vector2(1200, 40));
            Txt(_body, "⚠  This cannot be undone.", 24, new Color(1f, 0.8f, 0.4f), Mid(), new Vector2(0, 20), new Vector2(1200, 34));
            Btn(_body, Mid(), new Vector2(-220, -70), new Vector2(360, 66), "✔ CONFIRM EVOLUTION",
                new Color(0.28f, 0.52f, 0.36f), true, Confirm);
            Btn(_body, Mid(), new Vector2(220, -70), new Vector2(360, 66), "CANCEL",
                new Color(0.42f, 0.34f, 0.36f), true, () => { _pending = null; RefreshBody(); });
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
            t.verticalOverflow = VerticalWrapMode.Overflow;
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
            Text t = Txt(go.transform, label, 19, interactable ? Color.white : new Color(1, 1, 1, 0.6f), Mid(), Vector2.zero, size);
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

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectAscendant.Core;
using ProjectAscendant.Combat;
using ProjectAscendant.Deck;

namespace ProjectAscendant.UI
{
    // Per Epic 13 / §10.2 — the combat screen (uGUI). Drives a CombatController turn-by-turn
    // (Draw → Intent → Action[play cards / End Turn] → Resolution → TurnEnd → …), the same phase
    // loop as the Epic-4 sandbox, with a proper screen: player/enemy HP bars, the enemy intent
    // (Pillar 1 — telegraphed), AP, a clickable hand of move cards, End Turn, and a Victory/Defeat
    // banner. Builds its own overlay canvas on Begin(); destroys it on Continue and reports the
    // outcome. View-layer only.
    //
    // ⚠ TECH-DEBT (gap #38): uGUI (screenshot-verifiable); project mandates UI Toolkit.
    public sealed class CombatScreenUI : MonoBehaviour
    {
        private CombatController _cc;
        private Action<CombatController.CombatOutcome> _onComplete;
        private Font _font;
        private GameObject _root;

        private Text _header, _playerInfo, _enemyInfo, _enemyIntent, _banner;
        private Image _playerHp, _enemyHp;
        private RectTransform _hand;
        private GameObject _endTurn, _continue;

        public void Begin(CombatController cc, Action<CombatController.CombatOutcome> onComplete)
        {
            _cc = cc;
            _onComplete = onComplete;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _cc.Start(); // builds the deck from the team's moves before the first draw
            Build();
            BeginPlayerTurn();
            Refresh();
        }

        // ── Driving (mirrors the Epic-4 sandbox) ──────────────────────────────

        private void BeginPlayerTurn()
        {
            if (_cc.State.Outcome != CombatController.CombatOutcome.InProgress) return;
            _cc.DrawPhase();
            _cc.IntentPhase();
            _cc.State.CurrentPhase = CombatController.Phase.ActionPhase;
        }

        private void PlayCard(int handIndex)
        {
            if (_cc.State.Outcome != CombatController.CombatOutcome.InProgress) return;
            _cc.ExecuteAction(PlayerAction.PlaySkill(handIndex, enemySlot: 0));
            Refresh();
        }

        private void EndTurn()
        {
            if (_cc.State.Outcome != CombatController.CombatOutcome.InProgress) return;
            _cc.ResolutionPhase();
            _cc.TurnEnd();
            if (_cc.State.Outcome != CombatController.CombatOutcome.InProgress)
                _cc.CombatEnd();
            else
                BeginPlayerTurn();
            Refresh();
        }

        private void Continue()
        {
            CombatController.CombatOutcome outcome = _cc.State.Outcome;
            if (_root != null) Destroy(_root);
            _onComplete?.Invoke(outcome);
        }

        // ── Build (once) ──────────────────────────────────────────────────────

        private void Build()
        {
            _root = new GameObject("CombatCanvas");
            Canvas canvas = _root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20; // above the Map View
            CanvasScaler scaler = _root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            _root.AddComponent<GraphicRaycaster>();

            Image bg = Img(_root.transform, new Color(0.07f, 0.10f, 0.13f, 1f)); // opaque — blocks the map
            Stretch(bg.rectTransform);

            _header = Txt(_root.transform, "", 26, Color.white, Top(), new Vector2(0, -36), new Vector2(1500, 40));

            // Player panel (left).
            Img(_root.transform, new Color(0.13f, 0.18f, 0.16f, 1f)).rectTransform.also(rt => Place(rt, Mid(), new Vector2(-430, 230), new Vector2(620, 230)));
            _playerInfo = Txt(_root.transform, "", 24, new Color(0.85f, 0.95f, 0.85f), Mid(), new Vector2(-430, 300), new Vector2(580, 90));
            _playerHp = HpBar(_root.transform, new Vector2(-430, 215), new Vector2(560, 30));

            // Enemy panel (right).
            Img(_root.transform, new Color(0.20f, 0.13f, 0.14f, 1f)).rectTransform.also(rt => Place(rt, Mid(), new Vector2(430, 230), new Vector2(620, 230)));
            _enemyInfo = Txt(_root.transform, "", 24, new Color(0.95f, 0.85f, 0.85f), Mid(), new Vector2(430, 300), new Vector2(580, 90));
            _enemyHp = HpBar(_root.transform, new Vector2(430, 215), new Vector2(560, 30));
            _enemyIntent = Txt(_root.transform, "", 22, new Color(1f, 0.65f, 0.6f), Mid(), new Vector2(430, 150), new Vector2(580, 40));

            // Hand row.
            GameObject handGO = new("Hand", typeof(RectTransform));
            handGO.transform.SetParent(_root.transform, false);
            _hand = (RectTransform)handGO.transform;
            Place(_hand, Bottom(), new Vector2(0, 190), new Vector2(1700, 170));

            // End Turn + outcome.
            _endTurn = Btn(_root.transform, Bottom(), new Vector2(760, 80), new Vector2(220, 64), "END TURN",
                new Color(0.55f, 0.45f, 0.2f), true, EndTurn).gameObject;
            _banner = Txt(_root.transform, "", 64, Color.yellow, Mid(), new Vector2(0, 40), new Vector2(1200, 120));
            _banner.gameObject.SetActive(false);
            _continue = Btn(_root.transform, Mid(), new Vector2(0, -70), new Vector2(360, 70), "CONTINUE  ▶",
                new Color(0.25f, 0.55f, 0.35f), true, Continue).gameObject;
            _continue.SetActive(false);
        }

        // ── Refresh ───────────────────────────────────────────────────────────

        private void Refresh()
        {
            CombatController.CombatState s = _cc.State;
            bool over = s.Outcome != CombatController.CombatOutcome.InProgress;

            _header.text = $"Turn {s.TurnNumber}      AP {s.CurrentAP}/{s.Config.MaxAPPerTurn}      Phase {s.CurrentPhase}";

            PokemonInstance lead = s.PlayerTeam.Count > 0 ? s.PlayerTeam[Mathf.Clamp(s.LeadIndex, 0, s.PlayerTeam.Count - 1)] : null;
            PokemonInstance enemy = s.EnemyTeam.Count > 0 ? s.EnemyTeam[0] : null;
            SetMon(_playerInfo, _playerHp, "YOU", lead, false);
            SetMon(_enemyInfo, _enemyHp, "ENEMY", enemy, true);

            // Enemy intent (telegraphed).
            _enemyIntent.text = "";
            if (!over && enemy != null && enemy.CurrentHP > 0 && s.EnemyIntents.Count > 0)
            {
                Intent it = s.EnemyIntents[0];
                _enemyIntent.text = it.Move != null && it.Kind == IntentKind.Attack
                    ? $"⚔ Intent: {it.Move.DisplayName ?? it.Move.name} → slot {it.TargetSlot}"
                    : $"Intent: {it.Kind}";
            }

            BuildHand(s, over);

            _endTurn.SetActive(!over);
            _banner.gameObject.SetActive(over);
            _continue.SetActive(over);
            if (over)
            {
                bool win = s.Outcome == CombatController.CombatOutcome.Victory;
                _banner.text = win ? "VICTORY!" : "DEFEAT";
                _banner.color = win ? new Color(0.3f, 1f, 0.45f) : new Color(1f, 0.35f, 0.35f);
            }
        }

        private void SetMon(Text info, Image hpFill, string who, PokemonInstance p, bool enemy)
        {
            if (p == null || p.Species == null) { info.text = $"{who}: —"; SetFill(hpFill, 0); return; }
            int max = PokemonVitals.MaxHP(p);
            float pct = max > 0 ? Mathf.Clamp01((float)p.CurrentHP / max) : 0f;
            SetFill(hpFill, pct);
            string status = p.PrimaryStatus != StatusCondition.None ? $"   [{p.PrimaryStatus}]" : "";
            info.text = $"{who}:  {p.Species.DisplayName ?? p.Species.name}   Lv{p.Level}\nHP {p.CurrentHP} / {max}{status}";
        }

        private void BuildHand(CombatController.CombatState s, bool over)
        {
            for (int i = _hand.childCount - 1; i >= 0; i--) Destroy(_hand.GetChild(i).gameObject);
            if (over) return;

            List<int> idxs = new();
            for (int i = 0; i < s.SkillHand.Count; i++)
                if (s.SkillHand[i] != null && s.SkillHand[i].Move != null) idxs.Add(i);

            const float cardW = 220f, cardH = 150f, spacing = 18f;
            float totalW = idxs.Count * cardW + Mathf.Max(0, idxs.Count - 1) * spacing;
            float startX = -totalW / 2f + cardW / 2f;

            for (int slot = 0; slot < idxs.Count; slot++)
            {
                int i = idxs[slot];
                MoveSO m = s.SkillHand[i].Move;
                int cost = StatusModifiers.GetEffectiveAPCost(m, s.SkillHand[i].Owner, s.Config);
                bool playable = cost <= s.CurrentAP && s.CurrentPhase == CombatController.Phase.ActionPhase;
                float x = startX + slot * (cardW + spacing);
                Btn(_hand, Mid(), new Vector2(x, 0), new Vector2(cardW, cardH),
                    $"{m.DisplayName ?? m.name}\n{m.Type}  Pwr {m.BasePower}\nAP {cost}",
                    new Color(0.22f, 0.28f, 0.42f), playable, () => PlayCard(i));
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

        private Image HpBar(Transform parent, Vector2 pos, Vector2 size)
        {
            Image bg = Img(parent, new Color(0.15f, 0.15f, 0.15f, 1f));
            Place(bg.rectTransform, Mid(), pos, size);
            GameObject fillGO = new("Fill", typeof(RectTransform));
            fillGO.transform.SetParent(bg.transform, false);
            Image fill = fillGO.AddComponent<Image>(); fill.color = new Color(0.3f, 0.85f, 0.35f);
            RectTransform rt = fill.rectTransform;
            rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            return fill;
        }

        private static void SetFill(Image fill, float pct)
        {
            if (fill == null) return;
            fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(pct), 1f);
            fill.color = pct < 0.30f ? new Color(0.9f, 0.25f, 0.25f)
                       : pct < 0.60f ? new Color(0.9f, 0.7f, 0.25f)
                                     : new Color(0.3f, 0.85f, 0.35f);
        }

        private Button Btn(Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, string label, Color color, bool interactable, Action onClick)
        {
            GameObject go = new("Button", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>(); img.color = interactable ? color : color * 0.5f;
            Place((RectTransform)go.transform, anchor, pos, size);
            Button btn = go.AddComponent<Button>(); btn.interactable = interactable;
            if (onClick != null) btn.onClick.AddListener(() => onClick());
            Text t = Txt(go.transform, label, 19, Color.white, Mid(), Vector2.zero, size);
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

        private static Vector2 Top() => new(0.5f, 1f);
        private static Vector2 Mid() => new(0.5f, 0.5f);
        private static Vector2 Bottom() => new(0.5f, 0f);
    }

    internal static class RectExt
    {
        // Tiny fluent helper so panel Images can be positioned inline at build time.
        public static void also(this RectTransform rt, Action<RectTransform> a) => a(rt);
    }

    // Per §9.x — the player's combat agent. CombatScreenUI drives actions via ExecuteAction
    // (so DecideAction is never the input path); these are safe defaults. PickLeadReplacement
    // keeps the current Lead for the VS single-Pokémon team (a swap modal is a later milestone).
    internal sealed class UIPlayerAgent : IPlayerAgent
    {
        public PlayerAction DecideAction(CombatController.CombatState state) => PlayerAction.End();

        public int PickLeadReplacement(CombatController.CombatState state,
            System.Collections.Generic.IReadOnlyList<PokemonInstance> candidates) => state.LeadIndex;
    }
}

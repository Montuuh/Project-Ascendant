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

        private Text _header, _enemyInfo, _enemyIntent, _banner;
        private Image _enemyHp;
        private RectTransform _hand, _consumables, _playerTeam;
        private GameObject _endTurn, _continue;
        private GameObject _leadReplacementModal;
        // Per §7.4.4 (OPEN) — reinforcement wave telegraph panel
        private GameObject _wavePanel;
        private Text _waveText;
        // Per §3.2.6 (OPEN) — breather modal (blocking overlay)
        private GameObject _breatherModal;

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

        // ── Cheat hooks (DEV-ONLY — driven by CheatConsole) ───────────────────
        // True while a combat overlay is on screen (used to route F-key cheats to combat vs map).
        public bool IsActive => _root != null;

        // F2 — instantly KO every enemy, then run the normal end-of-turn resolution so the
        // Victory path + banner fire through the real flow (not a fake state poke).
        public void CheatWinCombat()
        {
            if (_cc == null || _cc.State.Outcome != CombatController.CombatOutcome.InProgress) return;
            foreach (PokemonInstance e in _cc.State.EnemyTeam) if (e != null) e.CurrentHP = 0;
            EndTurn();
        }

        // F3 — force-catch enemy slot 0 (bypasses the §7.3.4.1 HP/status gate). On a wild this
        // recruits via MapViewUI.OnCombatComplete; on a trainer it just clears the fight (dev aid).
        public void CheatCaptureEnemy()
        {
            if (_cc == null || _cc.State.Outcome != CombatController.CombatOutcome.InProgress) return;
            if (_cc.State.EnemyTeam.Count == 0 || _cc.State.EnemyTeam[0] == null) return;
            _cc.State.CaughtTarget = _cc.State.EnemyTeam[0];
            _cc.State.EnemyTeam.Clear();
            EndTurn();
        }

        // F4 — full-heal the whole active team (Lead + bench).
        public void CheatHealTeam()
        {
            if (_cc == null) return;
            foreach (PokemonInstance p in _cc.State.PlayerTeam)
                if (p != null) p.CurrentHP = PokemonVitals.MaxHP(p);
            Refresh();
        }

        // F5 — refill AP to the per-turn max.
        public void CheatRefillAP()
        {
            if (_cc == null || _cc.State.Config == null) return;
            _cc.State.CurrentAP = _cc.State.Config.MaxAPPerTurn;
            Refresh();
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

            // Player team column (left) — Lead + bench rows, rebuilt each Refresh.
            GameObject ptGO = new("PlayerTeam", typeof(RectTransform));
            ptGO.transform.SetParent(_root.transform, false);
            _playerTeam = (RectTransform)ptGO.transform;
            Place(_playerTeam, Mid(), new Vector2(-430, 150), new Vector2(660, 440));

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

            GameObject consGO = new("Consumables", typeof(RectTransform));
            consGO.transform.SetParent(_root.transform, false);
            _consumables = (RectTransform)consGO.transform;
            Place(_consumables, Bottom(), new Vector2(0, 400), new Vector2(1700, 130));

            // End Turn + outcome.
            _endTurn = Btn(_root.transform, Bottom(), new Vector2(760, 80), new Vector2(220, 64), "END TURN",
                new Color(0.55f, 0.45f, 0.2f), true, EndTurn).gameObject;
            _banner = Txt(_root.transform, "", 64, Color.yellow, Mid(), new Vector2(0, 40), new Vector2(1200, 120));
            _banner.gameObject.SetActive(false);
            _continue = Btn(_root.transform, Mid(), new Vector2(0, -70), new Vector2(360, 70), "CONTINUE  ▶",
                new Color(0.25f, 0.55f, 0.35f), true, Continue).gameObject;
            _continue.SetActive(false);

            // Per §7.4.4 (OPEN) — wave telegraph panel (bottom-left, persistent, shows next wave when non-empty)
            _wavePanel = new GameObject("WavePanel");
            _wavePanel.transform.SetParent(_root.transform, false);
            Image waveImg = _wavePanel.AddComponent<Image>();
            waveImg.color = new Color(0.15f, 0.12f, 0.18f, 0.92f);
            Place((RectTransform)_wavePanel.transform, new Vector2(0f, 0f), new Vector2(180, 80), new Vector2(320, 90));
            Border(_wavePanel, new Color(0.65f, 0.55f, 0.75f));
            _waveText = Txt(_wavePanel.transform, "", 18, new Color(0.92f, 0.85f, 0.95f), Mid(), Vector2.zero, new Vector2(300, 70));
            _wavePanel.SetActive(false); // hidden until wave is pending
        }

        // ── Refresh ───────────────────────────────────────────────────────────

        private void Refresh()
        {
            CombatController.CombatState s = _cc.State;
            bool over = s.Outcome != CombatController.CombatOutcome.InProgress;

            _header.text = $"Turn {s.TurnNumber}      AP {s.CurrentAP}/{s.Config.MaxAPPerTurn}      Phase {s.CurrentPhase}";

            PokemonInstance enemy = s.EnemyTeam.Count > 0 ? s.EnemyTeam[0] : null;
            BuildPlayerPanel(s, over);
            SetMon(_enemyInfo, _enemyHp, "ENEMY", enemy, true);

            // Enemy intent (telegraphed).
            _enemyIntent.text = "";
            if (!over && enemy != null && enemy.CurrentHP > 0 && s.EnemyIntents.Count > 0)
            {
                Intent it = s.EnemyIntents[0];
                _enemyIntent.text = it.Move != null && it.Kind == IntentKind.Attack
                    ? $"⚔ Intent: {it.Move.DisplayName ?? it.Move.name}  →  {SlotLabel(it.EffectiveTargetSlot(s.LeadIndex), s)}"
                    : $"Intent: {it.Kind}";
            }

            BuildHand(s, over);
            BuildConsumables(s, over);

            // Per §3.3.5 + Bug #10 — blocking Lead-replacement modal when the Lead faints.
            BuildLeadReplacementModal(s);

            // Per §7.4.4 (OPEN) — wave telegraph panel
            BuildWavePanel();

            // Per §3.2.6 (OPEN) — breather modal (blocks normal UI when pending)
            BuildBreatherModal(s);

            // Block End Turn while breather is pending (player must act or pass)
            bool breatherBlocks = s.BreatherPending;
            _endTurn.SetActive(!over && !breatherBlocks);
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

        // Per §4.3.2 — intents target POSITIONS (slots), not Pokémon. Translate a raw slot index to
        // its role relative to the current Lead: the Lead slot → "LEAD", the rest → "BENCH n" (1-based
        // by slot order). Keeps the telegraph readable as the Lead floats across swaps.
        private static string SlotRole(int slot, CombatController.CombatState s)
        {
            int lead = Mathf.Clamp(s.LeadIndex, 0, Mathf.Max(0, s.PlayerTeam.Count - 1));
            if (slot == lead) return "LEAD";
            int rank = 0;
            for (int i = 0; i < s.PlayerTeam.Count && i <= slot; i++)
                if (i != lead) rank++;
            return $"BENCH {rank}";
        }

        // Full intent target: "Slot LEAD (Bulbasaur)" / "Slot BENCH 1 (Geodude)" / "Slot BENCH 2 (empty)".
        private static string SlotLabel(int slot, CombatController.CombatState s)
        {
            PokemonInstance occ = slot >= 0 && slot < s.PlayerTeam.Count ? s.PlayerTeam[slot] : null;
            string name = occ?.Species != null ? (occ.Species.DisplayName ?? occ.Species.name) : "empty";
            return $"Slot {SlotRole(slot, s)} ({name})";
        }

        // Per §3.3 + Epic 6 — the active team (Lead + bench). Pillar 2 ("every swap is a
        // decision"): bench Pokémon show a ⇄ SWAP button with the live AP cost ladder
        // (1st=1, 2nd=2, 3rd=3 AP per §3.3.1), gated by SwapManager (Frozen-lock + AP).
        private void BuildPlayerPanel(CombatController.CombatState s, bool over)
        {
            for (int i = _playerTeam.childCount - 1; i >= 0; i--) Destroy(_playerTeam.GetChild(i).gameObject);

            int n = s.PlayerTeam.Count;
            if (n == 0) return;
            int lead = Mathf.Clamp(s.LeadIndex, 0, n - 1);
            int swapCost = SwapManager.NextSwapCost(s.SwapCounter);

            // Per Bug #3 — Lead first, then bench in stable order (BENCH 1, BENCH 2), regardless of slot index.
            List<int> order = new() { lead };
            for (int i = 0; i < n; i++) if (i != lead) order.Add(i);

            const float rowH = 112f, gap = 14f;
            float y = 200f; // top of the column, relative to the container centre
            int benchNum = 1; // 1-based bench numbering (stable across swaps)
            foreach (int slot in order)
            {
                bool isLead = (slot == lead);
                BuildMonRow(s.PlayerTeam[slot], slot, isLead, s, over, swapCost, new Vector2(0, y), isLead ? 0 : benchNum);
                if (!isLead) benchNum++;
                y -= rowH + gap;
            }
        }

        private void BuildMonRow(PokemonInstance p, int slot, bool isLead, CombatController.CombatState s,
                                 bool over, int swapCost, Vector2 pos, int benchNumber)
        {
            bool fainted = p != null && p.CurrentHP == 0;
            Color bg = isLead ? new Color(0.16f, 0.26f, 0.19f, 1f)
                     : fainted ? new Color(0.18f, 0.13f, 0.14f, 1f)
                               : new Color(0.13f, 0.16f, 0.18f, 1f);
            Image panel = Img(_playerTeam, bg);
            Place(panel.rectTransform, Mid(), pos, new Vector2(640, 104));
            if (isLead) Border(panel.gameObject, new Color(0.35f, 0.95f, 0.5f));

            string name = p?.Species != null ? (p.Species.DisplayName ?? p.Species.name) : "—";
            int max = p != null ? PokemonVitals.MaxHP(p) : 0;
            int hp = p?.CurrentHP ?? 0;
            string st = p != null && p.PrimaryStatus != StatusCondition.None ? $"  [{p.PrimaryStatus}]" : "";
            // Per Bug #3 — stable bench numbering (BENCH 1, BENCH 2) instead of SlotRole dynamic lookup.
            string tag = (isLead ? "▶ LEAD" : $"BENCH {benchNumber}") + (fainted ? "  ✖ FAINTED" : "");
            Color nameCol = fainted ? new Color(0.62f, 0.5f, 0.5f) : new Color(0.92f, 0.97f, 0.92f);
            // §5.5 — show the passive ability (low-HP type-boosters glow when armed).
            string ab = p?.Ability != null ? $"   ✦ {p.Ability.DisplayName ?? p.Ability.AbilityId}" : "";

            Txt(panel.transform, $"{tag}   {name}  Lv{p?.Level ?? 0}{st}", 20, nameCol, Mid(), new Vector2(-40, 28), new Vector2(440, 28));
            if (ab.Length > 0)
                Txt(panel.transform, ab, 16, new Color(0.72f, 0.85f, 0.95f), Mid(), new Vector2(150, 28), new Vector2(320, 24));
            Txt(panel.transform, $"HP {hp} / {max}", 17, Color.white, Mid(), new Vector2(-180, 1), new Vector2(200, 24));
            Image hb = HpBar(panel.transform, new Vector2(-20, -28), new Vector2(420, 18));
            SetFill(hb, max > 0 ? Mathf.Clamp01((float)hp / max) : 0f);

            if (!isLead && !over)
            {
                bool can = s.CurrentPhase == CombatController.Phase.ActionPhase
                           && SwapManager.CanManualSwap(s.LeadIndex, slot, s.PlayerTeam, s.CurrentAP, s.SwapCounter);
                Btn(panel.transform, Mid(), new Vector2(258, 0), new Vector2(120, 88),
                    $"⇄ SWAP\n{swapCost} AP", new Color(0.30f, 0.42f, 0.58f), can, () => SwapTo(slot));
            }
        }

        private void SwapTo(int benchSlot)
        {
            if (_cc.State.Outcome != CombatController.CombatOutcome.InProgress) return;
            _cc.ExecuteAction(PlayerAction.ManualSwap(benchSlot));
            Refresh(); // Lead changed → card eligibility + swap costs refresh
        }

        private static void Border(GameObject go, Color c)
        {
            Outline o = go.AddComponent<Outline>();
            o.effectColor = c; o.effectDistance = new Vector2(3, 3);
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
                MoveCardInstance card = s.SkillHand[i];
                MoveSO m = card.Move;
                PokemonInstance owner = card.Owner;
                bool ownerIsLead = s.PlayerTeam.IndexOf(owner) == s.LeadIndex;
                int cost = StatusModifiers.GetEffectiveAPCost(m, owner, s.Config);

                // Per §3.3 — a card is only playable if its owner is in the right position
                // (Ranged from anywhere; Melee only from Lead, unless Step-Forward). Greyed,
                // never hidden (ui.md), so the shared-hand swap tension is legible (Pillar 2).
                bool eligible = CardPlayValidator.Validate(card, s.PlayerTeam, s.LeadIndex)
                                == CardPlayValidator.PlayResult.Playable;
                bool playable = eligible && cost <= s.CurrentAP
                                && s.CurrentPhase == CombatController.Phase.ActionPhase;

                string ownerName = owner?.Species != null ? (owner.Species.DisplayName ?? owner.Species.name) : "?";
                string rangeTag = m.Range == MoveRange.Melee ? "Melee" : "Ranged";
                string lockHint = !eligible && m.Range == MoveRange.Melee && !ownerIsLead ? "\n(swap in to use)" : "";
                Color col = ownerIsLead ? new Color(0.22f, 0.28f, 0.42f) : new Color(0.32f, 0.25f, 0.44f);

                float x = startX + slot * (cardW + spacing);
                Btn(_hand, Mid(), new Vector2(x, 0), new Vector2(cardW, cardH),
                    $"{ownerName}\n{m.DisplayName ?? m.name}\n{m.Type} · {rangeTag} · Pwr {m.BasePower}\nAP {cost}{lockHint}",
                    col, playable, () => PlayCard(i));
            }
        }

        // Per §7.3.4 / §8.1 + Bug #2 — Consumable hand (Pokéball etc.). Catching: throw the ball below
        // 50% HP (or with a status) to recruit the wild. Gate Pokéball on WildCatchResolver.IsCatchable
        // (per UI rule: grayed-out non-playable, never hidden); show reason hint when not eligible.
        private void BuildConsumables(CombatController.CombatState s, bool over)
        {
            for (int i = _consumables.childCount - 1; i >= 0; i--) Destroy(_consumables.GetChild(i).gameObject);
            if (over || s.ConsumableHand == null) return;

            List<int> idxs = new();
            for (int i = 0; i < s.ConsumableHand.Count; i++)
                if (s.ConsumableHand[i] != null) idxs.Add(i);
            if (idxs.Count == 0) return;

            const float cw = 250f, ch = 110f, spacing = 18f;
            float startX = -(idxs.Count * cw + (idxs.Count - 1) * spacing) / 2f + cw / 2f;
            for (int slot = 0; slot < idxs.Count; slot++)
            {
                int i = idxs[slot];
                ConsumableSO c = s.ConsumableHand[i];
                bool isBall = c.Effect is CatchConsumableEffectSO;
                bool canAfford = c.APCost <= s.CurrentAP && s.CurrentPhase == CombatController.Phase.ActionPhase;

                // Per §7.3.4.1 + Bug #2 — check catch conditions for Pokéball eligibility
                bool catchOk = true;
                string catchHint = "";
                if (isBall && c.Effect is CatchConsumableEffectSO catchEff)
                {
                    PokemonInstance wild = s.EnemyTeam.Count > 0 ? s.EnemyTeam[0] : null;
                    if (wild == null || wild.CurrentHP <= 0)
                    {
                        catchOk = false;
                        catchHint = "\n(no valid target)";
                    }
                    else if (!WildCatchResolver.IsCatchable(wild, catchEff))
                    {
                        catchOk = false;
                        int max = wild.Species != null ? wild.Species.BaseStats.BaseHP + (wild.Species.GrowthCurve != null ? wild.Species.GrowthCurve.GetHPAt(wild.Level) : 0) : 0;
                        float pct = max > 0 ? (float)wild.CurrentHP / max : 1f;
                        catchHint = wild.PrimaryStatus == StatusCondition.None
                            ? $"\n(HP {(int)(pct * 100)}% — need <50% or status)"
                            : "\n(status active — reduce HP)";
                    }
                }

                bool playable = canAfford && (isBall ? catchOk : true);
                string label = isBall
                    ? $"⊙ {c.DisplayName ?? c.name}\nCATCH if HP < 50%\n(or any status)   AP {c.APCost}{catchHint}"
                    : $"{c.DisplayName ?? c.name}\nAP {c.APCost}";
                Color col = isBall ? new Color(0.62f, 0.26f, 0.30f) : new Color(0.30f, 0.42f, 0.32f);
                float x = startX + slot * (cw + spacing);
                Btn(_consumables, Mid(), new Vector2(x, 0), new Vector2(cw, ch), label, col, playable, () => PlayConsumable(i));
            }
        }

        private void PlayConsumable(int handIndex)
        {
            if (_cc.State.Outcome != CombatController.CombatOutcome.InProgress) return;
            _cc.ExecuteAction(PlayerAction.PlayConsumable(handIndex));

            // A successful catch clears the enemy team immediately; finalize the combat now
            // (the Outcome→Victory check otherwise waits for a Resolution/TurnEnd).
            if (_cc.State.CaughtTarget != null && _cc.State.Outcome == CombatController.CombatOutcome.InProgress)
                EndTurn();
            else
                Refresh();
        }

        // Per §3.3.5 + Bug #10 — modal picker when the Lead faints. Player chooses a bench Pokémon to
        // take the Lead position (no AP cost per §3.3.5). Blocks all other UI until a choice is made.
        private void BuildLeadReplacementModal(CombatController.CombatState s)
        {
            // Tear down any existing modal first
            if (_leadReplacementModal != null) { Destroy(_leadReplacementModal); _leadReplacementModal = null; }

            // Only show when candidates are pending
            if (s.PendingLeadReplacementCandidates == null || s.PendingLeadReplacementCandidates.Count == 0) return;

            _leadReplacementModal = new GameObject("LeadReplacementModal");
            _leadReplacementModal.transform.SetParent(_root.transform, false);
            Canvas modal = _leadReplacementModal.AddComponent<Canvas>();
            modal.overrideSorting = true;
            modal.sortingOrder = 30; // above everything

            // Semi-transparent backdrop
            Image backdrop = Img(_leadReplacementModal.transform, new Color(0, 0, 0, 0.75f));
            Stretch(backdrop.rectTransform);

            // Prompt
            Txt(_leadReplacementModal.transform, "LEAD FAINTED — Choose a replacement", 32, new Color(1f, 0.85f, 0.3f),
                Mid(), new Vector2(0, 180), new Vector2(1200, 50));

            // Candidate buttons (bench Pokémon that can take over)
            const float cardW = 280f, cardH = 140f, spacing = 30f;
            int n = s.PendingLeadReplacementCandidates.Count;
            float startX = -(n * cardW + (n - 1) * spacing) / 2f + cardW / 2f;

            for (int i = 0; i < n; i++)
            {
                PokemonInstance p = s.PendingLeadReplacementCandidates[i];
                int slotIndex = s.PlayerTeam.IndexOf(p);
                if (slotIndex < 0 || p == null) continue; // safety

                string name = p.Species != null ? (p.Species.DisplayName ?? p.Species.name) : "—";
                int max = PokemonVitals.MaxHP(p);
                string label = $"{name}  Lv{p.Level}\nHP {p.CurrentHP} / {max}";
                float x = startX + i * (cardW + spacing);

                Btn(_leadReplacementModal.transform, Mid(), new Vector2(x, 0), new Vector2(cardW, cardH), label,
                    new Color(0.28f, 0.52f, 0.38f), true, () => OnLeadReplacementChosen(slotIndex));
            }
        }

        private void OnLeadReplacementChosen(int newLeadIndex)
        {
            _cc.ApplyLeadReplacement(newLeadIndex);
            Refresh(); // modal will be torn down by next Refresh cycle
        }

        // Per §7.4.4 (OPEN) — wave telegraph panel. Shows PeekNextWave() when non-empty (upcoming
        // reinforcement wave). Non-blocking, persistent, bottom-left. Hides when no wave is queued.
        private void BuildWavePanel()
        {
            var nextWave = _cc.PeekNextWave();
            if (nextWave == null || nextWave.Count == 0)
            {
                _wavePanel.SetActive(false);
                return;
            }

            _wavePanel.SetActive(true);
            // VS: all intents are Attack, so show species + level + generic "incoming" tag
            if (nextWave.Count == 1)
            {
                var preview = nextWave[0];
                string species = preview.Species != null ? (preview.Species.DisplayName ?? preview.Species.name) : "?";
                _waveText.text = $"NEXT WAVE:\n{species}  Lv{preview.Level}\n⚔ incoming";
            }
            else
            {
                // Multiple enemies in the next wave — show count + first species
                var preview = nextWave[0];
                string species = preview.Species != null ? (preview.Species.DisplayName ?? preview.Species.name) : "?";
                _waveText.text = $"NEXT WAVE ({nextWave.Count}):\n{species}  Lv{preview.Level}\n⚔ incoming";
            }
        }

        // Per §3.2.6 (OPEN) — breather modal. Blocking overlay when BreatherPending is true. The
        // controller auto-grants +1 AP and auto-ends after the player's one action (any card play /
        // manual swap). UI provides an explicit "Pass" button → EndBreather(). Takes visual priority
        // over lead-replacement modal (controller resolves lead-replacement first).
        private void BuildBreatherModal(CombatController.CombatState s)
        {
            // Tear down any existing modal first
            if (_breatherModal != null) { Destroy(_breatherModal); _breatherModal = null; }

            // Only show when breather is pending
            if (!s.BreatherPending) return;

            _breatherModal = new GameObject("BreatherModal");
            _breatherModal.transform.SetParent(_root.transform, false);
            Canvas modal = _breatherModal.AddComponent<Canvas>();
            modal.overrideSorting = true;
            modal.sortingOrder = 30; // same layer as lead-replacement modal

            // Semi-transparent backdrop
            Image backdrop = Img(_breatherModal.transform, new Color(0, 0, 0, 0.75f));
            Stretch(backdrop.rectTransform);

            // Prompt
            Txt(_breatherModal.transform, "⚔ REINFORCEMENTS! You catch your breath — +1 AP",
                30, new Color(0.95f, 0.88f, 0.55f), Mid(), new Vector2(0, 140), new Vector2(1300, 50));
            Txt(_breatherModal.transform, "Take one action (play a card / swap) or pass to continue",
                24, new Color(0.85f, 0.92f, 0.95f), Mid(), new Vector2(0, 90), new Vector2(1200, 40));

            // "Pass / Continue" button
            Btn(_breatherModal.transform, Mid(), new Vector2(0, -20), new Vector2(320, 80),
                "PASS / CONTINUE  ▶", new Color(0.45f, 0.55f, 0.62f), true, OnBreatherPass);
        }

        private void OnBreatherPass()
        {
            _cc.EndBreather();
            Refresh(); // modal will be torn down by next Refresh cycle
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

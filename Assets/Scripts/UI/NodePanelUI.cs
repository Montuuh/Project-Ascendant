using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectAscendant.Core;
using ProjectAscendant.Map;

namespace ProjectAscendant.UI
{
    // Per Epic 13 / §7.6–7.9 — interactive overlay for the three utility nodes (Shop / Center /
    // Mystery), replacing RunAutoPilot's headless auto-resolve. Builds its own uGUI canvas above the
    // Map View, drives the live NodeController via its public API, and reports completion. View-layer
    // only — owns no game state; every mutation goes through the controller.
    //
    // ⚠ TECH-DEBT (gap #38): uGUI so the bridge can screenshot-verify; project mandates UI Toolkit.
    public sealed class NodePanelUI : MonoBehaviour
    {
        private NodeController _node;
        private RunContext _ctx;
        private RunStateSO _state;
        private Action _onComplete;
        private Font _font;
        private GameObject _root;
        private RectTransform _body;
        private MoveManagerUI _moveManager; // §5.10.2 — FREE entry from Center

        // Dojo two-step selection state: which Pokémon is selected, and whether the picker is
        // showing moves or abilities (null = top-level list).
        private PokemonInstance _dojoMon;
        private bool _dojoPickingAbility; // true = ability list; false = move list

        // Returns false if this node type has no interactive panel (caller should auto-resolve).
        public bool TryBegin(NodeController node, RunContext ctx, RunStateSO state, Action onComplete)
        {
            if (node is not (RegionShopNodeController or PokemonCenterNodeController
                             or MysteryEventNodeController or DojoNodeController))
                return false;

            _node = node; _ctx = ctx; _state = state; _onComplete = onComplete;
            _dojoMon = null; _dojoPickingAbility = false;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // §5.10.2 — lazy-init the Move Manager UI if needed.
            if (_moveManager == null)
                _moveManager = gameObject.AddComponent<MoveManagerUI>();

            Build();
            RefreshBody();
            return true;
        }

        private void Build()
        {
            _root = new GameObject("NodePanelCanvas");
            Canvas canvas = _root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 25; // above Map (0) and combat-less utility flow
            CanvasScaler scaler = _root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            _root.AddComponent<GraphicRaycaster>();

            Image bg = Img(_root.transform, new Color(0.08f, 0.10f, 0.13f, 1f));
            Stretch(bg.rectTransform);

            GameObject bodyGO = new("Body", typeof(RectTransform));
            bodyGO.transform.SetParent(_root.transform, false);
            _body = (RectTransform)bodyGO.transform;
            Place(_body, Mid(), Vector2.zero, new Vector2(1500, 980));
        }

        private void RefreshBody()
        {
            for (int i = _body.childCount - 1; i >= 0; i--) Destroy(_body.GetChild(i).gameObject);
            switch (_node)
            {
                case RegionShopNodeController s:      RenderShop(s); break;
                case PokemonCenterNodeController c:   RenderCenter(c); break;
                case MysteryEventNodeController m:    RenderMystery(m); break;
                case DojoNodeController d:            RenderDojo(d); break;
            }
        }

        private void Done()
        {
            if (_root != null) Destroy(_root);
            _onComplete?.Invoke();
        }

        // DEV-ONLY — rebuild the open panel so a cheat (e.g. +₽) re-evaluates affordability live.
        public void CheatRefreshIfActive()
        {
            if (_root != null) RefreshBody();
        }

        // ── Shop (§7.7) ───────────────────────────────────────────────────────

        private void RenderShop(RegionShopNodeController shop)
        {
            Title($"REGION SHOP", $"₽ {_state.PokeDollars}");
            float y = 320f;
            for (int i = 0; i < shop.Slots.Count; i++)
            {
                RegionShopNodeController.ShopSlot slot = shop.Slots[i];
                int idx = i;
                bool affordable = !slot.Purchased && _state.PokeDollars >= slot.Price;

                Color rowCol = slot.Purchased ? new Color(0.16f, 0.16f, 0.18f, 1f) : new Color(0.18f, 0.20f, 0.26f, 1f);
                Image row = Img(_body, rowCol);
                Place(row.rectTransform, Mid(), new Vector2(-60, y), new Vector2(1040, 64));
                Txt(row.transform, $"{KindLabel(slot.Kind)}   ·   {ItemName(slot.Item)}", 22,
                    slot.Purchased ? new Color(0.55f, 0.55f, 0.58f) : Color.white, Mid(), new Vector2(-330, 0), new Vector2(680, 50));
                Txt(row.transform, $"{slot.Price} ₽", 22, new Color(0.95f, 0.9f, 0.6f), Mid(), new Vector2(330, 0), new Vector2(200, 50));

                string label = slot.Purchased ? "SOLD" : "BUY";
                Btn(_body, Mid(), new Vector2(560, y), new Vector2(150, 56), label,
                    new Color(0.28f, 0.46f, 0.34f), affordable, () => { shop.Buy(idx); RefreshBody(); });
                y -= 72f;
            }

            y -= 16f;
            int rc = shop.NextRerollCost;
            string rl = rc < 0 ? "RE-ROLL  (maxed)" : $"RE-ROLL  ({rc} ₽)";
            Btn(_body, Mid(), new Vector2(-300, y), new Vector2(360, 64), rl,
                new Color(0.40f, 0.36f, 0.22f), rc >= 0 && _state.PokeDollars >= rc,
                () => { shop.TryReroll(); RefreshBody(); });
            Btn(_body, Mid(), new Vector2(300, y), new Vector2(360, 64), "LEAVE  ▶",
                new Color(0.30f, 0.42f, 0.55f), true, () => { shop.Leave(); Done(); });
        }

        // ── Pokémon Center (§7.6) — heal + therapy only (CL-009: tutor moved to Dojo) ──

        private void RenderCenter(PokemonCenterNodeController center)
        {
            Title("POKÉMON CENTER", $"₽ {_state.PokeDollars}");
            float y = 330f;

            Btn(_body, Mid(), new Vector2(0, y), new Vector2(520, 60), "✚  HEAL ALL  (free)",
                new Color(0.30f, 0.50f, 0.34f), true, () => { center.Heal(); RefreshBody(); });
            y -= 86f;

            List<PokemonInstance> box = _ctx?.Box?.Members ?? new List<PokemonInstance>();
            for (int i = 0; i < box.Count; i++)
            {
                PokemonInstance p = box[i];
                if (p == null) continue;
                int max = PokemonVitals.MaxHP(p);
                int trauma = p.TraumaStacks;
                Image row = Img(_body, new Color(0.15f, 0.18f, 0.20f, 1f));
                Place(row.rectTransform, Mid(), new Vector2(-60, y), new Vector2(1040, 60));
                string name = p.Species != null ? (p.Species.DisplayName ?? p.Species.name) : "—";
                Txt(row.transform, $"{name}  Lv{p.Level}    HP {p.CurrentHP}/{max}    Trauma {trauma}", 21,
                    Color.white, Mid(), new Vector2(-230, 0), new Vector2(680, 46));

                if (trauma > 0)
                {
                    int cost = center.TherapyCost(p);
                    PokemonInstance mon = p;
                    Btn(_body, Mid(), new Vector2(360, y), new Vector2(230, 50), $"THERAPY −1 ({cost}₽)",
                        new Color(0.46f, 0.30f, 0.40f), _state.PokeDollars >= cost,
                        () => { center.Therapy(mon); RefreshBody(); });
                }

                // §5.10.2 — FREE Move Manager from Center. Always available (no cost gate).
                PokemonInstance pokemonForMoves = p;
                Btn(_body, Mid(), new Vector2(520, y), new Vector2(140, 50), "MOVES",
                    new Color(0.34f, 0.48f, 0.56f), true, () => OpenMoveManager(pokemonForMoves));

                y -= 70f;
            }

            Btn(_body, Mid(), new Vector2(0, Mathf.Min(y - 20f, -360f)), new Vector2(360, 64), "LEAVE  ▶",
                new Color(0.30f, 0.42f, 0.55f), true, () => { center.Leave(); Done(); });
        }

        // §5.10.2 — open the Move Manager for a Pokémon. FREE at Center (no cost deduction).
        private void OpenMoveManager(PokemonInstance mon)
        {
            if (mon == null) return;

            // Close this panel and open the Move Manager. On close, re-open this panel.
            // Do not destroy the root — just hide it. RefreshBody when we return.
            if (_root != null) _root.SetActive(false);

            _moveManager.Open(mon, () =>
            {
                // Re-activate the panel and refresh.
                if (_root != null) _root.SetActive(true);
                RefreshBody();
            });
        }

        // ── Dojo (§7.14 / CL-009) — minimal stub; full UI = CL-023 ──────────

        private void RenderDojo(DojoNodeController dojo)
        {
            Title("DOJO", $"₽ {_state.PokeDollars}");
            Txt(_body, $"Move: {dojo.MoveCost()}₽  ·  Ability: {dojo.AbilityCost()}₽", 20,
                new Color(0.85f, 0.9f, 0.8f), Mid(), new Vector2(0, 370), new Vector2(1000, 32));

            float y = 290f;

            if (_dojoMon != null)
            {
                y = RenderDojoPicker(dojo, y);
            }
            else
            {
                List<PokemonInstance> box = _ctx?.Box?.Members ?? new List<PokemonInstance>();
                for (int i = 0; i < box.Count; i++)
                {
                    PokemonInstance p = box[i];
                    if (p == null) continue;
                    Image row = Img(_body, new Color(0.15f, 0.20f, 0.18f, 1f));
                    Place(row.rectTransform, Mid(), new Vector2(-60, y), new Vector2(1040, 60));
                    string name = p.Species != null ? (p.Species.DisplayName ?? p.Species.name) : "—";
                    string abilityLabel = p.Ability != null ? $"  [{p.Ability.DisplayName ?? p.Ability.AbilityId}]" : "";
                    Txt(row.transform, $"{name}  Lv{p.Level}{abilityLabel}", 21,
                        Color.white, Mid(), new Vector2(-280, 0), new Vector2(620, 46));

                    bool hasMoves = dojo.OfferMoves(p).Count > 0;
                    bool hasAbils = dojo.OfferAbilities(p).Count > 0;
                    PokemonInstance mon = p;
                    Btn(_body, Mid(), new Vector2(330, y), new Vector2(130, 50), $"MOVES",
                        new Color(0.30f, 0.42f, 0.58f), hasMoves,
                        () => { _dojoMon = mon; _dojoPickingAbility = false; RefreshBody(); });
                    Btn(_body, Mid(), new Vector2(480, y), new Vector2(140, 50), $"ABILITY",
                        new Color(0.42f, 0.30f, 0.55f), hasAbils,
                        () => { _dojoMon = mon; _dojoPickingAbility = true; RefreshBody(); });
                    y -= 70f;
                }
            }

            Btn(_body, Mid(), new Vector2(0, Mathf.Min(y - 20f, -360f)), new Vector2(360, 64), "LEAVE  ▶",
                new Color(0.30f, 0.42f, 0.55f), true, () => { dojo.Leave(); Done(); });
        }

        // Show the teaching picker for the selected Pokémon. Returns updated y after items.
        private float RenderDojoPicker(DojoNodeController dojo, float y)
        {
            string who = _dojoMon.Species != null ? (_dojoMon.Species.DisplayName ?? _dojoMon.Species.name) : "—";
            Btn(_body, Mid(), new Vector2(-580, y), new Vector2(100, 44), "◀ BACK",
                new Color(0.30f, 0.30f, 0.36f), true, () => { _dojoMon = null; RefreshBody(); });

            if (!_dojoPickingAbility)
            {
                Txt(_body, $"Teach a move to {who}  ({dojo.MoveCost()}₽ each)", 20,
                    new Color(0.8f, 0.9f, 1f), Mid(), new Vector2(40, y), new Vector2(900, 34));
                y -= 50f;
                List<MoveSO> moves = dojo.OfferMoves(_dojoMon);
                for (int i = 0; i < moves.Count; i++)
                {
                    MoveSO m = moves[i];
                    bool afford = _state.PokeDollars >= dojo.MoveCost();
                    MoveSO captured = m; PokemonInstance mon = _dojoMon;
                    Btn(_body, Mid(), new Vector2(0, y), new Vector2(700, 58),
                        $"{m.DisplayName ?? m.name}  {m.Type}  Pwr {m.BasePower}",
                        new Color(0.26f, 0.40f, 0.50f), afford,
                        () => { dojo.TeachMove(mon, captured); RefreshBody(); });
                    y -= 68f;
                }
            }
            else
            {
                Txt(_body, $"Teach an ability to {who}  ({dojo.AbilityCost()}₽)", 20,
                    new Color(0.9f, 0.8f, 1f), Mid(), new Vector2(40, y), new Vector2(900, 34));
                y -= 50f;
                List<AbilitySO> abils = dojo.OfferAbilities(_dojoMon);
                for (int i = 0; i < abils.Count; i++)
                {
                    AbilitySO a = abils[i];
                    bool current = _dojoMon.Ability == a;
                    bool afford = !current && _state.PokeDollars >= dojo.AbilityCost();
                    AbilitySO captured = a; PokemonInstance mon = _dojoMon;
                    string label = $"{a.DisplayName ?? a.AbilityId}" + (current ? "  ✓ equipped" : "");
                    Btn(_body, Mid(), new Vector2(0, y), new Vector2(700, 58),
                        label, new Color(0.36f, 0.26f, 0.50f), afford,
                        () => { dojo.TeachAbility(mon, captured); RefreshBody(); });
                    y -= 68f;
                }
            }
            return y;
        }

        // Per §7.9 + Bug #5 — Mystery event with outcome feedback after Choose.
        private void RenderMystery(MysteryEventNodeController mystery)
        {
            MysteryEventSO ev = mystery.SelectedEvent;
            if (ev == null)
            {
                Title("MYSTERY", "");
                Txt(_body, "Nothing stirs here.", 24, new Color(0.8f, 0.85f, 0.9f), Mid(), new Vector2(0, 120), new Vector2(1000, 40));
                Btn(_body, Mid(), new Vector2(0, -40), new Vector2(360, 64), "CONTINUE  ▶",
                    new Color(0.30f, 0.42f, 0.55f), true, () => { mystery.Choose(0); Done(); });
                return;
            }

            // Per Bug #5 — if a choice was made (controller called ResolveOutcome), show the result instead of choices.
            if (mystery.IsCompleted)
            {
                RenderMysteryOutcome(mystery, ev);
                return;
            }

            Title($"{ev.DisplayName ?? ev.EventId}", $"[{ev.RiskProfile}]");
            Txt(_body, ev.NarrativeText ?? "", 22, new Color(0.85f, 0.9f, 0.95f), Mid(), new Vector2(0, 200), new Vector2(1200, 160));

            List<MysteryChoice> choices = ev.Choices ?? new List<MysteryChoice>();
            float y = -20f;
            for (int i = 0; i < choices.Count; i++)
            {
                int idx = i;
                string text = string.IsNullOrEmpty(choices[i].ChoiceText) ? $"Option {i + 1}" : choices[i].ChoiceText;
                Btn(_body, Mid(), new Vector2(0, y), new Vector2(900, 70),
                    text, RiskColor(ev.RiskProfile, i), true, () => { mystery.Choose(idx); RefreshBody(); });
                y -= 88f;
            }
        }

        // Per Bug #5 — outcome display after a Mystery choice. Shows what the player got: relic, dollars,
        // heal, wager win/loss. Reads the controller's LastWagerWon + RunState changes made by ResolveOutcome.
        private void RenderMysteryOutcome(MysteryEventNodeController mystery, MysteryEventSO ev)
        {
            Title($"{ev.DisplayName ?? ev.EventId}", "[RESULT]");

            string outcome = ev.EventId switch
            {
                "mysterious_stone" => "Acquired: Random Relic",
                "berry_bush" => "Box fully healed / Potions acquired",
                "wandering_tutor" => $"Gained ₽{(_state != null ? _state.PokeDollars : 0)}",
                "slot_booth" => (mystery.LastWagerWon ? "✓ JACKPOT! Won coins!" : "✖ Lost the wager."),
                _ => "Event resolved."
            };

            Color col = mystery.LastWagerWon || ev.EventId != "slot_booth"
                ? new Color(0.5f, 0.9f, 0.6f) : new Color(0.9f, 0.5f, 0.5f);
            Txt(_body, outcome, 28, col, Mid(), new Vector2(0, 140), new Vector2(1100, 80));

            Btn(_body, Mid(), new Vector2(0, -80), new Vector2(400, 64), "CONTINUE  ▶",
                new Color(0.30f, 0.42f, 0.55f), true, Done);
        }

        private static Color RiskColor(MysteryRiskProfile risk, int choiceIndex) => choiceIndex == 0
            ? risk switch
            {
                MysteryRiskProfile.Gamble => new Color(0.52f, 0.30f, 0.30f),
                MysteryRiskProfile.Tradeoff => new Color(0.46f, 0.40f, 0.26f),
                _ => new Color(0.28f, 0.44f, 0.34f),
            }
            : new Color(0.28f, 0.34f, 0.46f);

        // ── Shared chrome ─────────────────────────────────────────────────────

        private void Title(string left, string right)
        {
            Txt(_body, left, 38, new Color(0.85f, 0.95f, 0.7f), Mid(), new Vector2(0, 430), new Vector2(1300, 56));
            if (!string.IsNullOrEmpty(right))
                Txt(_body, right, 28, new Color(0.95f, 0.9f, 0.6f), Mid(), new Vector2(0, 388), new Vector2(900, 40));
        }

        private static string KindLabel(RegionShopNodeController.ShopSlotKind k) => k switch
        {
            RegionShopNodeController.ShopSlotKind.Consumable => "Consumable",
            RegionShopNodeController.ShopSlotKind.CommonRelic => "Common Relic",
            RegionShopNodeController.ShopSlotKind.UncommonRelic => "Uncommon Relic",
            RegionShopNodeController.ShopSlotKind.Pokeball => "Pokéball",
            RegionShopNodeController.ShopSlotKind.HeldItem => "Held Item",
            RegionShopNodeController.ShopSlotKind.TM => "TM",
            _ => k.ToString(),
        };

        private static string ItemName(ScriptableObject so)
        {
            if (so == null) return "—";
            if (so is ConsumableSO c && !string.IsNullOrEmpty(c.DisplayName)) return c.DisplayName;
            return so.name;
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

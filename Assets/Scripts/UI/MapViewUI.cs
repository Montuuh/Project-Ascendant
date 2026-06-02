using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using ProjectAscendant.Core;
using ProjectAscendant.Map;
using ProjectAscendant.Combat;
using ProjectAscendant.Progression;

namespace ProjectAscendant.UI
{
    // Per Epic 13 / Task 9.9 — the Map View. Renders the WHOLE generated RegionMap as a connected
    // node-net (columns by layer, lines for each node's forward connections, incl. the L3→L4 branch),
    // and lets the player choose a route: only nodes connected to the current position are clickable;
    // the rest are visible but locked. Reads the wired RunController/RunStateSO from Services.
    //
    // ⚠ TECH-DEBT (gap #38): uGUI so the bridge can screenshot-verify; project mandates UI Toolkit.
    // View-layer only — owns no game state. Combat is auto-resolved for now (combat screen = next M).
    public sealed class MapViewUI : MonoBehaviour
    {
        private RunController _run;
        private RunStateSO _state;
        private RunContext _ctx;
        private RunContentCatalogSO _catalog;
        private CombatScreenUI _combat;
        private NodePanelUI _nodePanel;
        private CheatConsole _cheats;
        private TeamPanelUI _teamPanel;
        private EvolutionPanelUI _evolutionPanel;
        private TMPanelUI _tmPanel;

        private Text _header;
        private Text _log;
        private GameObject _teamButton;
        private GameObject _tmButton;
        private GameObject _hubButton;
        private GameObject _bagButton;
        private InventoryPanelUI _inventoryPanel;
        private StartingRelicPanelUI _startingRelicPanel;
        private HubPanelUI _hubPanel;
        private RectTransform _graph; // node-net canvas (absolute layout)
        private Font _font;

        private readonly HashSet<MapNode> _visited = new();
        private readonly StringBuilder _logText = new();

        private const float GraphW = 1720f, GraphH = 640f;
        private const float NodeW = 150f, NodeH = 46f;

        private void Start()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _run = Services.Has<RunController>() ? Services.Get<RunController>() : null;
            _state = Services.Has<RunStateSO>() ? Services.Get<RunStateSO>() : null;
            _ctx = Services.Has<RunContext>() ? Services.Get<RunContext>() : null;
            _catalog = Services.Has<RunContentCatalogSO>() ? Services.Get<RunContentCatalogSO>() : null;

            _combat = new GameObject("CombatScreen").AddComponent<CombatScreenUI>();
            _combat.transform.SetParent(transform, false);

            _nodePanel = new GameObject("NodePanel").AddComponent<NodePanelUI>();
            _nodePanel.transform.SetParent(transform, false);

            _teamPanel = new GameObject("TeamPanel").AddComponent<TeamPanelUI>();
            _teamPanel.transform.SetParent(transform, false);

            _evolutionPanel = new GameObject("EvolutionPanel").AddComponent<EvolutionPanelUI>();
            _evolutionPanel.transform.SetParent(transform, false);

            _tmPanel = new GameObject("TMPanel").AddComponent<TMPanelUI>();
            _tmPanel.transform.SetParent(transform, false);

            _hubPanel = new GameObject("HubPanel").AddComponent<HubPanelUI>();
            _hubPanel.transform.SetParent(transform, false);

            _inventoryPanel = new GameObject("InventoryPanel").AddComponent<InventoryPanelUI>();
            _inventoryPanel.transform.SetParent(transform, false);

            _startingRelicPanel = new GameObject("StartingRelicPanel").AddComponent<StartingRelicPanelUI>();
            _startingRelicPanel.transform.SetParent(transform, false);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _cheats = new GameObject("CheatConsole").AddComponent<CheatConsole>();
            _cheats.transform.SetParent(transform, false);
            _cheats.Init(this, _combat);
#endif

            BuildChrome();
            Refresh();
        }

        // ── Static chrome ─────────────────────────────────────────────────────

        private void BuildChrome()
        {
            EnsureEventSystem();

            GameObject canvasGO = new("MapViewCanvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGO.AddComponent<GraphicRaycaster>();

            Image bg = MakeImage(canvas.transform, new Color(0.09f, 0.11f, 0.15f, 1f));
            Stretch(bg.rectTransform);

            Text title = MakeText(canvas.transform, "PROJECT ASCENDANT  —  Region 1: Verdant Route", 38, new Color(0.85f, 0.95f, 0.7f));
            Anchor(title.rectTransform, Top(), Top(), new Vector2(0, -44), new Vector2(1500, 56));

            _header = MakeText(canvas.transform, "", 26, Color.white);
            Anchor(_header.rectTransform, Top(), Top(), new Vector2(0, -96), new Vector2(1500, 38));

            // Per §2.3 — open the Active-Team loadout manager (between nodes; hidden on starter-select
            // and at run end). Visibility is toggled in Refresh.
            _teamButton = MakeButton(canvas.transform, new Vector2(800, 470), new Vector2(200, 56), "⛉  TEAM",
                new Color(0.30f, 0.42f, 0.58f), true, OpenTeamPanel).gameObject;
            _teamButton.SetActive(false);

            // §5.4.1 — TM application entry point (Task 10.6.1). Toggled with the run like TEAM.
            _tmButton = MakeButton(canvas.transform, new Vector2(575, 470), new Vector2(200, 56), "🎒  TMs",
                new Color(0.40f, 0.36f, 0.24f), true, OpenTMPanel).gameObject;
            _tmButton.SetActive(false);

            // §6.4 — Trainer Hub entry (Task 11.2). Shown only OUTSIDE an active run (pre/post-run).
            _hubButton = MakeButton(canvas.transform, new Vector2(575, 470), new Vector2(200, 56), "🏛  HUB",
                new Color(0.30f, 0.34f, 0.52f), true, OpenHubPanel).gameObject;
            _hubButton.SetActive(false);

            // §8.6 — Inventory + equip (Task 12.9). Shown during a run, like TEAM/TMs.
            _bagButton = MakeButton(canvas.transform, new Vector2(350, 470), new Vector2(200, 56), "📦  BAG",
                new Color(0.36f, 0.32f, 0.44f), true, OpenInventoryPanel).gameObject;
            _bagButton.SetActive(false);

            GameObject g = new("Graph", typeof(RectTransform));
            g.transform.SetParent(canvas.transform, false);
            _graph = (RectTransform)g.transform;
            Anchor(_graph, Mid(), Mid(), new Vector2(0, 30), new Vector2(GraphW, GraphH));

            _log = MakeText(canvas.transform, "", 19, new Color(0.7f, 0.8f, 0.9f));
            Anchor(_log.rectTransform, Bottom(), Bottom(), new Vector2(0, 16), new Vector2(1800, 150));
            _log.alignment = TextAnchor.LowerCenter;
        }

        // ── Refresh / render ──────────────────────────────────────────────────

        private void Refresh()
        {
            for (int i = _graph.childCount - 1; i >= 0; i--) Destroy(_graph.GetChild(i).gameObject);

            if (_run == null) { _header.text = "<no RunController wired — is RunLauncher in the Boot scene?>"; return; }
            UpdateHeader();
            bool inRun = _run.Map != null && !_run.RunOver;
            if (_teamButton != null) _teamButton.SetActive(inRun);
            if (_tmButton != null) _tmButton.SetActive(inRun);
            if (_bagButton != null) _bagButton.SetActive(inRun);
            if (_hubButton != null) _hubButton.SetActive(!inRun); // §6.4 — Hub is a pre/post-run space

            if (_run.Map == null)
            {
                RenderStarterSelect();
                return;
            }

            RenderNet();

            if (_run.RunOver)
            {
                // §7.13 Gym victory vs §3.3.6 player wipe — distinct banner per outcome (defeat
                // falls back to a neutral close if no outcome was recorded).
                bool defeat = _run.Outcome == RunOutcome.Defeat;
                string label = defeat ? "✖  DEFEATED  ✖" : "★  VICTORY — REGION CLEARED  ★";
                Color color = defeat ? new Color(0.9f, 0.25f, 0.25f) : new Color(0.3f, 0.9f, 0.4f);
                Text done = MakeText(_graph, label, 40, color);
                Anchor(done.rectTransform, Mid(), Mid(), new Vector2(0, GraphH * 0.5f - 4), new Vector2(700, 60));

                // §2.1.7 / Task 11.4 — run summary + the meta progression banked this run.
                if (_run.LastSummary.HasValue)
                {
                    RunEndService.RunSummary sm = _run.LastSummary.Value;
                    string l1 = $"Combats {sm.CombatsCleared}   ·   Evolutions {sm.Evolutions}   ·   Max Trauma ⚠{sm.MaxTrauma}   ·   Layers {sm.LayersCleared}"
                              + (sm.AchievementsUnlocked > 0 ? $"   ·   🏆 +{sm.AchievementsUnlocked}" : "");
                    string l2 = sm.LeveledUp
                        ? $"+{sm.RunXpEarned} Trainer XP   ·   +{sm.TokensGained} Tokens   ·   Trainer Lv {sm.OldLevel} → {sm.NewLevel}  ↑"
                        : $"+{sm.RunXpEarned} Trainer XP   ·   +{sm.TokensGained} Tokens   ·   Trainer Lv {sm.NewLevel}";
                    Text t1 = MakeText(_graph, l1, 22, new Color(0.82f, 0.88f, 0.95f));
                    Anchor(t1.rectTransform, Mid(), Mid(), new Vector2(0, GraphH * 0.5f - 56), new Vector2(1200, 32));
                    Text t2 = MakeText(_graph, l2, 24, new Color(0.95f, 0.9f, 0.55f));
                    Anchor(t2.rectTransform, Mid(), Mid(), new Vector2(0, GraphH * 0.5f - 92), new Vector2(1200, 34));
                }
            }
        }

        private void RenderNet()
        {
            Dictionary<MapNode, Vector2> pos = ComputePositions();
            HashSet<MapNode> reachable = new(_run.SelectableNodes());

            foreach (KeyValuePair<MapNode, Vector2> kv in pos)
                foreach (MapNode child in kv.Key.Next)
                    if (pos.TryGetValue(child, out Vector2 cp))
                        MakeLine(kv.Key, kv.Value, cp, reachable);

            foreach (KeyValuePair<MapNode, Vector2> kv in pos)
            {
                MapNode node = kv.Key;
                bool isCurrent = node == _run.CurrentNode;
                bool isVisited = _visited.Contains(node);
                // Per Bug #4 — a node is interactable if it's reachable (forward-connected) OR it's the
                // current node (so the player can re-enter a utility node like Center after initial visit).
                // Already-passed nodes (visited but not current) are locked.
                bool isReach = !_run.RunOver && (reachable.Contains(node) || isCurrent);

                Color baseCol = NodeColor(node.NodeType);
                Color col = isVisited && !isCurrent ? baseCol * 0.75f : isReach || isCurrent ? baseCol : baseCol * 0.4f;
                col.a = isReach || isCurrent ? 1f : isVisited ? 0.85f : 0.5f;

                string prefix = isCurrent ? "▶ " : isVisited ? "✓ " : "";
                Button b = MakeButton(_graph, kv.Value, new Vector2(NodeW, NodeH),
                    prefix + NodeLabel(node.NodeType), col, isReach, () => OnNodeClicked(node));

                if (isReach) AddOutline(b.gameObject, Color.white);
                else if (isCurrent) AddOutline(b.gameObject, new Color(1f, 0.85f, 0.3f));
            }
        }

        private Dictionary<MapNode, Vector2> ComputePositions()
        {
            Dictionary<MapNode, Vector2> pos = new();
            int layers = _run.Map.LayerCount;
            float colStep = layers > 1 ? GraphW / (layers - 1) : 0f;

            for (int layer = 0; layer < layers; layer++)
            {
                List<MapNode> nodes = _run.Map.Layers[layer];
                float x = -GraphW * 0.5f + layer * colStep;
                int n = nodes.Count;
                float rowStep = n > 1 ? Mathf.Min(140f, (GraphH - NodeH) / (n - 1)) : 0f;
                for (int i = 0; i < n; i++)
                    pos[nodes[i]] = new Vector2(x, (i - (n - 1) * 0.5f) * rowStep);
            }
            return pos;
        }

        // Per §2.1.1 — pick a starter before the run begins.
        private void RenderStarterSelect()
        {
            Text prompt = MakeText(_graph, "Choose your starter", 34, new Color(0.85f, 0.95f, 0.7f));
            Anchor(prompt.rectTransform, Mid(), Mid(), new Vector2(0, 140), new Vector2(900, 50));

            List<PokemonSpeciesSO> starters = _catalog != null ? _catalog.Starters : null;
            if (starters == null || starters.Count == 0)
            {
                MakeButton(_graph, Vector2.zero, new Vector2(440, 70), "▶  START RUN",
                    new Color(0.2f, 0.6f, 0.3f), true, () => { _run.StartRun(); AppendLog("Run started."); Refresh(); });
                return;
            }

            int n = starters.Count;
            const float bw = 320f, bh = 90f, spacing = 40f;
            float startX = -(n * bw + (n - 1) * spacing) / 2f + bw / 2f;
            Color[] cols = { new Color(0.30f, 0.55f, 0.32f), new Color(0.70f, 0.40f, 0.25f), new Color(0.30f, 0.45f, 0.65f) };

            for (int i = 0; i < n; i++)
            {
                PokemonSpeciesSO sp = starters[i];
                if (sp == null) continue;
                string type = sp.Types != null && sp.Types.Count > 0 ? sp.Types[0].ToString() : "";
                float x = startX + i * (bw + spacing);
                MakeButton(_graph, new Vector2(x, 0), new Vector2(bw, bh),
                    $"{sp.DisplayName ?? sp.name}\n{type}", cols[i % cols.Length], true, () => OnStarterChosen(sp));
            }
        }

        private void OnStarterChosen(PokemonSpeciesSO starter)
        {
            if (_ctx != null) RunBootstrapper.SeedStarter(_ctx, starter, 5);
            _run.StartRun();
            AppendLog($"Chose {starter.DisplayName ?? starter.name}. Run started.");
            OfferStartingRelic();
            Refresh();
        }

        // §6.6.3 / Task 12.11 — offer 1 of 3 Starting Relics (Common/Uncommon, never Rare) at run start.
        private void OfferStartingRelic()
        {
            if (_startingRelicPanel == null || _ctx?.ShopPools == null || _state == null) return;
            List<RelicSO> pool = new();
            if (_ctx.ShopPools.CommonRelics != null) pool.AddRange(_ctx.ShopPools.CommonRelics);
            if (_ctx.ShopPools.UncommonRelics != null) pool.AddRange(_ctx.ShopPools.UncommonRelics);
            List<RelicSO> offer = StartingRelicService.Offer(pool, _ctx.Streams?.LootRNG, 3);
            if (offer.Count == 0) return;
            _startingRelicPanel.Open(offer, picked =>
            {
                if (picked != null)
                {
                    (_state.HeldRelics ??= new List<RelicSO>()).Add(picked); // §8.7 acquisition
                    EventBus.Publish(new RelicAcquiredContext(picked));       // §8.7 OnRelicAcquired (12.11.3)
                    AppendLog($"Starting Relic: {picked.DisplayName ?? picked.Id}.");
                }
                Refresh();
            });
        }

        private void OnNodeClicked(MapNode node)
        {
            _run.EnterNode(node);
            NodeController active = _run.ActiveNode;

            // Combat node → open the interactive combat screen; utility node → auto-resolve.
            CombatController cc = TryBuildCombat(active);
            if (cc != null && _combat != null)
            {
                AppendLog($"L{node.Layer} {node.NodeType} — combat begins…");
                _combat.Begin(cc, outcome => OnCombatComplete(node, active, cc, outcome));
                return;
            }

            // Interactive utility panel (Shop / Center / Mystery) → drives the live controller.
            if (_nodePanel != null && _nodePanel.TryBegin(active, _ctx, _state, () => OnUtilityComplete(node, active)))
            {
                AppendLog($"L{node.Layer} {node.NodeType} — {RunAutoPilot.Detail(active)}");
                return;
            }

            // Fallback: anything without a panel auto-resolves (defensive — all VS utility nodes have panels).
            string detail = RunAutoPilot.Detail(active);
            string res = RunAutoPilot.ResolveActive(_run);
            _run.CompleteActiveNode();
            _visited.Add(node);
            AppendLog($"L{node.Layer} {node.NodeType}: {detail}  →  {res}");
            Refresh();
        }

        // The player closed an interactive utility panel (Shop/Center/Mystery). The controller already
        // committed its mutations + called Complete; advance the run and refresh the map.
        private void OnUtilityComplete(MapNode node, NodeController active)
        {
            _run.CompleteActiveNode();
            _visited.Add(node);
            if (!_run.RunOver) AutoFillTeam(); // a Center/Berry heal can revive a fainted active member
            AppendLog($"L{node.Layer} {node.NodeType}: resolved   (₽ {(_state != null ? _state.PokeDollars : 0)})");
            Refresh();
        }

        private void OnCombatComplete(MapNode node, NodeController active, CombatController cc, CombatController.CombatOutcome outcome)
        {
            PokemonInstance caught = cc.State.CaughtTarget;
            ResolveCombatNode(active, caught, outcome);

            // §5.2 — on a win, the team that fought earns node-tier XP, then level-ups are processed
            // between nodes (before the run advances + before a freshly-caught mon joins). Defeat: none.
            string levelLog = outcome == CombatController.CombatOutcome.Victory
                ? AwardXpAndLevelUp(node.NodeType) : "";

            _run.CompleteActiveNode();
            _visited.Add(node);
            if (!_run.RunOver) AutoFillTeam(); // a recruited Pokémon joins the active team (up to 3)
            AppendLog($"L{node.Layer} {node.NodeType}: combat {outcome}" +
                      (caught != null ? $" — recruited {caught.Species?.DisplayName ?? caught.Species?.name}!" : "") +
                      levelLog);
            Refresh();
        }

        // §5.2.1 / §5.2.2 — award the cleared node's tier XP to the Active Team that fought, then
        // process between-node level-ups. Returns a log fragment ("+N XP  ▲ Name → LvX ...").
        private string AwardXpAndLevelUp(NodeType nodeType)
        {
            if (_ctx?.ProgressionConfig == null) return "";
            List<PokemonInstance> team = BuildPlayerTeam(out _);
            int xp = _ctx.ProgressionConfig.XPForNode(nodeType);
            if (xp <= 0 || team.Count == 0) return "";
            // §8.3.3 Lucky Egg Token — in-run XP ×1.15.
            xp = RelicResolver.ApplyXpMultiplier(xp, _state?.HeldRelics, _ctx.ProgressionConfig);
            XPAwarder.Award(team, xp);
            // §8.3.3 Exp Share — Box (non-active) Pokémon earn a fraction of Active Team XP.
            if (RelicHeld("exp_share") && _ctx.Box?.Members != null)
            {
                int boxXp = Mathf.FloorToInt(xp * _ctx.ProgressionConfig.ExpShareBoxFraction);
                if (boxXp > 0)
                    foreach (PokemonInstance p in _ctx.Box.Members)
                        if (p != null && !team.Contains(p)) p.CurrentXP += boxXp;
            }

            StringBuilder sb = new();
            for (int i = 0; i < team.Count; i++)
            {
                LevelUpResolver.Result r = LevelUpResolver.Process(team[i], _ctx.ProgressionConfig);
                if (r.LevelsGained <= 0) continue;
                string name = team[i].Species != null ? (team[i].Species.DisplayName ?? team[i].Species.name) : "?";
                sb.Append($"   ▲ {name} → Lv{r.NewLevel}");
                if (r.EvolutionUnlocked) sb.Append(" (ready to evolve!)");
            }
            return $"   +{xp} XP{sb}";
        }

        // §8.3 / Task 12.4 — true if the run holds a relic with the given Id.
        private bool RelicHeld(string id) =>
            _state?.HeldRelics != null && RelicResolver.Holds(_state.HeldRelics, id);

        // Per §2.3 / Pillar — the party IS the deck. Fills the Active Team with the first (up to 3)
        // non-fainted Box Pokémon so a freshly recruited one is immediately playable. (A full
        // Map-View loadout/Box-reorder screen is a later milestone.)
        private void AutoFillTeam()
        {
            if (_ctx?.Box == null || _ctx.Loadout == null) return;
            List<int> idx = new();
            for (int i = 0; i < _ctx.Box.Members.Count && idx.Count < 3; i++)
                if (_ctx.Box.Members[i] != null && _ctx.Box.Members[i].CurrentHP > 0) idx.Add(i);
            if (idx.Count == 0) return;
            int lead = _state != null ? Mathf.Clamp(_state.LeadIndex, 0, idx.Count - 1) : 0;
            _ctx.Loadout.Confirm(idx, lead);
        }

        // Per §2.3 / Task 9.10 — open the Active-Team loadout manager (only between nodes).
        private void OpenTeamPanel()
        {
            if (_teamPanel == null || _ctx?.Box == null || _ctx.Loadout == null) return;
            if (_run == null || _run.Map == null || _run.RunOver) return;
            _teamPanel.Open(_ctx.Box, _state, _ctx.Loadout, Refresh, OnEvolveRequested, _ctx.Economy);
        }

        // §6.4 / Task 11.2 — open the Trainer Hub (only outside an active run). Start Run closes the
        // Hub → reveals starter-select (the pre-run screen); the player then picks a starter + StartRun.
        private void OpenHubPanel()
        {
            if (_hubPanel == null || _ctx == null) return;
            if (_run != null && _run.Map != null && !_run.RunOver) return; // not during a run
            _hubPanel.Open(_ctx.Meta, _ctx.MetaConfig, _ctx.DifficultyChoices, OnHubStartRun, Refresh);
        }

        // §6.8 / Task 11.6 — apply the Hub's difficulty pick to the run before the player starts it.
        private void OnHubStartRun(DifficultyModifierSO selected)
        {
            if (_state != null)
                _state.ActiveDifficultyModifiers = selected != null
                    ? new System.Collections.Generic.List<DifficultyModifierSO> { selected }
                    : null;
            Refresh();
        }

        // §8.6 / Task 12.9 — open the Inventory + Held-Item equip panel (between nodes).
        private void OpenInventoryPanel()
        {
            if (_inventoryPanel == null || _ctx?.Box == null || _state == null) return;
            if (_run == null || _run.Map == null || _run.RunOver) return;
            _inventoryPanel.Open(_state, _ctx.Box, Refresh);
        }

        // §5.4.1 / Task 10.6 — open the TM application flow (only between nodes).
        private void OpenTMPanel()
        {
            if (_tmPanel == null || _ctx?.Box == null || _state == null) return;
            if (_run == null || _run.Map == null || _run.RunOver) return;
            _tmPanel.Open(_ctx.Box, _state, Refresh);
        }

        // §5.3.1 — the player chose to evolve an eligible Pokémon from the Team panel.
        private void OnEvolveRequested(PokemonInstance mon)
        {
            if (_evolutionPanel == null || mon == null) return;
            _evolutionPanel.Open(mon, _state, () =>
            {
                if (!_run.RunOver) AutoFillTeam(); // species/stat changes — re-confirm the active team
                Refresh();
            });
        }

        // ── Cheat hooks (DEV-ONLY — driven by CheatConsole) ───────────────────

        public bool CombatActive => _combat != null && _combat.IsActive;

        // F6 — grant Poké Dollars (live-refreshes an open Shop so BUY/RE-ROLL re-evaluate).
        public void CheatGiveMoney(int amount)
        {
            if (_state == null) return;
            _state.PokeDollars += amount;
            _nodePanel?.CheatRefreshIfActive();
            AppendLog($"CHEAT: +{amount} ₽  (now {_state.PokeDollars}).");
            Refresh();
        }

        // F4 (map context) — full-heal + revive every Box Pokémon.
        public void CheatHealBox()
        {
            if (_ctx?.Box == null) return;
            foreach (PokemonInstance p in _ctx.Box.Members)
                if (p != null) p.CurrentHP = PokemonVitals.MaxHP(p);
            AppendLog("CHEAT: healed the whole Box.");
            Refresh();
        }

        // F8 — grant test loot: a relic + 3 Pokéballs + 3 Potions.
        public void CheatGrantLoot()
        {
            if (_state == null || _catalog == null) return;
            if (_catalog.Relics != null && _catalog.Relics.Count > 0)
                (_state.HeldRelics ??= new List<RelicSO>()).Add(_catalog.Relics[0]);
            _state.Inventory ??= new List<ConsumableSO>();
            if (_catalog.Pokeball != null) for (int i = 0; i < 3; i++) _state.Inventory.Add(_catalog.Pokeball);
            if (_catalog.Potion != null) for (int i = 0; i < 3; i++) _state.Inventory.Add(_catalog.Potion);
            _nodePanel?.CheatRefreshIfActive();
            AppendLog("CHEAT: +1 relic, +3 Pokéballs, +3 Potions.");
            Refresh();
        }

        // F7 — auto-resolve nodes along the first-reachable path until the Gym is selectable, then
        // stop so the player can fight the boss for real. Combats are headless-won (no player damage).
        public void CheatSkipToGym()
        {
            if (_run == null || _run.RunOver || _run.Map == null) return;
            int guard = 0;
            while (!_run.RunOver && guard++ < 40)
            {
                IReadOnlyList<MapNode> sel = _run.SelectableNodes();
                if (sel == null || sel.Count == 0) break;
                MapNode next = null;
                bool gymReachable = false;
                for (int i = 0; i < sel.Count; i++)
                {
                    if (sel[i].NodeType == NodeType.Gym) { gymReachable = true; break; }
                    if (next == null) next = sel[i];
                }
                if (gymReachable || next == null) break;
                _run.EnterNode(next);
                RunAutoPilot.ResolveActive(_run);
                _run.CompleteActiveNode();
                _visited.Add(next);
            }
            AutoFillTeam();
            AppendLog("CHEAT: skipped ahead — the Gym is now reachable.");
            Refresh();
        }

        // Builds an interactive CombatController for a combat node, or null for utility nodes / no team.
        private CombatController TryBuildCombat(NodeController active)
        {
            if (_ctx == null || _ctx.BattleConfig == null) return null;
            List<PokemonInstance> team = BuildPlayerTeam(out int leadIndex);
            if (team.Count == 0) return null;

            List<ConsumableSO> inv = _state != null && _state.Inventory != null ? _state.Inventory : new List<ConsumableSO>();
            BattleConfigSO cfg = _ctx.BattleConfig;
            GameRNG rng = _ctx.Streams.CombatRNG;

            CombatController.CombatSetup setup;
            switch (active)
            {
                case TrainerBattleNodeController t: setup = t.BuildCombat(team, leadIndex, inv, FieldState.Empty, cfg, rng); break;
                case EliteNodeController e:         setup = e.BuildCombat(team, leadIndex, inv, FieldState.Empty, cfg, rng); break;
                case GymNodeController g:           setup = g.BuildCombat(team, leadIndex, inv, cfg, rng); break;
                case WildAreaNodeController w:      setup = w.SelectSpecies(0, team, leadIndex, inv, FieldState.Empty, cfg, rng); break;
                default: return null; // Center / Shop / Mystery
            }
            if (setup.EnemyTeam == null || setup.EnemyTeam.Count == 0) return null;
            setup.Economy = _ctx.Economy; // §6.2 / 11.1.8 — Trauma-aware EffectiveMaxHP for DoT + HP-bar max
            setup.Bestiary = _ctx.Bestiary; // §6.9 / 11.8.2 — enemy faints record kills
            setup.ActiveRelics = _state?.HeldRelics; // §8.3 / 12.3 — RelicResolver dispatch
            return new CombatController(setup, new UIPlayerAgent());
        }

        private void ResolveCombatNode(NodeController active, PokemonInstance caught, CombatController.CombatOutcome outcome)
        {
            switch (active)
            {
                case WildAreaNodeController w:      w.ResolveCombat(outcome, caught); break;
                case TrainerBattleNodeController t: t.ResolveCombat(outcome); break;
                case EliteNodeController e:         e.ResolveCombat(outcome); break;
                case GymNodeController g:           g.ResolveCombat(outcome); break;
            }
        }

        private List<PokemonInstance> BuildPlayerTeam(out int leadIndex)
        {
            List<PokemonInstance> team = new();
            leadIndex = 0;
            if (_ctx?.Box == null) return team;
            if (_state?.ActiveTeamIndices != null)
                foreach (int idx in _state.ActiveTeamIndices)
                    if (idx >= 0 && idx < _ctx.Box.Members.Count) team.Add(_ctx.Box.Members[idx]);
            if (team.Count == 0 && _ctx.Box.Members.Count > 0) team.Add(_ctx.Box.Members[0]);
            if (_state != null) leadIndex = Mathf.Clamp(_state.LeadIndex, 0, Mathf.Max(0, team.Count - 1));
            return team;
        }

        private void UpdateHeader()
        {
            int d = _state != null ? _state.PokeDollars : 0;
            int b = _state?.EarnedBadges?.Count ?? 0;
            int r = _state?.HeldRelics?.Count ?? 0;
            int team = _state?.ActiveTeamIndices?.Count ?? 0;
            int box = _ctx?.Box?.Members.Count ?? 0;
            string where = _run.Map == null ? "press Start" : _run.RunOver ? "run complete"
                : _run.CurrentNode == null ? "choose your first node" : $"Layer {_run.CurrentNode.Layer} — choose your route";
            _header.text = $"₽ {d}    Team {team}/3    Box {box}    Badges {b}    Relics {r}     {where}";
        }

        private void AppendLog(string line)
        {
            _logText.AppendLine(line);
            string[] lines = _logText.ToString().TrimEnd().Split('\n');
            int start = Mathf.Max(0, lines.Length - 5);
            StringBuilder shown = new();
            for (int i = start; i < lines.Length; i++) shown.AppendLine(lines[i]);
            if (_log != null) _log.text = shown.ToString();
        }

        // ── uGUI primitives ───────────────────────────────────────────────────

        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            GameObject es = new("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
        }

        private Image MakeImage(Transform parent, Color color)
        {
            GameObject go = new("Image", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>();
            img.color = color;
            return img;
        }

        private Text MakeText(Transform parent, string text, int size, Color color)
        {
            GameObject go = new("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Text t = go.AddComponent<Text>();
            t.font = _font; t.text = text; t.fontSize = size; t.color = color;
            t.alignment = TextAnchor.MiddleCenter; t.horizontalOverflow = HorizontalWrapMode.Overflow;
            return t;
        }

        private Button MakeButton(Transform parent, Vector2 anchoredPos, Vector2 size, string label, Color color, bool interactable, System.Action onClick)
        {
            GameObject go = new("Button", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>();
            img.color = color;
            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos; rt.sizeDelta = size;

            Button btn = go.AddComponent<Button>();
            btn.interactable = interactable;
            if (onClick != null) btn.onClick.AddListener(() => onClick());

            Text t = MakeText(go.transform, label, 19, interactable ? Color.white : new Color(1, 1, 1, 0.8f));
            Stretch(t.rectTransform);
            return btn;
        }

        private static void AddOutline(GameObject go, Color color)
        {
            Outline o = go.AddComponent<Outline>();
            o.effectColor = color;
            o.effectDistance = new Vector2(3, 3);
        }

        // Thin rotated Image between two local points; highlighted if it's part of the taken path.
        private void MakeLine(MapNode from, Vector2 a, Vector2 b, HashSet<MapNode> reachable)
        {
            bool active = _visited.Contains(from) || from == _run.CurrentNode ||
                          (_run.CurrentNode == null && from == _run.Map.Entry);
            Color col = active ? new Color(0.92f, 0.9f, 0.55f, 0.9f) : new Color(0.5f, 0.55f, 0.65f, 0.4f);

            GameObject go = new("Line", typeof(RectTransform));
            go.transform.SetParent(_graph, false);
            Image img = go.AddComponent<Image>();
            img.color = col; img.raycastTarget = false;

            Vector2 dir = b - a;
            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = a + dir * 0.5f;
            rt.sizeDelta = new Vector2(dir.magnitude, active ? 4f : 2.5f);
            rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        }

        // ── helpers ───────────────────────────────────────────────────────────

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        private static void Anchor(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
        }

        private static Vector2 Top() => new(0.5f, 1f);
        private static Vector2 Mid() => new(0.5f, 0.5f);
        private static Vector2 Bottom() => new(0.5f, 0f);

        private static string NodeLabel(NodeType t) => t switch
        {
            NodeType.Wild => "Wild", NodeType.Trainer => "Trainer", NodeType.Elite => "ELITE",
            NodeType.Center => "Center", NodeType.Shop => "Shop", NodeType.Mystery => "Mystery",
            NodeType.Gym => "GYM", _ => t.ToString(),
        };

        private static Color NodeColor(NodeType t) => t switch
        {
            NodeType.Wild => new Color(0.30f, 0.58f, 0.32f),
            NodeType.Trainer => new Color(0.34f, 0.42f, 0.64f),
            NodeType.Elite => new Color(0.58f, 0.34f, 0.62f),
            NodeType.Center => new Color(0.74f, 0.30f, 0.36f),
            NodeType.Shop => new Color(0.64f, 0.56f, 0.24f),
            NodeType.Mystery => new Color(0.28f, 0.52f, 0.62f),
            NodeType.Gym => new Color(0.78f, 0.46f, 0.20f),
            _ => new Color(0.4f, 0.4f, 0.4f),
        };
    }
}

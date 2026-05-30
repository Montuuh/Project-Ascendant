using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using ProjectAscendant.Core;
using ProjectAscendant.Map;
using ProjectAscendant.Combat;

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
        private CombatScreenUI _combat;

        private Text _header;
        private Text _log;
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

            _combat = new GameObject("CombatScreen").AddComponent<CombatScreenUI>();
            _combat.transform.SetParent(transform, false);

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

            if (_run.Map == null)
            {
                MakeButton(_graph, Vector2.zero, new Vector2(440, 70), "▶  START RUN",
                    new Color(0.2f, 0.6f, 0.3f), true, () => { _run.StartRun(); AppendLog("Run started — choose your route."); Refresh(); });
                return;
            }

            RenderNet();

            if (_run.RunOver)
            {
                Text done = MakeText(_graph, "★  RUN COMPLETE  ★", 40, new Color(1f, 0.85f, 0.3f));
                Anchor(done.rectTransform, Mid(), Mid(), new Vector2(0, GraphH * 0.5f - 4), new Vector2(700, 60));
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
                bool isReach = !_run.RunOver && reachable.Contains(node);
                bool isVisited = _visited.Contains(node);

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

            string detail = RunAutoPilot.Detail(active);
            string res = RunAutoPilot.ResolveActive(_run);
            _run.CompleteActiveNode();
            _visited.Add(node);
            AppendLog($"L{node.Layer} {node.NodeType}: {detail}  →  {res}");
            Refresh();
        }

        private void OnCombatComplete(MapNode node, NodeController active, CombatController cc, CombatController.CombatOutcome outcome)
        {
            ResolveCombatNode(active, cc.State.CaughtTarget, outcome);
            _run.CompleteActiveNode();
            _visited.Add(node);
            AppendLog($"L{node.Layer} {node.NodeType}: combat {outcome}");
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
            string where = _run.Map == null ? "press Start" : _run.RunOver ? "run complete"
                : _run.CurrentNode == null ? "choose your first node" : $"Layer {_run.CurrentNode.Layer} — choose your route";
            _header.text = $"₽ {d}     Badges {b}     Relics {r}        {where}";
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

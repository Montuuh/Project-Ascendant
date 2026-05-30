using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using ProjectAscendant.Core;
using ProjectAscendant.Map;

namespace ProjectAscendant.UI
{
    // Per Epic 13 / Task 9.9 (first interactive milestone) — a clickable Map View. Reads the wired
    // RunController + RunStateSO from Services and lets the player click through a Region-1 run:
    // Start Run → pick a node → (combat auto-resolved for now) → repeat → run-end.
    //
    // ⚠ TECH-DEBT: built with uGUI so it can be screenshot-verified via the bridge; the project
    // mandates UI Toolkit (ui.md / VERSION.md). Port to UI Toolkit is a follow-up (BACKLOG gap).
    // View-layer only: owns no game state — reads RunController/RunState and issues commands.
    public sealed class MapViewUI : MonoBehaviour
    {
        private RunController _run;
        private RunStateSO _state;

        private Text _header;
        private Text _log;
        private RectTransform _content; // holds the per-step buttons
        private Font _font;

        private readonly StringBuilder _logText = new();

        private void Start()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _run = Services.Has<RunController>() ? Services.Get<RunController>() : null;
            _state = Services.Has<RunStateSO>() ? Services.Get<RunStateSO>() : null;

            BuildChrome();
            Refresh();
        }

        // ── Layout chrome (built once) ────────────────────────────────────────

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

            // Background.
            Image bg = MakePanel(canvas.transform, new Color(0.10f, 0.12f, 0.16f, 1f));
            Stretch(bg.rectTransform);

            // Title.
            Text title = MakeText(canvas.transform, "PROJECT ASCENDANT  —  Region 1: Verdant Route", 40, new Color(0.85f, 0.95f, 0.7f));
            Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -50), new Vector2(1400, 60));
            title.alignment = TextAnchor.MiddleCenter;

            // Header (₽ / badges / position).
            _header = MakeText(canvas.transform, "", 28, Color.white);
            Anchor(_header.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -110), new Vector2(1400, 40));
            _header.alignment = TextAnchor.MiddleCenter;

            // Content area (buttons for the current step).
            GameObject contentGO = new("Content", typeof(RectTransform));
            contentGO.transform.SetParent(canvas.transform, false);
            _content = (RectTransform)contentGO.transform;
            Anchor(_content, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(900, 520));
            VerticalLayoutGroup vlg = contentGO.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.spacing = 14;
            vlg.childControlHeight = false; vlg.childControlWidth = false;
            vlg.childForceExpandHeight = false; vlg.childForceExpandWidth = false;

            // Log strip.
            _log = MakeText(canvas.transform, "", 20, new Color(0.7f, 0.8f, 0.9f));
            Anchor(_log.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 20), new Vector2(1700, 180));
            _log.alignment = TextAnchor.LowerCenter;
        }

        // ── Refresh content for the current run state ─────────────────────────

        private void Refresh()
        {
            ClearContent();

            if (_run == null)
            {
                _header.text = "<no RunController wired — is RunLauncher in the Boot scene?>";
                return;
            }

            UpdateHeader();

            if (_run.Map == null)
            {
                MakeButton(_content, "▶  START RUN", new Color(0.2f, 0.6f, 0.3f), () =>
                {
                    _run.StartRun();
                    AppendLog("Run started.");
                    Refresh();
                });
                return;
            }

            if (_run.RunOver)
            {
                Text done = MakeText(_content, "★  RUN COMPLETE  ★", 44, new Color(1f, 0.85f, 0.3f));
                done.alignment = TextAnchor.MiddleCenter;
                SetSize(done.rectTransform, 900, 80);
                return;
            }

            // In progress — one button per reachable node.
            IReadOnlyList<MapNode> options = _run.SelectableNodes();
            Text prompt = MakeText(_content, options.Count == 1 ? "Proceed:" : "Choose your path:", 26, Color.white);
            prompt.alignment = TextAnchor.MiddleCenter;
            SetSize(prompt.rectTransform, 700, 36);

            foreach (MapNode node in options)
            {
                MapNode captured = node;
                string label = $"L{node.Layer}   {NodeLabel(node.NodeType)}";
                MakeButton(_content, label, NodeColor(node.NodeType), () => OnNodeClicked(captured));
            }
        }

        private void OnNodeClicked(MapNode node)
        {
            _run.EnterNode(node);
            string detail = RunAutoPilot.Detail(_run.ActiveNode);
            string outcome = RunAutoPilot.ResolveActive(_run);
            _run.CompleteActiveNode();
            AppendLog($"L{node.Layer} {node.NodeType}: {detail}  →  {outcome}");
            Refresh();
        }

        private void UpdateHeader()
        {
            int dollars = _state != null ? _state.PokeDollars : 0;
            int badges = _state?.EarnedBadges?.Count ?? 0;
            int relics = _state?.HeldRelics?.Count ?? 0;
            string where = _run.CurrentNode == null ? "Start" : $"Layer {_run.CurrentNode.Layer}";
            _header.text = $"₽ {dollars}      Badges {badges}      Relics {relics}      —      {where}";
        }

        private void AppendLog(string line)
        {
            _logText.AppendLine(line);
            string[] lines = _logText.ToString().TrimEnd().Split('\n');
            int start = Mathf.Max(0, lines.Length - 6);
            StringBuilder shown = new();
            for (int i = start; i < lines.Length; i++) shown.AppendLine(lines[i]);
            if (_log != null) _log.text = shown.ToString();
        }

        // ── uGUI helpers ──────────────────────────────────────────────────────

        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            GameObject es = new("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>(); // new Input System (legacy module forbidden)
        }

        private Image MakePanel(Transform parent, Color color)
        {
            GameObject go = new("Panel", typeof(RectTransform));
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
            t.alignment = TextAnchor.MiddleLeft; t.horizontalOverflow = HorizontalWrapMode.Overflow;
            return t;
        }

        private Button MakeButton(Transform parent, string label, Color color, Action onClick)
        {
            GameObject go = new("Button", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>();
            img.color = color;
            SetSize((RectTransform)go.transform, 760, 64);
            Button btn = go.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick());

            Text t = MakeText(go.transform, label, 28, Color.white);
            t.alignment = TextAnchor.MiddleCenter;
            Stretch(t.rectTransform);
            return btn;
        }

        private void ClearContent()
        {
            for (int i = _content.childCount - 1; i >= 0; i--)
                Destroy(_content.GetChild(i).gameObject);
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        private static void Anchor(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 anchoredPos, Vector2 size)
        {
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos; rt.sizeDelta = size;
        }

        private static void SetSize(RectTransform rt, float w, float h) => rt.sizeDelta = new Vector2(w, h);

        private static string NodeLabel(NodeType t) => t switch
        {
            NodeType.Wild => "Wild Pokémon Area",
            NodeType.Trainer => "Trainer Battle",
            NodeType.Elite => "Elite Trainer",
            NodeType.Center => "Pokémon Center",
            NodeType.Shop => "Shop",
            NodeType.Mystery => "Mystery Event",
            NodeType.Gym => "GYM LEADER",
            _ => t.ToString(),
        };

        private static Color NodeColor(NodeType t) => t switch
        {
            NodeType.Wild => new Color(0.3f, 0.55f, 0.3f),
            NodeType.Trainer => new Color(0.35f, 0.4f, 0.6f),
            NodeType.Elite => new Color(0.55f, 0.35f, 0.6f),
            NodeType.Center => new Color(0.7f, 0.3f, 0.35f),
            NodeType.Shop => new Color(0.6f, 0.55f, 0.25f),
            NodeType.Mystery => new Color(0.3f, 0.5f, 0.6f),
            NodeType.Gym => new Color(0.7f, 0.45f, 0.2f),
            _ => new Color(0.4f, 0.4f, 0.4f),
        };
    }
}

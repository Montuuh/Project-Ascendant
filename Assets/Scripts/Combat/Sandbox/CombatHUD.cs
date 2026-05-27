using UnityEngine;
using ProjectAscendant.Core;

namespace ProjectAscendant.Combat.Sandbox
{
    // Per Epic 4 Task 4.1.9 — minimum-viable IMGUI HUD for the Play Mode
    // sandbox. Throwaway code. Epic 13 builds the real UI Toolkit screen.
    //
    // Lives next to a CombatBootstrap on the same GameObject. Reads state
    // every OnGUI frame and renders a 2-column layout (player | enemy) with
    // hand cards as buttons, plus an End Turn button and outcome banner.
    [RequireComponent(typeof(CombatBootstrap))]
    public sealed class CombatHUD : MonoBehaviour
    {
        private CombatBootstrap _boot;
        private GUIStyle _bigStyle;
        private GUIStyle _hpStyle;
        private GUIStyle _intentStyle;
        private GUIStyle _outcomeStyle;
        private GUIStyle _bgStyle;

        private void Awake()
        {
            _boot = GetComponent<CombatBootstrap>();
        }

        private void EnsureStyles()
        {
            if (_bigStyle != null) return;
            _bigStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = Color.white },
            };
            _hpStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                normal = { textColor = new Color(0.85f, 0.95f, 0.85f) },
            };
            _intentStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Italic,
                normal = { textColor = new Color(1f, 0.6f, 0.6f) },
            };
            _outcomeStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 48,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.yellow },
            };
            _bgStyle = new GUIStyle(GUI.skin.box);
        }

        private void OnGUI()
        {
            if (_boot == null || _boot.Controller == null) return;
            EnsureStyles();

            CombatController.CombatState s = _boot.Controller.State;

            float W = Screen.width;
            float H = Screen.height;

            // Background plate so labels are legible regardless of skybox.
            GUI.Box(new Rect(0, 0, W, H), GUIContent.none, _bgStyle);

            DrawHeader(s, W);
            DrawPlayerPanel(s, W, H);
            DrawEnemyPanel(s, W, H);
            DrawHand(s, W, H);
            DrawFooter(s, W, H);
            DrawOutcomeBanner(s, W, H);
        }

        // ── Header: turn + AP + field state ──────────────────────────────────
        private void DrawHeader(CombatController.CombatState s, float w)
        {
            string header =
                $"Turn {s.TurnNumber}    " +
                $"AP {s.CurrentAP}/{s.Config.MaxAPPerTurn}    " +
                $"Phase {s.CurrentPhase}    " +
                $"Field W={s.Field.Weather} / T={s.Field.Terrain}";
            GUI.Label(new Rect(10, 10, w - 20, 30), header, _bigStyle);
        }

        // ── Player panel: left half ─────────────────────────────────────────
        private void DrawPlayerPanel(CombatController.CombatState s, float w, float h)
        {
            PokemonInstance lead = s.PlayerTeam.Count > 0 ? s.PlayerTeam[0] : null;
            if (lead == null || lead.Species == null) return;

            Rect r = new(10, 50, w / 2 - 20, 140);
            GUI.Box(r, GUIContent.none, _bgStyle);
            GUI.Label(new Rect(r.x + 10, r.y + 5, r.width - 20, 30),
                      $"You: {lead.Species.name}", _bigStyle);

            int maxHP = lead.Species.BaseStats.BaseHP;
            DrawHPBar(new Rect(r.x + 10, r.y + 45, r.width - 20, 24),
                      lead.CurrentHP, maxHP);
            GUI.Label(new Rect(r.x + 10, r.y + 75, r.width - 20, 24),
                      $"HP {lead.CurrentHP}/{maxHP}    " +
                      $"Status {lead.PrimaryStatus}" +
                      (lead.SecondaryStatus != StatusCondition.None
                          ? $" + {lead.SecondaryStatus}" : ""),
                      _hpStyle);
            GUI.Label(new Rect(r.x + 10, r.y + 100, r.width - 20, 24),
                      $"Trauma {lead.TraumaStacks}    " +
                      $"AtkStg {StatStageManager.GetStage(lead, Stat.Attack)}  " +
                      $"DefStg {StatStageManager.GetStage(lead, Stat.Defense)}",
                      _hpStyle);
        }

        // ── Enemy panel: right half ─────────────────────────────────────────
        private void DrawEnemyPanel(CombatController.CombatState s, float w, float h)
        {
            PokemonInstance enemy = s.EnemyTeam.Count > 0 ? s.EnemyTeam[0] : null;
            if (enemy == null || enemy.Species == null) return;

            Rect r = new(w / 2 + 10, 50, w / 2 - 20, 140);
            GUI.Box(r, GUIContent.none, _bgStyle);
            GUI.Label(new Rect(r.x + 10, r.y + 5, r.width - 20, 30),
                      $"Enemy: {enemy.Species.name}", _bigStyle);

            int maxHP = enemy.Species.BaseStats.BaseHP;
            DrawHPBar(new Rect(r.x + 10, r.y + 45, r.width - 20, 24),
                      enemy.CurrentHP, maxHP);
            GUI.Label(new Rect(r.x + 10, r.y + 75, r.width - 20, 24),
                      $"HP {enemy.CurrentHP}/{maxHP}    Status {enemy.PrimaryStatus}",
                      _hpStyle);

            // Intent display — what the enemy is about to do
            if (s.EnemyIntents.Count > 0 && enemy.CurrentHP > 0)
            {
                Intent it = s.EnemyIntents[0];
                string text;
                if (it.Move != null && it.Kind == IntentKind.Attack)
                    text = $"Intent ⚔  {it.Move.DisplayName ?? it.Move.name} → Slot {it.TargetSlot}";
                else
                    text = $"Intent  {it.Kind}";
                GUI.Label(new Rect(r.x + 10, r.y + 105, r.width - 20, 24),
                          text, _intentStyle);
            }
        }

        // ── HP bar (filled-rect indicator) ──────────────────────────────────
        private static void DrawHPBar(Rect rect, int current, int max)
        {
            float pct = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
            // Background
            Color prev = GUI.color;
            GUI.color = new Color(0.15f, 0.15f, 0.15f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            // Fill
            GUI.color = pct < 0.30f ? new Color(0.9f, 0.2f, 0.2f) :
                        pct < 0.60f ? new Color(0.9f, 0.7f, 0.2f) :
                                      new Color(0.3f, 0.85f, 0.3f);
            Rect fill = rect;
            fill.width *= pct;
            GUI.DrawTexture(fill, Texture2D.whiteTexture);
            GUI.color = prev;
        }

        // ── Hand: row of buttons (one per skill card) ──────────────────────
        private void DrawHand(CombatController.CombatState s, float w, float h)
        {
            if (s.SkillHand.Count == 0)
            {
                GUI.Label(new Rect(10, 220, w - 20, 30),
                          "(No skill cards in hand — press End Turn)", _hpStyle);
                return;
            }

            float cardW = 200f;
            float spacing = 12f;
            float totalW = s.SkillHand.Count * cardW + (s.SkillHand.Count - 1) * spacing;
            float startX = Mathf.Max(10f, (w - totalW) / 2f);
            float y = h - 240f;

            for (int i = 0; i < s.SkillHand.Count; i++)
            {
                CardEntry card = s.SkillHand[i];
                MoveSO move = card.Move;
                if (move == null) continue;
                PokemonInstance owner = card.Owner;
                int effCost = StatusModifiers.GetEffectiveAPCost(move, owner, s.Config);
                string label =
                    $"{move.DisplayName ?? move.name}\n" +
                    $"{move.Type}  Pwr {move.BasePower}\n" +
                    $"AP {effCost}";

                Rect r = new(startX + i * (cardW + spacing), y, cardW, 120);
                bool disabled = effCost > s.CurrentAP ||
                                s.CurrentPhase != CombatController.Phase.ActionPhase ||
                                s.Outcome != CombatController.CombatOutcome.InProgress;
                GUI.enabled = !disabled;
                if (GUI.Button(r, label))
                    _boot.PlayCard(i);
                GUI.enabled = true;
            }
        }

        // ── Footer: End Turn + Restart buttons ──────────────────────────────
        private void DrawFooter(CombatController.CombatState s, float w, float h)
        {
            float bw = 160f;
            Rect end = new(w - bw - 20, h - 80, bw, 60);
            bool canEndTurn = s.Outcome == CombatController.CombatOutcome.InProgress;
            GUI.enabled = canEndTurn;
            if (GUI.Button(end, "End Turn")) _boot.AdvanceTurn();
            GUI.enabled = true;

            Rect restart = new(20, h - 80, bw, 60);
            if (GUI.Button(restart, "Restart")) _boot.RestartCombat();
        }

        // ── Outcome banner (Victory / Defeat) ───────────────────────────────
        private void DrawOutcomeBanner(CombatController.CombatState s, float w, float h)
        {
            if (s.Outcome == CombatController.CombatOutcome.InProgress) return;
            string text = s.Outcome == CombatController.CombatOutcome.Victory
                ? "VICTORY!" : "DEFEAT";
            Color prev = GUI.color;
            GUI.color = s.Outcome == CombatController.CombatOutcome.Victory
                ? new Color(0.3f, 1f, 0.4f) : new Color(1f, 0.3f, 0.3f);
            GUI.Label(new Rect(0, h / 2 - 50, w, 100), text, _outcomeStyle);
            GUI.color = prev;
        }
    }
}

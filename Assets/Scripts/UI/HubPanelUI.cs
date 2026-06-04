using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectAscendant.Core;

namespace ProjectAscendant.UI
{
    // Per §6.4 + Epic 11 Task 11.2 — the Trainer Hub, VS skeleton. A clean 2D kiosk menu (NOT a 3D
    // scene for launch, §6.4). VS ships two live kiosks — Trainer Card (§6.4.3 stats) and PC Terminal
    // (§6.4 bestiary/achievements/history) — plus greyed Daycare Lady + Mystery Door (post-launch,
    // §6.4.1). Reads the persisted MetaProgressionSO; view-layer only.
    //
    // ⚠ TECH-DEBT (gap #38): temp uGUI overlay so the bridge can screenshot-verify. The polished
    // UI-Toolkit Hub *scene* is Epic 13.8 (cross-linked). PC-Terminal CONTENTS fill in with 11.5
    // (achievements) + 11.8 (bestiary); difficulty-select with 11.6.
    public sealed class HubPanelUI : MonoBehaviour
    {
        private MetaProgressionSO _meta;
        private MetaProgressionConfigSO _cfg;
        private Action _onClosed;
        private Action _onOpenPokedex; // §6.9 — opens the Pokédex overlay (owned by MapViewUI)
        private bool _showAchievements; // §6.7.2 — PC Terminal achievement list view
        private Font _font;
        private GameObject _root;
        private RectTransform _body;

        public bool IsOpen => _root != null;

        // §6.4 — view-only Trainer Hub (Trainer Card + achievements). Difficulty selection moved to the
        // New Run flow (§6.8 / gap #43); the Hub no longer starts runs.
        public void Open(MetaProgressionSO meta, MetaProgressionConfigSO cfg, Action onClosed,
                         Action onOpenPokedex = null)
        {
            _meta = meta; _cfg = cfg; _showAchievements = false;
            _onClosed = onClosed;
            _onOpenPokedex = onOpenPokedex;
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

        private void Build()
        {
            _root = new GameObject("HubPanelCanvas");
            Canvas canvas = _root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 28;
            CanvasScaler scaler = _root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            _root.AddComponent<GraphicRaycaster>();

            Image bg = Img(_root.transform, new Color(0.06f, 0.09f, 0.12f, 1f));
            Stretch(bg.rectTransform);

            GameObject bodyGO = new("Body", typeof(RectTransform));
            bodyGO.transform.SetParent(_root.transform, false);
            _body = (RectTransform)bodyGO.transform;
            Place(_body, Mid(), Vector2.zero, new Vector2(1600, 1000));
        }

        private void RefreshBody()
        {
            for (int i = _body.childCount - 1; i >= 0; i--) Destroy(_body.GetChild(i).gameObject);

            if (_showAchievements) { RenderAchievements(); return; }

            Txt(_body, "TRAINER HUB", 40, new Color(0.85f, 0.95f, 0.7f), Mid(), new Vector2(0, 430), new Vector2(1300, 56));

            // ── Trainer Card kiosk (§6.4.3) ────────────────────────────────────
            int level = _meta != null ? Mathf.Max(1, _meta.TrainerLevel) : 1;
            int xp = _meta != null ? _meta.TrainerXP : 0;
            int tokens = _meta != null ? _meta.TrainerTokens : 0;
            int won = _meta != null ? _meta.TotalRunsCompleted : 0;
            int attempts = _meta != null ? _meta.TotalRunsAttempted : 0;
            int lost = Mathf.Max(0, attempts - won);
            int nextThreshold = _cfg != null ? _cfg.CumulativeXPForLevel(level + 1) : 0;

            Image card = Img(_body, new Color(0.13f, 0.18f, 0.23f, 1f));
            Place(card.rectTransform, Mid(), new Vector2(-360, 230), new Vector2(820, 300));
            Txt(card.transform, "TRAINER CARD", 24, new Color(0.8f, 0.9f, 1f), Mid(), new Vector2(0, 120), new Vector2(760, 32));
            Txt(card.transform, $"Trainer Level {level}", 30, new Color(0.95f, 0.95f, 0.8f), Mid(), new Vector2(0, 64), new Vector2(760, 38));
            string xpLine = nextThreshold > 0 ? $"XP {xp} / {nextThreshold} to Lv {level + 1}" : $"XP {xp}  (max level)";
            Txt(card.transform, xpLine, 22, new Color(0.85f, 0.9f, 0.95f), Mid(), new Vector2(0, 18), new Vector2(760, 30));
            Txt(card.transform, $"◆ {tokens} Trainer Tokens", 24, new Color(0.95f, 0.85f, 0.45f), Mid(), new Vector2(0, -34), new Vector2(760, 32));
            Txt(card.transform, $"Runs:  {won} won  ·  {lost} lost  ·  {attempts} total", 20, new Color(0.8f, 0.85f, 0.9f), Mid(), new Vector2(0, -84), new Vector2(760, 28));

            // ── PC Terminal kiosk (§6.4 — contents fill in with 11.5/11.8) ─────
            Image pc = Img(_body, new Color(0.13f, 0.18f, 0.23f, 1f));
            Place(pc.rectTransform, Mid(), new Vector2(360, 230), new Vector2(740, 300));
            Txt(pc.transform, "PC TERMINAL", 24, new Color(0.8f, 0.9f, 1f), Mid(), new Vector2(0, 120), new Vector2(700, 32));
            int doneCount = CompletedCount();
            Txt(pc.transform, $"Achievements  {doneCount} / {AchievementCatalog.All.Count}", 20, new Color(0.85f, 0.9f, 0.95f), Mid(), new Vector2(0, 64), new Vector2(700, 28));
            Btn(_body, Mid(), new Vector2(360, 196), new Vector2(360, 50), "🏆  VIEW ACHIEVEMENTS",
                new Color(0.34f, 0.40f, 0.30f), true, () => { _showAchievements = true; RefreshBody(); });
            Btn(_body, Mid(), new Vector2(360, 138), new Vector2(360, 50), "📖  VIEW POKÉDEX",
                new Color(0.30f, 0.38f, 0.44f), _onOpenPokedex != null, () => _onOpenPokedex?.Invoke());
            Txt(pc.transform, "Run History  (Epic 13)", 17, new Color(0.6f, 0.65f, 0.72f), Mid(), new Vector2(0, -92), new Vector2(700, 24));

            // ── Post-launch kiosks — greyed (§6.4.1) ───────────────────────────
            Btn(_body, Mid(), new Vector2(-360, 20), new Vector2(380, 56), "Daycare Lady  (Post-launch)", new Color(0.3f, 0.3f, 0.34f), false, null);
            Btn(_body, Mid(), new Vector2(360, 20), new Vector2(380, 56), "Mystery Door  (Post-launch)", new Color(0.3f, 0.3f, 0.34f), false, null);

            // ── Actions ────────────────────────────────────────────────────────
            // Difficulty selection moved to the New Run flow (§6.8 / gap #43); the Hub is view-only.
            Btn(_body, Mid(), new Vector2(0, -150), new Vector2(320, 60), "CLOSE  ✕", new Color(0.42f, 0.34f, 0.36f), true, Close);
        }

        private int CompletedCount()
        {
            int c = 0;
            foreach (AchievementSO a in AchievementCatalog.All)
                if (AchievementService.IsCompleted(_meta, a.AchievementId)) c++;
            return c;
        }

        // §6.7.2 — the PC Terminal achievement list: name (or ??? for hidden+incomplete, §6.7.3),
        // description, and status (✓ +XP / counter progress / locked).
        private void RenderAchievements()
        {
            Txt(_body, "ACHIEVEMENTS", 40, new Color(0.95f, 0.85f, 0.45f), Mid(), new Vector2(0, 440), new Vector2(1300, 56));
            Txt(_body, $"{CompletedCount()} / {AchievementCatalog.All.Count} unlocked", 22, new Color(0.8f, 0.85f, 0.9f), Mid(), new Vector2(0, 398), new Vector2(1000, 30));

            IReadOnlyList<AchievementSO> all = AchievementCatalog.All;
            float y = 340f;
            for (int i = 0; i < all.Count; i++)
            {
                AchievementSO a = all[i];
                bool done = AchievementService.IsCompleted(_meta, a.AchievementId);
                bool hideName = a.Hidden && !done;
                string name = hideName ? "??? (hidden)" : (a.DisplayName ?? a.AchievementId);
                string desc = hideName ? "Complete to reveal." : a.Description;
                int prog = AchievementService.GetProgress(_meta, a.AchievementId);
                string status = done ? $"✓  +{a.TrainerXPReward} XP"
                              : a.TargetCount > 1 ? $"{prog} / {a.TargetCount}"
                              : "locked";

                Image row = Img(_body, done ? new Color(0.16f, 0.24f, 0.18f, 1f) : new Color(0.14f, 0.15f, 0.18f, 1f));
                Place(row.rectTransform, Mid(), new Vector2(0, y), new Vector2(1300, 56));
                Txt(row.transform, $"{(done ? "🏆" : "•")}  {name}", 22,
                    done ? new Color(0.92f, 0.97f, 0.8f) : new Color(0.85f, 0.87f, 0.9f), Mid(), new Vector2(-450, 0), new Vector2(400, 30));
                Txt(row.transform, desc, 17, new Color(0.7f, 0.75f, 0.8f), Mid(), new Vector2(110, 0), new Vector2(640, 28));
                Txt(row.transform, status, 19, done ? new Color(0.6f, 0.95f, 0.6f) : new Color(0.8f, 0.8f, 0.5f), Mid(), new Vector2(560, 0), new Vector2(180, 28));
                y -= 64f;
            }

            Btn(_body, Mid(), new Vector2(0, y - 14f), new Vector2(300, 54), "◀ BACK",
                new Color(0.42f, 0.34f, 0.36f), true, () => { _showAchievements = false; RefreshBody(); });
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
            Text t = Txt(go.transform, label, 20, interactable ? Color.white : new Color(1, 1, 1, 0.55f), Mid(), Vector2.zero, size);
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

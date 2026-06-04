using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectAscendant.Core;

namespace ProjectAscendant.UI
{
    // Per §5.2 + playtest #7 — the post-combat "experience" moment (Pokémon-style). After a victory,
    // each Active-Team member's XP bar animates from its pre-combat fill toward the new total, rolling
    // over with a flash + level bump on each level gained. Pure presentation: it animates from captured
    // snapshots (the PokemonInstances are already updated by XPAwarder/LevelUpResolver), so it never
    // touches game state. View-layer only (gap #38 uGUI; UI-Toolkit polish = Epic 13).
    public sealed class XpRewardPanelUI : MonoBehaviour
    {
        // One Pokémon's XP gain, captured around the award so the bar can replay the fill.
        public struct XpGainRecord
        {
            public string Name;
            public int LevelBefore;
            public int XpBefore;     // CurrentXP within LevelBefore, before the award
            public int XpGained;     // total XP credited this combat
            public bool EvolutionReady;
        }

        private Func<int, int> _xpToNext;  // level → XP required to reach the next level (§5.2.3)
        private Action _onClosed;
        private Font _font;
        private GameObject _root;
        private bool _skipRequested;       // CONTINUE press fast-forwards the animation

        private const float FillSeconds = 0.45f;   // per level-segment fill duration
        private const float FlashSeconds = 0.22f;  // level-up flash duration
        private const float BarWidth = 520f;

        public bool IsOpen => _root != null;

        // Show the panel and animate. `xpToNext` resolves the per-level threshold (ProgressionConfigSO.XPToNext).
        public void Show(IReadOnlyList<XpGainRecord> records, Func<int, int> xpToNext, Action onClosed)
        {
            if (records == null || records.Count == 0 || xpToNext == null) { onClosed?.Invoke(); return; }
            _xpToNext = xpToNext;
            _onClosed = onClosed;
            _skipRequested = false;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Build(records);
            StartCoroutine(Animate(records));
        }

        private void Close()
        {
            if (_root != null) Destroy(_root);
            _root = null;
            Action cb = _onClosed; _onClosed = null;
            cb?.Invoke();
        }

        // Cached per-row widgets so the coroutine can drive them.
        private readonly List<Image> _fills = new();
        private readonly List<Image> _rowBgs = new();
        private readonly List<Text> _levelLabels = new();
        private Button _continueBtn;

        private void Build(IReadOnlyList<XpGainRecord> records)
        {
            _fills.Clear(); _rowBgs.Clear(); _levelLabels.Clear();

            _root = new GameObject("XpRewardCanvas");
            Canvas canvas = _root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30; // above map / node panels
            CanvasScaler scaler = _root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            _root.AddComponent<GraphicRaycaster>();

            Image dim = Img(_root.transform, new Color(0.03f, 0.05f, 0.07f, 0.92f));
            Stretch(dim.rectTransform);

            Txt(_root.transform, "EXPERIENCE", 44, new Color(0.9f, 0.97f, 0.7f),
                new Vector2(0, 380), new Vector2(900, 60));

            float y = 230f;
            for (int i = 0; i < records.Count; i++)
            {
                XpGainRecord rec = records[i];

                Image row = Img(_root.transform, new Color(0.12f, 0.16f, 0.21f, 1f));
                Place(row.rectTransform, new Vector2(0, y), new Vector2(900, 110));
                _rowBgs.Add(row);

                Txt(row.transform, rec.Name ?? "?", 26, new Color(0.92f, 0.95f, 1f),
                    new Vector2(-330, 28), new Vector2(300, 34), TextAnchor.MiddleLeft);

                Text lvl = Txt(row.transform, $"Lv {rec.LevelBefore}", 24, new Color(0.95f, 0.9f, 0.6f),
                    new Vector2(300, 28), new Vector2(120, 34), TextAnchor.MiddleRight);
                _levelLabels.Add(lvl);

                if (rec.XpGained > 0)
                    Txt(row.transform, $"+{rec.XpGained} XP", 20, new Color(0.7f, 0.9f, 0.95f),
                        new Vector2(-330, -30), new Vector2(300, 28), TextAnchor.MiddleLeft);
                if (rec.EvolutionReady)
                    Txt(row.transform, "★ ready to evolve!", 20, new Color(0.7f, 0.95f, 0.7f),
                        new Vector2(40, 28), new Vector2(320, 30), TextAnchor.MiddleCenter);

                // XP bar: dark track + animated fill (left-anchored width).
                Image track = Img(row.transform, new Color(0.06f, 0.09f, 0.12f, 1f));
                Place(track.rectTransform, new Vector2(0, -28), new Vector2(BarWidth, 22));
                Image fill = Img(track.transform, new Color(0.45f, 0.85f, 1f, 1f));
                RectTransform frt = fill.rectTransform;
                frt.anchorMin = new Vector2(0f, 0f); frt.anchorMax = new Vector2(0f, 1f);
                frt.pivot = new Vector2(0f, 0.5f);
                frt.anchoredPosition = Vector2.zero;
                frt.sizeDelta = new Vector2(0f, 0f); // width driven in SetFill
                _fills.Add(fill);

                float startFrac = Frac(rec.XpBefore, _xpToNext(rec.LevelBefore));
                SetFill(i, startFrac);

                y -= 130f;
            }

            _continueBtn = Btn(_root.transform, new Vector2(0, y - 10f), new Vector2(320, 60),
                "CONTINUE  ▶", new Color(0.30f, 0.42f, 0.34f), OnContinue);
        }

        private void OnContinue()
        {
            // First press fast-forwards the animation; once finished, it closes (handled in coroutine).
            _skipRequested = true;
        }

        private IEnumerator Animate(IReadOnlyList<XpGainRecord> records)
        {
            for (int i = 0; i < records.Count; i++)
                yield return AnimateRow(i, records[i]);

            // Animation done — turn CONTINUE into the close action.
            _skipRequested = false;
            if (_continueBtn != null)
            {
                _continueBtn.onClick.RemoveAllListeners();
                _continueBtn.onClick.AddListener(Close);
            }
        }

        private IEnumerator AnimateRow(int idx, XpGainRecord rec)
        {
            int level = rec.LevelBefore;
            int xp = rec.XpBefore;
            int remaining = rec.XpGained;
            int guard = 0;

            while (remaining > 0 && guard++ < 100)
            {
                int need = _xpToNext(level);
                if (need <= 0) break;
                int room = need - xp;

                if (remaining >= room)
                {
                    // Fill to the top of this level, then level up.
                    yield return FillTo(idx, Frac(xp, need), 1f);
                    remaining -= room;
                    level++;
                    xp = 0;
                    if (_levelLabels[idx] != null) _levelLabels[idx].text = $"Lv {level}";
                    yield return FlashRow(idx);
                    SetFill(idx, 0f);
                }
                else
                {
                    int target = xp + remaining;
                    yield return FillTo(idx, Frac(xp, need), Frac(target, need));
                    xp = target;
                    remaining = 0;
                }
            }
        }

        private IEnumerator FillTo(int idx, float from, float to)
        {
            if (_skipRequested) { SetFill(idx, to); yield break; }
            float t = 0f;
            while (t < FillSeconds)
            {
                if (_skipRequested) { SetFill(idx, to); yield break; }
                t += Time.unscaledDeltaTime;
                SetFill(idx, Mathf.Lerp(from, to, Mathf.Clamp01(t / FillSeconds)));
                yield return null;
            }
            SetFill(idx, to);
        }

        private IEnumerator FlashRow(int idx)
        {
            Image bg = idx < _rowBgs.Count ? _rowBgs[idx] : null;
            if (bg == null) yield break;
            Color baseC = new(0.12f, 0.16f, 0.21f, 1f);
            Color flashC = new(0.95f, 0.95f, 0.6f, 1f);
            if (_skipRequested) { bg.color = baseC; yield break; }
            float t = 0f;
            while (t < FlashSeconds)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.PingPong(t / FlashSeconds * 2f, 1f);
                bg.color = Color.Lerp(baseC, flashC, k);
                yield return null;
            }
            bg.color = baseC;
        }

        private void SetFill(int idx, float frac)
        {
            if (idx < 0 || idx >= _fills.Count || _fills[idx] == null) return;
            RectTransform rt = _fills[idx].rectTransform;
            rt.sizeDelta = new Vector2(BarWidth * Mathf.Clamp01(frac), rt.sizeDelta.y);
        }

        private static float Frac(int xp, int need) => need <= 0 ? 0f : Mathf.Clamp01((float)xp / need);

        // ── uGUI primitives (mirrors HubPanelUI idiom) ──────────────────────────

        private Image Img(Transform parent, Color c)
        {
            GameObject go = new("Image", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>(); img.color = c;
            return img;
        }

        private Text Txt(Transform parent, string text, int size, Color color, Vector2 pos, Vector2 sizeD,
                         TextAnchor align = TextAnchor.MiddleCenter)
        {
            GameObject go = new("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Text t = go.AddComponent<Text>();
            t.font = _font; t.text = text; t.fontSize = size; t.color = color;
            t.alignment = align; t.horizontalOverflow = HorizontalWrapMode.Overflow;
            Place(t.rectTransform, pos, sizeD);
            return t;
        }

        private Button Btn(Transform parent, Vector2 pos, Vector2 size, string label, Color color, Action onClick)
        {
            GameObject go = new("Button", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>(); img.color = color;
            Place((RectTransform)go.transform, pos, size);
            Button btn = go.AddComponent<Button>();
            if (onClick != null) btn.onClick.AddListener(() => onClick());
            Text t = Txt(go.transform, label, 22, Color.white, Vector2.zero, size);
            Stretch(t.rectTransform);
            return btn;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        private static void Place(RectTransform rt, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
        }
    }
}

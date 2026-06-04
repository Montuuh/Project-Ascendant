using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectAscendant.Core;

namespace ProjectAscendant.UI
{
    // Per §6.9 / §4.3.9.1 — the Pokédex screen (opened from the Hub PC Terminal). Read-only view of
    // discovery + mastery progress per species: seen/defeated/recruited counts, the current Bestiary
    // tier with a progress bar to the next tier, and Mastery-unlock status. Unseen species render as
    // "???" silhouettes (classic discovery). View-layer only (gap #38 uGUI; UI-Toolkit polish Epic 13).
    public sealed class PokedexPanelUI : MonoBehaviour
    {
        private Font _font;
        private GameObject _root;
        private Action _onClosed;

        public bool IsOpen => _root != null;

        // species = full VS roster (RunContentRegistry.AllSpecies); dex = discovery data; meta = mastery unlocks.
        public void Open(IReadOnlyList<PokemonSpeciesSO> species, BestiaryProgressSO dex,
                         MetaProgressionSO meta, Action onClosed)
        {
            _onClosed = onClosed;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Build(species ?? new List<PokemonSpeciesSO>(), dex, meta);
        }

        private void Close()
        {
            if (_root != null) Destroy(_root);
            _root = null;
            Action cb = _onClosed; _onClosed = null;
            cb?.Invoke();
        }

        private void Build(IReadOnlyList<PokemonSpeciesSO> species, BestiaryProgressSO dex, MetaProgressionSO meta)
        {
            _root = new GameObject("PokedexCanvas");
            Canvas canvas = _root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 31; // above the Hub (28)
            CanvasScaler scaler = _root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            _root.AddComponent<GraphicRaycaster>();

            Image bg = Img(_root.transform, new Color(0.05f, 0.07f, 0.10f, 1f));
            Stretch(bg.rectTransform);

            int seen = dex != null ? dex.SeenSpeciesCount() : 0;
            int total = species.Count;
            int pct = total > 0 ? Mathf.RoundToInt(100f * seen / total) : 0;
            Txt(_root.transform, "POKÉDEX", 40, new Color(0.9f, 0.95f, 0.7f),
                new Vector2(0, 478), new Vector2(1200, 54), TextAnchor.MiddleCenter);
            Txt(_root.transform, $"Seen {seen} / {total}  ·  {pct}% complete", 22,
                new Color(0.8f, 0.85f, 0.92f), new Vector2(0, 432), new Vector2(1000, 30), TextAnchor.MiddleCenter);

            // ── Scroll view ────────────────────────────────────────────────────
            const float rowH = 66f, rowGap = 6f, viewW = 1400f, viewH = 760f;
            GameObject scrollGO = new("Scroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(RectMask2D));
            scrollGO.transform.SetParent(_root.transform, false);
            Place((RectTransform)scrollGO.transform, new Vector2(0, -30), new Vector2(viewW, viewH));
            scrollGO.GetComponent<Image>().color = new Color(0.08f, 0.10f, 0.14f, 1f);
            ScrollRect sr = scrollGO.GetComponent<ScrollRect>();
            sr.horizontal = false; sr.vertical = true; sr.scrollSensitivity = 28f;
            sr.movementType = ScrollRect.MovementType.Clamped;

            RectTransform viewport = (RectTransform)scrollGO.transform; // self-masked viewport
            sr.viewport = viewport;

            GameObject contentGO = new("Content", typeof(RectTransform));
            contentGO.transform.SetParent(scrollGO.transform, false);
            RectTransform content = (RectTransform)contentGO.transform;
            content.anchorMin = new Vector2(0f, 1f); content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            float totalH = species.Count * (rowH + rowGap) + rowGap;
            content.sizeDelta = new Vector2(0f, totalH);
            content.anchoredPosition = Vector2.zero;
            sr.content = content;

            float y = -rowGap;
            for (int i = 0; i < species.Count; i++)
            {
                BuildRow(content, species[i], i + 1, dex, meta, y, rowH);
                y -= rowH + rowGap;
            }

            Btn(_root.transform, new Vector2(0, -468), new Vector2(300, 56), "CLOSE  ✕",
                new Color(0.42f, 0.34f, 0.36f), Close);
        }

        private void BuildRow(Transform parent, PokemonSpeciesSO sp, int dexNo, BestiaryProgressSO dex,
                              MetaProgressionSO meta, float y, float rowH)
        {
            string id = sp != null ? sp.SpeciesId : null;
            bool seen = dex != null && dex.IsSeen(id);

            Image row = Img(parent, seen ? new Color(0.13f, 0.17f, 0.22f, 1f) : new Color(0.10f, 0.11f, 0.14f, 1f));
            RectTransform rt = row.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f); rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, y);
            rt.offsetMin = new Vector2(12f, rt.offsetMin.y); rt.offsetMax = new Vector2(-12f, rt.offsetMax.y);
            rt.sizeDelta = new Vector2(-24f, rowH);

            string num = $"#{dexNo:000}";
            if (!seen)
            {
                Txt(row.transform, $"{num}  ??????", 24, new Color(0.45f, 0.48f, 0.55f),
                    new Vector2(-400, 0), new Vector2(540, rowH), TextAnchor.MiddleLeft);
                Txt(row.transform, "not yet seen", 18, new Color(0.4f, 0.43f, 0.5f),
                    new Vector2(430, 0), new Vector2(300, rowH), TextAnchor.MiddleRight);
                return;
            }

            string name = sp.DisplayName ?? sp.name;
            // §4.3.9.1 (Veteran) — Shiny variant unlocked → ✨ next to the name.
            bool shiny = meta != null && meta.IsShinyUnlocked(id);
            Txt(row.transform, shiny ? $"{num}  {name} ✨" : $"{num}  {name}", 24,
                shiny ? new Color(1f, 0.95f, 0.6f) : new Color(0.93f, 0.96f, 1f),
                new Vector2(-400, 14), new Vector2(540, 34), TextAnchor.MiddleLeft);

            // Counts.
            BestiaryEntry e = dex.GetOrCreate(id);
            Txt(row.transform, $"seen {e.TimesEncountered}  ·  def {e.TimesDefeated}  ·  rec {e.TimesRecruited}", 17,
                new Color(0.7f, 0.78f, 0.85f), new Vector2(-400, -16), new Vector2(560, 24), TextAnchor.MiddleLeft);

            // Tier + progress to next tier.
            BestiaryMasteryTier tier = dex.TierFor(id);
            int defeats = e.TimesDefeated;
            string tierLabel; float frac;
            if (tier >= BestiaryMasteryTier.Master) { tierLabel = "MASTER ✓"; frac = 1f; }
            else
            {
                BestiaryMasteryTier next = tier + 1;
                int need = dex.DefeatsForTier(next, sp.WildRarity);
                frac = need > 0 ? Mathf.Clamp01((float)defeats / need) : 0f;
                tierLabel = $"{TierShort(tier)} → {TierShort(next)}  {defeats}/{need}";
            }
            Txt(row.transform, tierLabel, 17, new Color(0.85f, 0.85f, 0.6f),
                new Vector2(250, 14), new Vector2(400, 24), TextAnchor.MiddleCenter);
            Image track = Img(row.transform, new Color(0.06f, 0.08f, 0.11f, 1f));
            Place(track.rectTransform, new Vector2(250, -16), new Vector2(380, 14));
            Image fill = Img(track.transform, new Color(0.5f, 0.85f, 1f, 1f));
            RectTransform frt = fill.rectTransform;
            frt.anchorMin = new Vector2(0f, 0f); frt.anchorMax = new Vector2(0f, 1f); frt.pivot = new Vector2(0f, 0.5f);
            frt.anchoredPosition = Vector2.zero;
            frt.sizeDelta = new Vector2(380f * frac, 0f);

            // Mastery-unlock status.
            bool hasMastery = sp.MasteryMove != null && !string.IsNullOrEmpty(sp.MasteryMove.MoveId);
            bool unlocked = hasMastery && meta != null && meta.IsMasteryUnlocked(sp.MasteryMove.MoveId);
            string mLabel = !hasMastery ? "—" : unlocked ? "✅ Mastery" : "🔒 Mastery";
            Txt(row.transform, mLabel, 18, unlocked ? new Color(0.6f, 0.95f, 0.6f) : new Color(0.75f, 0.78f, 0.85f),
                new Vector2(610, 0), new Vector2(160, rowH), TextAnchor.MiddleRight);
        }

        private static string TierShort(BestiaryMasteryTier t) => t switch
        {
            BestiaryMasteryTier.None => "—",
            BestiaryMasteryTier.Familiar => "Fam",
            BestiaryMasteryTier.Veteran => "Vet",
            BestiaryMasteryTier.Master => "Mas",
            _ => "—",
        };

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

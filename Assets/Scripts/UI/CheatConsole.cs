using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ProjectAscendant.UI
{
    // DEV-ONLY cheat console for fast, efficient playtesting. Polls the function keys via the new
    // Input System (legacy Input is forbidden, see engine-reference/unity/VERSION.md) and pokes the
    // live combat/run state through the public Cheat* hooks on CombatScreenUI / MapViewUI. F1 toggles
    // an on-screen key list. Created only in UNITY_EDITOR || DEVELOPMENT_BUILD (see MapViewUI.Start),
    // so it never ships in a release build.
    //
    // Keybinds (F1 fixed; the rest are easy to remap here):
    //   F1  Toggle this list                         F5  Refill AP                 [combat]
    //   F2  Win combat now (KO all enemies) [combat] F6  +1000 ₽
    //   F3  Capture wild now (ignores gate) [combat] F7  Skip ahead to the Gym     [map]
    //   F4  Heal team / Box to full                  F8  Grant loot (relic+balls+potions)
    public sealed class CheatConsole : MonoBehaviour
    {
        private const string HelpText =
            "CHEAT CODES        F1 to close\n" +
            "\n" +
            "F1   Toggle this list\n" +
            "F2   Win combat now  (KO all enemies)        [combat]\n" +
            "F3   Capture wild now  (ignores HP gate)     [combat]\n" +
            "F4   Heal team / Box to full\n" +
            "F5   Refill AP                               [combat]\n" +
            "F6   +1000 ₽\n" +
            "F7   Skip ahead to the Gym                   [map]\n" +
            "F8   Grant loot  (relic + 3 balls + 3 potions)";

        private MapViewUI _map;
        private CombatScreenUI _combat;
        private GameObject _overlay;
        private Font _font;

        // True iff a keyboard device is present (i.e. Active Input Handling includes the new system,
        // so the F-key polling below will actually fire). Surfaced for the dev verification probe.
        public static bool KeyboardAvailable => Keyboard.current != null;

        public void Init(MapViewUI map, CombatScreenUI combat)
        {
            _map = map;
            _combat = combat;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            if (kb.f1Key.wasPressedThisFrame) ToggleOverlay();

            if (kb.f2Key.wasPressedThisFrame && InCombat()) _combat.CheatWinCombat();
            if (kb.f3Key.wasPressedThisFrame && InCombat()) _combat.CheatCaptureEnemy();
            if (kb.f4Key.wasPressedThisFrame)
            {
                if (InCombat()) _combat.CheatHealTeam();
                else _map?.CheatHealBox();
            }
            if (kb.f5Key.wasPressedThisFrame && InCombat()) _combat.CheatRefillAP();
            if (kb.f6Key.wasPressedThisFrame) _map?.CheatGiveMoney(1000);
            if (kb.f7Key.wasPressedThisFrame && !InCombat()) _map?.CheatSkipToGym();
            if (kb.f8Key.wasPressedThisFrame) _map?.CheatGrantLoot();
        }

        private bool InCombat() => _combat != null && _combat.IsActive;

        private void ToggleOverlay()
        {
            if (_overlay != null) { Destroy(_overlay); _overlay = null; return; }

            _overlay = new GameObject("CheatOverlay");
            Canvas canvas = _overlay.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // above everything (combat=20, panels=25)
            CanvasScaler scaler = _overlay.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            // No GraphicRaycaster: the overlay is display-only and must not intercept game clicks.

            GameObject panelGO = new("Panel", typeof(RectTransform));
            panelGO.transform.SetParent(_overlay.transform, false);
            Image panel = panelGO.AddComponent<Image>();
            panel.color = new Color(0.05f, 0.06f, 0.09f, 0.93f);
            panel.raycastTarget = false;
            RectTransform prt = panel.rectTransform;
            prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(1040, 560);
            prt.anchoredPosition = Vector2.zero;

            GameObject txtGO = new("Text", typeof(RectTransform));
            txtGO.transform.SetParent(panel.transform, false);
            Text t = txtGO.AddComponent<Text>();
            t.font = _font; t.fontSize = 30; t.color = new Color(0.9f, 0.95f, 0.85f);
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.text = HelpText;
            t.raycastTarget = false;
            RectTransform trt = t.rectTransform;
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(40, 30); trt.offsetMax = new Vector2(-40, -30);
        }
    }
}

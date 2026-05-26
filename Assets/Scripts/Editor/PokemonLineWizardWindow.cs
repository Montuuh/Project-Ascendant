#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using ProjectAscendant.Core;

namespace ProjectAscendant.Editor
{
    // Per Task 3.4 DX — Pokémon Line Creation Wizard.
    // Creates all SOs for a new Pokémon line, pre-wired, in one click.
    // Open via: Project Ascendant ▶ Pokémon Line Wizard
    //
    // What gets created (example for "Magnemite", 2 Stage-2 branches):
    //   GrowthCurves/Magnemite_Line.asset
    //   Abilities/Magnemite/magnet_pull.asset
    //   Moves/Magnemite/thunder_shock.asset  × 4 (base learnset)
    //   Moves/Magnemite/mastery_zap.asset    (mastery — §4.3.9.2)
    //   Species/Wild/Magnemite/Magnemite.asset
    //   Species/Wild/Magnemite/Magneton_Vanguard.asset
    //   Species/Wild/Magnemite/Magnezone_VanguardA1.asset
    //   Species/Wild/Magnemite/Magnezone_VanguardA2.asset
    //   Branches/Magnemite/magnemite_vanguard.asset
    //   Branches/Magnemite/magneton_a1.asset
    //   Branches/Magnemite/magneton_a2.asset
    //
    // MoveUpgrades on branch SOs are left EMPTY — fill via Inspector.
    // All assets placed in per-line subfolders matching VS_AssetReorganiser layout.
    // Re-running with the same Line Name replaces existing assets (idempotent).
    public class PokemonLineWizardWindow : EditorWindow
    {
        // ══════════════════════════════════════════════════════════════════════
        // CONSTANTS
        // ══════════════════════════════════════════════════════════════════════

        private const string ROOT      = "Assets/ScriptableObjects/VS";
        private const int    LBL_WIDTH = 136;

        // ══════════════════════════════════════════════════════════════════════
        // NESTED DATA TYPE — move stub (serialised with the window)
        // ══════════════════════════════════════════════════════════════════════

        [Serializable]
        private class MoveStubData
        {
            public string      DisplayName = "";
            public PokemonType Type        = PokemonType.Normal;
            public MoveRole    Role        = MoveRole.Offensive;
            public MoveRange   Range       = MoveRange.Melee;
            public int         AP          = 1;
            public int         BP          = 50;
        }

        // ══════════════════════════════════════════════════════════════════════
        // SERIALISED WINDOW STATE  (survives domain reload via EditorWindow)
        // ══════════════════════════════════════════════════════════════════════

        // ─ Line info ──────────────────────────────────────────────────────────
        [SerializeField] private string _lineName = "";
        [SerializeField] private bool   _isWild   = false;
        [SerializeField] private string _gddRef   = "§5.3";

        // ─ Base form ──────────────────────────────────────────────────────────
        [SerializeField] private string      _baseDisplayName = "";
        [SerializeField] private PokemonType _baseType1       = PokemonType.Normal;
        [SerializeField] private bool        _baseHasType2    = false;
        [SerializeField] private PokemonType _baseType2       = PokemonType.Normal;
        [SerializeField] private int _baseHP = 45, _baseAtk = 45, _baseDef = 45, _baseSpd = 45;

        // ─ Stage 1 ────────────────────────────────────────────────────────────
        [SerializeField] private BranchArchetype _s1Archetype  = BranchArchetype.Vanguard;
        [SerializeField] private string          _s1Label      = "Vanguard";
        [SerializeField] private string          _s1Name       = "";
        [SerializeField] private bool            _s1Inherit    = true;
        [SerializeField] private PokemonType     _s1Type1      = PokemonType.Normal;
        [SerializeField] private bool            _s1HasType2   = false;
        [SerializeField] private PokemonType     _s1Type2      = PokemonType.Normal;
        [SerializeField] private int _s1HP = 60, _s1Atk = 65, _s1Def = 60, _s1Spd = 70;

        // ─ Stage 2 ────────────────────────────────────────────────────────────
        [SerializeField] private int _s2Count = 2;  // 0 = no stage 2, 1 = linear, 2 = forked

        [SerializeField] private string          _s2aName    = "";
        [SerializeField] private bool            _s2aInherit = true;
        [SerializeField] private PokemonType     _s2aType1   = PokemonType.Normal;
        [SerializeField] private bool            _s2aHasT2   = false;
        [SerializeField] private PokemonType     _s2aType2   = PokemonType.Normal;
        [SerializeField] private int _s2aHP = 78, _s2aAtk = 84, _s2aDef = 78, _s2aSpd = 90;
        [SerializeField] private string          _s2aLabel   = "A1";

        [SerializeField] private string          _s2bName    = "";
        [SerializeField] private bool            _s2bInherit = true;
        [SerializeField] private PokemonType     _s2bType1   = PokemonType.Normal;
        [SerializeField] private bool            _s2bHasT2   = false;
        [SerializeField] private PokemonType     _s2bType2   = PokemonType.Normal;
        [SerializeField] private int _s2bHP = 78, _s2bAtk = 84, _s2bDef = 78, _s2bSpd = 90;
        [SerializeField] private string          _s2bLabel   = "A2";

        // ─ Moves ──────────────────────────────────────────────────────────────
        [SerializeField] private MoveStubData[] _baseMoves   = new MoveStubData[4];
        [SerializeField] private MoveStubData   _masteryMove;

        // ─ Ability ────────────────────────────────────────────────────────────
        [SerializeField] private string          _abilityName     = "";
        [SerializeField] private AbilityCategory _abilityCategory = AbilityCategory.Combat;
        [SerializeField] private string          _abilityDesc     = "";

        // ─ UI state (not serialised — transient) ──────────────────────────────
        private Vector2     _scroll;
        private bool        _foldLine    = true;
        private bool        _foldBase    = true;
        private bool        _foldS1      = true;
        private bool        _foldS2      = true;
        private bool        _foldMoves   = true;
        private bool        _foldMastery = true;
        private bool        _foldAbility = true;
        private string      _statusMsg   = "";
        private MessageType _statusType  = MessageType.Info;

        // ══════════════════════════════════════════════════════════════════════
        // LIFECYCLE
        // ══════════════════════════════════════════════════════════════════════

        [MenuItem("Project Ascendant/Pokémon Line Wizard")]
        public static void Open()
        {
            var w = GetWindow<PokemonLineWizardWindow>(false, "Pokémon Line Wizard");
            w.minSize = new Vector2(420, 600);
            w.EnsureStubs();
        }

        private void OnEnable() => EnsureStubs();

        // Ensure move stub objects are initialised after domain reload or first open.
        private void EnsureStubs()
        {
            if (_baseMoves == null || _baseMoves.Length != 4)
                _baseMoves = new MoveStubData[4];
            for (int i = 0; i < 4; i++)
                _baseMoves[i] ??= new MoveStubData();
            _masteryMove ??= new MoveStubData { DisplayName = "Mastery Move", AP = 2, BP = 60 };
        }

        // ══════════════════════════════════════════════════════════════════════
        // GUI
        // ══════════════════════════════════════════════════════════════════════

        private void OnGUI()
        {
            EditorGUIUtility.labelWidth = LBL_WIDTH;
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            // ── Header ────────────────────────────────────────────────────────
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("✦  Pokémon Line Wizard",
                new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 });
            EditorGUILayout.LabelField(
                "Creates all SOs pre-wired. Fill MoveUpgrades in the Branch Inspector after.",
                EditorStyles.miniLabel);
            EditorGUILayout.Space(4);
            SOEditorUtils.DrawSeparator();
            EditorGUILayout.Space(4);

            // ── Sections ──────────────────────────────────────────────────────
            DrawLineInfoSection();
            DrawBaseFormSection();
            DrawStage1Section();
            DrawStage2Section();
            DrawMovesSection();
            DrawMasterySection();
            DrawAbilitySection();

            // ── Create button ─────────────────────────────────────────────────
            EditorGUILayout.Space(8);
            SOEditorUtils.DrawSeparator();
            EditorGUILayout.Space(4);

            bool ready = CanCreate();
            using (new EditorGUI.DisabledScope(!ready))
                if (GUILayout.Button("✦  CREATE LINE", GUILayout.Height(36)))
                    TryCreateLine();

            if (!ready)
                EditorGUILayout.HelpBox(
                    "Required fields (* marked) must be filled before the line can be created.",
                    MessageType.None);

            if (!string.IsNullOrEmpty(_statusMsg))
            {
                EditorGUILayout.Space(2);
                EditorGUILayout.HelpBox(_statusMsg, _statusType);
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.EndScrollView();
        }

        // ── Section draw methods ───────────────────────────────────────────────

        private void DrawLineInfoSection()
        {
            _foldLine = EditorGUILayout.Foldout(_foldLine, "Line Info", true, EditorStyles.foldoutHeader);
            if (!_foldLine) return;
            EditorGUI.indentLevel++;
            _lineName = EditorGUILayout.TextField("Line Name *", _lineName);
            EditorGUILayout.LabelField(
                "Folder name — use PascalCase (e.g. Magnemite).",
                EditorStyles.miniLabel);
            _isWild = EditorGUILayout.Toggle("Is Wild Line?", _isWild);
            _gddRef = EditorGUILayout.TextField("GDD Reference", _gddRef);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(4);
        }

        private void DrawBaseFormSection()
        {
            _foldBase = EditorGUILayout.Foldout(_foldBase, "Base Form", true, EditorStyles.foldoutHeader);
            if (!_foldBase) return;
            EditorGUI.indentLevel++;
            _baseDisplayName = EditorGUILayout.TextField("Display Name *", _baseDisplayName);
            DrawTypeRow(ref _baseType1, ref _baseHasType2, ref _baseType2);
            DrawStatRow(ref _baseHP, ref _baseAtk, ref _baseDef, ref _baseSpd);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(4);
        }

        private void DrawStage1Section()
        {
            _foldS1 = EditorGUILayout.Foldout(_foldS1, "Stage 1 — Branch & Species", true, EditorStyles.foldoutHeader);
            if (!_foldS1) return;
            EditorGUI.indentLevel++;
            _s1Archetype = (BranchArchetype)EditorGUILayout.EnumPopup("Archetype", _s1Archetype);
            _s1Label     = EditorGUILayout.TextField("Branch Label", _s1Label);
            EditorGUILayout.LabelField("Shown on the branch selection card (e.g. Vanguard).", EditorStyles.miniLabel);
            _s1Name      = EditorGUILayout.TextField("Species Name *", _s1Name);
            EditorGUILayout.LabelField("Asset file name (e.g. Wartortle_Vanguard).", EditorStyles.miniLabel);
            _s1Inherit   = EditorGUILayout.Toggle("Inherit Base Types", _s1Inherit);
            if (!_s1Inherit)
                DrawTypeRow(ref _s1Type1, ref _s1HasType2, ref _s1Type2);
            DrawStatRow(ref _s1HP, ref _s1Atk, ref _s1Def, ref _s1Spd);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(4);
        }

        private void DrawStage2Section()
        {
            _foldS2 = EditorGUILayout.Foldout(_foldS2, "Stage 2 — Sub-branches", true, EditorStyles.foldoutHeader);
            if (!_foldS2) return;
            EditorGUI.indentLevel++;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Sub-branch count");
            if (GUILayout.Toggle(_s2Count == 0, "None (2-stage)", EditorStyles.miniButtonLeft))  _s2Count = 0;
            if (GUILayout.Toggle(_s2Count == 1, "1 (linear)",     EditorStyles.miniButtonMid))   _s2Count = 1;
            if (GUILayout.Toggle(_s2Count == 2, "2 (forked)",     EditorStyles.miniButtonRight)) _s2Count = 2;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(2);

            if (_s2Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "2-stage line: Base → Stage 1 is the final form.",
                    MessageType.Info);
            }
            else
            {
                DrawSubBranch("Sub-branch A *",
                    ref _s2aLabel, ref _s2aName,
                    ref _s2aInherit, ref _s2aType1, ref _s2aHasT2, ref _s2aType2,
                    ref _s2aHP, ref _s2aAtk, ref _s2aDef, ref _s2aSpd);
            }

            if (_s2Count == 2)
            {
                EditorGUILayout.Space(2);
                DrawSubBranch("Sub-branch B *",
                    ref _s2bLabel, ref _s2bName,
                    ref _s2bInherit, ref _s2bType1, ref _s2bHasT2, ref _s2bType2,
                    ref _s2bHP, ref _s2bAtk, ref _s2bDef, ref _s2bSpd);
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(4);
        }

        private void DrawSubBranch(
            string header,
            ref string branchLabel, ref string displayName,
            ref bool inherit, ref PokemonType type1, ref bool hasType2, ref PokemonType type2,
            ref int hp, ref int atk, ref int def, ref int spd)
        {
            EditorGUILayout.LabelField(header, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            branchLabel = EditorGUILayout.TextField("Branch Label", branchLabel);
            displayName = EditorGUILayout.TextField("Species Name", displayName);
            inherit     = EditorGUILayout.Toggle("Inherit S1 Types", inherit);
            if (!inherit)
                DrawTypeRow(ref type1, ref hasType2, ref type2);
            DrawStatRow(ref hp, ref atk, ref def, ref spd);
            EditorGUI.indentLevel--;
        }

        private void DrawMovesSection()
        {
            _foldMoves = EditorGUILayout.Foldout(_foldMoves, "Base Learnset  (4 moves)", true, EditorStyles.foldoutHeader);
            if (!_foldMoves) return;
            EditorGUI.indentLevel++;
            for (int i = 0; i < 4; i++)
            {
                EditorGUILayout.LabelField($"Move {i + 1}", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                DrawMoveStub(_baseMoves[i]);
                EditorGUI.indentLevel--;
                if (i < 3) EditorGUILayout.Space(2);
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(4);
        }

        private void DrawMasterySection()
        {
            _foldMastery = EditorGUILayout.Foldout(_foldMastery, "Mastery Move  (§4.3.9.2)", true, EditorStyles.foldoutHeader);
            if (!_foldMastery) return;
            EditorGUI.indentLevel++;
            EditorGUILayout.HelpBox(
                "Immutable slot — cannot be replaced by TM or Tutor. (§4.3.9.2)",
                MessageType.Info);
            DrawMoveStub(_masteryMove);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(4);
        }

        private void DrawAbilitySection()
        {
            _foldAbility = EditorGUILayout.Foldout(_foldAbility, "Ability  (stub)", true, EditorStyles.foldoutHeader);
            if (!_foldAbility) return;
            EditorGUI.indentLevel++;
            _abilityName     = EditorGUILayout.TextField("Display Name *", _abilityName);
            _abilityCategory = (AbilityCategory)EditorGUILayout.EnumPopup("Category", _abilityCategory);
            _abilityDesc     = EditorGUILayout.TextField("Description", _abilityDesc);
            EditorGUILayout.HelpBox("Stub only — wire EffectHook in Epic 4.", MessageType.Info);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(4);
        }

        // ── UI primitives ─────────────────────────────────────────────────────

        private static void DrawMoveStub(MoveStubData s)
        {
            if (s == null) return;
            s.DisplayName = EditorGUILayout.TextField("Name",       s.DisplayName);
            s.Type        = (PokemonType)EditorGUILayout.EnumPopup("Type",       s.Type);
            s.Role        = (MoveRole)EditorGUILayout.EnumPopup("Role",          s.Role);
            s.Range       = (MoveRange)EditorGUILayout.EnumPopup("Range",        s.Range);
            s.AP          = EditorGUILayout.IntSlider("AP Cost",    s.AP, 0, 4);
            s.BP          = EditorGUILayout.IntField("Base Power",  s.BP);
        }

        private static void DrawTypeRow(
            ref PokemonType type1,
            ref bool hasType2, ref PokemonType type2)
        {
            type1    = (PokemonType)EditorGUILayout.EnumPopup("Primary Type",    type1);
            hasType2 = EditorGUILayout.Toggle("Secondary Type?",                 hasType2);
            if (hasType2)
                type2 = (PokemonType)EditorGUILayout.EnumPopup("Secondary Type", type2);
        }

        private static void DrawStatRow(ref int hp, ref int atk, ref int def, ref int spd)
        {
            hp  = EditorGUILayout.IntField("Base HP",  hp);
            atk = EditorGUILayout.IntField("Base Atk", atk);
            def = EditorGUILayout.IntField("Base Def", def);
            spd = EditorGUILayout.IntField("Base Spd", spd);
        }

        // ══════════════════════════════════════════════════════════════════════
        // VALIDATION
        // ══════════════════════════════════════════════════════════════════════

        private bool CanCreate()
        {
            if (string.IsNullOrWhiteSpace(_lineName))      return false;
            if (string.IsNullOrWhiteSpace(_baseDisplayName)) return false;
            if (string.IsNullOrWhiteSpace(_s1Name))         return false;
            if (_s2Count >= 1 && string.IsNullOrWhiteSpace(_s2aName)) return false;
            if (_s2Count >= 2 && string.IsNullOrWhiteSpace(_s2bName)) return false;
            if (string.IsNullOrWhiteSpace(_abilityName))   return false;
            return true;
        }

        private List<string> BuildValidationErrors()
        {
            var e = new List<string>();
            if (string.IsNullOrWhiteSpace(_lineName))        e.Add("• Line Name is required.");
            if (string.IsNullOrWhiteSpace(_baseDisplayName)) e.Add("• Base Form Display Name is required.");
            if (string.IsNullOrWhiteSpace(_s1Name))          e.Add("• Stage 1 Species Name is required.");
            if (_s2Count >= 1 && string.IsNullOrWhiteSpace(_s2aName))
                e.Add("• Stage 2 Sub-branch A Species Name is required.");
            if (_s2Count >= 2 && string.IsNullOrWhiteSpace(_s2bName))
                e.Add("• Stage 2 Sub-branch B Species Name is required.");
            if (string.IsNullOrWhiteSpace(_abilityName))     e.Add("• Ability Display Name is required.");
            return e;
        }

        // ══════════════════════════════════════════════════════════════════════
        // CREATION ENTRY POINT
        // ══════════════════════════════════════════════════════════════════════

        private void TryCreateLine()
        {
            _statusMsg = "";
            EnsureStubs();

            List<string> errors = BuildValidationErrors();
            if (errors.Count > 0)
            {
                _statusMsg  = "Cannot create:\n" + string.Join("\n", errors);
                _statusType = MessageType.Error;
                return;
            }

            AssetDatabase.StartAssetEditing();
            try
            {
                CreateLineAssets(out int count);
                _statusMsg  = $"✓ Created {count} assets for '{_lineName.Trim()}' line. " +
                              "Fill in MoveUpgrades via the Branch Inspector.";
                _statusType = MessageType.Info;
            }
            catch (Exception ex)
            {
                _statusMsg  = $"Error: {ex.Message}";
                _statusType = MessageType.Error;
                Debug.LogException(ex);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // ASSET CREATION  (two-pass: species first, wire branches second)
        // ══════════════════════════════════════════════════════════════════════

        private void CreateLineAssets(out int assetCount)
        {
            assetCount = 0;
            string ln   = _lineName.Trim();
            string kind = _isWild ? "Wild" : "Starters";

            // ── 1. Ensure folders ─────────────────────────────────────────────
            string fSpecies   = $"{ROOT}/Species/{kind}/{ln}";
            string fBranches  = $"{ROOT}/Branches/{ln}";
            string fAbilities = $"{ROOT}/Abilities/{ln}";
            string fMoves     = $"{ROOT}/Moves/{ln}";
            string fCurves    = $"{ROOT}/GrowthCurves";

            EnsureFolder(fSpecies);
            EnsureFolder(fBranches);
            EnsureFolder(fAbilities);
            EnsureFolder(fMoves);
            EnsureFolder(fCurves);

            // ── 2. Growth Curve ───────────────────────────────────────────────
            // Per §5.2.3 — growth arrays left null; user fills per-level values in Inspector.
            var curve = WizardCreateSO<StatGrowthCurveSO>($"{fCurves}/{ln}_Line.asset");
            EditorUtility.SetDirty(curve);
            assetCount++;

            // ── 3. Ability stub ───────────────────────────────────────────────
            string abilityId = ToId(_abilityName);
            var ability = WizardCreateSO<AbilitySO>($"{fAbilities}/{abilityId}.asset");
            ability.AbilityId    = abilityId;
            ability.DisplayName  = _abilityName.Trim();
            ability.Category     = _abilityCategory;
            ability.Description  = _abilityDesc.Trim();
            ability.GDDReference = _gddRef;
            EditorUtility.SetDirty(ability);
            assetCount++;

            // ── 4. Move stubs (4 base + 1 mastery) ───────────────────────────
            var learnset = new MoveSO[4];
            for (int i = 0; i < 4; i++)
            {
                learnset[i] = BuildMoveSO(fMoves, _baseMoves[i], isMastery: false);
                assetCount++;
            }
            MoveSO masteryMoveSO = BuildMoveSO(fMoves, _masteryMove, isMastery: true);
            assetCount++;

            // ── 5. Resolve type arrays ────────────────────────────────────────
            PokemonType[] baseTypes = BuildTypes(_baseType1, _baseHasType2, _baseType2);
            PokemonType[] s1Types   = _s1Inherit
                ? baseTypes
                : BuildTypes(_s1Type1, _s1HasType2, _s1Type2);
            PokemonType[] s2aTypes  = _s2aInherit
                ? s1Types
                : BuildTypes(_s2aType1, _s2aHasT2, _s2aType2);
            PokemonType[] s2bTypes  = _s2bInherit
                ? s1Types
                : BuildTypes(_s2bType1, _s2bHasT2, _s2bType2);

            // ── 6. Species — Pass 1 (Branches list empty) ────────────────────
            // Base form: PrimaryAbility = null (granted at first evolution, §5.5.1)
            var baseSpecies = BuildSpeciesSO(
                fSpecies, _baseDisplayName.Trim(), baseTypes,
                new BaseStats { BaseHP=_baseHP, BaseAtk=_baseAtk, BaseDef=_baseDef, BaseSpd=_baseSpd },
                curve, learnset, masteryMoveSO, primaryAbility: null);
            assetCount++;

            var s1Species = BuildSpeciesSO(
                fSpecies, _s1Name.Trim(), s1Types,
                new BaseStats { BaseHP=_s1HP, BaseAtk=_s1Atk, BaseDef=_s1Def, BaseSpd=_s1Spd },
                curve, learnset, masteryMoveSO, primaryAbility: ability);
            assetCount++;

            PokemonSpeciesSO s2aSpecies = null;
            PokemonSpeciesSO s2bSpecies = null;

            if (_s2Count >= 1)
            {
                s2aSpecies = BuildSpeciesSO(
                    fSpecies, _s2aName.Trim(), s2aTypes,
                    new BaseStats { BaseHP=_s2aHP, BaseAtk=_s2aAtk, BaseDef=_s2aDef, BaseSpd=_s2aSpd },
                    curve, learnset, masteryMoveSO, primaryAbility: ability);
                assetCount++;
            }
            if (_s2Count >= 2)
            {
                s2bSpecies = BuildSpeciesSO(
                    fSpecies, _s2bName.Trim(), s2bTypes,
                    new BaseStats { BaseHP=_s2bHP, BaseAtk=_s2bAtk, BaseDef=_s2bDef, BaseSpd=_s2bSpd },
                    curve, learnset, masteryMoveSO, primaryAbility: ability);
                assetCount++;
            }

            // ── 7. Branches ───────────────────────────────────────────────────
            // IDs derived from line/stage names — lowercase snake_case.
            string lnId = ToId(ln);
            string s1Id = ToId(_s1Name.Trim());
            string arch = _s1Archetype.ToString().ToLower();

            var stage1Branch = BuildBranchSO(
                fBranches,
                id:          $"{lnId}_{arch}",
                displayName: _s1Label.Trim(),
                archetype:   _s1Archetype,
                evolved:     s1Species);
            assetCount++;

            EvolutionBranchSO s2aBranch = null;
            EvolutionBranchSO s2bBranch = null;

            if (_s2Count >= 1)
            {
                s2aBranch = BuildBranchSO(
                    fBranches,
                    id:          $"{s1Id}_{ToId(_s2aLabel)}",
                    displayName: _s2aLabel.Trim(),
                    archetype:   _s1Archetype,
                    evolved:     s2aSpecies);
                assetCount++;
            }
            if (_s2Count >= 2)
            {
                s2bBranch = BuildBranchSO(
                    fBranches,
                    id:          $"{s1Id}_{ToId(_s2bLabel)}",
                    displayName: _s2bLabel.Trim(),
                    archetype:   _s1Archetype,
                    evolved:     s2bSpecies);
                assetCount++;
            }

            // ── 8. Wire branches onto species — Pass 2 ────────────────────────
            baseSpecies.Branches = new List<EvolutionBranchSO> { stage1Branch };
            EditorUtility.SetDirty(baseSpecies);

            s1Species.Branches = _s2Count switch
            {
                0 => new List<EvolutionBranchSO>(),
                1 => new List<EvolutionBranchSO> { s2aBranch },
                _ => new List<EvolutionBranchSO> { s2aBranch, s2bBranch }
            };
            EditorUtility.SetDirty(s1Species);

            // ── 9. Focus Project window on the base species ────────────────────
            EditorGUIUtility.PingObject(baseSpecies);
            Selection.activeObject = baseSpecies;
        }

        // ══════════════════════════════════════════════════════════════════════
        // ASSET FACTORY HELPERS
        // ══════════════════════════════════════════════════════════════════════

        private MoveSO BuildMoveSO(string folder, MoveStubData s, bool isMastery)
        {
            string name = string.IsNullOrWhiteSpace(s.DisplayName)
                ? (isMastery ? "Mastery Move" : "Stub Move")
                : s.DisplayName.Trim();
            string id   = isMastery ? $"mastery_{ToId(name)}" : ToId(name);

            MoveSO m = WizardCreateSO<MoveSO>($"{folder}/{id}.asset");
            m.MoveId      = id;
            m.DisplayName = name;
            m.Type        = s.Type;
            m.Role        = s.Role;
            m.Range       = s.Range;
            m.BasePower   = s.BP;
            m.APCost      = Mathf.Clamp(s.AP, 0, 4);
            // Per §9.3.2.2 — 0.75 for Ranged, 1.0 for Melee.
            m.RangeModifierMultiplier = s.Range == MoveRange.Ranged ? 0.75f : 1f;
            m.Effects     = new List<MoveEffectSO>();
            m.GDDReference = _gddRef;
            EditorUtility.SetDirty(m);
            return m;
        }

        private PokemonSpeciesSO BuildSpeciesSO(
            string folder, string assetName,
            PokemonType[] types, BaseStats stats,
            StatGrowthCurveSO curve, MoveSO[] learnset, MoveSO mastery,
            AbilitySO primaryAbility)
        {
            PokemonSpeciesSO s = WizardCreateSO<PokemonSpeciesSO>($"{folder}/{assetName}.asset");
            s.SpeciesId        = ToId(assetName);
            s.DisplayName      = assetName;
            s.Types            = new List<PokemonType>(types);
            s.BaseStats        = stats;
            s.GrowthCurve      = curve;
            s.Branches         = new List<EvolutionBranchSO>();     // wired in Pass 2
            s.BaseLearnset     = new List<MoveSO>(learnset);
            s.TutorLearnset    = new List<MoveSO>();
            s.TMCompatibility  = new List<TMSO>();
            s.MasteryMove      = mastery;
            s.PrimaryAbility   = primaryAbility;
            s.StatusImmunities = new List<StatusCondition>();
            s.SpawnBiomes      = new List<Biome>();
            s.WildRarity       = RarityTier.Common;
            s.GDDReference     = _gddRef;
            EditorUtility.SetDirty(s);
            return s;
        }

        private EvolutionBranchSO BuildBranchSO(
            string folder, string id, string displayName,
            BranchArchetype archetype, PokemonSpeciesSO evolved)
        {
            EvolutionBranchSO b = WizardCreateSO<EvolutionBranchSO>($"{folder}/{id}.asset");
            b.BranchId       = id;
            b.DisplayName    = displayName;
            b.Archetype      = archetype;
            b.EvolvedSpecies = evolved;
            b.MoveUpgrades   = new List<MoveUpgradePair>();  // user fills via Inspector
            b.NewMoves       = new List<MoveSO>();
            b.GrantedAbility = null;                          // user wires in Inspector
            b.SubBranches    = new List<EvolutionBranchSO>();
            b.GDDReference   = _gddRef;
            EditorUtility.SetDirty(b);
            return b;
        }

        // ══════════════════════════════════════════════════════════════════════
        // STATIC UTILITIES
        // ══════════════════════════════════════════════════════════════════════

        // Creates or replaces the SO at 'path'. Idempotent re-seed.
        private static T WizardCreateSO<T>(string path) where T : ScriptableObject
        {
            AssetDatabase.DeleteAsset(path);
            T a = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(a, path);
            return a;
        }

        private static PokemonType[] BuildTypes(PokemonType t1, bool hasT2, PokemonType t2)
            => hasT2 ? new[] { t1, t2 } : new[] { t1 };

        // Convert display name to snake_case asset ID.
        private static string ToId(string displayName)
        {
            if (string.IsNullOrEmpty(displayName)) return "unnamed";
            string s = Regex.Replace(displayName.Trim().ToLower(), @"[^a-z0-9]+", "_");
            s = s.Trim('_');
            return s.Length > 0 ? s : "unnamed";
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath)) return;
            string parent = folderPath.Substring(0, folderPath.LastIndexOf('/'));
            string name   = folderPath.Substring(folderPath.LastIndexOf('/') + 1);
            EnsureFolder(parent);   // recurse to ensure parent exists first
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif

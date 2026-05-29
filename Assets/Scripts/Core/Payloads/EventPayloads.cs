namespace ProjectAscendant.Core
{
    // Per §9.4.1.1 + Task 2.2.5 — Event payload value types.
    // All fields readonly — immutability enforced at compile time (§9.4.2).

    // Fired when damage lands on a slot. Per §4.3.2 — intents target positions, not Pokémon.
    public readonly struct DamageContext
    {
        public readonly int SlotIndex;
        public readonly int BaseDamage;
        public readonly int FinalDamage;
        public readonly bool IsCrit;
        public readonly bool IsStab;
        public readonly float TypeMultiplier;

        public DamageContext(int slotIndex, int baseDamage, int finalDamage,
                             bool isCrit, bool isStab, float typeMultiplier)
        {
            SlotIndex       = slotIndex;
            BaseDamage      = baseDamage;
            FinalDamage     = finalDamage;
            IsCrit          = isCrit;
            IsStab          = isStab;
            TypeMultiplier  = typeMultiplier;
        }
    }

    // Fired at TurnStart and TurnEnd.
    public readonly struct TurnContext
    {
        public readonly int TurnNumber;
        public readonly int RemainingAP;

        public TurnContext(int turnNumber, int remainingAP)
        {
            TurnNumber   = turnNumber;
            RemainingAP  = remainingAP;
        }
    }

    // Fired when a Pokémon faints. Per §2.4.1 — CurrentHP == 0 is the fainted state.
    public readonly struct FaintContext
    {
        public readonly int SlotIndex;
        public readonly bool WasLead;

        public FaintContext(int slotIndex, bool wasLead)
        {
            SlotIndex = slotIndex;
            WasLead   = wasLead;
        }
    }

    // Fired when the Lead slot changes. Per §3.3.1 — swap cost 1st=1AP, 2nd=2AP, 3rd=3AP.
    public readonly struct LeadChangeContext
    {
        public readonly int PreviousSlotIndex;
        public readonly int NewSlotIndex;
        public readonly int APCost;

        public LeadChangeContext(int previousSlotIndex, int newSlotIndex, int apCost)
        {
            PreviousSlotIndex = previousSlotIndex;
            NewSlotIndex      = newSlotIndex;
            APCost            = apCost;
        }
    }

    // Fired when a Pokémon evolves.
    public readonly struct EvolutionContext
    {
        public readonly int SlotIndex;
        // TODO: Epic 3 — replace int IDs with PokemonSpeciesSO references once data layer exists.
        public readonly int PreviousSpeciesId;
        public readonly int NewSpeciesId;

        public EvolutionContext(int slotIndex, int previousSpeciesId, int newSpeciesId)
        {
            SlotIndex         = slotIndex;
            PreviousSpeciesId = previousSpeciesId;
            NewSpeciesId      = newSpeciesId;
        }
    }

    // Fired when the player enters a map node (Epic 9 Task 9.2). Published by NodeController.Enter
    // AFTER the save-on-entry write (§9.8.1), so subscribers see committed run state.
    public readonly struct NodeEnteredContext
    {
        public readonly NodeType NodeType;
        public readonly int Layer;
        public readonly int Lane;

        public NodeEnteredContext(NodeType nodeType, int layer, int lane)
        {
            NodeType = nodeType;
            Layer    = layer;
            Lane     = lane;
        }
    }

    // Fired when a map node finishes resolving (Epic 9 Task 9.2). Published by NodeController.Complete.
    // The driving NodeState reads Outcome to choose the HSM transition (see NodeOutcome).
    public readonly struct NodeCompletedContext
    {
        public readonly NodeType NodeType;
        public readonly int Layer;
        public readonly int Lane;
        public readonly NodeOutcome Outcome;

        public NodeCompletedContext(NodeType nodeType, int layer, int lane, NodeOutcome outcome)
        {
            NodeType = nodeType;
            Layer    = layer;
            Lane     = lane;
            Outcome  = outcome;
        }
    }

    // Placeholder — §9.4.1.1 specifies GameEventSO<RelicSO>.
    // TODO: Epic 3 — replace with RelicSO reference once RelicSO is defined.
    public readonly struct RelicAcquiredContext
    {
        public readonly int RelicId;
        public RelicAcquiredContext(int relicId) { RelicId = relicId; }
    }

    // Fired when a run ends (victory or defeat).
    public readonly struct RunEndedContext
    {
        public readonly bool Victory;
        public readonly int TurnCount;
        public readonly int NodesCleared;

        public RunEndedContext(bool victory, int turnCount, int nodesCleared)
        {
            Victory      = victory;
            TurnCount    = turnCount;
            NodesCleared = nodesCleared;
        }
    }
}

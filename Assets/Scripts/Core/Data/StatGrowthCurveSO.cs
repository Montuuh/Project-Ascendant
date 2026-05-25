using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §5.2.3 + §9.3.2.1 — per-level flat stat growth for a Pokémon species.
    // Growth curves are custom-tuned for the roguelike context (not Gen I stat tables).
    // Index 0 = growth applied on levelling from 1→2; index N = growth on (N+1)→(N+2).
    [CreateAssetMenu(fileName = "New Growth Curve", menuName = "Project Ascendant/Data/Stat Growth Curve")]
    public class StatGrowthCurveSO : ScriptableObject
    {
        // Per §4.1.1 — HP, Attack, Defense, Speed only (no SpAtk/SpDef split).
        public int[] HPGrowthPerLevel;
        public int[] AttackGrowthPerLevel;
        public int[] DefenseGrowthPerLevel;
        public int[] SpeedGrowthPerLevel;

        // Returns total accumulated HP growth from level 1 to targetLevel (exclusive of base HP).
        public int GetHPAt(int targetLevel)
        {
            if (HPGrowthPerLevel == null || targetLevel <= 1) return 0;
            int total = 0;
            int steps = Mathf.Min(targetLevel - 1, HPGrowthPerLevel.Length);
            for (int i = 0; i < steps; i++)
                total += HPGrowthPerLevel[i];
            return total;
        }

        public int GetAttackAt(int targetLevel)
        {
            if (AttackGrowthPerLevel == null || targetLevel <= 1) return 0;
            int total = 0;
            int steps = Mathf.Min(targetLevel - 1, AttackGrowthPerLevel.Length);
            for (int i = 0; i < steps; i++)
                total += AttackGrowthPerLevel[i];
            return total;
        }

        public int GetDefenseAt(int targetLevel)
        {
            if (DefenseGrowthPerLevel == null || targetLevel <= 1) return 0;
            int total = 0;
            int steps = Mathf.Min(targetLevel - 1, DefenseGrowthPerLevel.Length);
            for (int i = 0; i < steps; i++)
                total += DefenseGrowthPerLevel[i];
            return total;
        }
    }
}

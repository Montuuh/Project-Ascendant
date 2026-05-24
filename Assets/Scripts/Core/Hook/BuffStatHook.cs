using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §8.7 — writes stat and stage delta to EventContext; combat system applies to Target.
    // Example use: Defense Curl Charm (+1 Def on swap), Adrenal Surge (+1 Atk on faint).
    [CreateAssetMenu(menuName = "ProjectAscendant/Hooks/BuffStat")]
    public sealed class BuffStatHook : ScriptableHook
    {
        [SerializeField] private Stat _stat;
        [SerializeField] private int  _stages = 1;

        public Stat Stat   { get => _stat;   set => _stat   = value; }
        public int  Stages { get => _stages; set => _stages = value; }

        public override void OnFire(EventContext context)
        {
            context.StatTarget      = _stat;
            context.StatStageChange = _stages;
        }
    }
}

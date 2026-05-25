using System.Collections.Generic;
using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §8.7 + §5.4.1 + §8.5 — Definition SO for a Technical Machine.
    // TMs are a Consumable-class item that NEVER enter the in-combat Consumable Pile.
    // They are applied from the Map View. Mastery Move slot is exempt (§4.3.9.2).
    [CreateAssetMenu(fileName = "New TM", menuName = "Project Ascendant/Items/TM")]
    public class TMSO : ScriptableObject
    {
        [Header("Identity")]
        public string Id;
        public string DisplayName;
        public Sprite Icon;

        // Per §8.5 — the move this TM teaches.
        public MoveSO MoveTeach;

        // Per §5.4.1 — species that can learn this TM. UI greys out incompatible options.
        public List<PokemonSpeciesSO> CompatibleSpecies;

        [Header("Shop & Drop")]
        [Tooltip("Price in Poké Dollars at Region/City Shop.")]
        public int ShopPrice = 250;

        [Tooltip("GDD section for this TM. Per §9.15.")]
        public string GDDReference;
    }
}

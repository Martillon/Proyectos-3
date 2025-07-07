using UnityEngine;
using System.Collections.Generic;

namespace Scripts.Core.Progression
{
    [CreateAssetMenu(fileName = "BountyBoard", menuName = "My Game/Progression/Bounty Board")]
    public class BountyBoard : ScriptableObject
    {
        [Tooltip("The complete, ordered list of all bounties in the game.")]
        public List<Bounty> allBounties;
    }
}
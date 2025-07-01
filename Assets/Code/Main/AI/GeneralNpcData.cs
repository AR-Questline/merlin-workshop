using Awaken.Utility.Maths;
using Awaken.Utility.Maths.Data;
using UnityEngine;

namespace Awaken.TG.Main.AI {
    [CreateAssetMenu(fileName = "General", menuName = "NpcData/General")]
    public class GeneralNpcData : ScriptableObject {
        [SerializeField] float workingRange;
        [SerializeField] float workingRangeHysteresis;

        Hysteresis? _workingRangeSq;
        [UnityEngine.Scripting.Preserve]
        public Hysteresis WorkingRangeSq => _workingRangeSq ??= Hysteresis.Centered(workingRange, workingRangeHysteresis).Sq();
    }
}
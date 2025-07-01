using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Character.Features.Config {
    public class BlendShapeConfigSO : ScriptableObject {
        [TableList(IsReadOnly = true, ShowPaging = true, NumberOfItemsPerPage = 18, CellPadding = 5, AlwaysExpanded = true)]
        [Tooltip("A - Active\nL - Locked From Random\nE - Allow Extremes"), InfoBox("                    A - Active  |  L - Locked From Random  |  E - Allow Extremes", InfoMessageType.None)]
        public List<BlendShapeConfig> configs = new List<BlendShapeConfig>();
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using Awaken.Utility.Enums;
using UnityEngine;

namespace Awaken.TG.Main.Cameras.Controllers
{
    public class ControlType : RichEnum {
        [UnityEngine.Scripting.Preserve]
        public static readonly IEnumerable<Type> AllComponentsTypes = new List<Type>() {
            typeof(VCDollyZoom),
        };

        [UnityEngine.Scripting.Preserve]
        public static readonly ControlType
            TPP = new ControlType(nameof(TPP), new List<Type>()),
            DollyZoom = new ControlType(nameof(DollyZoom), new List<Type>() {
                typeof(VCDollyZoom)
            }),
            None = new ControlType(nameof(None), new List<Type>());

        List<Type> _activeComponentsTypes;

        [UnityEngine.Scripting.Preserve] public IEnumerable<Type> ActiveComponentsTypes => _activeComponentsTypes;

        protected ControlType(string enumName, List<Type> activeComponentsTypes) : base(enumName) {
            _activeComponentsTypes = activeComponentsTypes;
        }
    }
}

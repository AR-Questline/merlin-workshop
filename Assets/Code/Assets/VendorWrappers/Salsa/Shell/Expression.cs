using System.Collections.Generic;
using UnityEngine;

namespace CrazyMinnow.SALSA
{
    public class Expression
    {
        public string name;
        public List<ExpressionComponent> components;
        [SerializeField]
        public List<InspectorControllerHelperData> controllerVars;
        public bool inspFoldout;
        public bool previewDisplayMode;
        public bool collectionExpand;
    }
}
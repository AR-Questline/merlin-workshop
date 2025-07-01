using Awaken.VendorWrappers.Salsa;
using UnityEngine;

namespace CrazyMinnow.SALSA
{
    public class EyesExpression
    {
        [SerializeField]
        public Expression expData = new Expression();
        public EyeGizmo gizmo;
        public int referenceIdx;
    }
}
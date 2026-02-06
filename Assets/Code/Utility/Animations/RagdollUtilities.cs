using System;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using UnityEngine;

namespace Awaken.Utility.Animations {
    [ExecuteInEditMode, Obsolete]
    public class RagdollUtilities : MonoBehaviour {
        void Awake() {
            Log.Critical?.Error("RagdollUtilities is obsolete, Remove it! " + gameObject.HierarchyPath(), this);
        }
    }
}

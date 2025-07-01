using Awaken.CommonInterfaces.Assets;
using Awaken.Utility.Assets;
using UnityEngine;

namespace Awaken.TG.Main.Utility {
    public class StaticRootTag : MonoBehaviour, IEditorOnlyTransform {
        bool IEditorOnlyTransform.PreserveChildren => true;
    }
}
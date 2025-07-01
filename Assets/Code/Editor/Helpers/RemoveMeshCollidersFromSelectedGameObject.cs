using System.Linq;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Templates.Specs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations;
using Awaken.TG.MVC;
using Awaken.TG.Utility.Attributes.Tags;
using Awaken.Utility.GameObjects;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Awaken.TG.Editor.Helpers{

public class Helpers : MonoBehaviour{
    [MenuItem("TG/Scene Tools/Remove All Mesh Colliders")]
    static void RemoveMeshCollidersFromSelectedGameObject(){
        GameObject SelectedObject = Selection.activeTransform.gameObject;
        MeshCollider[] MeshCollidery = SelectedObject.GetComponents<MeshCollider>();
        foreach (Component currentComponent in MeshCollidery)
             {  
                currentComponent.hideFlags |= HideFlags.HideInInspector;
                DestroyImmediate(currentComponent);
             }
    }
}
}
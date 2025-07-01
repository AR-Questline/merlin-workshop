using Awaken.TG.Graphics;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Graphics {
    public class TexelDensityDebugMenu : MonoBehaviour {
        [MenuItem ("TG/Debug/Texel Density Debug")]
	
        static void CreateTexelDensityDebug() {
            if(Selection.activeGameObject != null) {
                //Did the user select a TexelDensityDebug?
                if (Selection.activeGameObject.name == "TexelDensityDebug") {
                    AddNewTexelDensityDebug(Selection.activeGameObject);
                }else{ 
                    if(GameObject.Find("TexelDensityDebug") != null) {
                        EditorUtility.DisplayDialog("Texel Density Debug Warning","Oops, You need to select a Texel Density Debug to add an additional copy of the tool.","OK");
                    }else{
                        CreateNewTexelDensityDebug();
                    }
                }
            }else{
                if(GameObject.Find("TexelDensityDebug") != null) {
                    AddNewTexelDensityDebug(GameObject.Find("TexelDensityDebug"));
                }else{
                    CreateNewTexelDensityDebug();	
                }
            }
        }
	
        static void CreateNewTexelDensityDebug() {
            GameObject go = new GameObject("TexelDensityDebug");
            go.transform.position = Vector3.zero;
            go.AddComponent(typeof(TexelDensityDebug));
            Selection.activeGameObject = go;
        }
	
        static void AddNewTexelDensityDebug(GameObject go) {
            go.AddComponent(typeof(TexelDensityDebug));
        }
    }
}
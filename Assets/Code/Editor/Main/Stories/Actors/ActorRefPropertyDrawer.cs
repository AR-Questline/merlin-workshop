using Awaken.TG.Main.Stories.Actors;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Main.Stories.Actors {
    [CustomPropertyDrawer(typeof(ActorRef))]
    public class ActorRefPropertyDrawer : PropertyDrawer {
        ActorRefGenericDrawer _drawer = new();
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            _drawer.Draw(position, property, label);
        }
    }
}
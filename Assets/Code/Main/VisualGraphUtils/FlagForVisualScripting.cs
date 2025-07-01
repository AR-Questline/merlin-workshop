using Awaken.TG.Main.Memories;
using Awaken.TG.MVC;
using Awaken.TG.Utility.Attributes.Tags;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using Unity.VisualScripting;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.VisualGraphUtils {
    [RequireComponent(typeof(Variables))]
    [ExecuteInEditMode]
    public class FlagForVisualScripting : MonoBehaviour {
        [HideLabel][Tags(TagsCategory.Flag)]
        public string flag = "";
        Variables Variables => GetComponent<Variables>();
        
        void OnValidate() {
            this.Variables.declarations.Set("flagName", flag);
        }

        [Button, DisableInEditorMode]
        public void DisplayFlagValue() {
            Log.Important?.Error($"Flag: {flag}, value: {World.Services.Get<GameplayMemory>().Context().Get<bool>(flag)}");
        }
    }
}

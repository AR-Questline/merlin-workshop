using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;

namespace Awaken.TG.Editor.Localizations
{
    public class LocalizationCopyWindow : OdinEditorWindow {
        public string idToCopyFrom;
        public string idToCopyTo;
        
        [MenuItem("TG/Localization/Localization Copy Machine BFG 10K")]
        public static void ShowWindow() {
            var window = CreateWindow<LocalizationCopyWindow>("Localization Copy Machine BFG 10K");
            window.Show();
        }
        
        [Button]
        public void ExecuteCopy()
        {
            LocalizationUtils.CopyLocalizationData(idToCopyFrom, idToCopyTo);
            AssetDatabase.SaveAssets();
        }
    }
}

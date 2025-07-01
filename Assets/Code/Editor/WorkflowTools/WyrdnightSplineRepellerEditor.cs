using System;
using Awaken.TG.Assets;
using Awaken.TG.Editor.Assets;
using Awaken.TG.Editor.Utility;
using Awaken.TG.Graphics.DayNightSystem;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.WorkflowTools {
    [CustomEditor(typeof(WyrdnightSplineRepeller))]
    public class WyrdnightSplineRepellerEditor : OdinEditor {
        ARAssetReferenceSettingsAttribute _settings;

        protected override void OnEnable() {
            base.OnEnable();
            var meshReference = serializedObject.FindProperty(nameof(WyrdnightSplineRepeller.repellerMesh));
            _settings = meshReference.ExtractAttribute<ARAssetReferenceSettingsAttribute>();
        }

        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
            WyrdnightSplineRepeller repeller = (WyrdnightSplineRepeller) target;

            if (repeller.generatedMesh == null) {
                repeller.repellerMesh = null;
                return;
            }
            
            Mesh mesh = repeller.generatedMesh;
            var group = _settings.GroupName;

            Func<Object, AddressableAssetEntry, string> addressProvider = null;
            if (_settings.NameProvider != null) {
                addressProvider = (o, _) => _settings.NameProvider(o);
            }

            var guid = AddressableHelper.AddEntry(
                new AddressableEntryDraft.Builder(mesh)
                    .WithAddressProvider(addressProvider)
                    .InGroup(group)
                    .WithLabels(_settings.Labels)
                    .Build());
            
            if (repeller.repellerMesh == null || repeller.repellerMesh.AssetGUID != guid) {
                repeller.repellerMesh = new(guid);
                EditorUtility.SetDirty(repeller);
            }
        }
    }
}

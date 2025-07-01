using Awaken.TG.Assets;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Saving.DefaultValues;
using Awaken.TG.MVC;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Newtonsoft.Json;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Awaken.TG.Main.Locations {
    public partial class RuntimeLocationInitializer : LocationInitializer {
        public override ushort TypeForSerialization => SavedTypes.RuntimeLocationInitializer;

        [Saved] public LocationTemplate TemplateSaved { get => Template; private set => Template = value; }
        [Saved] public Vector3 SpecInitialPositionSaved { get => SpecInitialPosition; private set => SpecInitialPosition = value; }
        [Saved, DefaultValueQuaternionIdentity] public Quaternion SpecInitialRotationSaved { get => SpecInitialRotation; private set => SpecInitialRotation = value; }
        [Saved, DefaultValueVector3(1, 1, 1)] public Vector3 SpecInitialScaleSaved { get => SpecInitialScale; private set => SpecInitialScale = value; }
        
        [Saved] public string OverridenLocationName { get; private set; }
        
        public override bool ShouldBeSaved => true;
        
        Scene? _desiredScene;

        [JsonConstructor, UnityEngine.Scripting.Preserve] RuntimeLocationInitializer() { }
        
        public RuntimeLocationInitializer(in RuntimeLocationData data) {
            Template = data.Template;
            OverridenLocationPrefab = data.OverridenLocationPrefab;
            OverridenLocationName = data.OverridenLocationName;
            
            SpecInitialPosition = data.InitialPosition;
            SpecInitialRotation = data.InitialRotation;
            SpecInitialScale = data.InitialScale;
            
            _desiredScene = data.DesiredScene;
        }
        
        public override void WriteSavables(JsonWriter jsonWriter, JsonSerializer serializer) {
            base.WriteSavables(jsonWriter, serializer);
            
            JsonUtils.JsonWrite(jsonWriter, serializer, nameof(TemplateSaved), TemplateSaved);

            if (!string.IsNullOrEmpty(OverridenLocationName)) {
                JsonUtils.JsonWrite(jsonWriter, serializer, nameof(OverridenLocationName), OverridenLocationName);
            }

            JsonUtils.JsonWrite(jsonWriter, serializer, nameof(SpecInitialPositionSaved), SpecInitialPositionSaved);
            JsonUtils.JsonWrite(jsonWriter, serializer, nameof(SpecInitialRotationSaved), SpecInitialRotationSaved);
            JsonUtils.JsonWrite(jsonWriter, serializer, nameof(SpecInitialScaleSaved), SpecInitialScaleSaved, new Vector3(1, 1, 1));
        }

        public override void PrepareSpec(Location location) {
            Spec = Template.GetComponent<LocationSpec>();
        }

        public override Transform PrepareViewParent(Location location) {
            var viewParent = new GameObject(Template.name).transform;
            viewParent.SetParent(World.Services.Get<ViewHosting>().LocationsHost(location.CurrentDomain, _desiredScene));
            viewParent.SetPositionAndRotation(SpecInitialPosition, SpecInitialRotation);
            viewParent.localScale = SpecInitialScale;
            CopyVisualScriptingFromTemplate(viewParent);
            return viewParent;
        }
        
        void CopyVisualScriptingFromTemplate(Transform viewParent) {
            CopyVariables(viewParent);
            CopyScriptMachineCollection(viewParent);
        }
        
        void CopyVariables(Transform viewParent) {
            if (Template.TryGetComponent(out Variables variables)) {
                Variables newVariables = viewParent.gameObject.AddComponent<Variables>();
                
                foreach (VariableDeclaration v in variables.declarations) {
                    newVariables.declarations.Set(v.name, v.value);
                }
            }
        }
        
        void CopyScriptMachineCollection(Transform viewParent) {
            ScriptMachine[] machines = Template.GetComponents<ScriptMachine>();
            
            foreach (ScriptMachine m in machines) {
                ScriptMachine newMachine = viewParent.gameObject.AddComponent<ScriptMachine>();
                newMachine.nest.embed = m.nest.embed;
                newMachine.nest.source = m.nest.source;
                newMachine.nest.macro = m.nest.macro;
                newMachine.nest.nester = m.nest.nester;
            }
        }
    }
    
    public struct RuntimeLocationData {
        public LocationTemplate Template { get; }
        public Vector3 InitialPosition { get; }
        public Quaternion InitialRotation { get; }
        public Vector3 InitialScale { get; }
        public Scene? DesiredScene { get; }
        public ARAssetReference OverridenLocationPrefab { get; }
        public string OverridenLocationName { get; }
        
        public RuntimeLocationData(LocationTemplate template, Vector3 position, Quaternion rotation, Vector3 scale,ARAssetReference overridenLocationPrefab, string overridenLocationName, Scene? desiredScene) {
            Template = template;
            InitialPosition = position;
            InitialRotation = rotation;
            InitialScale = scale;
            OverridenLocationPrefab = overridenLocationPrefab;
            OverridenLocationName = overridenLocationName;
            DesiredScene = desiredScene;
        }
    }
}

using System;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Locations.Spawners;
using Awaken.TG.Main.Locations.Views;
using Awaken.TG.VisualScripts.Units;
using Awaken.TG.VisualScripts.Units.Typing;
using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.Main.VisualGraphUtils {
    [UnitCategory("Generated")]
    [UnityEngine.Scripting.Preserve]
    public class SpawnLocationSpec : ARUnit {
        [DoNotSerialize] public ControlInput enter;
        [DoNotSerialize] public ControlOutput exit;
        
        [DoNotSerialize] public RequiredValueInput<TemplateWrapper<LocationTemplate>> objectToSpawn;
        [DoNotSerialize] public ValueInput spawnPosition;
        [DoNotSerialize] public ValueInput spawnRotation;
        [DoNotSerialize, PortLabel("View Parent")] public ValueOutput spawnedLocation;
        [DoNotSerialize] public ValueOutput locationSpec;
        [DoNotSerialize] public ValueOutput location;
        [DoNotSerialize] public ValueOutput locationView;
        
        protected override void Definition() {
            enter = ControlInput("enter", Enter);
            exit = ControlOutput("exit");

            objectToSpawn = RequiredARValueInput<TemplateWrapper<LocationTemplate>>("LocationTemplate");
            spawnPosition = ValueInput<Vector3>("position");
            spawnRotation = ValueInput<Quaternion>("rotation");
            spawnedLocation = ValueOutput<GameObject>("spawnedLocation");
            locationSpec = ValueOutput<LocationSpec>("locationSpec");
            location = ValueOutput<Location>("location");
            locationView = ValueOutput<VLocation>("locationView");
            
            Succession(enter, exit);
        }
        
        ControlOutput Enter(Flow flow) {
            var templateReference = objectToSpawn.Value(flow);
            Vector3 position = flow.GetValue<Vector3>(spawnPosition);
            Quaternion rotation = flow.GetValue<Quaternion>(spawnRotation);

            var location = SpawnLocationObject(templateReference.Template, position, rotation);
            flow.SetValue(spawnedLocation, location.ViewParent.gameObject);
            flow.SetValue(locationSpec, location.Spec);
            flow.SetValue(this.location, location);
            flow.SetValue(locationView, location.LocationView);
            return exit;
        }

        static Location SpawnLocationObject(LocationTemplate locationTemplate, Vector3 pos, Quaternion rot) {
            if (!locationTemplate) {
                throw new Exception("Object to spawn has to have LocationTemplate Component attached");
            }

            pos = BaseLocationSpawner.VerifyPosition(pos, locationTemplate, false);

            Location location = locationTemplate.SpawnLocation(pos, rot);
            RepetitiveNpcUtils.Check(location);
            return location;
        }
    }
}
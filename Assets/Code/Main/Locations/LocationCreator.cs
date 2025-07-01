using System;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Locations {
    public static class LocationCreator {
        const string RuntimeLocationIdPrefix = "RuntimeLocation";
        
        public static Location CreateSceneLocation(LocationSpec spec) {
            Location location = new(new SceneLocationInitializer());
            location.AssignID(spec.GetLocationId());
            location.Setup(spec);
            
            return location;
        }
        
        public static Location CreateRuntimeLocation(string contextName, in RuntimeLocationData data) {
            Location location = new(new RuntimeLocationInitializer(data));
            var id = World.Services.Get<IdStorage>().NextIdFor(location);
            location.AssignID($"{RuntimeLocationIdPrefix}:{contextName}:{id}");
            
            if (data.Template.TryGetComponent(out LocationSpec spec)) {
                location.Setup(spec);
            } else {
                throw new NullReferenceException($"No {nameof(LocationSpec)} in {nameof(LocationTemplate)} {data.Template.name}");
            }
            
            return location;
        }
    }
}
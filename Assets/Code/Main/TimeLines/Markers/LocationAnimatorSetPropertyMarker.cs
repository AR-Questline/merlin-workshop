using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Utility.Animations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Awaken.TG.Main.TimeLines.Markers {
    public class LocationAnimatorSetPropertyMarker : Marker, INotification {
        public PropertyName id => GetHashCode();
        
        public LocationReference locationReference;
        public string parameterName;
        [InlineProperty, HideLabel]
        public SavedAnimatorParameter parameterValue;
    }
}
using Awaken.TG.Utility.Attributes.Tags;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Awaken.TG.Main.TimeLines.Markers {
    public class StoryFlagMarker : Marker, INotification {
        public PropertyName id => GetHashCode();

        [SerializeField, Tags(TagsCategory.Flag)] string flag;
        [SerializeField] bool value;
        
        public string Flag => flag;
        public bool Value => value;
    }
}
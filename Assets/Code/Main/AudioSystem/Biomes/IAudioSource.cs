using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Extensions;
using FMODUnity;
using Sirenix.OdinInspector;
using UnityEngine;
// ReSharper disable InconsistentNaming

namespace Awaken.TG.Main.AudioSystem.Biomes {
    /// <summary>
    /// Sources are things like a biome. Combat. 
    /// </summary>
    public interface IAudioSource {
        /// <summary>
        /// Which priority this source should belong to.
        /// </summary>
        public int PriorityOverride();
        /// <summary>
        /// The event to be played if this audioSource is determined to be active
        /// </summary>
        public EventReference EventReference();
        /// <summary>
        /// If set, EventEmitter will be moved to this position.
        /// </summary>
        Vector3? Position { get; }
        /// <summary>
        /// If copyrighted, it will not be played in influencer mode
        /// </summary>
        bool IsCopyrighted { get; }

        public void SetRefreshCallback(System.Action callback);
        public void SetPosition(Vector3 position);
        public bool IsValid(PriorityManager manager) {
            if (EventReference().IsNull) {
                return false;
            }
            return !IsCopyrighted || !manager.InfluencerMode.Enabled;
        }

        public bool ShouldBeReplacedBy(IAudioSource other) => !IsCopyrighted && (other?.IsCopyrighted ?? false);
    }
    
    public static class AudioSourceExtension {
        public static int GetPriority(this IAudioSource audioSource) {
            int priorityOverride = audioSource.PriorityOverride();
            return priorityOverride < 0 ? 0 : Mathf.Min(25, priorityOverride);
        }
    }

    /// <summary>
    /// Example implementation of IAudioSource. Also for easy testing
    /// </summary>
    [System.Serializable]
    public class BaseAudioSource : IAudioSource {
        [SerializeField] EventReference eventRef;
        [Space(10)]
        [SerializeField, HideInInlineEditors, HorizontalGroup, LabelWidth(100)] int priorityOverride;
        [Space(10)]
        [SerializeField, HideInInlineEditors, HorizontalGroup, LabelWidth(90)] bool isCopyrighted;
        
        public bool IsCopyrighted => isCopyrighted;
        public int PriorityOverride() => priorityOverride;
        public EventReference EventReference() => eventRef;
        public Vector3? Position { get; private set; }
        public void SetRefreshCallback(System.Action callback) { }
        //public string EDITOR_Label => $"{priorityOverride}    {eventRef.PathOrGuid?.Split('/')[^1]}";

        /// <summary>
        /// Only for editor creation.
        /// </summary>
        public BaseAudioSource() {
            priorityOverride = -1;
        }

        public BaseAudioSource(int priorityOverride) {
            this.priorityOverride = priorityOverride;
        }

        public BaseAudioSource(int priorityOverride, EventReference eventRef) {
            this.priorityOverride = priorityOverride;
            this.eventRef = eventRef;
        }
        
        public void SetPosition(Vector3 position) {
            Position = position;
        }

        public static SerializationAccessor Serialization(BaseAudioSource instance) => new(instance);
        public struct SerializationAccessor {
            BaseAudioSource _instance;
            
            public SerializationAccessor(BaseAudioSource instance) {
                _instance = instance;
            }

            public ref EventReference EventRef => ref _instance.eventRef;
            public ref int PriorityOverride => ref _instance.priorityOverride;
            public ref bool IsCopyrighted => ref _instance.isCopyrighted;
        }
    }
    
    [System.Serializable]
    public partial class CombatMusicAudioSource : IAudioSource {
        public ushort TypeForSerialization => SavedTypes.CombatMusicAudioSource;

        [Saved, SerializeField] EventReference eventRef;
        [Space(10)]
        [Saved, SerializeField, HideInInlineEditors, HorizontalGroup] CombatMusicTier tier;
        [Space(10)]
        [Saved(false), SerializeField, HideInInlineEditors, HorizontalGroup, LabelWidth(90)] bool isCopyrighted;
        
        public bool IsCopyrighted => isCopyrighted;
        public int PriorityOverride() => (int)tier;
        public EventReference EventReference() => eventRef;
        public Vector3? Position { get; private set; }
        public void SetRefreshCallback(System.Action callback) { }
        
        /// <summary>
        /// Only for editor creation.
        /// </summary>
        public CombatMusicAudioSource() { }
        
        public void SetPosition(Vector3 position) {
            Position = position;
        }
        
        //public string EDITOR_Label => $"Tier: {tier.ToStringFast().Replace("Tier", "")}    {eventRef.PathOrGuid?.Split('/')[^1]}";
    }

    [System.Serializable]
    public class WorldAudioSource : IAudioSource {
        [SerializeField] EventReference eventRef;
        [SerializeField] bool isCopyrighted;
        
        public bool IsCopyrighted => isCopyrighted;
        public int PriorityOverride() => -1;
        public EventReference EventReference() => eventRef;
        public Vector3? Position => null;
        public void SetRefreshCallback(System.Action callback) { }
        //public string EDITOR_Label => $"{eventRef.PathOrGuid?.Split('/')[^1]}";

        public WorldAudioSource(EventReference eventRef, bool? copyrighted = null) {
            this.eventRef = eventRef;
            if (copyrighted.HasValue) {
                isCopyrighted = copyrighted.Value;
            }
        }
        
        public void SetPosition(Vector3 position) { }
    }

    public enum CombatMusicTier {
        [UnityEngine.Scripting.Preserve] Tier0 = 0,
        [UnityEngine.Scripting.Preserve] Tier1 = 1,
        [UnityEngine.Scripting.Preserve] Tier2 = 2,
        [UnityEngine.Scripting.Preserve] Tier3 = 3,
        [UnityEngine.Scripting.Preserve] Tier4 = 4,
        [UnityEngine.Scripting.Preserve] Tier5 = 5,
    }
}
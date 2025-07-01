using System;
using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Fights.Factions.Markers {
    public abstract partial class AntagonismMarker : Element<ICharacter> {
        [Saved] AntagonismLayer _layer;
        [Saved] AntagonismType _type;
        [Saved] Antagonism _antagonism;

        public AntagonismLayer Layer => _layer;
        
        [JsonConstructor, UnityEngine.Scripting.Preserve] protected AntagonismMarker() { }
        protected AntagonismMarker(AntagonismLayer layer, AntagonismType type, Antagonism antagonism) {
            _layer = layer;
            _type = type;
            _antagonism = antagonism;
        }

        public bool TryGetAntagonismTo(IWithFaction withFaction, out Antagonism antagonism) {
            antagonism = _antagonism;
            return _type != AntagonismType.From && RefersTo(withFaction);
        }

        public bool TryGetAntagonismsFrom(IWithFaction withFaction, out Antagonism antagonism) {
            antagonism = _antagonism;
            return _type != AntagonismType.To && RefersTo(withFaction);
        }
        
        protected abstract bool RefersTo(IWithFaction withFaction);

        protected bool Equals(AntagonismMarker other) {
            return _antagonism == other._antagonism && _type == other._type;
        }

        /// <summary>
        /// Applies marker only when analogous marker is not already applied
        /// </summary>
        public static bool TryApplySingleton<TMarker, TDuration>(TMarker marker, TDuration duration, ICharacter target) 
            where TMarker : AntagonismMarker, IEquatable<TMarker> 
            where TDuration : IDuration, IEquatable<TDuration> {
            
            if (!target.Elements<TMarker>().Any(m => IsAnalogous(m, marker, duration))) {
                TMarker antagonismMarker = target.AddElement(marker);
                if (target is Hero) {
                    antagonismMarker.MarkedNotSaved = true;
                }
                antagonismMarker.AddElement(new AntagonismDuration(duration));
                return true;
            } else {
                return false;
            }
        }

        public static bool IsAnalogous<TMarker, TDuration>(TMarker marker, TMarker analogousMarker, TDuration analogousDuration) 
            where TMarker : AntagonismMarker, IEquatable<TMarker> 
            where TDuration : IDuration, IEquatable<TDuration> {
            return marker.Equals(analogousMarker)
                   && marker.TryGetElement<AntagonismDuration>()?.Duration is TDuration duration
                   && duration.Equals(analogousDuration);
        }
    }
    
    public enum AntagonismType : byte {
        To,
        From,
        Mutual,
    }

    public enum AntagonismLayer : byte {
        Default,
        Story,
        Duel,
    }
}
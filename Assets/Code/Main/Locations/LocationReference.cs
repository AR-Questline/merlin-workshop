using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.NPCs.Presences;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Actors;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Stories.Steps.Helpers;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Utility.RichLabels;
using Awaken.TG.MVC;
using Awaken.TG.Utility.Attributes;
using Awaken.TG.Utility.Attributes.List;
using Awaken.TG.Utility.Attributes.Tags;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Locations {
    [Serializable, InlineProperty, HideLabel]
    public partial class LocationReference {
        public ushort TypeForSerialization => SavedTypes.LocationReference;

        public static Action<LocationReference> findWindowCallback;
        
        [Saved, NodeEnum, InlineButton(nameof(NavigateToLocation), "Find")] 
        public TargetType targetTypes = TargetType.Self;

        [Tooltip("All locations with these tags will be matched.")]
        [Saved, Tags(TagsCategory.Location), ShowIf(nameof(TargetsTags)), HideLabel] 
        public string[] tags;

        [Tooltip("All locations created with these templates will be matched")]
        [Saved, TemplateType(typeof(LocationTemplate)), ShowIf(nameof(TargetsTemplates))] 
        public TemplateReference[] locationRefs;
        
        [Tooltip("All locations using this actor will be matched")]
        [Saved, ShowIf(nameof(TargetsActors))]
        public ActorRef[] actors;
        
        [InfoBox("References to Location Specs placed directly in scene. Not prefabs in projects, nor spawned from spawners")]
        [ShowIf(nameof(TargetsSpecs))]
        [ListDrawerSettings(AddCopiesLastElement = false), List(ListEditOption.FewButtons | ListEditOption.NullNewElement)]
        public LocationSpec[] locationSpecsReferences;

        bool _useCache;
        [NonSerialized] Actor[] _cachedActors;
        [NonSerialized] LocationTemplate[] _cachedLocationTemplates;
        
        public bool TargetsTags => targetTypes is TargetType.Tags or TargetType.AnyTag;
        public bool TargetsTemplates => targetTypes == TargetType.Templates;
        public bool TargetsActors => targetTypes == TargetType.Actor;
        public bool TargetsSpecs => targetTypes == TargetType.UnityReferences;
        
        public IEnumerable<Actor> Actors => _useCache ? _cachedActors ??= ActorsArray : ActorsEnumerable;
        public IEnumerable<LocationTemplate> LocationTemplates => _useCache
            ? _cachedLocationTemplates ??= LocationTemplatesArray
            : LocationTemplatesEnumerable;

        public bool IsSet {
            get {
                if (TargetsTags && tags != null && tags.Any()) {
                    return true;
                }
                if (TargetsTemplates && locationRefs != null && LocationTemplates.Any()) {
                    return true;
                }
                if (TargetsActors && Actors.Any(actor => actor.IsSet)) {
                    return true;
                }
                return TargetsSpecs && locationSpecsReferences != null && locationSpecsReferences.Any();
            }
        }
        
        IEnumerable<LocationTemplate> LocationTemplatesEnumerable => locationRefs.Select(static r => r.Get<LocationTemplate>());
        LocationTemplate[] LocationTemplatesArray => ArrayUtils.Select(locationRefs, static r => r.Get<LocationTemplate>());
        IEnumerable<Actor> ActorsEnumerable => actors.Select(static actor => actor.Get());
        Actor[] ActorsArray => ArrayUtils.Select(actors, static actor => actor.Get());
        
        public LocationReference() {}

        public LocationReference(LocationReference other) {
            targetTypes = other.targetTypes;
            tags = ArrayUtils.CreateCopy(other.tags);
            locationRefs = ArrayUtils.CreateCopy(other.locationRefs);
            actors = ArrayUtils.CreateCopy(other.actors);
            locationSpecsReferences = ArrayUtils.CreateCopy(other.locationSpecsReferences);
        }
        
        public static bool TryGetDistinctiveMatches(RichLabelUsageEntry[] richLabelGuids, out Match[] matches) {
            matches = ArrayUtils.Select(richLabelGuids, _ => (Match)new MatchByRichLabel(richLabelGuids));
            return matches.Any();
        }
        
        public bool TryGetDistinctiveMatches(out Match[] matches) {
            matches = targetTypes switch {
                TargetType.Self => null,
                TargetType.Tags => new Match[] { new MatchByAllTags(tags) },
                TargetType.Templates => ArrayUtils.Select(locationRefs, static r => (Match)new MatchByTemplates(r.Get<LocationTemplate>())),
                TargetType.Actor => ArrayUtils.Select(actors, static actor => (Match)new MatchByActor(actor)),
                TargetType.UnityReferences => null,
                TargetType.AnyTag => ArrayUtils.Select(tags, static t => (Match)new MatchByTag(t)),
                _ => throw new ArgumentOutOfRangeException()
            };
            return matches != null;
        }
        
        /// <summary>
        /// Find all locations in world which meet our conditions
        /// </summary>
        public IEnumerable<Location> MatchingLocations([CanBeNull] Story api) {
            if (targetTypes != TargetType.Self && !IsSet) {
                Log.Important?.Warning("LocationReference is not set, returning empty list");
                yield break;
            }
            
            StartUsingCache();
            try { 
                foreach (var location in World.All<Location>()) {
                    if (IsMatching(api, location)) {
                        yield return location;
                    }
                }
            } finally {
                StopUsingCache();
            }
        }
        
        public Location FirstOrDefault([CanBeNull] Story api) {
            return MatchingLocations(api).FirstOrDefault();
        }

        /// <summary>
        /// See if location meet our conditions
        /// </summary>
        public bool IsMatching([CanBeNull] Story api, Location loc) {
            return targetTypes switch {
                TargetType.Self => loc == api?.FocusedLocation,
                TargetType.Tags => tags.Length > 0 && tags.All(t => loc.Tags.Contains(t, StringComparer.InvariantCultureIgnoreCase)),
                TargetType.Templates => LocationTemplates.Any(t => t == loc.Template || t == loc.Spec?.GetComponent<LocationTemplate>()),
                TargetType.Actor => Actors.Any(actor => loc.TryGetElement<IWithActor>()?.Actor == actor),
                TargetType.UnityReferences => locationSpecsReferences.Any(s => s == loc.Spec),
                TargetType.AnyTag => tags.Length > 0 && tags.Any(t => loc.Tags.Contains(t, StringComparer.InvariantCultureIgnoreCase)),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        void StartUsingCache() {
            _useCache = true;
        }

        void StopUsingCache() {
            _useCache = false;
            _cachedActors = null;
            _cachedLocationTemplates = null;
        }

        void NavigateToLocation() {
            findWindowCallback?.Invoke(this);
        }

        public override string ToString() {
            string result = "LocationReference: ";

            if (TargetsTags) {
                result += $"Tags={string.Join(", ", tags ?? Array.Empty<string>())}";
            } else if (TargetsTemplates) {
                result += $"LocationRefs={string.Join(", ", locationRefs?.Select(r => r.Get<LocationTemplate>().name) ?? Array.Empty<string>())}";
            } else if (TargetsActors) {
                result += $"Actors={string.Join(", ", actors?.Select(a => ActorsRegister.Get.Editor_GetActorName(a.guid)) ?? Array.Empty<string>())}";
            } else if (TargetsSpecs) {
                result += $"LocationSpecsReferences={string.Join(", ", locationSpecsReferences?.Select(s => s.name) ?? Array.Empty<string>())}";
            }

            return result;
        }

        public abstract partial class Match {
            public virtual IEnumerable<Location> Find() {
                foreach (var location in World.All<Location>()) {
                    if (IsMatching(location)) {
                        yield return location;
                    }
                }
            }
            
            public abstract ushort TypeForSerialization { get; }
            protected abstract bool IsMatching(Location loc);
        }

        public sealed partial class MatchByRichLabel : Match {
            public override ushort TypeForSerialization => SavedTypes.MatchByRichLabel;

            [Saved] RichLabelUsageEntry[] _richLabelGuids;
            
            public MatchByRichLabel(RichLabelUsageEntry[] richLabelGuids) {
                _richLabelGuids = richLabelGuids;
            }

            public override IEnumerable<Location> Find() {
                foreach (var presence in World.All<NpcPresence>()) {
                    if (RichLabelUtilities.IsMatchingRichLabel(presence.RichLabelSet, _richLabelGuids)) {
                        yield return presence.ParentModel;
                    }
                }
            }

            protected override bool IsMatching(Location loc) {
                throw new InvalidOperationException("Shouldn't happen, because Find is overriden");
            }
        }
        
        public sealed partial class MatchByAllTags : Match {
            public override ushort TypeForSerialization => SavedTypes.MatchByAllTags;

            [Saved] string[] _tags;
            
            public MatchByAllTags(string[] tags) {
                _tags = tags;
            }
            
            protected override bool IsMatching(Location loc) {
                int count = _tags.Length;
                for (int i = 0; i < count; i++) {
                    if (loc.Tags.Contains(_tags[i]) == false) {
                        return false;
                    }
                }

                return true;
            }
        }
        
        public sealed partial class MatchByTag : Match {
            public override ushort TypeForSerialization => SavedTypes.MatchByTag;

            [Saved] string _tag;
            
            public MatchByTag(string tag) {
                _tag = tag;
            }
            
            protected override bool IsMatching(Location loc) {
                return loc.Tags.Contains(_tag);
            }
        }
        
        public sealed partial class MatchByTemplates : Match {
            public override ushort TypeForSerialization => SavedTypes.MatchByTemplates;

            [Saved] LocationTemplate _template;
            
            public MatchByTemplates(LocationTemplate template) {
                _template = template;
            }
            
            protected override bool IsMatching(Location loc) {
                return loc.Template == _template;
            }
        }
        
        public sealed partial class MatchByActor : Match {
            public override ushort TypeForSerialization => SavedTypes.MatchByActor;

            [Saved] ActorRef _actorRef;
            Actor? _actor;
            
            public MatchByActor(ActorRef actorRef) {
                _actorRef = actorRef;
            }

            public override IEnumerable<Location> Find() {
                _actor ??= _actorRef.Get();
                return StoryUtils.MatchActorLocations(_actor.Value);
            }

            protected override bool IsMatching(Location loc) {
                throw new InvalidOperationException("Shouldn't happen, because Find is overriden");
            }
        }
    }

    public enum TargetType : byte {
        Self = 0,
        Tags = 1,
        Templates = 2,
        Actor = 4,
        UnityReferences = 5,
        AnyTag = 6
    }
}

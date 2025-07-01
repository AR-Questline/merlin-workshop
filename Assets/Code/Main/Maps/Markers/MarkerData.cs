using System;
using Awaken.TG.Assets;
using Awaken.TG.Main.Heroes.CharacterSheet.Map.Markers;
using Awaken.Utility.Extensions;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Awaken.TG.Main.Maps.Markers {
    [Serializable, InlineProperty]
    public class MarkerData {
        [BoxGroup("Marker Icons"), ARAssetReferenceSettings(new[] { typeof(Sprite), typeof(Texture2D) }, true), SerializeField]
        protected ShareableSpriteReference defaultIcon;

        [SerializeField] 
        bool visibleOnMap;
        
        [SerializeField, HideIf(nameof(IsOverridenVisibleOnMapUnderFogOfWar)), EnableIf(nameof(visibleOnMap))] 
        bool visibleOnMapUnderFogOfWar;

        [Space]
        [SerializeField]
        CompassMarkerType compassMarkerType;
        
        [SerializeField] 
        bool hasMapMarkerOrderOverride;

        [ShowIf(nameof(hasMapMarkerOrderOverride)), SerializeField]
        int mapMarkerOrderOverride;
        
        [FormerlySerializedAs("forceShowOnCompass")] [SerializeField] bool ignoreDistanceRequirement;
        [FormerlySerializedAs("alwaysVisibleOnCompass")] [SerializeField] bool ignoreAngleRequirement;

        public bool VisibleOnMap => visibleOnMap;
        public virtual bool VisibleOnMapUnderFogOfWar => visibleOnMapUnderFogOfWar;
        public ShareableSpriteReference MarkerIcon => defaultIcon;
        public CompassMarkerType CompassMarkerType => compassMarkerType;
        public int MapMarkerOrderOverride => hasMapMarkerOrderOverride ? mapMarkerOrderOverride : MapMarkerOrder.Default.ToInt();
        public bool IgnoreDistanceRequirement => ignoreDistanceRequirement;
        public bool IgnoreAngleRequirement => ignoreAngleRequirement;

        bool IsOverridenVisibleOnMapUnderFogOfWar => this is DiscoveryMarkerData || this is QuestMarkerData || this is NpcMarkerData;
    }

    [Serializable, InlineProperty]
    public class DiscoveryMarkerData : MarkerData {
        [BoxGroup("Marker Icons"), ARAssetReferenceSettings(new[] { typeof(Sprite), typeof(Texture2D) }, true), SerializeField]
        ShareableSpriteReference undiscoveredMarkerIcon;

        [BoxGroup("Marker Icons"), ARAssetReferenceSettings(new[] { typeof(Sprite), typeof(Texture2D) }, true), SerializeField]
        ShareableSpriteReference inactiveMarkerIcon;

        public ShareableSpriteReference UndiscoveredMarkerIcon => undiscoveredMarkerIcon;
        public ShareableSpriteReference InactiveMarkerIcon => inactiveMarkerIcon;
        public override bool VisibleOnMapUnderFogOfWar => true;
    }

    [Serializable, InlineProperty]
    public class NpcMarkerData : MarkerData {
        [BoxGroup("Marker Icons"), ARAssetReferenceSettings(new[] { typeof(Sprite), typeof(Texture2D) }, true), SerializeField]
        ShareableSpriteReference friendlyMarkerIcon;

        [BoxGroup("Marker Icons"), ARAssetReferenceSettings(new[] { typeof(Sprite), typeof(Texture2D) }, true), SerializeField]
        ShareableSpriteReference hostileMarkerIcon;

        [BoxGroup("Marker Icons"), ARAssetReferenceSettings(new[] { typeof(Sprite), typeof(Texture2D) }, true), SerializeField]
        ShareableSpriteReference combatMarkerIcon;

        [SerializeField]
        bool hideOutsideOfCombat;

        public ShareableSpriteReference FriendlyMarkerIcon => friendlyMarkerIcon;
        public ShareableSpriteReference HostileMarkerIcon => hostileMarkerIcon;
        public ShareableSpriteReference CombatMarkerIcon => combatMarkerIcon;
        public bool HideOutsideOfCombat => hideOutsideOfCombat;
        public override bool VisibleOnMapUnderFogOfWar => false;
    }

    [Serializable]
    public class QuestMarkerData : MarkerData {
        [BoxGroup("Marker Icons"), ARAssetReferenceSettings(new[] { typeof(Sprite), typeof(Texture2D) }, true), SerializeField]
        ShareableSpriteReference activeQuestMarker;
        
        [BoxGroup("Marker Icons"), ARAssetReferenceSettings(new[] { typeof(Sprite), typeof(Texture2D) }, true), SerializeField]
        ShareableSpriteReference questMarkerExit;
        [BoxGroup("Marker Icons"), ARAssetReferenceSettings(new[] { typeof(Sprite), typeof(Texture2D) }, true), SerializeField]
        ShareableSpriteReference activeQuestMarkerExit;
        
        public ShareableSpriteReference QuestMarker => defaultIcon;
        public ShareableSpriteReference ActiveQuestMarker => activeQuestMarker;
        public ShareableSpriteReference QuestMarkerExit => questMarkerExit;
        public ShareableSpriteReference ActiveQuestMarkerExit => activeQuestMarkerExit;
        public override bool VisibleOnMapUnderFogOfWar => true;
    }
}
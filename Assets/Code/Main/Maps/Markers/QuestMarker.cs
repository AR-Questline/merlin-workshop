using System;
using Awaken.TG.Assets;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Maps.Compasses;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Stories.Quests.UI;

namespace Awaken.TG.Main.Maps.Markers {
    public partial class QuestMarker : LocationMarker {
        public sealed override bool IsNotSaved => true;

        protected override bool IsVisibleUnderFogOfWar => true;
        static CommonReferences CommonReferences => Services.Get<CommonReferences>();

        public QuestMarker(ShareableSpriteReference icon, bool isNumberVisible)
            : base(CommonReferences.QuestMainCompassMarkerData, icon, isNumberVisible) { }

        protected override CompassMarker SpawnCompassMarker() {
            return ParentModel.TryGetElement(out LocationArea area) 
                ? new QuestAreaCompassMarker(this, area) 
                : base.SpawnCompassMarker();
        }

        public static ShareableSpriteReference GetCompassMarkerIcon(IGrounded target, bool isActive, QuestType questType) {
            QuestMarkerData markerData = questType switch {
                QuestType.Main => CommonReferences.QuestMainCompassMarkerData,
                QuestType.Side => CommonReferences.QuestSideCompassMarkerData,
                QuestType.Challenge or QuestType.Achievement or QuestType.Misc => CommonReferences.QuestOtherCompassMarkerData,
                _ => throw new ArgumentOutOfRangeException(nameof(questType), questType, null)
            };
            
            return GetMarkerIcon(markerData, target, isActive);
        }
        
        public static ShareableSpriteReference Get3DMarkerIcon(IGrounded target, bool isActive) {
            QuestMarkerData markerData = CommonReferences.Quest3DMarkerData;
            return GetMarkerIcon(markerData, target, isActive);
        }

        static ShareableSpriteReference GetMarkerIcon(QuestMarkerData markerData, IGrounded target, bool isActive) {
            return (target.TryGetElement<Portal>()?.IsFrom, isActive) switch {
                (true, true) => markerData.ActiveQuestMarkerExit,
                (true, false) => markerData.QuestMarkerExit,
                (_, true) => markerData.ActiveQuestMarker,
                (_, false) => markerData.QuestMarker
            };
        }
    }
}
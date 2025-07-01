using Awaken.TG.Code.Utility;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Stories.Actors;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Fights.NPCs {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Common, "Used by simple NPCs, mainly enemies (doesn't support multiple presences).")]
    public class RepetitiveNpcAttachment : NpcAttachment {
        
        [SerializeField, FoldoutGroup("Story"), PropertyOrder(-1), Tooltip("Used to randomly select an actor. If left empty, the default actor from the Actor property will be used.")]
        ActorRef[] potentialActors = { };

        protected override bool ShowActorProperty => potentialActors.Length == 0;
        public override bool IsUnique => false;
        public override ActorRef GetActor() => potentialActors.Length > 0 ? RandomUtil.UniformSelect(potentialActors) : base.GetActor();

        [Button, EnableIf(nameof(CanSpawnNpc))]
        void SpawnNpc() {
            LocationTemplate locationTemplate = GetComponent<LocationTemplate>();
            if (locationTemplate != null) {
                Vector3 distance = Hero.Current.Forward();
                Quaternion rotation = Quaternion.LookRotation(-distance);
                distance.y = 0;
                locationTemplate.SpawnLocation(Hero.Current.Coords + distance * 2, initialRotation: rotation);
            } else {
                Log.Important?.Error("NpcAttachment doesn't contain LocationTemplate");
            }
        }

        bool CanSpawnNpc() {
            return Hero.Current != null;
        }

#if UNITY_EDITOR
        public override ActorRef Editor_GetActorForCache() => potentialActors.Length > 0 ? potentialActors[0] : base.Editor_GetActorForCache();
#endif
    }
}
using Awaken.TG.Main.Locations.Attachments.Attachment;
using Awaken.TG.Main.Stories.Actors;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;
using Awaken.Utility.GameObjects;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public partial class LocationWithActor : Element<Location>, IRefreshedByAttachment<LocationWithActorAttachment>, ILocationElementWithActor, IWithLookAt {
        public override ushort TypeForSerialization => SavedModels.LocationWithActor;

        public Actor Actor { get; private set; }
        public Transform ActorTransform => ParentModel.MainView.transform;
        public Transform Head { get; private set; }
        public Transform LookAtTarget => Head;
        
        public void InitFromAttachment(LocationWithActorAttachment spec, bool isRestored) {
            Actor = spec.actorRef.Get();
        }

        protected override void OnInitialize() {
            ParentModel.OnVisualLoaded(t => {
                Head = t.gameObject.FindChildWithTagRecursively("Head");
            });
        }
    }
}
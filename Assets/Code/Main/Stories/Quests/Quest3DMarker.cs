using Awaken.TG.Assets;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Domains;

namespace Awaken.TG.Main.Stories.Quests {
    [SpawnsView(typeof(VQuest3DMarker))]
    public partial class Quest3DMarker : Model {
        public override Domain DefaultDomain => Domain.CurrentScene();
        public sealed override bool IsNotSaved => true;

        public readonly IGrounded groundedTarget;
        public readonly int orderNumber;
        public readonly bool isNumberVisible;
        public readonly SpriteReference questIcon;

        public Quest3DMarker(IGrounded target, ShareableSpriteReference questIcon, int orderNumber, bool isNumberVisible) {
            groundedTarget = target;
            this.orderNumber = orderNumber;
            this.isNumberVisible = isNumberVisible;
            this.questIcon = questIcon.Get();
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            questIcon.Release();
        }
    }
}
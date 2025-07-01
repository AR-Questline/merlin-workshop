using Awaken.TG.Main.Character;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Fights.NPCs {
    public partial class NpcHandOwner : Element<NpcElement>, IHandOwner<NpcElement> {
        public sealed override bool IsNotSaved => true;

        public float WeaponColliderDivider => 1.5f;
        public LayerMask HitLayerMask => ParentModel.Template.npcHitMask;
        [UnityEngine.Scripting.Preserve] public void OnAttackRelease() {}
        [UnityEngine.Scripting.Preserve] public void OnAttackRecovery() {}
        public void OnFinisherRelease(Vector3 position) {}
        public void OnBackStabRelease() {}
    }
}
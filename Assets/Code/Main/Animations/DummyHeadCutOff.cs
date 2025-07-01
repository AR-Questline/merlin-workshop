using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.Items.Attachments.Interfaces;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;
using UnityEngine;

namespace Awaken.TG.Main.Animations {
    public partial class DummyHeadCutOff : Element<NpcDummy> {
        public override ushort TypeForSerialization => SavedModels.DummyHeadCutOff;

        Transform _neck;
        
        protected override void OnInitialize() {
            ParentModel.OnCompletelyInitialized(_ => Init());
        }

        void Init() {
            _neck = ParentModel.Neck;
            if (_neck != null) {
                _neck.localScale = Vector3.zero;
            } else {
                Discard();
            }
        }
    }
}
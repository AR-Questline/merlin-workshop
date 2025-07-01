using Awaken.Utility;
using System;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Animations;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Attachments.Customs {
    [Serializable]
    public partial class Sharg : CustomCombatBaseClass {
        public override ushort TypeForSerialization => SavedModels.Sharg;

        [SerializeField, TemplateType(typeof(StatusTemplate))]
        TemplateReference sleepStatusTemplate;
        
        public bool IsSleeping => NpcElement.Statuses.HasStatus(sleepStatusTemplate.Get<StatusTemplate>());

        public Vector3 ShargPetHeroPosition => Hero.TppActive 
            ? Coords + NpcElement.Forward().ToHorizontal3() * 3.25f + NpcElement.Right().ToHorizontal3() * -1.15f
            : Coords + NpcElement.Forward().ToHorizontal3() * 2.5f + NpcElement.Right().ToHorizontal3() * -0.25f;
        
        public override void InitFromAttachment(CustomCombatAttachment spec, bool isRestored) {
            Sharg copyFrom = (Sharg)spec.CustomCombatBaseClass;
            sleepStatusTemplate = new TemplateReference(copyFrom.sleepStatusTemplate.GUID);
            base.InitFromAttachment(spec, isRestored);
        }

        protected override void OnInitialize() {
            base.OnInitialize();
            ParentModel.AddElement(new PetShargAction());
            ParentModel.AfterFullyInitialized(() => NpcElement.ListenTo(IAlive.Events.BeforeDeath, ParentModel.RemoveElementsOfType<PetShargAction>), this);
        }
    }
}
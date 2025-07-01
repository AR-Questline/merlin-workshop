using Awaken.TG.Code.Utility;
using Awaken.TG.Main.AI.Grid;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using UnityEngine;

namespace Awaken.TG.Main.Skills.Passives {
    public partial class PassiveStatusSpread : Element<Skill>, IPassiveEffect {
        public sealed override bool IsNotSaved => true;

        readonly float _chance;
        readonly float _radius;

        public PassiveStatusSpread(float chance, float radius) {
            _chance = chance;
            _radius = radius;
        }

        protected override void OnInitialize() {
            World.EventSystem.ListenTo(EventSelector.AnySource, CharacterStatuses.Events.AddedStatus, this, OnStatusAdded);
        }

        void OnStatusAdded(Status status) {
            if (!CanSpreadStatus(status)) return;
            
            if (RandomUtil.WithProbability(_chance)) {
                SpreadStatus(status);
            }
        }

        void SpreadStatus(Status status) {
            Vector3 position = status.Character.Coords;
            
            // Change character in source info - it prevents infinite loop
            var sourceInfo = new StatusSourceInfo(status.SourceInfo)
                .WithCharacter(status.Character);
            
            foreach (var npc in Services.Get<NpcGrid>().GetNpcsInSphere(position, _radius)) {
                if (status.Character != npc) {
                    npc.Statuses.AddStatus(status.Template, sourceInfo);
                }
            }
        }
        
        bool CanSpreadStatus(Status status) {
            return status.Template.IsBuildupAble 
                   && status.Character is NpcElement 
                   && status.SourceInfo?.SourceCharacter.Get() == ParentModel.Owner;
        }
    }
}
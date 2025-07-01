using Awaken.Utility;
using Newtonsoft.Json;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Main.AI;
using Awaken.TG.Main.AI.Idle.Data.Runtime;
using Awaken.TG.Main.AI.Idle.Finders;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.NPCs.Presences;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Saving.Models;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public partial class NpcDisappearedElement : Element<NpcElement> {
        public override ushort TypeForSerialization => SavedModels.NpcDisappearedElement;

        readonly float _duration;
        readonly ICharacter _applier;
        readonly ShareableARAssetReference _vfx;
        [Saved] Vector3 _position;
        InteractionOverride _interactionOverride;

        [UnityEngine.Scripting.Preserve]
        public static bool CanNpcBeDisappeared(NpcElement npc) {
            return npc.IsAlive && !npc.IsDying && !npc.HasElement<WaitingToBeResurrectedElement>();
        }
        
        [UnityEngine.Scripting.Preserve]
        public static void DisappearNpc(NpcElement npc, float duration, ShareableARAssetReference vfxEffect = null, ICharacter applier = null) {
            npc.AddElement(new NpcDisappearedElement(duration, vfxEffect, applier));
        }
        
        [JsonConstructor, UnityEngine.Scripting.Preserve]
        NpcDisappearedElement() { }
        
        public NpcDisappearedElement(float duration, ShareableARAssetReference vfxEffect = null, ICharacter applier = null) {
            _duration = duration;
            _applier = applier;
            if (vfxEffect is { IsSet: true }) {
                _vfx = vfxEffect;
            }
        }

        protected override void OnInitialize() {
            _position = ParentModel.Coords;
            HeroCombat.forceCombatCount++;

            if (_applier != null) {
                ParentModel.NpcAI.NotifyAlliesAboutOngoingFight(_applier);
            }

            if (ParentModel.NpcAI.InCombat) {
                ParentModel.GetTargeting();
                ParentModel.NpcAI.ExitCombat(true, true);
                DelayDisappear().Forget();
                return;
            }
            Disappear();
        }

        // Needs to be handled manually because if it's [NotSaved] NPC will stay in abyss forever after leaving and reentering scene
        protected override void OnRestore() {
            HeroCombat.forceCombatCount++;
            if (NpcPresence.InAbyss(ParentModel.ParentModel.Coords)) {
                ParentModel.ParentModel.SafelyMoveTo(_position, true);
            }
            ParentModel.ParentModel.SetInteractability(LocationInteractability.Active);
            Discard();
        }

        async UniTaskVoid DelayDisappear() {
            if (!await AsyncUtil.DelayFrame(this)) {
                return;
            }
            Disappear();
        }

        void Disappear() {
            ParentModel.ParentModel.SetInteractability(LocationInteractability.Hidden);
            ParentModel.ParentModel.SafelyMoveTo(NpcPresence.AbyssPosition, true);
            
            // Prevent NPCs from searching new interactions while disappeared
            _interactionOverride = new InteractionOverride(new InteractionUniqueFinder(""), null, "");
            _interactionOverride.MarkedNotSaved = true;
            ParentModel.Behaviours.AddOverride(_interactionOverride);

            if (_vfx?.IsSet ?? false) {
                PrefabPool.InstantiateAndReturn(_vfx, _position, Quaternion.identity, _duration).Forget();
            }

            // All characters attacking this NPC should start searching for new target
            var targetingList = ParentModel.GetTargeting().Where(n => n.IsAlive && n.GetCurrentTarget() == ParentModel).ToList();
            foreach (ICharacter targeting in targetingList) {
                if (targeting is NpcElement targetingNpc) {
                    targetingNpc.RecalculateTarget();
                }
            }
            
            // Inform "friendly" NPCs that something weird has happened
            ParentModel.NpcAI.AlertStack.NewPoi(AlertStack.AlertStrength.Weak, _position);
            
            WaitToAppearAgain().Forget();
        }

        async UniTaskVoid WaitToAppearAgain() {
            if (!await AsyncUtil.DelayTime(this, _duration)) {
                return;
            }
            Appear();
        }

        void Appear() {
            ParentModel.ParentModel.SafelyMoveTo(_position, true);
            ParentModel.ParentModel.SetInteractability(LocationInteractability.Active);
            _interactionOverride.Discard();
            
            ParentModel.RecalculateTarget();
            ParentModel.NpcAI.AlertStack.NewPoi(AlertStack.AlertStrength.Strong, _position);
            Discard();
        }
        
        protected override void OnDiscard(bool fromDomainDrop) {
            HeroCombat.forceCombatCount--;
        }
    }
}

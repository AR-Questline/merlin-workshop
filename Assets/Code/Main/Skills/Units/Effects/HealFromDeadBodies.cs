using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.Animations.FSM.Heroes.States.Magic;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Utils;
using UnityEngine;

namespace Awaken.TG.Main.Skills.Units.Effects {
    /// <summary>
    /// This element is used strictly by VisualScripting with skill that is responsible for healing player from dead body.
    /// Even though this element directly doesn't heal player it's name is still HealFromDeadBodies because it's used by HealFromDeadBodies skill.
    /// </summary>
    public partial class HealFromDeadBodies : Element<ICharacter> {
        public sealed override bool IsNotSaved => true;

        readonly WeakModelRef<Skill> _spawnedWithSkill;
        readonly float _manaCostPerTick;
        readonly float _range;
        readonly float _maxRangeSqr;
        Transform _corpse;
        
        public Skill Skill => _spawnedWithSkill.Get();

        public HealFromDeadBodies(Skill skill, float manaCostPerTick, float range) {
            _spawnedWithSkill = skill;
            _manaCostPerTick = manaCostPerTick;
            _range = range;
            _maxRangeSqr = (range + 1) * (range + 1);
        }

        protected override void OnInitialize() {
            _corpse = FindNearestCorpse(ParentModel.Coords, _range);
            ParentModel.GetOrCreateTimeDependent().WithUpdate(OnUpdate);
            Skill.SourceItem.Trigger(VCCharacterMagicVFX.Events.VFXTargetChanged, _corpse);
            ParentModel.ListenTo(Stat.Events.StatChanged(CharacterStatType.Mana), OnManaDrained, this);
        }

        void OnUpdate(float deltaTime) {
            if (_corpse == null) {
                Discard();
                return;
            }
            
            float distanceToCorpseSqr = (_corpse.position - ParentModel.Coords).sqrMagnitude;
            if (distanceToCorpseSqr > _maxRangeSqr) {
                Discard();
            }
        }

        void OnManaDrained(Stat stat) {
            if (_manaCostPerTick > stat.ModifiedValue) {
                Discard();
            }
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            ParentModel.GetTimeDependent()?.WithoutUpdate(OnUpdate);
            if (_spawnedWithSkill.TryGet(out var skill) && skill.SourceItem != null) {
                skill.SourceItem.Trigger(VCCharacterMagicVFX.Events.VFXTargetChanged, null);
                skill.SourceItem.Trigger(MagicFSM.Events.EndCasting, MagicEndState.MagicEnd);
            }
        }

        public static Transform FindNearestCorpse(Vector3 coords, float maxRange) {
            float maxRangeSqr = maxRange * maxRange;
            float closestRangeSqr = float.MaxValue;
            NpcDummy closest = null;
            foreach (var dummy in World.All<NpcDummy>()) {
                float distanceSqr = (dummy.Coords - coords).sqrMagnitude;
                if (distanceSqr < closestRangeSqr) {
                    closestRangeSqr = distanceSqr;
                    closest = dummy;
                }
            }
            return closestRangeSqr <= maxRangeSqr ? closest?.ParentTransform : null;
        }
    }
}
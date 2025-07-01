using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.AI.Combat.Behaviours.BaseBehaviours;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.Utils;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat {
    public class CircleAroundTargetService : IService {
        // === Fields
        readonly Dictionary<WeakModelRef<ICharacter>, CircleAroundData> _circleData = new();

        float _combatPositionUpdateDelay;

        public void Init() {
            World.EventSystem.ListenTo(EventSelector.AnySource, Hero.Events.HeroLongTeleported, this, Cleanup);
        }

        public Vector3 GetNextCirclingPoint(CircleAroundTargetBehaviour behaviour, ICharacter target) {
            CircleAroundData data = GetData(target);
            var npc = behaviour.ParentModel.NpcElement;
            int version = behaviour.CurrentVersion;
            int index = behaviour.CurrentIndex;
            var point = data.GetNextPoint(npc.Coords, npc.Forward(), ref version, ref index, behaviour.AscendingDirection, behaviour.Offset, out bool changedDirection);
            behaviour.CurrentVersion = version;
            behaviour.CurrentIndex = index;
            if (changedDirection) {
                behaviour.ChangeDirection(!behaviour.AscendingDirection);
            }
            return point;
        }
        
        public Vector3 GetClosestCirclingPoint(CircleAroundTargetBehaviour behaviour, ICharacter target) {
            CircleAroundData data = GetData(target);
            var npc = behaviour.ParentModel.NpcElement;
            var point = data.GetClosestPoint(npc.Coords, npc.Forward(), out int version, out int index, behaviour.Offset);
            behaviour.CurrentVersion = version;
            behaviour.CurrentIndex = index;
            return point;
        }

        public bool CanLeaveCircling(CircleAroundTargetBehaviour behaviour, ICharacter target) {
            CircleAroundData data = GetData(target);
            return data.CanLeaveCircling();
        }
        
        public void LeftCircling(CircleAroundTargetBehaviour behaviour, ICharacter target) {
            CircleAroundData data = GetData(target);
            data.LeaveCircling(behaviour.LeaveCircleCooldown);
        }
        
        CircleAroundData GetData(ICharacter target) {
            var weakModel = new WeakModelRef<ICharacter>(target);
            if (!_circleData.TryGetValue(weakModel, out CircleAroundData data)) {
                data = new CircleAroundData(target);
                _circleData.Add(weakModel, data);
                target.ListenToLimited(IAlive.Events.BeforeDeath, dmg => TryRemoveData(dmg.Target as ICharacter), target);
                target.ListenToLimited(Model.Events.BeforeDiscarded, model => TryRemoveData(model as ICharacter), target);
                target.ListenToLimited(ICharacter.Events.CombatExited, TryRemoveData, target);
            }
            return data;
        }

        void TryRemoveData(ICharacter character) {
            var weakModel = new WeakModelRef<ICharacter>(character);
            if (_circleData.TryGetValue(weakModel, out CircleAroundData data)) {
                data.ClearDebugVisuals();
                _circleData.Remove(weakModel);
            }
            Cleanup();
        }

        void Cleanup() {
            foreach (var pair in _circleData.Where(pair => !pair.Key.Exists()).ToArray()) {
                _circleData.Remove(pair.Key);
            }
        }
    }
}
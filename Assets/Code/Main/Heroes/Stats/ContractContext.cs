using Awaken.TG.MVC;

namespace Awaken.TG.Main.Heroes.Stats {
    public class ContractContext {
        [UnityEngine.Scripting.Preserve] public IModel giver;
        [UnityEngine.Scripting.Preserve] public IModel receiver;
        public ChangeReason reason;

        public ContractContext(IModel giver, IModel receiver, ChangeReason reason) {
            this.giver = giver;
            this.receiver = receiver;
            this.reason = reason;
        }
    }

    public enum ChangeReason : byte {
        [UnityEngine.Scripting.Preserve] Story = 0,
        [UnityEngine.Scripting.Preserve] FightReward = 1,
        [UnityEngine.Scripting.Preserve] Trade = 2,
        [UnityEngine.Scripting.Preserve] Exploration = 3,
        [UnityEngine.Scripting.Preserve] Skill = 4,
        [UnityEngine.Scripting.Preserve] AttackBehaviour = 5,
        [UnityEngine.Scripting.Preserve] CombatDamage = 6,
        [UnityEngine.Scripting.Preserve] Forceful = 7,
        [UnityEngine.Scripting.Preserve] LevelUp = 8,
    }
}
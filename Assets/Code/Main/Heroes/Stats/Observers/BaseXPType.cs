using Awaken.Utility.Enums;

namespace Awaken.TG.Main.Heroes.Stats.Observers {
    public class BaseXPType : RichEnum {
        delegate float TypeSpecifics(float skillBaseXP, float skillSourceParameter);
        TypeSpecifics _typeSpecificModificationFunction = (skillBaseXP, skillSourceParameter) => skillBaseXP * skillSourceParameter;
        BaseXPType(string enumName) : base(enumName, enumName) { }

        [UnityEngine.Scripting.Preserve]
        BaseXPType(string enumName, TypeSpecifics typeSpecificModificationFunction) : base(enumName, enumName) {
            _typeSpecificModificationFunction = typeSpecificModificationFunction;
        }

        public static readonly BaseXPType
            Walk = new(nameof(Walk)),
            Sprint = new(nameof(Sprint)),
            FastSwim = new(nameof(FastSwim)),

            Dash = new(nameof(Dash)),
            Slide = new(nameof(Slide)),

            Jump = new(nameof(Jump)),
            FallDmg = new(nameof(FallDmg)),

            DmgDealt = new(nameof(DmgDealt)),
            DmgBlocked = new(nameof(DmgBlocked)),
            DmgReceived = new(nameof(DmgReceived)),
            RangedDmgDealt = new(nameof(RangedDmgDealt)),
            SummonDmgDealt = new(nameof(SummonDmgDealt)),
            
            PickpocketSuccess = new(nameof(PickpocketSuccess)),
            PickpocketFail = new(nameof(PickpocketFail)),
            LockUnlocked = new(nameof(LockUnlocked)),
            PickBroke = new(nameof(PickBroke)),
            
            Cooking = new(nameof(Cooking)),
            Alchemy = new(nameof(Alchemy)),
            Handcrafting = new(nameof(Handcrafting)),
            
            Sneak = new(nameof(Sneak));

        public float ApplyTypeSpecificFunction(float skillBaseXP, float skillSourceParameter) {
            return _typeSpecificModificationFunction(skillBaseXP, skillSourceParameter);
        }
    }
}
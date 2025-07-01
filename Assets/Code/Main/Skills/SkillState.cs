namespace Awaken.TG.Main.Skills {
    public struct SkillState {
        public bool learned;
        public bool equipped;
        public bool submitted;

        [UnityEngine.Scripting.Preserve] public static readonly SkillState None = new();
        public static readonly SkillState Learned = new() { learned = true };

        public readonly void Apply(Skill skill) {
            if (learned) {
                skill.Learn();
            }
            if (equipped) {
                skill.Equip();
            }
            if (submitted) {
                skill.Submit();
            }
        }
    }
}
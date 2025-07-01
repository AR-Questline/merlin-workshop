using System;
using Awaken.Utility.Enums;

namespace Awaken.TG.Main.AI.Movement.Controllers {
    public class VelocityScheme : RichEnum {
        readonly Func<RootMotions.RootMotion, float> _getter;

        public VelocityScheme(string enumName, Func<RootMotions.RootMotion, float> getter) : base(enumName) {
            _getter = getter;
        }

        public static readonly VelocityScheme
            NoMove = new(nameof(NoMove), _ => 0),
            SlowWalk = new(nameof(SlowWalk), rm => rm.WalkSpeed * 0.5f),
            Walk = new(nameof(Walk), rm => rm.WalkSpeed),
            Trot = new(nameof(Trot), rm => rm.TrotSpeed),
            Run = new(nameof(Run), rm => rm.RunSpeed);
        
        [UnityEngine.Scripting.Preserve]
        public float SpeedNormalized(NpcController npcController) => Speed(npcController) / npcController.RootMotion.MaxSpeed;
        public float Speed(NpcController npcController) => _getter.Invoke(npcController.RootMotion);
    }
}
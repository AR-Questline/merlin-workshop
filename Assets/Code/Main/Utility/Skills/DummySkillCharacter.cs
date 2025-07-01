using Awaken.TG.MVC;
using Awaken.Utility;
using JetBrains.Annotations;

namespace Awaken.TG.Main.Utility.Skills {
    public partial class DummySkillCharacter : DummyCharacter {
        public override ushort TypeForSerialization => SavedModels.DummySkillCharacter;

        // === Constructing
        DummySkillCharacter() { }
        
        public static DummySkillCharacter GetOrCreateInstance {
            get {
                return Instance ??= World.Add(new DummySkillCharacter());;
            }
        }
        [CanBeNull] public static DummySkillCharacter Instance { get; private set; }

        // === LifeCycle
        protected override void OnDiscard(bool fromDomainDrop) {
            Instance = null;
        }
    }
}

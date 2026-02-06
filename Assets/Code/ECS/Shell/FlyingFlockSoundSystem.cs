using System;
using Unity.Entities;

namespace Awaken.ECS.Flocks {
    public partial class FlyingFlockSoundSystem : SystemBase {
        public int FlyingBirdsCount { get; private set; }
        public int RestingBirdsCount { get; private set; }
        public int TakingOffBirdsCount { get; private set; }

        protected override void OnCreate() {
            throw new NotImplementedException();
        }

        protected override void OnUpdate() {
            throw new NotImplementedException();
        }

        protected override void OnDestroy() {
            throw new NotImplementedException();
        }
    }
}
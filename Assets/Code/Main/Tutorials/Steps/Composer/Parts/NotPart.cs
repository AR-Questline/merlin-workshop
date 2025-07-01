using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Tutorials.Steps.Composer.Parts {
    [Serializable]
    public class NotPart : IStepPart {
        [SerializeReference]
        public IStepPart part = new BasePart();
        public bool IsTutorialBlocker => part?.IsTutorialBlocker ?? false;
        public bool IsConcurrent => false;

        public string Name => "Not";

        public async UniTask<bool> Run(TutorialContext context) {
            if (part == null) {
                return true;
            }
            bool success = await part.Run(context);
            return !success;
        }

        public void TestRun(TutorialContext context) {
            part?.TestRun(context);
        }
    }
}
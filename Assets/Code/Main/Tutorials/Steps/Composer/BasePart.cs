using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Tutorials.Steps.Composer {
    [Serializable]
    public class BasePart : IStepPart {
        [Tooltip("Used only as editor label")][HorizontalGroup("Name")]
        public string name;
        [GUIColor(0.9f,0.9f,1f)][HorizontalGroup("Name")]
        public bool isConcurrent;
        [SerializeReference][ListDrawerSettings(ListElementLabelName = nameof(Name))][LabelText("Parts - Children")]
        public List<IStepPart> parts = new List<IStepPart>();

        public string Name => name;
        public bool IsConcurrent => isConcurrent;
        public virtual bool IsTutorialBlocker => parts.Any(p => p.IsTutorialBlocker);

        public async UniTask<bool> Run(TutorialContext context) {
            bool allSuccess = await OnRun(context);
            
            if (allSuccess) {
                foreach (var part in parts) {
                    if (context.vc == null) {
                        context.Finish();
                        return false;
                    }
                    
                    if (part.IsConcurrent) {
                        RunConcurrent(part, context).Forget();
                    } else {
                        try {
                            bool success = await part.Run(context);
                            allSuccess = allSuccess && success;
                        } catch (Exception e) {
                            Debug.LogException(e);
                        }
                    }
                }
            }

            return allSuccess;
        }

        async UniTaskVoid RunConcurrent(IStepPart part, TutorialContext context) {
            try {
                await part.Run(context);
            } catch (Exception e) {
                Debug.LogException(e);
            }
        }

        public virtual UniTask<bool> OnRun(TutorialContext context) => UniTask.FromResult(true);

        // === Editor
        public virtual void TestRun(TutorialContext context) {
            foreach (var part in parts) {
                part.TestRun(context);
            }
        }
    }
}
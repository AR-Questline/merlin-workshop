using System;
using System.Linq;
using System.Threading.Tasks;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.Utility.Debugging;
using Awaken.Utility.Extensions;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.Tutorials.Steps.Composer {
    public class VCTutorialComposer : ViewComponent, ITutorialStep {
        
        // === References
        public TutKeys enumKey = TutKeys.None;
        public TutorialInputController controller = TutorialInputController.Any;
        public string Key => TutorialKeys.FullKey(enumKey);

        [SerializeReference]
        public IStepPart basePart = new BasePart();
        [SerializeReference]
        public IStepPart accompanyPart;
        [SerializeReference]
        public IStepPart entryCondition;

        [ShowIf("@entryCondition!=null")]
        public bool pollCondition;
        
        public bool CanBePerformed => HasPart(basePart);
        bool _wasAdded;

        // === Attach
        protected override void OnAttach() {
            // check preconditions
            if (TutorialKeys.IsConsumed(enumKey)) return;
            bool isCorrectController = controller == TutorialInputController.Any
                                       || RewiredHelper.IsGamepad && controller.HasFlagFast(TutorialInputController.Gamepad)
                                       || !RewiredHelper.IsGamepad && controller.HasFlagFast(TutorialInputController.MouseKeyboard);
            if (!isCorrectController) return;
            
            // try adding step
            TutorialContext context = new TutorialContext() {
                target = GenericTarget,
                vc = this,
            };
            TryAddTutorialStepEntry(context).Forget();
        }

        async UniTaskVoid TryAddTutorialStepEntry(TutorialContext context) {
            if (!await CanAddEntry(context)) return;

            Refresh();
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelAdded<TutorialBlocker>(), this, Refresh);
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelDiscarded<TutorialBlocker>(), this, Refresh);
        }

        // === Perform
        public TutorialContext Perform(Action onFinish) {
            if (!HasPart(basePart)) {
                return null;
            }

            TutorialContext context = new TutorialContext() {
                target = GenericTarget,
                vc = this,
                onFinish = onFinish,
            };
            Perform(basePart, context);
            
            // add tutorial blocker to prevent other tutorials
            if (basePart?.IsTutorialBlocker ?? false) {
                World.Add(new TutorialBlocker(context));
            }
            
            return context;
        }

        public void Accompany(TutorialContext context) {
            if (!HasPart(accompanyPart)) {
                return;
            }
            Perform(accompanyPart, context);
        }

        void Perform(IStepPart part, TutorialContext context) {
            try {
                part.Run(context).Forget();
            } catch (Exception e) {
                Log.Important?.Error($"Exception happened in {gameObject.name}", gameObject);
                Debug.LogException(e);
            }
        }
        
        // === Refresh Logic
        void Refresh() {
            bool isReadyToPlay = !(basePart is {IsTutorialBlocker: true}) || CanStartPlaying();

            if (!isReadyToPlay && _wasAdded) {
                World.Any<TutorialMaster>()?.RemoveTutorialStep(this);
                _wasAdded = false;
            } else if (isReadyToPlay && !_wasAdded) {
                TutorialMaster master = World.Any<TutorialMaster>();
                if (master != null) {
                    master.AddTutorialStep(this);
                    _wasAdded = true;
                } else {
                    ModelUtils.DoForFirstModelOfType<TutorialMaster>(Refresh, this);
                }
            }
        }
        
        // === Destroy
        protected override void OnDestroy() {
            World.Any<TutorialMaster>()?.RemoveTutorialStep(this);
        }
        
        // === Helpers
        bool CanStartPlaying() {
            if (TutorialKeys.IsConsumed(enumKey)) return false;
            bool noTutorialBlockers = !World.HasAny<TutorialBlocker>();
            return noTutorialBlockers;
        }
        
        bool HasPart(IStepPart part) {
            if (part == null) return false;
            if (part is BasePart bp && (bp.parts == null || !bp.parts.Any())) return false;
            return true;
        }
        
        async Task<bool> CanAddEntry(TutorialContext context) {
            bool canAdd = entryCondition == null || await entryCondition.Run(context);
            if (!canAdd && pollCondition) {
                // we can't add yet, but maybe in future
                canAdd = await PollForAdd(context);
            }
            // nope, neither now nor in future
            if (!canAdd) return false;

            // additional, component-based conditions
            foreach (IUITutorialStepCondition condition in GetComponents<IUITutorialStepCondition>()) {
                if (!condition.CanRun(this)) {
                    return false;
                }
            }

            return true;
        }

        async UniTask<bool> PollForAdd(TutorialContext context) {
            while (this != null && gameObject != null) {
                if (await entryCondition.Run(context)) {
                    return true;
                }
                await UniTask.Delay(500);
            }
            return false;
        }

        // === Test
        TutorialContext _testContext;
        bool HasAccompanyPart => HasPart(accompanyPart);
        
        [Button][ShowIf(nameof(CanBePerformed))]
        void TestRun() {
            _testContext = new TutorialContext() {
                vc = this,
            };
            basePart.TestRun(_testContext);
        }
        
        [Button][ShowIf(nameof(HasAccompanyPart))]
        void TestRunAccompany() {
            _testContext = new TutorialContext() {
                vc = this,
            };
            accompanyPart.TestRun(_testContext);
        }

        [Button][ShowIf("@_testContext != null")]
        void FinishTest() {
            _testContext?.Finish();
            _testContext = null;
        }
    }
}
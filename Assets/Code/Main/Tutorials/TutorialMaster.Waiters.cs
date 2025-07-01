using System;
using System.Linq;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Tutorials.Steps;
using Awaken.TG.Main.Tutorials.Steps.Composer;
using Awaken.TG.Main.Tutorials.TutorialPrompts;
using Awaken.TG.Main.Utility.UI.Keys.Components;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Tutorials {
    public partial class TutorialMaster {
        
        public delegate TutorialWaiter WaiterSpawner(string key);
        
        [UnityEngine.Scripting.Preserve]
        WaiterSpawner OnFirstModelOfType<T>(out Reference<IEventListener> listener, Action action = null) where T : Model {
            var listenerReference = new Reference<IEventListener>();
            listener = listenerReference;
            return key => {
                TutorialWaiter tutorial = TutorialWaiter.TryCreate(key, action);
                if (tutorial != null) {
                    listenerReference.item = ModelUtils.DoForFirstModelOfType<T>(tutorial.Perform, this);
                    tutorial.AddListener(listenerReference.item);
                }

                return tutorial;
            };
        }

        WaiterSpawner AfterCustomEvent<TSource, T>(Event<TSource, T> evt, out Reference<IEventListener> listener, Action action = null) {
            var listenerReference = new Reference<IEventListener>();
            listener = listenerReference;
            return key => {
                TutorialWaiter tutorial = TutorialWaiter.TryCreate(key, action);
                if (tutorial != null) {
                    listenerReference.item = World.EventSystem.ListenTo(EventSelector.AnySource, evt, this, tutorial.Perform);
                    tutorial.AddListener(listenerReference.item);
                }

                return tutorial;
            };
        }
        
        [UnityEngine.Scripting.Preserve]
        WaiterSpawner AfterCustomEvent<TSource, TPayload>(Event<TSource, TPayload> evt, Func<TPayload,bool> evtArgCondition, out Reference<IEventListener> listener, Action action = null) {
            var listenerReference = new Reference<IEventListener>();
            listener = listenerReference;
            return key => {
                TutorialWaiter tutorial = TutorialWaiter.TryCreate(key, action);
                if (tutorial != null) {
                    listenerReference.item = World.EventSystem.ListenTo(EventSelector.AnySource, evt, this, TryPerformTutorial);
                    tutorial.AddListener(listenerReference.item);
                }

                void TryPerformTutorial(TPayload arg) {
                    if (evtArgCondition(arg)) {
                        tutorial.Perform();
                    }
                }

                return tutorial;
            };
        }

        WaiterSpawner AfterCustomEvent<TSource, TPayload>(TSource source, IEvent<TSource, TPayload> evt, out Reference<IEventListener> listener, Action action = null) where TSource : IModel {
            var listenerReference = new Reference<IEventListener>();
            listener = listenerReference;
            return key => {
                TutorialWaiter tutorial = TutorialWaiter.TryCreate(key, action);
                if (tutorial != null) {
                    listenerReference.item = source.ListenTo(evt, _ => tutorial.Perform(), this);
                    tutorial.AddListener(listenerReference.item);
                }
                
                return tutorial;
            };
        }

        WaiterSpawner AfterCustomEvent<TSource, TPayload>(TSource source, IEvent<TSource, TPayload> evt, Func<TPayload, bool> evtArgCondition,
            out Reference<IEventListener> listener, Action action = null) where TSource : IModel {
            var listenerReference = new Reference<IEventListener>();
            listener = listenerReference;
            return key => {
                TutorialWaiter tutorial = TutorialWaiter.TryCreate(key, action);
                if (tutorial != null) {
                    listenerReference.item = source.ListenTo(evt, TryPerformTutorial, this);
                    tutorial.AddListener(listenerReference.item);
                }

                void TryPerformTutorial(TPayload arg) {
                    if (evtArgCondition(arg)) {
                        tutorial.Perform();
                    }
                }

                return tutorial;
            };
        }

        [UnityEngine.Scripting.Preserve]
        WaiterSpawner AfterStatChange(StatType stat, Action action, bool onlyPositive, out Reference<IEventListener> listener) {
            var listenerReference = new Reference<IEventListener>();
            listener = listenerReference;
            return key => {
                TutorialWaiter tutorial = TutorialWaiter.TryCreate(key, action);
                if (tutorial != null) {
                    void TryPerform(Stat.StatChange change) {
                        if (!onlyPositive || change.value > 0f) {
                            tutorial.Perform();
                        }
                    }

                    listenerReference.item = World.EventSystem.ListenTo(EventSelector.AnySource, Stat.Events.StatChangedBy(stat), this, TryPerform);
                    tutorial.AddListener(listenerReference.item);
                }

                return tutorial;
            };
        }
        
        WaiterSpawner RemoveListener(Reference<IEventListener> listener, Action callback = null) {
            return key => {
                TutorialWaiter tutorial = TutorialWaiter.TryCreate(key, callback);
                if (tutorial != null) {
                    if (listener.item != null) {
                        World.EventSystem.RemoveListener(listener.item);
                        listener.item = null;
                    }
                    tutorial.Perform();
                }
                return tutorial;
            };
        }

        WaiterSpawner ShowPrompt(out PromptReference reference, string description, KeyIcon.Data firstKeyData, KeyIcon.Data? secondKeyData = null, Action callback = null) {
            var promptReference = new PromptReference();
            reference = promptReference;
            return key => {
                TutorialWaiter tutorial = TutorialWaiter.TryCreate(key, callback);
                if (tutorial != null) {
                    promptReference.Prompt = TutorialPrompt.Show(description, firstKeyData, secondKeyData);
                    tutorial.Perform();
                }
                return tutorial;
            };
        }

        WaiterSpawner HidePrompt(PromptReference reference, Action callback = null) {
            return key => {
                TutorialWaiter tutorial = TutorialWaiter.TryCreate(key, callback);
                if (tutorial != null) {
                    reference.Prompt?.Discard();
                    reference.Prompt = null;
                    tutorial.Perform();
                }
                return tutorial;
            };
        }
        
        WaiterSpawner Instantly(Action action) {
            return key => {
                TutorialWaiter tutorial = TutorialWaiter.TryCreate(key, action);
                tutorial?.Perform();
                return null;
            };
        }

        public WaiterSpawner RunStep(out Reference<IEventListener> listener, Action action = null) {
            var listenerReference = new Reference<IEventListener>();
            listener = listenerReference;
            return key => {
                var tutorial = TutorialWaiter.TryCreate(key, action);
                if (tutorial == null) return null;

                PerformOrWaitForStep(key, tutorial, listenerReference);
                return tutorial;
            };
        }

        [UnityEngine.Scripting.Preserve]
        // perform waiter when predicate returns TRUE required amount of times with given interval
        WaiterSpawner RunStepOnCondition(Func<bool> predicate, out Reference<IEventListener> listener, int interval = 500, int requiredSuccesses = 1) {
            var listenerReference = new Reference<IEventListener>();
            listener = listenerReference;
            return key => {
                var tutorial = TutorialWaiter.TryCreate(key, null);
                if (tutorial == null) return null;
                
                Poll(predicate, () => PerformOrWaitForStep(key, tutorial, listenerReference), requiredSuccesses, interval).Forget();
                return tutorial;
            };
        }

        [UnityEngine.Scripting.Preserve]
        // perform waiter when predicate returns TRUE required amount of times with given interval
        WaiterSpawner PollingCondition(Func<bool> predicate, int interval = 500, int requiredSuccesses = 1) {
            return key => {
                var waiter = TutorialWaiter.TryCreate(key, null);
                if (waiter != null) {
                    Poll(predicate, () => waiter.Perform(), requiredSuccesses, interval).Forget();
                }
                return waiter;
            };
        }

        [UnityEngine.Scripting.Preserve]
        WaiterSpawner InstantCondition(Func<bool> predicate) {
            return key => {
                var waiter = TutorialWaiter.TryCreate(key, null);
                if (waiter != null && !predicate()) {
                    return TutorialWaiter.BreakExecution;
                }
                return null;
            };
        }

        [UnityEngine.Scripting.Preserve]
        WaiterSpawner Delay(int millisecondsDelay) {
            return key => {
                var waiter = TutorialWaiter.TryCreate(key, null);
                if (waiter != null) {
                    UniTask.Void(async () => {
                        await UniTask.Delay(millisecondsDelay);
                        waiter.Perform();
                    });
                }
                return waiter;
            };
        }
        
        [UnityEngine.Scripting.Preserve]
        WaiterSpawner DelayFrame(int frameCount) {
            return key => {
                var waiter = TutorialWaiter.TryCreate(key, null);
                if (waiter != null) {
                    UniTask.Void(async () => {
                        await UniTask.DelayFrame(frameCount);
                        waiter.Perform();
                    });
                }
                return waiter;
            };
        }

        [UnityEngine.Scripting.Preserve]
        WaiterSpawner AppendCondition(Func<WaiterSpawner> spawner, Func<bool> predicate) {
            return key => {
                if (predicate()) {
                    return spawner()(key);
                } else {
                    return null;
                }
            };
        }

        [UnityEngine.Scripting.Preserve]
        WaiterSpawner Log(string text) {
            return key => {
                Awaken.Utility.Debugging.Log.Critical?.Error(text);
                return null;
            };
        }
        
        [UnityEngine.Scripting.Preserve]
        WaiterSpawner GiveItem(string guid, bool onlyIfNotThereYet = true) {
            return key => {
                var waiter = TutorialWaiter.TryCreate(key, null);
                if (waiter != null) {
                    Hero hero = Hero.Current;
                    ItemTemplate template = TemplatesUtil.Load<ItemTemplate>(guid);

                    bool shouldGive = !onlyIfNotThereYet || hero.Inventory.Items.All(i => i.Template != template);
                    if (shouldGive) {
                        template.ChangeQuantity(hero.Inventory, 1);
                    }
                }

                return null;
            };
        }
        
        // === Helpers

        async UniTaskVoid Poll(Func<bool> predicate, Action onFinish, int requiredSuccesses = 1, int interval = 500) {
            var successes = 0;

            // repeat until done
            while (successes < requiredSuccesses && Application.isPlaying && !WasDiscarded) {
                if (predicate()) {
                    successes++;
                } else {
                    successes = 0;
                }

                if (successes < requiredSuccesses) {
                    await UniTask.Delay(interval);
                }
            }

            // success
            if (successes >= requiredSuccesses) {
                onFinish.Invoke();
            }
        }
        
        void PerformOrWaitForStep(string key, TutorialWaiter tutorial, Reference<IEventListener> listener) {
            bool performed = PerformStep(tutorial, key);
            if (!performed) {
                // wait for step to be created
                listener.item = this.ListenTo(Events.StepAdded, addedKey => {
                    if (addedKey != key) return;
                    performed = PerformStep(tutorial, key);
                    if (performed) {
                        World.EventSystem.RemoveListener(listener.item);
                        listener.item = null;
                    }
                }, this);
            }
        }

        bool PerformStep(TutorialWaiter tutorial, string key) {
            if (!_uiSteps.TryGetValue(key, out var steps) || !steps.Any(s => s.CanBePerformed)) {
                return false;
            }

            if (DebugMode) {
                Awaken.Utility.Debugging.Log.Critical?.Error($"TutorialMaster {key} - Run Step");
            }
            ITutorialStep stepToPerform = steps.First(s => s.CanBePerformed);
            TutorialContext context = stepToPerform.Perform(tutorial.Perform);
            if (context == null && DebugMode) {
                Awaken.Utility.Debugging.Log.Critical?.Error($"TutorialMaster {key} - Abort Run Step");
            }

            foreach (var step in steps.Except(stepToPerform.Yield())) {
                step.Accompany(context);
            }

            return true;
        }
        
        [UnityEngine.Scripting.Preserve]
        Action GraphToAction(TemplateReference graph, Type viewType = null) => () => {
            if (graph == null || !graph.IsSet) {
                return;
            }

            if (StoryBookmark.ToInitialChapter(graph, out var bookmark)) {
                Story.StartStory(StoryConfig.Base(bookmark, viewType));
            }
        };
        
        [UnityEngine.Scripting.Preserve]
        Func<bool> ModelCondition<T>(Func<T, bool> condition) where T : Model {
            return () => World.HasAny<T>() && condition(World.Any<T>());
        }

        class PromptReference {
            public TutorialPrompt Prompt { get; set; }
        }
        
        [UnityEngine.Scripting.Preserve]
        class ListenerReference {
            [UnityEngine.Scripting.Preserve]
            public IEventListener Listener { get; set; }
        }
    }
}
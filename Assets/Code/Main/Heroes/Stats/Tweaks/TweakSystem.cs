using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.UI.Bugs;
using Awaken.TG.MVC;
using Awaken.Utility.Collections;
using Unity.Mathematics;
using UnityEngine.Pool;

namespace Awaken.TG.Main.Heroes.Stats.Tweaks {
    public class TweakSystem : IService {
        // === Properties
        MultiMap<TweakSelector, Tweak> _bySelector = new MultiMap<TweakSelector, Tweak>();
        MultiMap<ITweaker, Tweak> _byTweaker = new MultiMap<ITweaker, Tweak>();
        MultiMap<IWithStats, Tweak> _byOwner = new MultiMap<IWithStats, Tweak>();
        List<List<Tweak>> _tweaksByPriority;

        public TweakSystem() {
            // we group tweaks by priority to avoid stacking multiplications
            // (f.e. when we have baseValue = 10, and two tweaks +50%, we will end with 20, instead of 22.5)
            _tweaksByPriority = new();
            foreach (var _ in Enum.GetValues(typeof(TweakPriority))) {
                _tweaksByPriority.Add(new List<Tweak>());
            }
        }

        // === Adding new tweaks
        public Tweak Tweak(Stat stat, ITweaker tweaker, TweakPriority priority) {
            var selector = new TweakSelector(stat.Owner, stat.Type);
            var tweak = new Tweak(selector, tweaker, priority);
            return AddTweak(tweak);
        }

        Tweak AddTweak(Tweak tweak) {
            // Remove once this bug is resolved
            AutoBugReporting.SendReportIfNotMainThread();
            
            if (!tweak.Selector.StatType.IsTweakable) {
                throw new InvalidOperationException($"{tweak.Selector.StatType.EnumName} is not a tweakable stat.");
            }
            _bySelector.Add(tweak.Selector, tweak);
            _byTweaker.Add(tweak.TweakOwner, tweak);

            _byOwner.Add(tweak.Selector.Model, tweak);
            tweak.Selector.Stat.RecalculateTweaks();

            return tweak;
        }

        // === Removing tweaks

        public void RemoveAllTweaksAttachedTo(IModel model) {
            if (!(model is IWithStats owner)) return;

            // Remove once this bug is resolved
            AutoBugReporting.SendReportIfNotMainThread();
            
            TweakRefreshBatch refreshBatch = GenericPool<TweakRefreshBatch>.Get();

            foreach (Tweak tweak in _byOwner.GetValues(owner, true)) {
                _byTweaker.Remove(tweak.TweakOwner, tweak);
                _bySelector.Remove(tweak.Selector, tweak);

                if (!tweak.Selector.Model.IsBeingDiscarded) {
                    refreshBatch.Add(tweak.Selector.Stat);
                }
            }

            _byOwner.RemoveAll(owner);
            refreshBatch.Trigger();
            GenericPool<TweakRefreshBatch>.Release(refreshBatch);
        }

        public void RemoveAllTweakedBy(IModel model) {
            if (model is not ITweaker tweaker) {
                return;
            }
            // Remove once this bug is resolved
            AutoBugReporting.SendReportIfNotMainThread();
            
            TweakRefreshBatch refreshBatch = GenericPool<TweakRefreshBatch>.Get();

            foreach (Tweak tweak in _byTweaker.GetValues(tweaker, true)) {
                _bySelector.Remove(tweak.Selector, tweak);

                _byOwner.Remove(tweak.Selector.Model, tweak);
                if (!tweak.Selector.Model.IsBeingDiscarded) {
                    refreshBatch.Add(tweak.Selector.Stat);
                }
            }
            _byTweaker.RemoveAll(tweaker);
            refreshBatch.Trigger();
            GenericPool<TweakRefreshBatch>.Release(refreshBatch);
        }

        public void RemoveTweak(Tweak tweak) {
            // Remove once this bug is resolved
            AutoBugReporting.SendReportIfNotMainThread();
            
            TweakRefreshBatch refreshBatch = GenericPool<TweakRefreshBatch>.Get();

            _byTweaker.Remove(tweak.TweakOwner, tweak);
            _bySelector.Remove(tweak.Selector, tweak);

            _byOwner.Remove(tweak.Selector.Model, tweak);
            if (!tweak.Selector.Model.HasBeenDiscarded) {
                refreshBatch.Add(tweak.Selector.Stat);
            }

            refreshBatch.Trigger();
            GenericPool<TweakRefreshBatch>.Release(refreshBatch);
        }

        // Apply all tweaks and return tweaked values
        public float Recalculate(Stat stat) {
            if (stat.Type == null) {
                return 0;
            }

            ClearTweaksCache();

            // gather all tweaks related to this stat
            foreach (TweakSelector selector in SelectorsForStat(stat)) {
                if (_bySelector.TryGetValue(selector, out var selectorTweaks)) {
                    foreach (Tweak tweak in selectorTweaks) {
                        _tweaksByPriority[(int) tweak.Priority].Add(tweak);
                    }
                }
            }

            // perform calculations in correct order
            float baseVal = stat.BaseValue;
            float curVal = baseVal;

            foreach (var tweaksList in _tweaksByPriority) {
                bool wasOverriden = false;
                
                foreach (var tweak in tweaksList) {
                    float tweakResult = tweak.Apply(baseVal);
                    float diff = tweakResult - baseVal;
                    
                    if (tweak.OperationType == OperationType.Override) {
                        // overrides have the highest priority within a tweak priority set for a deterministic result
                        // this means we can ignore all other tweaks in this set if any overrides are present
                        if (!wasOverriden) {
                            curVal = tweakResult;
                            wasOverriden = true;
                        } else if (math.abs(curVal - baseVal) > math.abs(diff)) {
                            // if we have multiple overrides,
                            // we want to keep the one that is the closest to zero for a deterministic result
                            curVal = tweakResult;
                        }
                    } else if (!wasOverriden) {
                        curVal += diff;
                    }
                }

                baseVal = curVal;
                tweaksList.Clear();
            }


            if (stat is LimitedStat ls) {
                if (baseVal > ls.UpperLimit && !ls.AllowOverflow) {
                    baseVal = ls.UpperLimit;
                } else if (baseVal < ls.LowerLimit) {
                    baseVal = ls.LowerLimit;
                }
            }

            return baseVal;
        }

        // === All selectors for given stat

        IEnumerable<TweakSelector> SelectorsForStat(Stat stat) {
            if (stat is CompoundStat compound) {
                foreach (var selector in compound.Stats.SelectMany(SelectorsForStat)) {
                    yield return selector;
                }
            } else {
                yield return new TweakSelector(stat.Owner, stat.Type);
            }
        }

        // === Helpers
        void ClearTweaksCache() {
            for (int i = 0; i < _tweaksByPriority.Count; i++) {
                _tweaksByPriority[i].Clear();
            }
        }
    }
}
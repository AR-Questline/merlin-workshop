using System;
using System.Linq;
using Awaken.TG.Code.Utility;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Timing;
using Awaken.TG.MVC;
using Awaken.Utility.Collections;
using Awaken.Utility.Extensions;
using Sirenix.OdinInspector;

namespace Awaken.TG.Main.Heroes.Fishing {
    [Serializable]
    public struct FishTable {
        public FishWithProbability[] entries;

        public readonly ref readonly FishData GetRandomFish() {
            var localEntries = this.entries.Where(fish => fish.IsCurrentlyAvailable()).ToArray();

            if (!localEntries.IsEmpty()) {
                int index = RandomUtil.WeightedSelect(0, localEntries.Length - 1, i => localEntries[i].GetProbability());
                return ref localEntries[index].data;
                
            }
            
            return ref CommonReferences.Get.FishingData.genericFishTable.GetRandomFish();
        }
    }

    [Serializable]
    public struct FishWithProbability {
        public FishData data;
        public bool overrideProbability;
        public TimeOfDay occurrence;
        [ShowIf(nameof(overrideProbability))] public float probability;

        public float GetProbability() {
            return overrideProbability ? probability : data?.Probability ?? 1;
        }

        public bool IsCurrentlyAvailable() {
            var currentHour = World.Any<GameRealTime>().WeatherTime.Hour;

            TimeOfDay acceptableTimeOfDay = currentHour switch {
                >= 3 and < 9 => TimeOfDay.Morning,
                >= 9 and < 15 => TimeOfDay.Day,
                >= 15 and < 21 => TimeOfDay.Evening,
                >= 21 or < 3 => TimeOfDay.Night
            };

            return occurrence.HasFlagFast(acceptableTimeOfDay);
        }
    }

    public enum FishQuality : byte {
        Garbage,
        Common,
        Uncommon,
        Rare,
        Legendary,
    }

    [Flags]
    public enum TimeOfDay : byte {
        [UnityEngine.Scripting.Preserve] Morning = 1,
        [UnityEngine.Scripting.Preserve] Day = 2,
        [UnityEngine.Scripting.Preserve] Evening = 4,
        [UnityEngine.Scripting.Preserve] Night = 8,
        [UnityEngine.Scripting.Preserve] Always = Morning | Day | Evening | Night
    }
}
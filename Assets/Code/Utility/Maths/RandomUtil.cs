using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Unity.IL2CPP.CompilerServices;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;
using Random = System.Random;

namespace Awaken.TG.Code.Utility {
    [Il2CppEagerStaticClassConstruction]
    public static class RandomUtil {
        static Random rng = new Random();
        
        public static void SetSeed(int seed) {
            rng = new Random(seed);
        }

        public static T UniformSelect<T>(ICollection<T> items, Predicate<T> condition = null) {
            T item;
            bool matches = false;
            int i = 0;
            do {
                int index = rng.Next(items.Count);
                item = items.ElementAt(index);
                matches = condition?.Invoke(item) ?? true;
            } while (!matches && i++ < 1000);

            if (!matches) {
                throw new InfiniteLoopException("UniformSelect couldn't find correct match in 1000 iterations.");
            }
            return item;
        }
        
        public static T UniformSelect<T>(StructList<T> items, Predicate<T> condition = null) {
            T item;
            bool matches = false;
            int i = 0;
            do {
                int index = rng.Next(items.Count);
                item = items[index];
                matches = condition?.Invoke(item) ?? true;
            } while (!matches && i++ < 1000);

            if (!matches) {
                throw new InfiniteLoopException("UniformSelect couldn't find correct match in 1000 iterations.");
            }
            return item;
        }
        
        public static T UniformSelectSafe<T>(IList<T> items, Predicate<T> condition = null) {
            if (condition != null) {
                items = items.Where(i => condition(i)).ToList();
            }
            if (!items.Any()) {
                return default(T);
            }
            return UniformSelect(items);
        }

        public static IEnumerable<T> UniformSelectMultiple<T>(ICollection<T> items, int howMany) {
            if (items.Count < howMany) {
                foreach (var i in items) {
                    yield return i;
                }

                yield break;
            }
            int[] alreadySelected = Enumerable.Repeat(-1, howMany).ToArray();
            for (int i = 0; i < howMany; i++) {
                // look for something not selected
                int selected;
                do {
                    selected = rng.Next(0, items.Count);
                } while (alreadySelected.Contains(selected));
                // got an item
                alreadySelected[i] = selected;
                yield return items.ElementAt(selected);
            }
        }

        public static int WeightedSelect(int lowInclusive, int highInclusive, Func<int, float> weightFn) {
            float totalWeight = 0f;
            for (int i = lowInclusive; i <= highInclusive; i++) {
                totalWeight += weightFn(i);
            }

            if (totalWeight == 0) {
                Log.Important?.Warning("Sum of weights equals 0!");
                return RandomUtil.UniformInt(lowInclusive, highInclusive);
            }
            float randomValue = (float) (rng.NextDouble() * totalWeight);
            float cutoff = 0f;
            for (int i = lowInclusive; i <= highInclusive; i++) {
                cutoff += weightFn(i);
                if (randomValue <= cutoff) return i;
            }
            // fallback
            return highInclusive;
        }

        public static T WeightedSelect<T>(ICollection<T> collection, Func<T, float> weightFn) {
            int index = WeightedSelect(0, collection.Count - 1, (i) => weightFn(collection.ElementAt(i)));
            return collection.ElementAt(index);
        }

        public static T WeightedSelect<T>(StructList<T> collection, Func<T, float> weightFn) {
            int index = WeightedSelect(0, collection.Count - 1, (i) => weightFn(collection[i]));
            return collection[index];
        }
        
        public static IEnumerable<T> WeightedShuffle<T>(IEnumerable<T> collection, Func<T, float> weightFn) {
            var collectionArray = collection.ToArray();
            while (collectionArray.Length > 0) {
                var select = WeightedSelect(collectionArray, weightFn);
                collectionArray = collectionArray.Except(select.Yield()).ToArray();
                yield return select;
            }
        }

        public static int UniformInt(int lowInclusive, int highInclusive) {
            return lowInclusive + rng.Next(highInclusive - lowInclusive + 1);
        }

        public static uint UniformUInt(uint lowInclusive, uint highInclusive) {
            return lowInclusive + (uint)rng.Next((int)(highInclusive - lowInclusive + 1));
        }
        
        public static int UniformInt(int lowInclusive, int highInclusive, Random customRandom) {
            return lowInclusive + customRandom.Next(highInclusive - lowInclusive + 1);
        }

        public static float UniformFloat(float lowInclusive, float highExclusive) {
            return lowInclusive + (float)rng.NextDouble() * (highExclusive - lowInclusive);
        }
        
        public static float UniformFloat(float lowInclusive, float highExclusive, Random customRandom) {
            return lowInclusive + (float)customRandom.NextDouble() * (highExclusive - lowInclusive);
        }
        
        public static float NormalDistribution(float mi, float sigma, bool clamp = true) {
            float invCdf = (float) Awaken.Utility.Maths.NormalDistribution.InvCDF(mi, sigma, rng.NextDouble());
            return clamp ? Mathf.Clamp(invCdf, 0, 100) : invCdf;
        }

        public static bool WithProbability(float probability) {
            return rng.NextDouble() < probability;
        }

        public static int RandomSign() {
            return WithProbability(0.5f) ? 1 : -1;
        }

        public static IEnumerator<float> HaltonSequence(int prime) {
            for (int index = 10;; index++) {
                int i = index;
                float f = 1;
                float r = 0;

                while (i > 0) {
                    f /= prime;
                    r += f * (i % prime);
                    i /= prime;
                }

                yield return r;
            }
        }

        /// <summary>
        /// Uses hardcoded values (5, 11) that are known to be good for 2D halton sequence.
        /// </summary>
        public static IEnumerator<Vector2> HaltonSequence2D() {
            return HaltonSequence2D(5, 11);
        }

        public static IEnumerator<Vector2> HaltonSequence2D(int prime1, int prime2) {
            IEnumerator<float> xE = HaltonSequence(prime1);
            IEnumerator<float> yE = HaltonSequence(prime2);
            while (true) {
                xE.MoveNext();
                yE.MoveNext();
                yield return new Vector2(xE.Current, yE.Current);
            }
        }

        public static T ProbabilitySelect<T>(ICollection<T> collection, Func<T, float> probabilityFn) where T : class {
            var randomValue = (float)rng.NextDouble();
            int index = 0;
            int maxIndex = collection.Count;
            while (index < maxIndex && randomValue - probabilityFn(collection.ElementAt(index)) >= 0) {
                randomValue -= probabilityFn(collection.ElementAt(index));
                ++index;
            }
            return index == maxIndex ? null : collection.ElementAt(index);
        }

        public static Vector2 OnUnitCircle() {
            float angle = (float)rng.NextDouble() * Mathf.PI * 2;
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }
    }

    public class InfiniteLoopException : Exception {
        public InfiniteLoopException(string msg) : base(msg) { }
    }
}

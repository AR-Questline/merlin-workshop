using System;
using UnityEngine;

namespace Awaken.TG.Utility {
    public static class M {
        public const float Epsilon =
#if UNITY_PS5
            1.17549435E-38f;
#else
            float.Epsilon;
#endif
        
        /// <summary>
        /// Returns the middle one of three provided values.
        /// </summary>
        public static int Mid(int a, int b, int c) {
            if (a < b) {
                if (c < a) return a;
                return (b < c) ? b : c;
            } else {
                if (c < b) return b;
                return (a < c) ? a : c;
            }
        }

        /// <summary>
        /// Returns the middle one of three provided values.
        /// </summary>
        public static float Mid(float a, float b, float c) {
            if (a < b) {
                if (c < a) return a;
                return (b < c) ? b : c;
            } else {
                if (c < b) return b;
                return (a < c) ? a : c;
            }
        }

        /// <summary>
        /// Returns the middle one of three provided values.
        /// </summary>
        public static T Mid<T>(T a, T b, T c) where T : IComparable<T> {
            if (a.CompareTo(b) < 0) {
                if (c.CompareTo(a) < 0) return a;
                return (b.CompareTo(c) < 0) ? b : c;
            } else {
                if (c.CompareTo(b) < 0) return b;
                return (a.CompareTo(c) < 0) ? a : c;
            }
        }

        /// <summary>
        /// Lerps towards a target value, taking into account delta time for
        /// a smooth, reproducible transition regardless of frame rate.
        /// https://www.rorydriscoll.com/2016/03/07/frame-rate-independent-damping-using-lerp/
        /// </summary>
        /// <param name="originalValue">value to track from</param>
        /// <param name="targetValue">value to change towards</param>
        /// <param name="speed">speed of change</param>
        /// <param name="smoothing">smoothing of lerp (must be between 0 - 0.99f range)</param>
        /// <param name="deltaTime">current deltaTime</param>
        /// <returns>new lerped value</returns>
        public static float FrameAccurateLerpTo(float originalValue, float targetValue, float deltaTime, float speed = 1, float smoothing = 0.75f) {
            return Mathf.Lerp(originalValue, targetValue, (1 - Mathf.Pow(smoothing, deltaTime)) * speed);
        }
        
        /// <inheritdoc cref="FrameAccurateLerpTo(float,float,float,float,float)"/>
        public static Vector3 FrameAccurateLerpTo(Vector3 originalValue, Vector3 targetValue, float deltaTime, float speed = 1, float smoothing = 0.75f) {
            return Vector3.Lerp(originalValue, targetValue, (1 - Mathf.Pow(smoothing, deltaTime)) * speed);
        }
        
        public static uint GreatestCommonDivisor(uint a, uint b)
        {
            while (a != 0 && b != 0)
            {
                if (a > b)
                    a %= b;
                else
                    b %= a;
            }

            return a == 0 ? b : a;
        }

        /// <summary>
        /// Remaps value from the range (from1 - to1). To a corresponding value in (from2 - to2)
        /// </summary>
        /// <param name="value">the value to remap</param>
        /// <param name="minOriginalValue">lower boundary of input value</param>
        /// <param name="maxOriginalValue">upper boundary of input value</param>
        /// <param name="minRemappedValue">lower boundary of output value</param>
        /// <param name="maxRemappedValue">upper boundary of output value</param>
        /// <param name="clamp">should the result be clamped to output boundaries</param>
        /// <returns></returns>
        public static float Remap (this float value, float minOriginalValue, float maxOriginalValue, float minRemappedValue, float maxRemappedValue, bool clamp = false) {
            float t = (value - minOriginalValue) / (maxOriginalValue - minOriginalValue); // Normalizes value to 0-1 range
            if (clamp) {
                if (t > 1f) return maxRemappedValue;
                if (t < 0f) return minRemappedValue;
            }
            return minRemappedValue + (maxRemappedValue - minRemappedValue) * t; // Applies final range
        }

        public static float RemapInt(this int value, int minOriginalValue, int maxOriginalValue, float minRemappedValue, float maxRemappedValue, bool clamp = false) {
            return Remap(value, minOriginalValue, maxOriginalValue, minRemappedValue, maxRemappedValue, clamp);
        }
        
        public static float Remap01(this float value, float minRemappedValue, float maxRemappedValue, bool clamp = false) {
            return Remap(value, 0, 1, minRemappedValue, maxRemappedValue, clamp);
        }
        
        public static float RemapTo01(this float value, float minOriginalValue, float maxOriginalValue, bool clamp = false) {
            return Remap(value, minOriginalValue, maxOriginalValue, 0, 1, clamp);
        }

        public static Vector2 ToHorizontal2(this Vector3 vector) {
            return new Vector2(vector.x, vector.z);
        }

        public static Vector3 ToHorizontal3(this Vector2 vector) {
            return new Vector3(vector.x, 0, vector.y);
        }
        public static Vector3 ToHorizontal3(this Vector3 vector) {
            return new Vector3(vector.x, 0, vector.z);
        }

        public static int Squared(this int x) {
            return x * x;
        }
        public static float Squared(this float x) {
            return x * x;
        }

        public static bool OutsideOfRange(this float value, float min, float max) {
            return value > max || value < min;
        }

        public static string HumanReadableBytes(long byteCount) {
            return HumanReadableBytes((ulong)byteCount);
        }

        public static string HumanReadableBytes(float byteCount) {
            return HumanReadableBytes((ulong)byteCount);
        }
        
        public static string HumanReadableBytes(ulong byteCount) {
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
            if (byteCount == 0) return "0" + suf[0];
            int place = Convert.ToInt32(Math.Floor(Math.Log(byteCount, 1024)));
            double num = Math.Round(byteCount / Math.Pow(1024, place), 1);
            return $"{num:f1} {suf[place]}";
        }

        /// <summary>
        /// n-arty cartesian product <br/> 
        /// Returns array of variations of elements so that exactly one element from each set is present in each one
        /// </summary>
        public static T[][] CartesianProduct<T>(T[][] arrayOfSets) {
            int variationLength = arrayOfSets.Length;
            int variationsCount = 1;
            foreach (var variants in arrayOfSets) {
                variationsCount *= variants.Length;
            }
            
            var variations = new T[variationsCount][];
            var reusableVariation = new T[variationLength];
            
            int variationIndex = 0;
            FillVariationsWithFixedBeginning(0);
            return variations;
            
            // fills variations with all variations with fixed first n preprocessors
            void FillVariationsWithFixedBeginning(int n) {
                if (n == variationLength) {
                    var variation = new T[variationLength];
                    reusableVariation.CopyTo(variation, 0);
                    variations[variationIndex++] = variation;
                    return;
                }
                for (int i = 0; i < arrayOfSets[n].Length; i++) {
                    reusableVariation[n] = arrayOfSets[n][i];
                    FillVariationsWithFixedBeginning(n + 1);
                }
            }
        }
        
        public static long NewtonBinomial(int n, int k) {
            long result = 1;
            int i;

            for (i = 1; i <= k; i++) {
                result = result * (n - i + 1) / i;
            }

            return result;
        }
        
        public static float DistanceToRay(Ray ray, Vector3 point) {
            return Vector3.Cross(ray.direction, point - ray.origin).magnitude;
        }
        
        /// <summary>
        /// Estimates the unbiased population standard deviation from the provided samples.
        /// On a dataset of size N will use an N-1 normalizer (Bessel's correction).
        /// Returns NaN if data has less than two entries or if any entry is NaN.
        /// </summary>
        /// <param name="samples">A subset of samples, sampled from the full population.</param>
        public static double StandardDeviation(float[] samples)
        {
            return Math.Sqrt(Variance(samples));
        }
        
        /// <summary>
        /// Estimates the unbiased population variance from the provided samples as unsorted array.
        /// On a dataset of size N will use an N-1 normalizer (Bessel's correction).
        /// Returns NaN if data has less than two entries or if any entry is NaN.
        /// </summary>
        /// <param name="samples">Sample array, no sorting is assumed.</param>
        public static float Variance(float[] samples)
        {
            if (samples.Length <= 1)
                return float.NaN;
            float num1 = 0f;
            float sample = samples[0];
            for (int index = 1; index < samples.Length; ++index)
            {
                sample += samples[index];
                float num2 = (index + 1) * samples[index] - sample;
                num1 += num2 * num2 / ((index + 1f) * index);
            }
            return num1 / (samples.Length - 1);
        }
        
        public static float MergeMultipliers(float multiplier1, float multiplier2) => multiplier1 + multiplier2 - 1;
    }
}

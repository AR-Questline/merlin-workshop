// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

using System;
using static UnityEngine.Mathf;
using NormalizedDelegate = System.Func<float, float>;

namespace Animancer
{
    /// <summary>A set of common <see href="https://easings.net">easing functions</see>.</summary>
    /// <remarks>
    /// There are several different types of functions:
    /// <list type="bullet">
    ///   <item>In: accelerating from zero velocity.</item>
    ///   <item>Out: decelerating to zero velocity.</item>
    ///   <item>InOut: uses the corresponding In function until halfway, then the Out function after that.</item>
    ///   <item>Normalized: methods with a single parameter (<see cref="float"/> value) expect values from 0 to 1.</item>
    ///   <item>Ranged: methods with 3 parameters (<see cref="float"/> start, <see cref="float"/> end,
    ///     <see cref="float"/> value) use the specified range instead ot 0 to 1.</item>
    ///   <item>Derivative: calculates the gradient of their corresponding non-derivative function.
    ///     The more complex derivative functions were made with 'https://www.derivative-calculator.net'.</item>
    /// </list>
    /// </remarks>
    /// https://kybernetik.com.au/animancer/api/Animancer/Easing
    /// 
    public static class Easing
    {
        /************************************************************************************************************************/
        #region Delegates
        /************************************************************************************************************************/

        /// <summary>The natural log of 2.</summary>
        public const float Ln2 = 0.693147180559945f;

        /************************************************************************************************************************/

        /// <summary>A variant of a <see cref="NormalizedDelegate"/> with a custom range instead of 0 to 1.</summary>
        public delegate float RangedDelegate(float start, float end, float value);

        /************************************************************************************************************************/

        /// <summary>The name of an easing function.</summary>
        /// <remarks>The <see cref="Easing"/> class contains various extension methods for this enum.</remarks>
        /// https://kybernetik.com.au/animancer/api/Animancer/Function
        /// 
        public enum Function
        {
            /// <summary><see cref="Easing.Linear(float)"/></summary>
            Linear,

            /// <summary><see cref="Quadratic.In(float)"/></summary>
            QuadraticIn,
            /// <summary><see cref="Quadratic.Out(float)"/></summary>
            QuadraticOut,
            /// <summary><see cref="Quadratic.InOut(float)"/></summary>
            QuadraticInOut,

            /// <summary><see cref="Cubic.In(float)"/></summary>
            CubicIn,
            /// <summary><see cref="Cubic.Out(float)"/></summary>
            CubicOut,
            /// <summary><see cref="Cubic.InOut(float)"/></summary>
            CubicInOut,

            /// <summary><see cref="Quartic.In(float)"/></summary>
            QuarticIn,
            /// <summary><see cref="Quartic.Out(float)"/></summary>
            QuarticOut,
            /// <summary><see cref="Quartic.InOut(float)"/></summary>
            QuarticInOut,

            /// <summary><see cref="Quintic.In(float)"/></summary>
            QuinticIn,
            /// <summary><see cref="Quintic.Out(float)"/></summary>
            QuinticOut,
            /// <summary><see cref="Quintic.InOut(float)"/></summary>
            QuinticInOut,

            /// <summary><see cref="Sine.In(float)"/></summary>
            SineIn,
            /// <summary><see cref="Sine.Out(float)"/></summary>
            SineOut,
            /// <summary><see cref="Sine.InOut(float)"/></summary>
            SineInOut,

            /// <summary><see cref="Exponential.In(float)"/></summary>
            ExponentialIn,
            /// <summary><see cref="Exponential.Out(float)"/></summary>
            ExponentialOut,
            /// <summary><see cref="Exponential.InOut(float)"/></summary>
            ExponentialInOut,

            /// <summary><see cref="Circular.In(float)"/></summary>
            CircularIn,
            /// <summary><see cref="Circular.Out(float)"/></summary>
            CircularOut,
            /// <summary><see cref="Circular.InOut(float)"/></summary>
            CircularInOut,

            /// <summary><see cref="Back.In(float)"/></summary>
            BackIn,
            /// <summary><see cref="Back.Out(float)"/></summary>
            BackOut,
            /// <summary><see cref="Back.InOut(float)"/></summary>
            BackInOut,

            /// <summary><see cref="Bounce.In(float)"/></summary>
            BounceIn,
            /// <summary><see cref="Bounce.Out(float)"/></summary>
            BounceOut,
            /// <summary><see cref="Bounce.InOut(float)"/></summary>
            BounceInOut,

            /// <summary><see cref="Elastic.In(float)"/></summary>
            ElasticIn,
            /// <summary><see cref="Elastic.Out(float)"/></summary>
            ElasticOut,
            /// <summary><see cref="Elastic.InOut(float)"/></summary>
            ElasticInOut,
        }

        /// <summary>The total number of <see cref="Function"/> values.</summary>
        public const int FunctionCount = (int)Function.ElasticInOut + 1;

        /************************************************************************************************************************/

        private static NormalizedDelegate[] _FunctionDelegates;

        /// <summary>[Animancer Extension]
        /// Returns a cached delegate representing the specified `function` with a normalized range.
        /// </summary>
        public static NormalizedDelegate GetDelegate(this Function function)
        {
            return default;
        }

        /************************************************************************************************************************/

        private static NormalizedDelegate[] _DerivativeDelegates;

        /// <summary>[Animancer Extension]
        /// Returns a cached delegate representing the derivative of the specified `function` with a normalized range.
        /// </summary>
        public static NormalizedDelegate GetDerivativeDelegate(this Function function)
        {
            return default;
        }

        /************************************************************************************************************************/

        private static RangedDelegate[] _RangedFunctionDelegates;

        /// <summary>[Animancer Extension]
        /// Returns a cached delegate representing the specified `function` with a custom range.
        /// </summary>
        public static RangedDelegate GetRangedDelegate(this Function function)
        {
            return default;
        }

        /************************************************************************************************************************/

        private static RangedDelegate[] _RangedDerivativeDelegates;

        /// <summary>[Animancer Extension]
        /// Returns a cached delegate representing the derivative of the specified `function` with a custom range.
        /// </summary>
        public static RangedDelegate GetRangedDerivativeDelegate(this Function function)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>Returns a linearly interpolated value between the `start` and `end` based on a normalized `value`.</summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item><c>value = 0</c> returns <c>start</c>.</item>
        /// <item><c>value = 0.5</c> returns <c>(start + end) / 2</c>.</item>
        /// <item><c>value = 1</c> returns <c>end</c>.</item>
        /// </list>
        /// This method is identical to <see cref="LerpUnclamped"/>.
        /// </remarks>
        public static float Lerp(float start, float end, float value) => start + (end - start) * value;

        /// <summary>Returns a normalized value indicating how far the `value` is between the `start` and `end`.</summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item><c>value = start</c> returns <c>0</c>.</item>
        /// <item><c>value = (start + end) / 2</c> returns <c>0.5</c>.</item>
        /// <item><c>value = end</c> returns <c>1</c>.</item>
        /// <item><c>start = end</c> returns <c>0</c>.</item>
        /// </list>
        /// This method is like <see cref="InverseLerp"/> except that it doesn't clamp the result between 0 and 1.
        /// </remarks>
        public static float UnLerp(float start, float end, float value) => start == end ? 0 : (value - start) / (end - start);

        /************************************************************************************************************************/

        /// <summary>Re-scales the result of the `function` to use a custom range instead of 0 to 1.</summary>
        public static float ReScale(float start, float end, float value, NormalizedDelegate function)
            => Lerp(start, end, function(UnLerp(start, end, value)));

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Linear
        /************************************************************************************************************************/

        /// <summary>Directly returns the `value`. Interpolates the `value` based on the line <c>y = x</c>.</summary>
        public static float Linear(float value) => value;

        /************************************************************************************************************************/

        /// <summary>Returns 1. The derivative of <see cref="Linear(float)"/>.</summary>
        public static float LinearDerivative(float value) => 1;

        /************************************************************************************************************************/

        /// <summary>Directly returns the `value`. Interpolates the `value` based on the line <c>y = x</c>.</summary>
        public static float Linear(float start, float end, float value) => value;

        /************************************************************************************************************************/

        /// <summary>Returns <c>end - start</c>. The derivative of <see cref="Linear(float, float, float)"/>.</summary>
        public static float LinearDerivative(float start, float end, float value) => end - start;

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Quadratic
        /************************************************************************************************************************/

        /// <summary>Functions based on quadratic equations (<c>x^2</c>).</summary>
        /// https://kybernetik.com.au/animancer/api/Animancer/Quadratic
        /// 
        public static class Quadratic
        {
            /************************************************************************************************************************/

            /// <summary>Interpolates the `value` based on the line <c>y = x^2</c>.</summary>
            /// <remarks><see href="https://easings.net/#easeInQuad">Easings.net has a graph of this function.</see></remarks>
            public static float In(float value) => value * value;

            /// <summary>Interpolates the `value` based on the line <c>y = 1 - (x - 1)^2</c>.</summary>
            /// <remarks><see href="https://easings.net/#easeOutQuad">Easings.net has a graph of this function.</see></remarks>
            public static float Out(float value)
            {
                return default;
            }

            /// <summary>Interpolate using <see cref="In"/> (0 to 0.5) or <see cref="Out"/> (0.5 to 1).</summary>
            /// <remarks><see href="https://easings.net/#easeInOutQuad">Easings.net has a graph of this function.</see></remarks>
            public static float InOut(float value)
            {
                return default;
            }

            /************************************************************************************************************************/

            /// <summary>Returns the derivative of <see cref="In(float)"/> (<c>y = 2x</c>).</summary>
            public static float InDerivative(float value) => 2 * value;

            /// <summary>Returns the derivative of <see cref="Out(float)"/> (<c>y = -2x + 2</c>).</summary>
            public static float OutDerivative(float value) => 2 - 2 * value;

            /// <summary>Returns the derivative of <see cref="InOut(float)"/>.</summary>
            public static float InOutDerivative(float value)
            {
                return default;
            }

            /************************************************************************************************************************/
            // Ranged Variants.
            /************************************************************************************************************************/

            /// <summary>A variant of <see cref="In(float)"/> with a custom range instead of 0 to 1.</summary>
            public static float In(float start, float end, float value) => Lerp(start, end, In(UnLerp(start, end, value)));
            /// <summary>A variant of <see cref="Out(float)"/> with a custom range instead of 0 to 1.</summary>
            public static float Out(float start, float end, float value) => Lerp(start, end, Out(UnLerp(start, end, value)));
            /// <summary>A variant of <see cref="InOut(float)"/> with a custom range instead of 0 to 1.</summary>
            public static float InOut(float start, float end, float value) => Lerp(start, end, InOut(UnLerp(start, end, value)));

            /// <summary>A variant of <see cref="InDerivative(float)"/> with a custom range instead of 0 to 1.</summary>
            public static float InDerivative(float start, float end, float value) => InDerivative(UnLerp(start, end, value)) * (end - start);
            /// <summary>A variant of <see cref="InDerivative(float)"/> with a custom range instead of 0 to 1.</summary>
            public static float OutDerivative(float start, float end, float value) => OutDerivative(UnLerp(start, end, value)) * (end - start);
            /// <summary>A variant of <see cref="InDerivative(float)"/> with a custom range instead of 0 to 1.</summary>
            public static float InOutDerivative(float start, float end, float value) => InOutDerivative(UnLerp(start, end, value)) * (end - start);

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Cubic
        /************************************************************************************************************************/

        /// <summary>Functions based on cubic equations (<c>x^3</c>).</summary>
        /// https://kybernetik.com.au/animancer/api/Animancer/Cubic
        /// 
        public static class Cubic
        {
            /************************************************************************************************************************/

            /// <summary>Interpolates the `value` based on the line <c>y = x^3</c>.</summary>
            /// <remarks><see href="https://easings.net/#easeInCubic">Easings.net has a graph of this function.</see></remarks>
            public static float In(float value) => value * value * value;

            /// <summary>Interpolates the `value` based on the line <c>y = 1 + (x - 1)^3</c>.</summary>
            /// <remarks><see href="https://easings.net/#easeOutCubic">Easings.net has a graph of this function.</see></remarks>
            public static float Out(float value)
            {
                return default;
            }

            /// <summary>Interpolate using <see cref="In"/> (0 to 0.5) or <see cref="Out"/> (0.5 to 1).</summary>
            /// <remarks><see href="https://easings.net/#easeInOutCubic">Easings.net has a graph of this function.</see></remarks>
            public static float InOut(float value)
            {
                return default;
            }

            /************************************************************************************************************************/

            /// <summary>Returns the derivative of <see cref="In(float)"/> (<c>y = 3x</c>).</summary>
            public static float InDerivative(float value) => 3 * value * value;

            /// <summary>Returns the derivative of <see cref="Out(float)"/> (<c>y = 3 * (x - 1)</c>).</summary>
            public static float OutDerivative(float value)
            {
                return default;
            }

            /// <summary>Returns the derivative of <see cref="InOut(float)"/>.</summary>
            public static float InOutDerivative(float value)
            {
                return default;
            }

            /************************************************************************************************************************/
            // Ranged Variants.
            /************************************************************************************************************************/

            /// <summary>A variant of <see cref="In(float)"/> with a custom range instead of 0 to 1.</summary>
            public static float In(float start, float end, float value) => Lerp(start, end, In(UnLerp(start, end, value)));
            /// <summary>A variant of <see cref="Out(float)"/> with a custom range instead of 0 to 1.</summary>
            public static float Out(float start, float end, float value) => Lerp(start, end, Out(UnLerp(start, end, value)));
            /// <summary>A variant of <see cref="InOut(float)"/> with a custom range instead of 0 to 1.</summary>
            public static float InOut(float start, float end, float value) => Lerp(start, end, InOut(UnLerp(start, end, value)));

            /// <summary>A variant of <see cref="InDerivative(float)"/> with a custom range instead of 0 to 1.</summary>
            public static float InDerivative(float start, float end, float value) => InDerivative(UnLerp(start, end, value)) * (end - start);
            /// <summary>A variant of <see cref="InDerivative(float)"/> with a custom range instead of 0 to 1.</summary>
            public static float OutDerivative(float start, float end, float value) => OutDerivative(UnLerp(start, end, value)) * (end - start);
            /// <summary>A variant of <see cref="InDerivative(float)"/> with a custom range instead of 0 to 1.</summary>
            public static float InOutDerivative(float start, float end, float value) => InOutDerivative(UnLerp(start, end, value)) * (end - start);

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Quartic
        /************************************************************************************************************************/

        /// <summary>Functions based on quartic equations (<c>x^4</c>).</summary>
        /// https://kybernetik.com.au/animancer/api/Animancer/Quartic
        /// 
        public static class Quartic
        {
            /************************************************************************************************************************/

            /// <summary>Interpolates the `value` based on the line <c>y = x^4</c>.</summary>
            /// <remarks><see href="https://easings.net/#easeInQuart">Easings.net has a graph of this function.</see></remarks>
            public static float In(float value) => value * value * value * value;

            /// <summary>Interpolates the `value` based on the line <c>y = 1 - (x - 1)^4</c>.</summary>
            /// <remarks><see href="https://easings.net/#easeOutQuart">Easings.net has a graph of this function.</see></remarks>
            public static float Out(float value)
            {
                return default;
            }

            /// <summary>Interpolate using <see cref="In"/> (0 to 0.5) or <see cref="Out"/> (0.5 to 1).</summary>
            /// <remarks><see href="https://easings.net/#easeInOutQuart">Easings.net has a graph of this function.</see></remarks>
            public static float InOut(float value)
            {
                return default;
            }

            /************************************************************************************************************************/

            /// <summary>Returns the derivative of <see cref="In(float)"/> (<c>y = 4x</c>).</summary>
            public static float InDerivative(float value) => 4 * value * value * value;

            /// <summary>Returns the derivative of <see cref="Out(float)"/> (<c>y = -4 * (x - 1)</c>).</summary>
            public static float OutDerivative(float value)
            {
                return default;
            }

            /// <summary>Returns the derivative of <see cref="InOut(float)"/>.</summary>
            public static float InOutDerivative(float value)
            {
                return default;
            }

            /************************************************************************************************************************/
            // Ranged Variants.
            /************************************************************************************************************************/

            /// <summary>A variant of <see cref="In(float)"/> with a custom range instead of 0 to 1.</summary>
            public static float In(float start, float end, float value) => Lerp(start, end, In(UnLerp(start, end, value)));
            /// <summary>A variant of <see cref="Out(float)"/> with a custom range instead of 0 to 1.</summary>
            public static float Out(float start, float end, float value) => Lerp(start, end, Out(UnLerp(start, end, value)));
            /// <summary>A variant of <see cref="InOut(float)"/> with a custom range instead of 0 to 1.</summary>
            public static float InOut(float start, float end, float value) => Lerp(start, end, InOut(UnLerp(start, end, value)));

            /// <summary>A variant of <see cref="InDerivative(float)"/> with a custom range instead of 0 to 1.</summary>
            public static float InDerivative(float start, float end, float value) => InDerivative(UnLerp(start, end, value)) * (end - start);
            /// <summary>A variant of <see cref="InDerivative(float)"/> with a custom range instead of 0 to 1.</summary>
            public static float OutDerivative(float start, float end, float value) => OutDerivative(UnLerp(start, end, value)) * (end - start);
            /// <summary>A variant of <see cref="InDerivative(float)"/> with a custom range instead of 0 to 1.</summary>
            public static float InOutDerivative(float start, float end, float value) => InOutDerivative(UnLerp(start, end, value)) * (end - start);

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Quintic
        /************************************************************************************************************************/

        /// <summary>Functions based on quintic equations (<c>x^5</c>).</summary>
        /// https://kybernetik.com.au/animancer/api/Animancer/Quintic
        /// 
        public static class Quintic
        {
            /************************************************************************************************************************/

            /// <summary>Interpolates the `value` based on the line <c>y = x^5</c>.</summary>
            /// <remarks><see href="https://easings.net/#easeInQuint">Easings.net has a graph of this function.</see></remarks>
            public static float In(float value) => value * value * value * value * value;

            /// <summary>Interpolates the `value` based on the line <c>y = 1 + (x - 1)^5</c>.</summary>
            /// <remarks><see href="https://easings.net/#easeOutQuint">Easings.net has a graph of this function.</see></remarks>
            public static float Out(float value)
            {
                return default;
            }

            /// <summary>Interpolate using <see cref="In"/> (0 to 0.5) or <see cref="Out"/> (0.5 to 1).</summary>
            /// <remarks><see href="https://easings.net/#easeInOutQuint">Easings.net has a graph of this function.</see></remarks>
            public static float InOut(float value)
            {
                return default;
            }

            /************************************************************************************************************************/

            /// <summary>Returns the derivative of <see cref="In(float)"/> (<c>y = 5x</c>).</summary>
            public static float InDerivative(float value) => 5 * value * value * value * value;

            /// <summary>Returns the derivative of <see cref="Out(float)"/> (<c>y = -5 * (x - 1)</c>).</summary>
            public static float OutDerivative(float value)
            {
                return default;
            }

            /// <summary>Returns the derivative of <see cref="InOut(float)"/>.</summary>
            public static float InOutDerivative(float value)
            {
                return default;
            }

            /************************************************************************************************************************/
            // Ranged Variants.
            /************************************************************************************************************************/

            /// <summary>A variant of <see cref="In(float)"/> with a custom range instead of 0 to 1.</summary>
            public static float In(float start, float end, float value) => Lerp(start, end, In(UnLerp(start, end, value)));
            /// <summary>A variant of <see cref="Out(float)"/> with a custom range instead of 0 to 1.</summary>
            public static float Out(float start, float end, float value) => Lerp(start, end, Out(UnLerp(start, end, value)));
            /// <summary>A variant of <see cref="InOut(float)"/> with a custom range instead of 0 to 1.</summary>
            public static float InOut(float start, float end, float value) => Lerp(start, end, InOut(UnLerp(start, end, value)));

            /// <summary>A variant of <see cref="InDerivative(float)"/> with a custom range instead of 0 to 1.</summary>
            public static float InDerivative(float start, float end, float value) => InDerivative(UnLerp(start, end, value)) * (end - start);
            /// <summary>A variant of <see cref="InDerivative(float)"/> with a custom range instead of 0 to 1.</summary>
            public static float OutDerivative(float start, float end, float value) => OutDerivative(UnLerp(start, end, value)) * (end - start);
            /// <summary>A variant of <see cref="InDerivative(float)"/> with a custom range instead of 0 to 1.</summary>
            public static float InOutDerivative(float start, float end, float value) => InOutDerivative(UnLerp(start, end, value)) * (end - start);

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Sine
        /************************************************************************************************************************/

        /// <summary>Functions based on sinusoidal equations.</summary>
        /// https://kybernetik.com.au/animancer/api/Animancer/Sine
        /// 
        public static class Sine
        {
            /************************************************************************************************************************/

            /// <summary>Interpolates the `value` based on a quarter-cycle of a sine wave.</summary>
            /// <remarks><see href="https://easings.net/#easeInSine">Easings.net has a graph of this function.</see></remarks>
            public static float In(float value) => -Cos(value * (PI * 0.5f)) + 1;

            /// <summary>Interpolates the `value` based on a quarter-cycle of a sine wave.</summary>
            /// <remarks><see href="https://easings.net/#easeOutSine">Easings.net has a graph of this function.</see></remarks>
            public static float Out(float value) => Sin(value * (PI * 0.5f));

            /// <summary>Interpolate using <see cref="In"/> (0 to 0.5) or <see cref="Out"/> (0.5 to 1).</summary>
            /// <remarks><see href="https://easings.net/#easeInOutSine">Easings.net has a graph of this function.</see></remarks>
            public static float InOut(float value) => -0.5f * (Cos(PI * value) - 1);

            /************************************************************************************************************************/

            /// <summary>Returns the derivative of <see cref="In(float)"/>.</summary>
            public static float InDerivative(float value) => 0.5f * PI * Sin(0.5f * PI * value);

            /// <summary>Returns the derivative of <see cref="Out(float)"/>.</summary>
            public static float OutDerivative(float value) => PI * 0.5f * Cos(value * (PI * 0.5f));

            /// <summary>Returns the derivative of <see cref="InOut(float)"/>.</summary>
            public static float InOutDerivative(float value) => 0.5f * PI * Sin(PI * value);

            /************************************************************************************************************************/
            // Ranged Variants.
            /************************************************************************************************************************/

            /// <summary>A variant of <see cref="In(float)"/> with a custom range instead of 0 to 1.</summary>
            public static float In(float start, float end, float value) => Lerp(start, end, In(UnLerp(start, end, value)));
            /// <summary>A variant of <see cref="Out(float)"/> with a custom range instead of 0 to 1.</summary>
            public static float Out(float start, float end, float value) => Lerp(start, end, Out(UnLerp(start, end, value)));
            /// <summary>A variant of <see cref="InOut(float)"/> with a custom range instead of 0 to 1.</summary>
            public static float InOut(float start, float end, float value) => Lerp(start, end, InOut(UnLerp(start, end, value)));

            /// <summary>A variant of <see cref="InDerivative(float)"/> with a custom range instead of 0 to 1.</summary>
            public static float InDerivative(float start, float end, float value) => InDerivative(UnLerp(start, end, value)) * (end - start);
            /// <summary>A variant of <see cref="InDerivative(float)"/> with a custom range instead of 0 to 1.</summary>
            public static float OutDerivative(float start, float end, float value) => OutDerivative(UnLerp(start, end, value)) * (end - start);
            /// <summary>A variant of <see cref="InDerivative(float)"/> with a custom range instead of 0 to 1.</summary>
            public static float InOutDerivative(float start, float end, float value) => InOutDerivative(UnLerp(start, end, value)) * (end - start);

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Exponential
        /************************************************************************************************************************/

        /// <summary>Functions based on exponential equations (<c>2^(10(x))</c>).</summary>
        /// https://kybernetik.com.au/animancer/api/Animancer/Exponential
        /// 
        public static class Exponential
        {
            /************************************************************************************************************************/

            /// <summary>Interpolates the `value` based on the line (<c>y = 2^(10 * (x - 1))</c>).</summary>
            /// <remarks><see href="https://easings.net/#easeInExpo">Easings.net has a graph of this function.</see></remarks>
            public static float In(float value) => Pow(2, 10 * (value - 1));

            /// <summary>Interpolates the `value` based on the line (<c>y = -2^(-10x) + 1</c>).</summary>
            /// <remarks><see href="https://easings.net/#easeOutExpo">Easings.net has a graph of this function.</see></remarks>
            public static float Out(float value) => -Pow(2, -10 * value) + 1;

            /// <summary>Interpolate using <see cref="In"/> (0 to 0.5) or <see cref="Out"/> (0.5 to 1).</summary>
            /// <remarks><see href="https://easings.net/#easeInOutExpo">Easings.net has a graph of this function.</see></remarks>
            public static float InOut(float value)
            {
                return default;
            }

            /************************************************************************************************************************/

            /// <summary>Returns the derivative of <see cref="In(float)"/> (<c>y = 10 * ln(2) * 2^(10 * (x - 1))</c>).</summary>
            public static float InDerivative(float value) => 10 * Ln2 * Pow(2, 10 * (value - 1));

            /// <summary>Returns the derivative of <see cref="Out(float)"/> (<c>y = 5 * ln(2) * 2^(-10 * (x - 1) + 1)</c>).</summary>
            public static float OutDerivative(float value) => 5 * Ln2 * Pow(2, 1 - 10 * value);

            /// <summary>Returns the derivative of <see cref="InOut(float)"/>.</summary>
            public static float InOutDerivative(float value)
            {
                return default;
            }

            /************************************************************************************************************************/
            // Ranged Variants.
            /************************************************************************************************************************/

            /// <summary>A variant of <see cref="In(float)"/> with a custom range instead of 0 to 1.</summary>
            public static float In(float start, float end, float value) => Lerp(start, end, In(UnLerp(start, end, value)));
            /// <summary>A variant of <see cref="Out(float)"/> with a custom range instead of 0 to 1.</summary>
            public static float Out(float start, float end, float value) => Lerp(start, end, Out(UnLerp(start, end, value)));
            /// <summary>A variant of <see cref="InOut(float)"/> with a custom range instead of 0 to 1.</summary>
            public static float InOut(float start, float end, float value) => Lerp(start, end, InOut(UnLerp(start, end, value)));

            /// <summary>A variant of <see cref="InDerivative(float)"/> with a custom range instead of 0 to 1.</summary>
            public static float InDerivative(float start, float end, float value) => InDerivative(UnLerp(start, end, value)) * (end - start);
            /// <summary>A variant of <see cref="InDerivative(float)"/> with a custom range instead of 0 to 1.</summary>
            public static float OutDerivative(float start, float end, float value) => OutDerivative(UnLerp(start, end, value)) * (end - start);
            /// <summary>A variant of <see cref="InDerivative(float)"/> with a custom range instead of 0 to 1.</summary>
            public static float InOutDerivative(float start, float end, float value) => InOutDerivative(UnLerp(start, end, value)) * (end - start);

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Circular
        /************************************************************************************************************************/

        /// <summary>Functions based on circular equations.</summary>
        /// https://kybernetik.com.au/animancer/api/Animancer/Circular
        /// 
        public static class Circular
        {
            /************************************************************************************************************************/

            /// <summary>Interpolates the `value` based on a shifted quadrant IV of a unit circle.</summary>
            /// <remarks><see href="https://easings.net/#easeInCirc">Easings.net has a graph of this function.</see></remarks>
            public static float In(float value) => -(Sqrt(1 - value * value) - 1);

            /// <summary>Interpolates the `value` based on a shifted quadrant II of a unit circle.</summary>
            /// <remarks><see href="https://easings.net/#easeOutCirc">Easings.net has a graph of this function.</see></remarks>
            public static float Out(float value)
            {
                return default;
            }

            /// <summary>Interpolate using <see cref="In"/> (0 to 0.5) or <see cref="Out"/> (0.5 to 1).</summary>
            /// <remarks><see href="https://easings.net/#easeInOutCirc">Easings.net has a graph of this function.</see></remarks>
            public static float InOut(float value)
            {
                return default;
            }

            /************************************************************************************************************************/

            /// <summary>Returns the derivative of <see cref="In(float)"/>.</summary>
            public static float InDerivative(float value) => value / Sqrt(1 - value * value);

            /// <summary>Returns the derivative of <see cref="Out(float)"/>.</summary>
            public static float OutDerivative(float value)
            {
                return default;
            }

            /// <summary>Returns the derivative of <see cref="InOut(float)"/>.</summary>
            public static float InOutDerivative(float value)
            {
                return default;
            }

            /************************************************************************************************************************/
            // Ranged Variants.
            /************************************************************************************************************************/

            /// <summary>A variant of <see cref="In(float)"/> with a custom range instead of 0 to 1.</summary>
            public static float In(float start, float end, float value) => Lerp(start, end, In(UnLerp(start, end, value)));
            /// <summary>A variant of <see cref="Out(float)"/> with a custom range instead of 0 to 1.</summary>
            public static float Out(float start, float end, float value) => Lerp(start, end, Out(UnLerp(start, end, value)));
            /// <summary>A variant of <see cref="InOut(float)"/> with a custom range instead of 0 to 1.</summary>
            public static float InOut(float start, float end, float value) => Lerp(start, end, InOut(UnLerp(start, end, value)));

            /// <summary>A variant of <see cref="InDerivative(float)"/> with a custom range instead of 0 to 1.</summary>
            public static float InDerivative(float start, float end, float value) => InDerivative(UnLerp(start, end, value)) * (end - start);
            /// <summary>A variant of <see cref="InDerivative(float)"/> with a custom range instead of 0 to 1.</summary>
            public static float OutDerivative(float start, float end, float value) => OutDerivative(UnLerp(start, end, value)) * (end - start);
            /// <summary>A variant of <see cref="InDerivative(float)"/> with a custom range instead of 0 to 1.</summary>
            public static float InOutDerivative(float start, float end, float value) => InOutDerivative(UnLerp(start, end, value)) * (end - start);

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Back
        /************************************************************************************************************************/

        /// <summary>Functions based on equations which go out of bounds then come back.</summary>
        /// https://kybernetik.com.au/animancer/api/Animancer/Back
        /// 
        public static class Back
        {
            /************************************************************************************************************************/

            private const float C = 1.758f;

            /************************************************************************************************************************/

            /// <remarks><see href="https://easings.net/#easeInBack">Easings.net has a graph of this function.</see></remarks>
            public static float In(float value) => value * value * ((C + 1) * value - C);

            /// <remarks><see href="https://easings.net/#easeOutBack">Easings.net has a graph of this function.</see></remarks>
            public static float Out(float value)
            {
                return default;
            }

            /// <summary>Interpolate using <see cref="In"/> (0 to 0.5) or <see cref="Out"/> (0.5 to 1).</summary>
            /// <remarks><see href="https://easings.net/#easeInOutBack">Easings.net has a graph of this function.</see></remarks>
            public static float InOut(float value)
            {
                return default;
            }

            /************************************************************************************************************************/

            /// <summary>Returns the derivative of <see cref="In(float)"/>.</summary>
            public static float InDerivative(float value) => 3 * (C + 1) * value * value - 2 * C * value;

            /// <summary>Returns the derivative of <see cref="Out(float)"/>.</summary>
            public static float OutDerivative(float value)
            {
                return default;
            }

            /// <summary>Returns the derivative of <see cref="InOut(float)"/>.</summary>
            public static float InOutDerivative(float value)
            {
                return default;
            }

            /************************************************************************************************************************/
            // Ranged Variants.
            /************************************************************************************************************************/

            /// <summary>A variant of <see cref="In(float)"/> with a custom range instead of 0 to 1.</summary>
            public static float In(float start, float end, float value) => Lerp(start, end, In(UnLerp(start, end, value)));
            /// <summary>A variant of <see cref="Out(float)"/> with a custom range instead of 0 to 1.</summary>
            public static float Out(float start, float end, float value) => Lerp(start, end, Out(UnLerp(start, end, value)));
            /// <summary>A variant of <see cref="InOut(float)"/> with a custom range instead of 0 to 1.</summary>
            public static float InOut(float start, float end, float value) => Lerp(start, end, InOut(UnLerp(start, end, value)));

            /// <summary>A variant of <see cref="InDerivative(float)"/> with a custom range instead of 0 to 1.</summary>
            public static float InDerivative(float start, float end, float value) => InDerivative(UnLerp(start, end, value)) * (end - start);
            /// <summary>A variant of <see cref="InDerivative(float)"/> with a custom range instead of 0 to 1.</summary>
            public static float OutDerivative(float start, float end, float value) => OutDerivative(UnLerp(start, end, value)) * (end - start);
            /// <summary>A variant of <see cref="InDerivative(float)"/> with a custom range instead of 0 to 1.</summary>
            public static float InOutDerivative(float start, float end, float value) => InOutDerivative(UnLerp(start, end, value)) * (end - start);

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Bounce
        /************************************************************************************************************************/

        /// <summary>Functions based on equations with sharp bounces.</summary>
        /// https://kybernetik.com.au/animancer/api/Animancer/Bounce
        /// 
        public static class Bounce
        {
            /************************************************************************************************************************/

            /// <remarks><see href="https://easings.net/#easeInBounce">Easings.net has a graph of this function.</see></remarks>
            public static float In(float value)
            {
                return default;
            }

            /// <remarks><see href="https://easings.net/#easeOutBounce">Easings.net has a graph of this function.</see></remarks>
            public static float Out(float value)
            {
                return default;
            }

            /// <summary>Interpolate using <see cref="In"/> (0 to 0.5) or <see cref="Out"/> (0.5 to 1).</summary>
            /// <remarks><see href="https://easings.net/#easeInOutBounce">Easings.net has a graph of this function.</see></remarks>
            public static float InOut(float value)
            {
                return default;
            }

            /************************************************************************************************************************/

            /// <summary>Returns the derivative of <see cref="In(float)"/>.</summary>
            public static float InDerivative(float value) => OutDerivative(1 - value);

            /// <summary>Returns the derivative of <see cref="Out(float)"/>.</summary>
            public static float OutDerivative(float value)
            {
                return default;
            }

            /// <summary>Returns the derivative of <see cref="InOut(float)"/>.</summary>
            public static float InOutDerivative(float value)
            {
                return default;
            }

            /************************************************************************************************************************/
            // Ranged Variants.
            /************************************************************************************************************************/

            /// <summary>A variant of <see cref="In(float)"/> with a custom range instead of 0 to 1.</summary>
            public static float In(float start, float end, float value) => Lerp(start, end, In(UnLerp(start, end, value)));
            /// <summary>A variant of <see cref="Out(float)"/> with a custom range instead of 0 to 1.</summary>
            public static float Out(float start, float end, float value) => Lerp(start, end, Out(UnLerp(start, end, value)));
            /// <summary>A variant of <see cref="InOut(float)"/> with a custom range instead of 0 to 1.</summary>
            public static float InOut(float start, float end, float value) => Lerp(start, end, InOut(UnLerp(start, end, value)));

            /// <summary>A variant of <see cref="InDerivative(float)"/> with a custom range instead of 0 to 1.</summary>
            public static float InDerivative(float start, float end, float value) => InDerivative(UnLerp(start, end, value)) * (end - start);
            /// <summary>A variant of <see cref="InDerivative(float)"/> with a custom range instead of 0 to 1.</summary>
            public static float OutDerivative(float start, float end, float value) => OutDerivative(UnLerp(start, end, value)) * (end - start);
            /// <summary>A variant of <see cref="InDerivative(float)"/> with a custom range instead of 0 to 1.</summary>
            public static float InOutDerivative(float start, float end, float value) => InOutDerivative(UnLerp(start, end, value)) * (end - start);

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Elastic
        /************************************************************************************************************************/

        /// <summary>Functions based on equations with soft bounces.</summary>
        /// https://kybernetik.com.au/animancer/api/Animancer/Elastic
        /// 
        public static class Elastic
        {
            /************************************************************************************************************************/

            /// <summary><c>2 / 3 * pi</c></summary>
            public const float TwoThirdsPi = 2f / 3f * PI;

            /************************************************************************************************************************/

            /// <remarks><see href="https://easings.net/#easeInElastic">Easings.net has a graph of this function.</see></remarks>
            public static float In(float value)
            {
                return default;
            }

            /// <remarks><see href="https://easings.net/#easeOutElastic">Easings.net has a graph of this function.</see></remarks>
            public static float Out(float value)
            {
                return default;
            }

            /// <summary>Interpolate using <see cref="In"/> (0 to 0.5) or <see cref="Out"/> (0.5 to 1).</summary>
            /// <remarks><see href="https://easings.net/#easeInOutElastic">Easings.net has a graph of this function.</see></remarks>
            public static float InOut(float value)
            {
                return default;
            }

            /************************************************************************************************************************/

            /// <summary>Returns the derivative of <see cref="In(float)"/>.</summary>
            public static float InDerivative(float value)
            {
                return default;
            }

            /// <summary>Returns the derivative of <see cref="Out(float)"/>.</summary>
            public static float OutDerivative(float value)
            {
                return default;
            }

            /// <summary>Returns the derivative of <see cref="InOut(float)"/>.</summary>
            public static float InOutDerivative(float value)
            {
                return default;
            }

            /************************************************************************************************************************/
            // Ranged Variants.
            /************************************************************************************************************************/

            /// <summary>A variant of <see cref="In(float)"/> with a custom range instead of 0 to 1.</summary>
            public static float In(float start, float end, float value) => Lerp(start, end, In(UnLerp(start, end, value)));
            /// <summary>A variant of <see cref="Out(float)"/> with a custom range instead of 0 to 1.</summary>
            public static float Out(float start, float end, float value) => Lerp(start, end, Out(UnLerp(start, end, value)));
            /// <summary>A variant of <see cref="InOut(float)"/> with a custom range instead of 0 to 1.</summary>
            public static float InOut(float start, float end, float value) => Lerp(start, end, InOut(UnLerp(start, end, value)));

            /// <summary>A variant of <see cref="InDerivative(float)"/> with a custom range instead of 0 to 1.</summary>
            public static float InDerivative(float start, float end, float value) => InDerivative(UnLerp(start, end, value)) * (end - start);
            /// <summary>A variant of <see cref="InDerivative(float)"/> with a custom range instead of 0 to 1.</summary>
            public static float OutDerivative(float start, float end, float value) => OutDerivative(UnLerp(start, end, value)) * (end - start);
            /// <summary>A variant of <see cref="InDerivative(float)"/> with a custom range instead of 0 to 1.</summary>
            public static float InOutDerivative(float start, float end, float value) => InOutDerivative(UnLerp(start, end, value)) * (end - start);

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

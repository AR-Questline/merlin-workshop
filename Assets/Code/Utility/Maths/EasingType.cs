using System;
using Awaken.Utility.Enums;

namespace Awaken.TG.Utility {
    public class EasingType : RichEnum {
        readonly Func<float, float> _func;
        public float Calculate(float k) => _func(k);

        protected EasingType(string enumName, string category, Func<float, float> func) : base(enumName, category) {
            _func = func;
        }

        public static readonly EasingType
            Linear = new(nameof(Linear), nameof(Linear), Easing.Linear),
            QuadraticIn = new(nameof(QuadraticIn), nameof(QuadraticIn), Easing.Quadratic.In),
            QuadraticOut = new(nameof(QuadraticOut), nameof(QuadraticOut), Easing.Quadratic.Out),
            QuadraticInOut = new(nameof(QuadraticInOut), nameof(QuadraticInOut), Easing.Quadratic.InOut),
            CubicIn = new(nameof(CubicIn), nameof(CubicIn), Easing.Cubic.In),
            CubicOut = new(nameof(CubicOut), nameof(CubicOut), Easing.Cubic.Out),
            CubicInOut = new(nameof(CubicInOut), nameof(CubicInOut), Easing.Cubic.InOut),
            QuarticIn = new(nameof(QuarticIn), nameof(QuarticIn), Easing.Quartic.In),
            QuarticOut = new(nameof(QuarticOut), nameof(QuarticOut), Easing.Quartic.Out),
            QuarticInOut = new(nameof(QuarticInOut), nameof(QuarticInOut), Easing.Quartic.InOut),
            QuinticIn = new(nameof(QuinticIn), nameof(QuinticIn), Easing.Quintic.In),
            QuinticOut = new(nameof(QuinticOut), nameof(QuinticOut), Easing.Quintic.Out),
            QuinticInOut = new(nameof(QuinticInOut), nameof(QuinticInOut), Easing.Quintic.InOut),
            SinusoidalIn = new(nameof(SinusoidalIn), nameof(SinusoidalIn), Easing.Sinusoidal.In),
            SinusoidalOut = new(nameof(SinusoidalOut), nameof(SinusoidalOut), Easing.Sinusoidal.Out),
            SinusoidalInOut = new(nameof(SinusoidalInOut), nameof(SinusoidalInOut), Easing.Sinusoidal.InOut),
            ExponentialIn = new(nameof(ExponentialIn), nameof(ExponentialIn), Easing.Exponential.In),
            ExponentialOut = new(nameof(ExponentialOut), nameof(ExponentialOut), Easing.Exponential.Out),
            ExponentialInOut = new(nameof(ExponentialInOut), nameof(ExponentialInOut), Easing.Exponential.InOut),
            CircularIn = new(nameof(CircularIn), nameof(CircularIn), Easing.Circular.In),
            CircularOut = new(nameof(CircularOut), nameof(CircularOut), Easing.Circular.Out),
            CircularInOut = new(nameof(CircularInOut), nameof(CircularInOut), Easing.Circular.InOut),
            ElasticIn = new(nameof(ElasticIn), nameof(ElasticIn), Easing.Elastic.In),
            ElasticOut = new(nameof(ElasticOut), nameof(ElasticOut), Easing.Elastic.Out),
            ElasticInOut = new(nameof(ElasticInOut), nameof(ElasticInOut), Easing.Elastic.InOut),
            BackIn = new(nameof(BackIn), nameof(BackIn), Easing.Back.In),
            BackOut = new(nameof(BackOut), nameof(BackOut), Easing.Back.Out),
            BackInOut = new(nameof(BackInOut), nameof(BackInOut), Easing.Back.InOut),
            BounceIn = new(nameof(BounceIn), nameof(BounceIn), Easing.Bounce.In),
            BounceOut = new(nameof(BounceOut), nameof(BounceOut), Easing.Bounce.Out),
            BounceInOut = new(nameof(BounceInOut), nameof(BounceInOut), Easing.Bounce.InOut);
    }
}
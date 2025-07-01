using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Awaken.Utility.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Utility.Maths {
    /// <summary>
    /// Quadratic, exponential and logarithmic equations in one class
    /// </summary>
    [Serializable]
    public class Equation {
        public float quadratic;
        public float linear;
        public float constant;
        public float exponential;
        [ShowIf("@exponential != 0")]
        public float exponentialMul;
        public float logMult;
        [ShowIf("@logMult != 0")]
        public float logBase;
        public float xAdd;
        public float xMult = 1f;

        [SerializeReference]
        public Equation[] subEquations = new Equation[0];
        public ResultOverride[] overrides;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Get(float x) {
            float modifiedX = x * xMult + xAdd;
            ResultOverride resultOverride = overrides?.FirstOrDefault(o => o.forX == x);
            if (resultOverride != null) {
                return resultOverride.value;
            }
            return ExponentialPart(modifiedX) + QuadraticPart(modifiedX) + LogarithmicPart(modifiedX) + OtherEquationsPart(x);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        float ExponentialPart(float x) => exponential == 0 ? 0 : exponentialMul * Mathf.Pow(exponential, x);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        float QuadraticPart(float x) => quadratic * Mathf.Pow(x, 2) + linear * x + constant;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        float LogarithmicPart(float x) => logBase <= 1 || x == 0 ? 0 : logMult * Mathf.Log(x + 1, logBase);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        float OtherEquationsPart(float x) => subEquations?.Sum(e => e.Get(x)) ?? 0f;

        // === Inspector Helper
        [ShowInInspector] string EquationExpression => string.Join(" + ", ExpressionParts().WhereNotNull());

        IEnumerable<string> ExpressionParts() {
            yield return exponential != 0 ? $"{exponentialMul}*{exponential}^{XWithModifiers}" : null;
            yield return quadratic != 0 ? $"{quadratic}*{XWithModifiers}*{XWithModifiers}" : null;
            yield return linear != 0 ? $"{linear}*{XWithModifiers}" : null;
            yield return constant != 0 ? $"{constant}" : null;
            yield return logBase > 0 && logMult != 0 ? $"{logMult}*log({XWithModifiers}, {logBase})" : null;

            if (subEquations != null) {
                foreach (var part in subEquations.SelectMany(e => e.ExpressionParts())) {
                    yield return part;
                }
            }
        }

        string XWithModifiers {
            get {
                bool useMult = xMult != 1;
                bool useAdd = xAdd != 0;
                bool useParenthesis = useMult || useAdd;

                StringBuilder builder = new StringBuilder("");
                if (useParenthesis) builder.Append("(");
                if (useMult) builder.Append($"{xMult}*");
                builder.Append("x");
                if (useAdd) builder.Append($"+{xAdd}");
                if (useParenthesis) builder.Append(")");
                return builder.ToString();
            }
        }

        [ShowInInspector]
        Dictionary<int, float> SomeResults {
            get {
                Dictionary<int, float> results = new Dictionary<int, float>();
                for (int i = 0; i < 100; i++) {
                    results[i] = Get(i);
                }
                return results;
            }
        }

        [Serializable]
        public class ResultOverride {
            public int forX;
            public float value;
        }
    }
}
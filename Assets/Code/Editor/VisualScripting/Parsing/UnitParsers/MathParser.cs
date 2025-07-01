using System.Linq;
using Awaken.TG.Editor.VisualScripting.Parsing.Scripts;
using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.Editor.VisualScripting.Parsing.UnitParsers {
    public static class MathParser {
        public static void Add<T>(Add<T> add, FunctionScript script) {
            script.AddFlow($"{script.Type<T>()} {script.Variable(add.sum)} = {script.Variable(add.a)} + {script.Variable(add.b)};");
        }

        public static void Sum<T>(Sum<T> sum, FunctionScript script) {
            string arguments = string.Join(" + ", sum.multiInputs.Select(script.Variable));
            string type = typeof(T) == typeof(object) ? "var" : script.Type<T>();
            script.AddFlow($"{type} {script.Variable(sum.sum)} = {arguments};");
        }
        
        public static void Subtract<T>(Subtract<T> subtract, FunctionScript script) {
            string type = typeof(T) == typeof(object) ? "var" : script.Type<T>();
            script.AddFlow($"{type} {script.Variable(subtract.difference)} = {script.Variable(subtract.minuend)} - {script.Variable(subtract.subtrahend)};");
        }

        public static void Multiply<T>(Multiply<T> multiply, FunctionScript script) {
            string type = typeof(T) == typeof(object) ? "var" : script.Type<T>();
            script.AddFlow($"{type} {script.Variable(multiply.product)} = {script.Variable(multiply.a)} * {script.Variable(multiply.b)};");
        }
        
        public static void Divide<T>(Divide<T> divide, FunctionScript script) {
            string type = typeof(T) == typeof(object) ? "var" : script.Type<T>();
            script.AddFlow($"{type} {script.Variable(divide.quotient)} = {script.Variable(divide.dividend)} / {script.Variable(divide.divisor)};");
        }

        public static void Modulo<T>(Modulo<T> modulo, FunctionScript script) {
            string type = typeof(T) == typeof(object) ? "var" : script.Type<T>();
            script.AddFlow($"{type} {script.Variable(modulo.remainder)} = {script.Variable(modulo.dividend)} % {script.Variable(modulo.divisor)};");
        }

        public static void Vector3Normalize(Vector3Normalize normalize, FunctionScript script) {
            script.AddFlow($"{script.Type<Vector3>()} {script.Variable(normalize.output)} = {script.Variable(normalize.input)}.normalized;");
        }

        public static void ScalarExponentiate(ScalarExponentiate exponentiate, FunctionScript script) {
            script.AddFlow($"{script.Type<float>()} {script.Variable(exponentiate.power)} = {script.Type<Mathf>()}.Pow({script.Variable(exponentiate.@base)}, {script.Variable(exponentiate.exponent)});");
        }

        public static void ScalarMoveTowards(ScalarMoveTowards moveTowards, FunctionScript script) {
            string maxDelta = $"{script.Variable(moveTowards.maxDelta)}";
            if (moveTowards.perSecond) {
                maxDelta += $" * {script.Type<Time>()}.deltaTime";
            }
            script.AddFlow($"{script.Type<float>()} {script.Variable(moveTowards.result)} = {script.Type<Mathf>()}.MoveTowards({script.Variable(moveTowards.current)}, {script.Variable(moveTowards.target)}, {maxDelta});");
        }
    }
}
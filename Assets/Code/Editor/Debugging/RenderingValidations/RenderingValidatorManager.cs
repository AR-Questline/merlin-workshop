using System;
using System.Collections.Generic;
using Awaken.TG.Editor.Debugging.RenderingValidations.StaticValidators;
using UnityEditor;

namespace Awaken.TG.Editor.Debugging.RenderingValidations {
    public static class RenderingValidatorManager {
        static readonly HashSet<IStaticRenderingValidator> StaticRenderingValidators = new();
        static readonly List<RenderingErrorMessage> MessagesBuffer = new();

        public static void Check(HashSet<RenderingContextObject> contexts, List<RenderingError> errorsBuffer) {
            errorsBuffer.Clear();
            foreach (var validator in StaticRenderingValidators) {
                if (validator is IStaticRenderingValidatorWithInit initValidator) {
                    initValidator.Init();
                }
            }
            foreach (var context in contexts) {
                if (context.context == null) {
                    continue;
                }
                if (Check(context, MessagesBuffer, out var error)) {
                    errorsBuffer.Add(error);
                }
            }
        }

        static bool Check(in RenderingContextObject context, List<RenderingErrorMessage> messagesBuffer, out RenderingError renderingError) {
            foreach (var validator in StaticRenderingValidators) {
                validator.Check(context, messagesBuffer);
            }
            if (messagesBuffer.Count > 0) {
                renderingError = new RenderingError(context, messagesBuffer.ToArray());
                messagesBuffer.Clear();
                return true;
            }
            messagesBuffer.Clear();
            renderingError = default;
            return false;
        }

        [InitializeOnLoadMethod]
        static void ScanForTypes() {
            var staticValidators = TypeCache.GetTypesDerivedFrom<IStaticRenderingValidator>();
            foreach (var validatorsType in staticValidators) {
                if (validatorsType.IsAbstract || validatorsType.IsInterface) {
                    continue;
                }
                StaticRenderingValidators.Add((IStaticRenderingValidator)Activator.CreateInstance(validatorsType));
            }
        }
    }
}

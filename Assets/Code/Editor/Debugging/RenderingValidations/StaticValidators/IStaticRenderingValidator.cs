using System.Collections.Generic;
using UnityEngine;

namespace Awaken.TG.Editor.Debugging.RenderingValidations.StaticValidators {
    public interface IStaticRenderingValidator {
        void Check(in RenderingContextObject context, List<RenderingErrorMessage> errorMessages);
    }

    public interface IStaticRenderingValidator<in T> : IStaticRenderingValidator where T : Object {
        void IStaticRenderingValidator.Check(in RenderingContextObject context, List<RenderingErrorMessage> errorMessages) {
            if (context.context is T castContext) {
                Check(castContext, errorMessages);
            }
        }

        void Check(T contextObject, List<RenderingErrorMessage> errorMessages);
    }

    public interface IStaticRenderingValidatorWithInit : IStaticRenderingValidator {
        void Init();
    }
}

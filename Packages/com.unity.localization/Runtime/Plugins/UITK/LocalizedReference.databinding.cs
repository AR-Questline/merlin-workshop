#if MODULE_UITK && UNITY_2023_3_OR_NEWER

using System;
using Unity.Properties;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.UIElements;

namespace UnityEngine.Localization
{
    [UxmlObject]
    public partial class LocalizedReference : CustomBinding
    {
        int m_ActivatedCount;

        [UxmlAttribute("table")]
        internal TableReference TableReferenceUXML
        {
            get => TableReference;
            set => TableReference = value;
        }

        [UxmlAttribute("entry")]
        internal TableEntryReference TableEntryReferenceUXML
        {
            get => TableEntryReference;
            set => TableEntryReference = value;
        }

        [UxmlAttribute("fallback")]
        internal FallbackBehavior FallbackStateUXML
        {
            get => FallbackState;
            set => FallbackState = value;
        }

        /// <summary>
        /// Creates a new instance of the LocalizedReference.
        /// </summary>
        public LocalizedReference()
        {
            updateTrigger = BindingUpdateTrigger.WhenDirty;
        }

        /// <summary>
        /// Initializes the data binding by subscribing to change event.
        /// </summary>
        /// <param name="context">Context object.</param>
        protected override void OnActivated(in BindingActivationContext context)
        {
            base.OnActivated(context);

            m_ActivatedCount++;
            if (m_ActivatedCount == 1)
                Initialize();
        }

        /// <summary>
        /// Cleans up the data binding by unsubscribing from the change event.
        /// </summary>
        /// <param name="context">Context object.</param>
        protected override void OnDeactivated(in BindingActivationContext context)
        {
            base.OnDeactivated(context);

            m_ActivatedCount--;
            if (m_ActivatedCount == 0)
                Cleanup();
        }

        /// <summary>
        /// Called the first time when <see cref="OnActivated(in BindingActivationContext)" is called./>
        /// </summary>
        protected abstract void Initialize();

        /// <summary>
        /// Called the last time when <see cref="OnDeactivated(in BindingActivationContext)" is called./>
        /// </summary>
        protected abstract void Cleanup();

        internal BindingResult CreateErrorResult(in BindingContext context, VisitReturnCode errorCode, Type sourceType)
        {
            var element = context.targetElement;
            var bindingTypename = TypeUtility.GetTypeDisplayName(GetType());
            var bindingId = $"{TypeUtility.GetTypeDisplayName(element.GetType())}.{context.bindingId}";

            return errorCode switch
            {
                VisitReturnCode.InvalidPath => new BindingResult(BindingStatus.Failure, $"{bindingTypename}: Binding id `{bindingId}` is either invalid or contains a `null` value."),
                VisitReturnCode.InvalidCast => new BindingResult(BindingStatus.Failure, $"{bindingTypename}: Invalid conversion from {sourceType} for binding id `{bindingId}`"),
                VisitReturnCode.AccessViolation => new BindingResult(BindingStatus.Failure, $"{bindingTypename}: Trying set value for binding id `{bindingId}`, but it is read-only."),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}

#endif

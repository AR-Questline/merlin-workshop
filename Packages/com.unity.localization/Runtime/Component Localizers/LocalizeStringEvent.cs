using System.Collections.Generic;
using UnityEngine.Localization.Events;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace UnityEngine.Localization.Components
{
    /// <summary>
    /// Component that can be used to Localize a string.
    /// Provides an update event <see cref="UpdateString(string)"/> that can be used to automatically update the string
    /// when the <see cref="LocalizationSettings.SelectedLocale"/> or <see cref="StringReference"/> changes.
    /// Allows for configuring optional arguments that will be used by **Smart Format** or [String.Format](https://docs.microsoft.com/en-us/dotnet/api/system.string.format).
    /// </summary>
    /// <example>
    /// This example shows how a Localized String Event can be dynamically updated with a different localized string or new formatting data.
    /// <code source="../../DocCodeSamples.Tests/LocalizeStringEventExample.cs"/>
    /// </example>
    /// <example>
    /// ![](../manual/images/scripting/LocalizeStringEventExample_Inspector.png)
    /// </example>
    /// <example>
    /// Example of String Table Contents
    ///
    /// ![Example of String Table Contents](../manual/images/scripting/LocalizeStringEventExample_StringTable.png)
    /// </example>
    /// <example>
    /// Example results in Game
    ///
    /// ![Example results in Game](../manual/images/scripting/LocalizeStringEventExample_GameView.gif)
    /// </example>
    [AddComponentMenu("Localization/Localize String Event")]
    public class LocalizeStringEvent : LocalizedMonoBehaviour
    {
        [SerializeField]
        LocalizedString m_StringReference = new LocalizedString();

        [SerializeField]
        List<Object> m_FormatArguments = new List<Object>();

        [SerializeField]
        UnityEventString m_UpdateString = new UnityEventString();

        LocalizedString.ChangeHandler m_ChangeHandler;

        /// <summary>
        /// References the <see cref="StringTable"/> and <see cref="StringTableEntry"/> of the localized string.
        /// </summary>
        public LocalizedString StringReference
        {
            get => m_StringReference;
            set
            {
                // Unsubscribe from the old string reference.
                ClearChangeHandler();

                m_StringReference = value;

                if (isActiveAndEnabled)
                    RegisterChangeHandler();
            }
        }

        /// <summary>
        /// Event that will be sent when the localized string is available.
        /// </summary>
        public UnityEventString OnUpdateString
        {
            get => m_UpdateString;
            set => m_UpdateString = value;
        }

        /// <summary>
        /// Forces the string to be regenerated, such as when the string formatting argument values have changed.
        /// </summary>
        public void RefreshString()
        {
            StringReference?.RefreshString();
        }

        /// <summary>
        /// Changes the <see cref="TableReference"/> value of a LocalizeString.
        /// </summary>
        /// <param name="tableReference">A reference to the table that will be set to StringReference of a LocalizeString</param>
        public void SetTable(string tableReference)
        {
            if (StringReference == null)
                StringReference = new LocalizedString();
            StringReference.TableReference = tableReference;
        }

        /// <summary>
        /// Changes the <see cref="TableEntry"/> value of a LocalizeString.
        /// </summary>
        /// <param name="entryName">A reference to the entry in the table that will be set to StringReference of a LocalizeString</param>
        public void SetEntry(string entryName)
        {
            if (StringReference == null)
                StringReference = new LocalizedString();
            StringReference.TableEntryReference = entryName;
        }

        /// <summary>
        /// Starts listening for changes to <see cref="StringReference"/>.
        /// </summary>
        protected virtual void OnEnable() => RegisterChangeHandler();

        /// <summary>
        /// Stops listening for changes to <see cref="StringReference"/>.
        /// </summary>
        protected virtual void OnDisable() => ClearChangeHandler();

        void OnDestroy() => ClearChangeHandler();

        /// <summary>
        /// Invokes the <see cref="OnUpdateString"/> event.
        /// </summary>
        /// <param name="value"></param>
        protected virtual void UpdateString(string value)
        {
            #if UNITY_EDITOR
            if (!LocalizationSettings.Instance.IsPlayingOrWillChangePlaymode)
            {
                if (StringReference == null || StringReference.IsEmpty)
                {
                    Editor_UnregisterKnownDrivenProperties(OnUpdateString);
                    return;
                }

                Editor_RegisterKnownDrivenProperties(OnUpdateString);
                OnUpdateString.Invoke(value);
                Editor_RefreshEventObjects(OnUpdateString);
            }
            else
            #endif
            {
                OnUpdateString.Invoke(value);
            }
        }

        void OnValidate()
        {
            RefreshString();
        }

        internal virtual void RegisterChangeHandler()
        {
            if (StringReference == null)
                return;

            if (m_FormatArguments.Count > 0)
            {
                StringReference.Arguments = m_FormatArguments.ToArray();

                if (Application.isPlaying)
                    Debug.LogWarningFormat("LocalizeStringEvent({0}) is using the deprecated Format Arguments field which will be removed in the future. Consider upgrading to use String Reference Local Variables instead.", name, this);
            }

            if (m_ChangeHandler == null)
                m_ChangeHandler = UpdateString;

            StringReference.StringChanged += m_ChangeHandler;
        }

        internal virtual void ClearChangeHandler()
        {
            #if UNITY_EDITOR
            Editor_UnregisterKnownDrivenProperties(OnUpdateString);
            #endif

            if (StringReference != null)
                StringReference.StringChanged -= m_ChangeHandler;
        }
    }
}

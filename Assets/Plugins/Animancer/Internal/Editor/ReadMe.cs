// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //
// FlexiMotion // https://kybernetik.com.au/flexi-motion // Copyright 2023 Kybernetik //

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value.

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Animancer.Editor
//namespace FlexiMotion.Editor
{
    /// <summary>[Editor-Only] A welcome screen for an asset.</summary>
    /// https://kybernetik.com.au/animancer/api/Animancer.Editor/ReadMe
    /// https://kybernetik.com.au/flexi-motion/api/FlexiMotion.Editor/ReadMe
    /// 
    public abstract class ReadMe : ScriptableObject
    {
        /************************************************************************************************************************/
        #region Fields and Properties
        /************************************************************************************************************************/

        /// <summary>The release ID of the current version.</summary>
        protected abstract int ReleaseNumber { get; }

        /// <summary>The display name of this product version.</summary>
        protected abstract string VersionName { get; }

        /// <summary>The URL for the change log of this version.</summary>
        protected abstract string ChangeLogURL { get; }

        /// <summary>The key used to save the release number.</summary>
        protected abstract string PrefKey { get; }

        /// <summary>An introductory explanation of this asset.</summary>
        protected virtual string Introduction => null;

        /// <summary>The base name of this product (without any "Lite", "Pro", "Demo", etc.).</summary>
        protected abstract string BaseProductName { get; }

        /// <summary>The name of this product.</summary>
        protected virtual string ProductName => BaseProductName;

        /// <summary>The URL for the documentation.</summary>
        protected abstract string DocumentationURL { get; }

        /// <summary>The display name for the examples section.</summary>
        protected virtual string ExamplesLabel => "Examples";

        /// <summary>The URL for the example documentation.</summary>
        protected abstract string ExampleURL { get; }

        /// <summary>The URL to check for the latest version.</summary>
        protected virtual string UpdateURL => null;

        /************************************************************************************************************************/

        /// <summary>
        /// The <see cref="ReadMe"/> file name ends with the <see cref="VersionName"/> to detect if the user imported
        /// this version without deleting a previous version.
        /// </summary>
        /// <remarks>
        /// When Unity's package importer sees an existing file with the same GUID as one in the package, it will
        /// overwrite that file but not move or rename it if the name has changed. So it will leave the file there with
        /// the old version name.
        /// </remarks>
        private bool HasCorrectName => name.EndsWith(VersionName);

        /************************************************************************************************************************/

        [SerializeField] private DefaultAsset _ExamplesFolder;

        /// <summary>Sections to be displayed below the examples.</summary>
        public LinkSection[] LinkSections { get; set; }

        /// <summary>Extra sections to be displayed with the examples.</summary>
        public LinkSection[] ExtraExamples { get; set; }

        /************************************************************************************************************************/

        /// <summary>Creates a new <see cref="ReadMe"/> and sets the <see cref="LinkSections"/>.</summary>
        public ReadMe(params LinkSection[] linkSections)
        {
        }

        /************************************************************************************************************************/

        /// <summary>A heading with a link to be displayed in the Inspector.</summary>
        public class LinkSection
        {
            /************************************************************************************************************************/

            /// <summary>The main label.</summary>
            public readonly string Heading;

            /// <summary>A short description to be displayed near the <see cref="Heading"/>.</summary>
            public readonly string Description;

            /// <summary>A link that can be opened by clicking the <see cref="Heading"/>.</summary>
            public readonly string URL;

            /// <summary>An optional user-friendly version of the <see cref="URL"/>.</summary>
            public readonly string DisplayURL;

            /************************************************************************************************************************/

            /// <summary>Creates a new <see cref="LinkSection"/>.</summary>
            public LinkSection(string heading, string description, string url, string displayURL = null)
            {
            }

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        /// <summary>Returns a <c>mailto</c> link.</summary>
        public static string GetEmailURL(string address, string subject)
            => $"mailto:{address}?subject={subject.Replace(" ", "%20")}";

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Show On Startup and Check for Updates
        /************************************************************************************************************************/

        [SerializeField] private bool _DontShowOnStartup;

        [NonSerialized] private string _CheckForUpdatesKey;
        [NonSerialized] private bool _CheckedForUpdates;
        [NonSerialized] private bool _NewVersionAvailable;
        [NonSerialized] private string _UpdateCheckFailureMessage;
        [NonSerialized] private string _LatestVersionName;
        [NonSerialized] private string _LatestVersionChangeLogURL;
        [NonSerialized] private int _LatestVersionNumber;

        private bool CheckForUpdates
        {
            get => EditorPrefs.GetBool(_CheckForUpdatesKey, true);
            set => EditorPrefs.SetBool(_CheckForUpdatesKey, value);
        }

        /************************************************************************************************************************/

        private static readonly Dictionary<Type, IDisposable>
            TypeToUpdateCheck = new Dictionary<Type, IDisposable>();

        static ReadMe()
        {
        }

        /************************************************************************************************************************/

        /// <summary>Automatically selects a <see cref="ReadMe"/> on startup.</summary>
        //[InitializeOnLoadMethod]
        private static void ShowReadMe()
        {
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Finds the most recently modified <see cref="ReadMe"/> asset with <see cref="_DontShowOnStartup"/> disabled.
        /// </summary>
        private static List<ReadMe> FindInstances(out ReadMe autoSelect)
        {
            autoSelect = default(ReadMe);
            return default;
        }

        /************************************************************************************************************************/

        protected virtual void OnEnable()
        {
        }

        /************************************************************************************************************************/

        private void StartCheckForUpdates()
        {
        }

        /************************************************************************************************************************/

        private void OnUpdateCheckComplete(string text)
        {
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Custom Editor
        /************************************************************************************************************************/

        /// <summary>[Editor-Only] A custom Inspector for <see cref="ReadMe"/>.</summary>
        [CustomEditor(typeof(ReadMe), editorForChildClasses: true)]
        public class Editor : UnityEditor.Editor
        {
            /************************************************************************************************************************/

            private static readonly GUIContent
                GUIContent = new GUIContent();

            [NonSerialized] private ReadMe _Target;
            [NonSerialized] private Texture2D _Icon;
            [NonSerialized] private string _ReleaseNumberPrefKey;
            [NonSerialized] private int _PreviousVersion;
            [NonSerialized] private string _ExamplesDirectory;
            [NonSerialized] private List<ExampleGroup> _Examples;
            [NonSerialized] private string _Title;
            [NonSerialized] private SerializedProperty _DontShowOnStartupProperty;

            /************************************************************************************************************************/

            /// <summary>Don't use any margins.</summary>
            public override bool UseDefaultMargins() => false;

            /************************************************************************************************************************/

            protected virtual void OnEnable()
            {
            }

            /************************************************************************************************************************/

            protected override void OnHeaderGUI()
            {
            }

            /************************************************************************************************************************/

            /// <inheritdoc/>
            public override void OnInspectorGUI()
            {
            }

            /************************************************************************************************************************/

            protected static void DoSpace() => GUILayout.Space(EditorGUIUtility.singleLineHeight * 0.2f);

            /************************************************************************************************************************/

            private void DoIntroduction()
            {
            }

            /************************************************************************************************************************/

            private void DoNewVersionDetails()
            {
            }

            /************************************************************************************************************************/

            private void DoCheckForUpdates()
            {
            }

            /************************************************************************************************************************/

            private void DoShowOnStartup()
            {
            }

            /************************************************************************************************************************/

            private void DoWarnings()
            {
            }

            /************************************************************************************************************************/

            /// <summary>Asks if the user wants to delete the `directory` and does so if they confirm.</summary>
            private void CheckDeleteDirectory(string directory)
            {
            }

            /************************************************************************************************************************/

            /// <summary>
            /// Returns true and uses the current event if it is <see cref="EventType.MouseUp"/> inside the specified
            /// `area`.
            /// </summary>
            public static bool TryUseClickEvent(Rect area, int button = -1)
            {
                return default;
            }

            /// <summary>
            /// Returns true and uses the current event if it is <see cref="EventType.MouseUp"/> inside the last GUI Layout
            /// <see cref="Rect"/> that was drawn.
            /// </summary>
            public static bool TryUseClickEventInLastRect(int button = -1)
                => TryUseClickEvent(GUILayoutUtility.GetLastRect(), button);

            /************************************************************************************************************************/

            protected virtual void DoIntroductionBlock()
            {
            }

            /************************************************************************************************************************/

            protected virtual void DoExampleBlock()
            {
            }

            /************************************************************************************************************************/

            protected virtual void DoExtraExamples()
            {
            }

            /************************************************************************************************************************/

            protected virtual void DoSupportBlock()
            {
            }

            /************************************************************************************************************************/

            protected void DoHeadingLink(
                string heading,
                string description,
                string url,
                string displayURL = null,
                int fontSize = 22)
            {
            }

            /************************************************************************************************************************/

            protected Rect DoLinkButton(string text, string url, GUIStyle style, int fontSize = 22)
            {
                return default;
            }

            /************************************************************************************************************************/

            /// <summary>Draws a line between the `start` and `end` using the `color`.</summary>
            public static void DrawLine(Vector2 start, Vector2 end, Color color)
            {
            }

            /************************************************************************************************************************/

            /// <summary>Various <see cref="GUIStyle"/>s used by the <see cref="Editor"/>.</summary>
            protected static class Styles
            {
                /************************************************************************************************************************/

                public static readonly GUIStyle TitleArea = "In BigTitle";

                public static readonly GUIStyle Title = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 26,
                };

                public static readonly GUIStyle Block = GUI.skin.box;

                public static readonly GUIStyle HeaderLabel = new GUIStyle(GUI.skin.label)
                {
                    stretchWidth = false,
                };

                public static readonly GUIStyle HeaderLink = new GUIStyle(HeaderLabel);

                public static readonly GUIStyle Description = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.LowerLeft,
                };

                public static readonly GUIStyle URL = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 9,
                    alignment = TextAnchor.LowerLeft,
                };

                /************************************************************************************************************************/

                static Styles()
                {
                }

                /************************************************************************************************************************/
            }

            /************************************************************************************************************************/

            /// <summary>A group of example scenes.</summary>
            private class ExampleGroup
            {
                /************************************************************************************************************************/

                /// <summary>The name of this group.</summary>
                public readonly string Name;

                /// <summary>The scenes in this group.</summary>
                public readonly List<SceneAsset> Scenes = new List<SceneAsset>();

                /// <summary>The folder paths of each of the <see cref="Scenes"/>.</summary>
                public readonly List<string> Directories = new List<string>();

                /// <summary>Indicates whether this group should show its contents in the GUI.</summary>
                private bool _IsExpanded;

                /// <summary>Is this group always expanded?</summary>
                private bool _AlwaysExpanded;

                /************************************************************************************************************************/

                public static List<ExampleGroup> Gather(DefaultAsset rootDirectoryAsset, out string examplesDirectory)
                {
                    examplesDirectory = default(string);
                    return default;
                }

                /************************************************************************************************************************/

                public static ExampleGroup Gather(string rootDirectory, string directory)
                {
                    return default;
                }

                /************************************************************************************************************************/

                public ExampleGroup(string rootDirectory, string directory, List<SceneAsset> scenes)
                {
                }

                /************************************************************************************************************************/

                public static void DoExampleGUI(List<ExampleGroup> examples)
                {
                }

                public void DoExampleGUI()
                {
                }

                /************************************************************************************************************************/
            }

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

#endif


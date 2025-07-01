using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Localization.UI
{
    internal static class HelpBoxFactory
    {
        internal const string k_Name = "unity-helpbox";

        internal static VisualElement CreateDefaultHelpBox(string message)
        {
#if UNITY_2020_1_OR_NEWER
            return new HelpBox(message, HelpBoxMessageType.Warning) { name = k_Name };
#else
            return CreateHelpBox(message);
#endif
        }

        /// <summary>
        /// For 2019.4 which doesn't have UIElements.HelpBox
        /// </summary>
        private static VisualElement CreateHelpBox(string message)
        {
            const float margin = 2;
            const float padding = 1;
            var helpBox = new VisualElement
            {
                name = k_Name,
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    marginBottom = margin,
                    marginRight = margin,
                    marginLeft = margin,
                    marginTop = margin,
                    paddingTop = padding,
                    paddingBottom = padding,
                    paddingRight = padding,
                    paddingLeft = padding,
                    fontSize = 9,
                    height = EditorGUIUtility.singleLineHeight * 1.25f
                }
            };
            helpBox.AddToClassList("unity-box");
            helpBox.Add(new Image
                {image = EditorGUIUtility.FindTexture("d_console.warnicon"), scaleMode = ScaleMode.ScaleToFit});
            helpBox.Add(new Label(message));

            return helpBox;
        }
    }
}

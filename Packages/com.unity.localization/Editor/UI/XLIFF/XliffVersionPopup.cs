using System;
using System.Collections.Generic;
using UnityEditor.Localization.Plugins.XLIFF;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEditor.Localization.UI
{
    /// <summary>
    /// Provides a field for selecting an XLIFF version.
    /// </summary>
    #if UNITY_2023_3_OR_NEWER
    [UxmlElement]
    #endif
    public partial class XliffVersionPopup : PopupField<XliffVersion>
    {
        #if UNITY_2023_3_OR_NEWER
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
        #endif
        public new class UxmlFactory : UxmlFactory<XliffVersionPopup> {}

        /// <summary>
        /// Creates a new instance of the field.
        /// </summary>
        public XliffVersionPopup() :
            base("XLIFF Version", new List<XliffVersion> { XliffVersion.V12, XliffVersion.V20 }, 1, VersionLabel, VersionLabel)
        {
        }

        static string VersionLabel(XliffVersion version)
        {
            if (version == XliffVersion.V12)
                return "1.2";
            return "2.0";
        }
    }
}

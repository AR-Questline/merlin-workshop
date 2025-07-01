#if UNITY_ANDROID
using System.IO;
using System.Text;
using System.Xml;

namespace UnityEditor.Localization.Platform.Android
{
    internal class GradleProjectSettings
    {
        internal readonly string LabelName = "@string/app_name";
        internal readonly string IconLabelName = "@mipmap/app_icon";
        internal readonly string RoundIconLabelName = "@mipmap/app_icon_round";

        string m_ManifestFilePath;

        internal string GetManifestPath(string basePath)
        {
            if (string.IsNullOrEmpty(m_ManifestFilePath))
            {
                var pathBuilder = new StringBuilder(basePath);
                GetSourcePath(pathBuilder);
                pathBuilder.Append(Path.DirectorySeparatorChar).Append("AndroidManifest.xml");
                m_ManifestFilePath = pathBuilder.ToString();
            }
            return m_ManifestFilePath;
        }

        internal string GetResFolderPath(string basePath)
        {
            var pathBuilder = new StringBuilder(basePath);
            GetSourcePath(pathBuilder);
            pathBuilder.Append(Path.DirectorySeparatorChar).Append("res");
            return pathBuilder.ToString();
        }

        void GetSourcePath(StringBuilder sb)
        {
            sb.Append(Path.DirectorySeparatorChar).Append("src");
            sb.Append(Path.DirectorySeparatorChar).Append("main");
        }
    }


    internal class AndroidManifest : XmlDocument
    {
        string m_Path;
        protected XmlNamespaceManager m_XmlNameSpaceManager;
        readonly XmlElement m_Element;
        public const string m_Namespace = "http://schemas.android.com/apk/res/android";

        public AndroidManifest(string path)
        {
            m_Path = path;
            using (var reader = new XmlTextReader(m_Path))
            {
                reader.Read();
                Load(reader);
            }
            m_XmlNameSpaceManager = new XmlNamespaceManager(NameTable);
            m_XmlNameSpaceManager.AddNamespace("android", m_Namespace);
            m_Element = SelectSingleNode("/manifest/application") as XmlElement;
        }

        public void SaveIfModified()
        {
            var manifestOnDisk = new AndroidManifest(m_Path);

            // We do this check for incremental pipeline i.e.., when we update the AndroidManifest file in our gradle project instead of overriding it on every build.
            // we check whether our changes are new and only then we save the Updated Manifest file.
            if (manifestOnDisk.OuterXml != OuterXml)
                Save();
        }

        public string Save()
        {
            return SaveAs(m_Path);
        }

        public string SaveAs(string path)
        {
            using (var writer = new XmlTextWriter(path, new UTF8Encoding(false)))
            {
                writer.Formatting = Formatting.Indented;
                Save(writer);
            }
            return path;
        }

        XmlAttribute CreateAndroidAttribute(string key, string value)
        {
            XmlAttribute attr = CreateAttribute("android", key, m_Namespace);
            attr.Value = value;
            return attr;
        }

        /// <summary>
        /// Updates the selected atrribute of the application's components with the provided Value in the AndroidManifest file in the Gradle Project.
        /// Refer Android documentation for more details https://developer.android.com/guide/topics/manifest/application-element
        /// </summary>
        /// <param name="key">The selected attribute of the application component.</param>
        /// <param name="value">The value for the specified attribute.</param>
        internal void SetAtrribute(string key, string value)
        {
            var AttributesElement = "android:" + key;
            if (m_Element?.Attributes[AttributesElement] != null)// Check if attribute already exist
            {
                if (m_Element?.Attributes[AttributesElement].Value != value)// Check if attribute value is different
                {
                    m_Element.Attributes[AttributesElement].Value = value;
                }
            }
            else
            {
                m_Element.Attributes.Append(CreateAndroidAttribute(key, value));
            }
        }
    }
}
#endif

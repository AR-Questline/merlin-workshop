using System;
using System.Diagnostics;

namespace Awaken.TG.Utility.Attributes
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true), Conditional("UNITY_EDITOR")]
    public class ShowAssetPreviewAttribute : Attribute
    {
        public int Width { get; private set; }
        public int Height { get; private set; }

        public ShowAssetPreviewAttribute(int width = 64, int height = 64)
        {
            this.Width = width;
            this.Height = height;
        }
    }
}

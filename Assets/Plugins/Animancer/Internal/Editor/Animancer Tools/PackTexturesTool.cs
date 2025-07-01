// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Animancer.Editor.Tools
{
    /// <summary>[Editor-Only] [Pro-Only] 
    /// A <see cref="AnimancerToolsWindow.Tool"/> for packing multiple <see cref="Texture2D"/>s into a single image.
    /// </summary>
    /// <remarks>
    /// Documentation: <see href="https://kybernetik.com.au/animancer/docs/manual/tools/pack-textures">Pack Textures</see>
    /// </remarks>
    /// https://kybernetik.com.au/animancer/api/Animancer.Editor.Tools/PackTexturesTool
    /// 
    [Serializable]
    public class PackTexturesTool : AnimancerToolsWindow.Tool
    {
        /************************************************************************************************************************/

        [SerializeField] private List<Object> _AssetsToPack;
        [SerializeField] private int _Padding;
        [SerializeField] private int _MaximumSize = 8192;

        [NonSerialized] private ReorderableList _TexturesDisplay;

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override int DisplayOrder => 0;

        /// <inheritdoc/>
        public override string Name => "Pack Textures";

        /// <inheritdoc/>
        public override string HelpURL => Strings.DocsURLs.PackTextures;

        /// <inheritdoc/>
        public override string Instructions
        {
            get
            {
                if (_AssetsToPack.Count == 0)
                    return "Add the texture, sprites, and folders you want to pack to the list.";

                return "Set the other details then click Pack and it will ask where you want to save the combined texture.";
            }
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override void OnEnable(int index)
        {
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override void DoBodyGUI()
        {
        }

        /************************************************************************************************************************/

        /// <summary>Removes any items from the `list` that are the same as earlier items.</summary>
        private static void RemoveDuplicates<T>(IList<T> list)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Combines the <see cref="_AssetsToPack"/> into a new texture and saves it.</summary>
        private void Pack()
        {
        }

        /************************************************************************************************************************/

        private HashSet<Texture2D> GatherTextures()
        {
            return default;
        }

        /************************************************************************************************************************/

        private HashSet<Sprite> GatherTightSprites()
        {
            return default;
        }

        /************************************************************************************************************************/

        private static void GatherTightSprites(ICollection<Sprite> sprites, Texture2D texture)
        {
        }

        /************************************************************************************************************************/

        private static Sprite CreateTightSprite(Sprite sprite)
        {
            return default;
        }

        /************************************************************************************************************************/

        private static bool MakeTexturesReadable(HashSet<Texture2D> textures)
        {
            return default;
        }

        /************************************************************************************************************************/

        private static void ForEachTextureInFolder(string path, Action<Texture2D> action)
        {
        }

        /************************************************************************************************************************/

        private static void CopyCompressionSettings(TextureImporter copyTo, IEnumerable<Texture2D> copyFrom)
        {
        }

        /************************************************************************************************************************/

        private static bool IsHigherQuality(TextureImporterCompression higher, TextureImporterCompression lower)
        {
            return default;
        }

        /************************************************************************************************************************/

        private static string GetCommonDirectory<T>(IList<T> objects) where T : Object
        {
            return default;
        }

        /************************************************************************************************************************/

        private static SpriteAlignment GetAlignment(Vector2 pivot)
        {
            return default;
        }

        /************************************************************************************************************************/

        private static TextureImporter GetTextureImporter(Object asset)
        {
            return default;
        }

        private static TextureImporter GetTextureImporter(string path)
            => AssetImporter.GetAtPath(path) as TextureImporter;

        /************************************************************************************************************************/
    }
}

#endif


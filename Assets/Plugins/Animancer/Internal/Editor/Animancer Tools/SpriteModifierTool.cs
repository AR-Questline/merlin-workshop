// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Animancer.Editor.Tools
{
    /// <summary>[Editor-Only] [Pro-Only]
    /// A base <see cref="AnimancerToolsWindow.Tool"/> for modifying <see cref="Sprite"/>s.
    /// </summary>
    /// <remarks>
    /// Documentation: <see href="https://kybernetik.com.au/animancer/docs/manual/tools">Animancer Tools</see>
    /// </remarks>
    /// https://kybernetik.com.au/animancer/api/Animancer.Editor.Tools/SpriteModifierTool
    /// 
    [Serializable]
    public abstract class SpriteModifierTool : AnimancerToolsWindow.Tool
    {
        /************************************************************************************************************************/

        private static readonly List<Sprite> SelectedSprites = new List<Sprite>();
        private static bool _HasGatheredSprites;

        /// <summary>The currently selected <see cref="Sprite"/>s.</summary>
        public static List<Sprite> Sprites
        {
            get
            {
                if (!_HasGatheredSprites)
                {
                    _HasGatheredSprites = true;
                    GatherSelectedSprites(SelectedSprites);
                }

                return SelectedSprites;
            }
        }

        /// <inheritdoc/>
        public override void OnSelectionChanged()
        {
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override void DoBodyGUI()
        {
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Adds all <see cref="Sprite"/>s in the <see cref="Selection.objects"/> or their sub-assets to the
        /// list of `sprites`.
        /// </summary>
        public static void GatherSelectedSprites(List<Sprite> sprites)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Returns all the <see cref="Sprite"/> sub-assets of the `texture`.</summary>
        public static Sprite[] LoadAllSpritesInTexture(Texture2D texture)
            => LoadAllSpritesAtPath(AssetDatabase.GetAssetPath(texture));

        /// <summary>Returns all the <see cref="Sprite"/> assets at the `path`.</summary>
        public static Sprite[] LoadAllSpritesAtPath(string path)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>Calls <see cref="EditorUtility.NaturalCompare"/> on the <see cref="Object.name"/>s.</summary>
        public static int NaturalCompare(Object a, Object b) => EditorUtility.NaturalCompare(a.name, b.name);

        /************************************************************************************************************************/

        /// <summary>The message to confirm that the user is certain they want to apply the changes.</summary>
        protected virtual string AreYouSure => "Are you sure you want to modify these Sprites?";

        /// <summary>Called immediately after the user confirms they want to apply changes.</summary>
        protected virtual void PrepareToApply() {
        }

        /// <summary>Applies the desired modifications to the `data` before it is saved.</summary>
        protected virtual void Modify(SpriteDataEditor data, int index, Sprite sprite)
        {
        }

        /// <summary>Applies the desired modifications to the `data` before it is saved.</summary>
        protected virtual void Modify(TextureImporter importer, List<Sprite> sprites)
        {
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Asks the user if they want to modify the target <see cref="Sprite"/>s and calls <see cref="Modify"/>
        /// on each of them before saving any changes.
        /// </summary>
        protected void AskAndApply()
        {
        }

        /************************************************************************************************************************/
    }
}

#endif


// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Animancer.Editor.Tools
{
    /// <summary>[Editor-Only] [Pro-Only] 
    /// A <see cref="SpriteModifierTool"/> for generating <see cref="AnimationClip"/>s from <see cref="Sprite"/>s.
    /// </summary>
    /// <remarks>
    /// Documentation: <see href="https://kybernetik.com.au/animancer/docs/manual/tools/generate-sprite-animations">Generate Sprite Animations</see>
    /// </remarks>
    /// https://kybernetik.com.au/animancer/api/Animancer.Editor.Tools/GenerateSpriteAnimationsTool
    /// 
    [Serializable]
    public class GenerateSpriteAnimationsTool : SpriteModifierTool
    {
        /************************************************************************************************************************/
        #region Tool
        /************************************************************************************************************************/

        [NonSerialized] private List<string> _Names;
        [NonSerialized] private Dictionary<string, List<Sprite>> _NameToSprites;
        [NonSerialized] private ReorderableList _Display;
        [NonSerialized] private bool _NamesAreDirty;

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override int DisplayOrder => 3;

        /// <inheritdoc/>
        public override string Name => "Generate Sprite Animations";

        /// <inheritdoc/>
        public override string HelpURL => Strings.DocsURLs.GenerateSpriteAnimations;

        /// <inheritdoc/>
        public override string Instructions
        {
            get
            {
                if (Sprites.Count == 0)
                    return "Select the Sprites you want to generate animations from.";

                return "Click Generate.";
            }
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override void OnEnable(int index)
        {
        }

        /************************************************************************************************************************/

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
        #endregion
        /************************************************************************************************************************/
        #region Methods
        /************************************************************************************************************************/

        /// <summary>Uses <see cref="GatherNameToSprites"/> and creates new animations from those groups.</summary>
        private static void GenerateAnimationsBySpriteName(List<Sprite> sprites)
        {
        }

        /************************************************************************************************************************/

        private static char[] _Numbers, _TrimOther;

        /// <summary>Groups the `sprites` by name into the `nameToSptires`.</summary>
        private static void GatherNameToSprites(List<Sprite> sprites, Dictionary<string, List<Sprite>> nameToSprites)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Creates and saves a new <see cref="AnimationClip"/> that plays the `sprites`.</summary>
        private static void CreateAnimation(string path, params Sprite[] sprites)
        {
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Menu Functions
        /************************************************************************************************************************/

        private const string GenerateAnimationsBySpriteNameFunctionName = "Generate Animations By Sprite Name";

        /************************************************************************************************************************/

        /// <summary>Should <see cref="GenerateAnimationsBySpriteName()"/> be enabled or greyed out?</summary>
        [MenuItem(Strings.CreateMenuPrefix + GenerateAnimationsBySpriteNameFunctionName, validate = true)]
        private static bool ValidateGenerateAnimationsBySpriteName()
        {
            return default;
        }

        /// <summary>Calls <see cref="GenerateAnimationsBySpriteName(List{Sprite})"/> with the selected <see cref="Sprite"/>s.</summary>
        [MenuItem(Strings.CreateMenuPrefix + GenerateAnimationsBySpriteNameFunctionName, priority = Strings.AssetMenuOrder + 13)]
        private static void GenerateAnimationsBySpriteName()
        {
        }

        /************************************************************************************************************************/

        private static List<Sprite> _CachedSprites;

        /// <summary>
        /// Returns a list of <see cref="Sprite"/>s which will be passed into
        /// <see cref="GenerateAnimationsBySpriteName(List{Sprite})"/> by <see cref="EditorApplication.delayCall"/>.
        /// </summary>
        private static List<Sprite> GetCachedSpritesToGenerateAnimations()
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Adds the <see cref="MenuCommand.context"/> to the <see cref="GetCachedSpritesToGenerateAnimations"/>.
        /// </summary>
        [MenuItem("CONTEXT/" + nameof(Sprite) + GenerateAnimationsBySpriteNameFunctionName)]
        private static void GenerateAnimationsFromSpriteByName(MenuCommand command)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Should <see cref="GenerateAnimationsFromTextureBySpriteName"/> be enabled or greyed out?</summary>
        [MenuItem("CONTEXT/" + nameof(TextureImporter) + GenerateAnimationsBySpriteNameFunctionName, validate = true)]
        private static bool ValidateGenerateAnimationsFromTextureBySpriteName(MenuCommand command)
        {
            return default;
        }

        /// <summary>
        /// Adds all <see cref="Sprite"/> sub-assets of the <see cref="MenuCommand.context"/> to the
        /// <see cref="GetCachedSpritesToGenerateAnimations"/>.
        /// </summary>
        [MenuItem("CONTEXT/" + nameof(TextureImporter) + GenerateAnimationsBySpriteNameFunctionName)]
        private static void GenerateAnimationsFromTextureBySpriteName(MenuCommand command)
        {
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

#endif


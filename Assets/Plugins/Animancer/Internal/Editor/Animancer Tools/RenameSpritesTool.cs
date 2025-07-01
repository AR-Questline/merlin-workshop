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
    /// <summary>[Editor-Only] [Pro-Only] A <see cref="SpriteModifierTool"/> for bulk-renaming <see cref="Sprite"/>s.</summary>
    /// <remarks>
    /// Documentation: <see href="https://kybernetik.com.au/animancer/docs/manual/tools/rename-sprites">Rename Sprites</see>
    /// </remarks>
    /// https://kybernetik.com.au/animancer/api/Animancer.Editor.Tools/RenameSpritesTool
    /// 
    [Serializable]
    public class RenameSpritesTool : SpriteModifierTool
    {
        /************************************************************************************************************************/

        [NonSerialized] private List<string> _Names;
        [NonSerialized] private bool _NamesAreDirty;
        [NonSerialized] private ReorderableList _SpritesDisplay;
        [NonSerialized] private ReorderableList _NamesDisplay;

        [SerializeField] private string _NewName = "";
        [SerializeField] private int _MinimumDigits;

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override int DisplayOrder => 2;

        /// <inheritdoc/>
        public override string Name => "Rename Sprites";

        /// <inheritdoc/>
        public override string HelpURL => Strings.DocsURLs.RenameSprites;

        /// <inheritdoc/>
        public override string Instructions
        {
            get
            {
                if (Sprites.Count == 0)
                    return "Select the Sprites you want to rename.";

                return "Enter the new name(s) you want to give the Sprites then click Apply.";
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

        /// <summary>Refreshes the <see cref="_Names"/>.</summary>
        private void UpdateNames()
        {
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override void DoBodyGUI()
        {
        }

        /************************************************************************************************************************/

        // We could prevent it from causing animations to lose their data by using ISpriteEditorDataProvider
        // instead of TextureImporter, but it's in the 2D Sprite package which Animancer does not otherwise require.

        private const string ReferencesLostMessage =
            "Any references to the renamed Sprites will be lost (including animations that use them)" +
            " but you can use the 'Remap Sprite Animations' tool to reassign them afterwards.";

        /************************************************************************************************************************/

        /// <inheritdoc/>
        protected override string AreYouSure =>
            "Are you sure you want to rename these Sprites?" +
            "\n\n" + ReferencesLostMessage;

        /************************************************************************************************************************/

        private static Dictionary<Sprite, string> _SpriteToName;

        /// <inheritdoc/>
        protected override void PrepareToApply()
        {
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        protected override void Modify(SpriteDataEditor data, int index, Sprite sprite)
        {
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        protected override void Modify(TextureImporter importer, List<Sprite> sprites)
        {
        }

        /************************************************************************************************************************/
    }
}

#endif


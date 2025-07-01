// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Animancer.Editor.Tools
{
    /// <summary>[Editor-Only] [Pro-Only] 
    /// An <see cref="AnimationModifierTool"/> for changing which <see cref="Sprite"/>s an
    /// <see cref="AnimationClip"/> uses.
    /// </summary>
    /// <remarks>
    /// Documentation: <see href="https://kybernetik.com.au/animancer/docs/manual/tools/remap-sprite-animation">Remap Sprite Animation</see>
    /// </remarks>
    /// https://kybernetik.com.au/animancer/api/Animancer.Editor.Tools/RemapSpriteAnimationTool
    /// 
    [Serializable]
    public class RemapSpriteAnimationTool : AnimationModifierTool
    {
        /************************************************************************************************************************/

        [SerializeField] private List<Sprite> _NewSprites;

        [NonSerialized] private List<Sprite> _OldSprites;
        [NonSerialized] private bool _OldSpritesAreDirty;
        [NonSerialized] private ReorderableList _OldSpriteDisplay;
        [NonSerialized] private ReorderableList _NewSpriteDisplay;
        [NonSerialized] private EditorCurveBinding _SpriteBinding;
        [NonSerialized] private ObjectReferenceKeyframe[] _SpriteKeyframes;

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override int DisplayOrder => 4;

        /// <inheritdoc/>
        public override string Name => "Remap Sprite Animation";

        /// <inheritdoc/>
        public override string HelpURL => Strings.DocsURLs.RemapSpriteAnimation;

        /// <inheritdoc/>
        public override string Instructions
        {
            get
            {
                if (Animation == null)
                    return "Select the animation you want to remap.";

                if (_OldSprites.Count == 0)
                    return "The selected animation does not use Sprites.";

                return "Assign the New Sprites that you want to replace the Old Sprites with then click Save As." +
                    " You can Drag and Drop multiple Sprites onto the New Sprites list at the same time.";
            }
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override void OnEnable(int index)
        {
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        protected override void OnAnimationChanged()
        {
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override void DoBodyGUI()
        {
        }

        /************************************************************************************************************************/

        /// <summary>Gathers the <see cref="_OldSprites"/> from the <see cref="AnimationModifierTool.Animation"/>.</summary>
        private void GatherOldSprites()
        {
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        protected override void Modify(AnimationClip animation)
        {
        }

        /************************************************************************************************************************/
    }
}

#endif


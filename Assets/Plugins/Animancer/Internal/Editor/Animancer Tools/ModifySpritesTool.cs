// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

#if UNITY_EDITOR

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value.

using System;
using UnityEditor;
using UnityEngine;

namespace Animancer.Editor.Tools
{
    /// <summary>[Editor-Only] [Pro-Only] 
    /// A <see cref="SpriteModifierTool"/> for modifying <see cref="Sprite"/> detauls.
    /// </summary>
    /// <remarks>
    /// Documentation: <see href="https://kybernetik.com.au/animancer/docs/manual/tools/modify-sprites">Modify Sprites</see>
    /// </remarks>
    /// https://kybernetik.com.au/animancer/api/Animancer.Editor.Tools/ModifySpritesTool
    /// 
    [Serializable]
    public class ModifySpritesTool : SpriteModifierTool
    {
        /************************************************************************************************************************/

        [SerializeField] private OffsetRectMode _RectMode;
        [SerializeField] private Rect _RectOffset;

        [SerializeField] private bool _SetPivot;
        [SerializeField] private Vector2 _Pivot;

        [SerializeField] private bool _SetAlignment;
        [SerializeField] private SpriteAlignment _Alignment;

        [SerializeField] private bool _SetBorder;
        [SerializeField] private RectOffset _Border;

        [SerializeField] private bool _ShowDetails;

        /************************************************************************************************************************/

        private enum OffsetRectMode { None, Add, Subtract }
        private static readonly string[] OffsetRectModes = { "None", "Add", "Subtract" };

        private SerializedProperty _SerializedProperty;

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override int DisplayOrder => 1;

        /// <inheritdoc/>
        public override string Name => "Modify Sprites";

        /// <inheritdoc/>
        public override string HelpURL => Strings.DocsURLs.ModifySprites;

        /// <inheritdoc/>
        public override string Instructions
        {
            get
            {
                if (Sprites.Count == 0)
                    return "Select the Sprites you want to modify.";

                if (!IsValidModification())
                    return "The current Rect Offset would move some Sprites outside the texture bounds.";

                return "Enter the desired modifications and click Apply.";
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

        private bool IsValidModification()
        {
            return default;
        }

        /************************************************************************************************************************/

        private Rect GetOffset()
        {
            return default;
        }

        private static Rect Add(Rect a, Rect b)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        protected override string AreYouSure => "Are you sure you want to modify the borders of these Sprites?";

        /************************************************************************************************************************/

        /// <inheritdoc/>
        protected override void Modify(SpriteDataEditor data, int index, Sprite sprite)
        {
        }

        /************************************************************************************************************************/
    }
}

#endif


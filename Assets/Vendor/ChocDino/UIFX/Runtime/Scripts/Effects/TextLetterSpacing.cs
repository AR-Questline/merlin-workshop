using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityInternal = UnityEngine.Internal;

namespace ChocDino.UIFX
{
	/// <summary>
	/// Adjust letter spacing (kerning) on Text UGUI component
	/// Supports multi-lines, spaces and richtext
	/// </summary>
	[ExecuteInEditMode]
	[RequireComponent(typeof(Text))]
	[HelpURL("https://www.chocdino.com/products/unity-assets/")]
	[AddComponentMenu("UI/Chocolate Dinosaur UIFX/Effects/UIFX - Text Letter Spacing")]
	public class TextLetterSpacing : UIBehaviour, IMeshModifier
	{
		[SerializeField] float _letterSpacing = 0f;
		[SerializeField, Range(0f, 1f)] float _strength = 1f;

		private Text _text;
		private static UIVertex[] s_vt = new UIVertex[4];
		private List<UICharInfo> _charList = new List<UICharInfo>(32);
		private List<UILineInfo> _lineList = new List<UILineInfo>(4);

		protected Text TextComponent
		{
			get	{ if (_text == null) { _text = GetComponent<Text>(); } return _text; }
		}

		public float LetterSpacing
		{
			get { return _letterSpacing; }
			set { if (value != _letterSpacing) { _letterSpacing = value; ForceUpdate(); } }
		}

		public float Strength
		{
			get { return _strength; }
			set { value = Mathf.Clamp01(value); if (value != _strength) { _strength = value; ForceUpdate(); } }
		}

		private void ForceUpdate()
        {
        }

#if UNITY_EDITOR
        protected override void Reset()
        {
        }

        protected override void OnValidate()
        {
        }
#endif

        protected override void OnDisable()
        {
        }

        public void ModifyMesh(VertexHelper vh)
        {
        }

        [UnityInternal.ExcludeFromDocs]
        [System.Obsolete("use IMeshModifier.ModifyMesh (VertexHelper verts) instead, or set useLegacyMeshGeneration to false", false)]
        public void ModifyMesh(Mesh mesh)
        {
        }
    }
}
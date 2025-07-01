//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;

namespace ChocDino.UIFX
{
	/// <summary>
	/// Generates mipmap texture for UI component allowing less aliasing when scaling down.
	/// This is most useful for world-space rendering.
	/// </summary>
	[AddComponentMenu("UI/Chocolate Dinosaur UIFX/Filters/UIFX - Mipmap Filter", 100)]
	public class MipmapFilter : FilterBase
	{
		[SerializeField] bool _generateMipMap = true;
		[SerializeField, Range(-12f, 12f)] float _mipMapBias = 0f;
		[SerializeField, Range(0f, 16f)] int _anisoLevel = 4;
		
		/// <summary></summary>
		public bool GenerateMipMap { get { return _generateMipMap; } set { ChangeProperty(ref _generateMipMap, value); } }

		/// <summary></summary>
		public float MipMapBias { get { return _mipMapBias; } set { ChangeProperty(ref _mipMapBias, value); } }

		/// <summary></summary>
		public int AnisoLevel { get { return _anisoLevel; } set { ChangeProperty(ref _anisoLevel, Mathf.Clamp(value, 0, 16)); } }

		private RenderTexture _rt;

		protected override bool DoParametersModifySource()
        {
            return default;
        }

        protected override void OnEnable()
        {
        }

        protected override void OnDisable()
        {
        }

        protected override RenderTexture RenderFilters(RenderTexture source)
        {
            return default;
        }
    }
}
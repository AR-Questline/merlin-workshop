//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;

namespace ChocDino.UIFX
{
	/// <summary>Which axes to blur in a 2D blur</summary>
	public enum BlurAxes2D
	{
		/// <summary>`Default` - Blur both horizontally and vertically.</summary>
		Default,
		/// <summary>`Horizontal` - Only blur horizontally. This is faster than `Default`.</summary>
		Horizontal,
		/// <summary>`Vertical	` - Only blur vertically. This is faster than `Default`.</summary>
		Vertical,
	}

	interface ITextureBlur
	{
		BlurAxes2D BlurAxes2D { get; set; }
		Downsample Downsample { get; set; }
		void ForceDirty();
		void SetBlurSize(float diagonalPercent);
		void AdjustBoundsSize(ref Vector2Int leftDown, ref Vector2Int rightUp);
		RenderTexture Process(RenderTexture source);
		void FreeResources();
	}
}
//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

namespace ChocDino.UIFX
{
	/// <summary>Which vertex modifiers affect are used to calculate the vertex modifier effect.</summary>
	public enum VertexModifierSource
	{
		/// <summary>Only Transform changes affect the effect.</summary>
		Transform,
		/// <summary>Only vertex changes (usually through IMeshModifier effects) affect the effect.</summary>
		Vertex,
		/// <summary>Both Transform changes and vertex changes (usually through IMeshModifier effects) affect the effect.  This is the most expensive mode.</summary>
		TranformAndVertex,
	}

	/// <summary>Modes describing how a gradient wraps.</summary>
	public enum GradientWrapMode
	{
		/// <summary>No wrapping, edge values will be used.</summary>
		None,
		/// <summary>The gradient repeats.</summary>
		Wrap,
		/// <summary>The gradient repeats with mirroring.</summary>
		Mirror,
	}

	/// <summary>How to much downsample the texture by.</summary>
	public enum Downsample
	{
		/// <summary>Automatic downsampling will depend on the platform.</summary>
		Auto = 0,
		/// <summary>No downsampling.</summary>
		None = 1,
		/// <summary>Downsample to half the size.</summary>
		Half = 2,
		/// <summary>Downsample to a quarter the size.</summary>
		Quarter = 4,
		/// <summary>Downsample to an eighth the size.</summary>
		Eighth = 8,
	}
}
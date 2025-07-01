#if UIFX_BETA

//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;

namespace ChocDino.UIFX
{
	/// <summary>
	/// </summary>
	[AddComponentMenu("UI/Chocolate Dinosaur UIFX/Filters/UIFX - Blur Zoom Filter")]
	public class BlurZoomFilter : FilterBase
	{
		[Tooltip("")]
		[SerializeField] Vector2 _center = Vector2.zero;

		[Tooltip("")]
		[Range(1f, 10f)]
		[SerializeField] float _scale = 2f;

		[Tooltip("The type of weights to use for the blur, this changes the visual appearance with Falloff looking higher quality, for little extra cost.")]
		[SerializeField] BlurDirectionalWeighting _weights = BlurDirectionalWeighting.Falloff;

		[Tooltip("")]
		[Range(0f, 8f)]
		[SerializeField] float _weightsPower = 1f;

		[Tooltip("")]
		[SerializeField] BlurDirectionalSide _side = BlurDirectionalSide.One;

		[Tooltip("The amount of dithering to apply, useful to hide banding artifacts and also for styling. Range is [0..1]. Default is 0.0")]
		[Range(0f, 1f)]
		[SerializeField] float _dither = 0f;

		[Tooltip("Toggle the use of the alpha curve to fade to transparent as Strength increases.")]
		[SerializeField] bool _applyAlphaCurve = false;

		[Tooltip("An optional curve to allow the Graphic to fade to transparent as the Strength property increases.")]
		[SerializeField] AnimationCurve _alphaCurve = new AnimationCurve(new Keyframe(0f, 1f, -1f, -1f), new Keyframe(1f, 0f, -1f, -1f));

		[Tooltip("Tint (multiply) the blurred color by this for styling.")]
		[SerializeField] Color _tintColor = Color.white;

		[Tooltip("")]
		[Range(0f, 2f)]
		[SerializeField] float _power = 1f;

		[Tooltip("")]
		[Range(0f, 8f)]
		[SerializeField] float _intensity = 1f;

		[Tooltip("How the source graphic and the blurred graphic are blended/composited together.")]
		[SerializeField] BlurDirectionalBlend _blend = BlurDirectionalBlend.Replace;

		/// <summary></summary>
		public Vector2 Center { get { return _center; } set { ChangeProperty(ref _center, value); } }

		/// <summary></summary>
		public float Scale { get { return _scale; } set { ChangeProperty(ref _scale, value); } }

		/// <summary>The type of weights to use for the blur, this changes the visual appearance with Falloff looking higher quality, for little extra cost.</summary>
		public BlurDirectionalWeighting Weights { get { return _weights; } set { ChangeProperty(ref _weights, value); } }

		/// <summary></summary>
		public float WeightsPower { get { return _weightsPower; } set { ChangeProperty(ref _weightsPower, value); } }

		/// <summary></summary>
		public BlurDirectionalSide Side { get { return _side; } set { ChangeProperty(ref _side, value); } }

		/// <summary>The amount of dithering to apply, useful to hide banding artifacts and also for styling. Range is [0..1]. Default is 0.0</summary>
		public float Dither { get { return _dither; } set { ChangeProperty(ref _dither, value); } }

		/// <summary>Toggle the use of the alpha curve to fade to transparent as Strength increases.</summary>
		public bool ApplyAlphaCurve { get { return _applyAlphaCurve; } set { ChangeProperty(ref _applyAlphaCurve, value); } }

		/// <summary>An optional curve to allow the Graphic to fade to transparent as the Strength property increases.</summary>
		public AnimationCurve AlphaCurve { get { return _alphaCurve; } set { ChangePropertyRef(ref _alphaCurve, value); } }

		/// <summary>Tint (multiply) the blurred color by this for styling.</summary>
		public Color TintColor { get { return _tintColor; } set { ChangeProperty(ref _tintColor, value); } }

		/// <summary></summary>
		public float Power { get { return _power; } set { ChangeProperty(ref _power, value); } }

		/// <summary></summary>
		public float Intensity { get { return _intensity; } set { ChangeProperty(ref _intensity, value); } }

		/// <summary>How the source graphic and the blurred graphic are blended/composited together.</summary>
		public BlurDirectionalBlend Blend { get { return _blend; } set { ChangeProperty(ref _blend, value); } }

		class BlurShader
		{
			const string Path = "Hidden/ChocDino/UIFX/Blur-Zoom";

			static class Prop
			{
				public readonly static int CenterInvScale = Shader.PropertyToID("_CenterInvScale");
				public readonly static int Dither = Shader.PropertyToID("_Dither");
				public readonly static int WeightsPower = Shader.PropertyToID("_WeightsPower");
			}
			static class Pass
			{
				public const int Linear = 0;
				public const int Falloff = 1;
			}
			static class Keyword
			{
				public const string UseDither = "USE_DITHER";
				public const string DirBoth = "DIR_BOTH";
			}

			private RenderTexture _rt;
			private Material _material;

			void CreateResources()
			{
				if (_material == null)
				{
					Shader shader = Shader.Find(Path);
					if (shader != null)
					{
						_material = new Material(shader);
					}
				}
			}

			internal void FreeResources()
			{
				ObjectHelper.Destroy(ref _material);
				RenderTextureHelper.ReleaseTemporary(ref _rt);
			}

			internal RenderTexture Render(RenderTexture sourceTexture, float scale, Vector2 center, bool weightsLinear, float weightsPower, float dither, BlurDirectionalSide side)
			{
				int width = sourceTexture.width;
				int height = sourceTexture.height;
				if (_rt != null && (_rt.width != width || _rt.height != height))
				{
					RenderTextureHelper.ReleaseTemporary(ref _rt);
				}
				if (_rt == null)
				{
					RenderTextureFormat format = sourceTexture.format;
					if ((Filters.PerfHint & PerformanceHint.UseLessPrecision) != 0)
					{
						// TODO: create based on the input texture format, but just with less precision
						if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.Default))
						{
							format = RenderTextureFormat.Default;
						}
					}
					else if ((Filters.PerfHint & PerformanceHint.UseMorePrecision) != 0)
					{
						// TODO: create based on the input texture format, but just with more precision
						if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf))
						{
							format = RenderTextureFormat.ARGBHalf;
						}
					}
					_rt = RenderTexture.GetTemporary(width, height, 0, format, RenderTextureReadWrite.Linear);
				}

				CreateResources();

				_material.SetVector(Prop.CenterInvScale, new Vector3(center.x, center.y, 1f / scale));
				if (dither > 0f)
				{
					_material.SetFloat(Prop.Dither, dither);
					_material.EnableKeyword(Keyword.UseDither);
				}
				else
				{
					_material.DisableKeyword(Keyword.UseDither);
				}

				switch (side)
				{
					case BlurDirectionalSide.One:
					_material.DisableKeyword(Keyword.DirBoth);
					break;
					case BlurDirectionalSide.Both:
					_material.EnableKeyword(Keyword.DirBoth);
					break;
				}

				if (!weightsLinear)
				{
					_material.SetFloat(Prop.WeightsPower, weightsPower);
				}

				Graphics.Blit(sourceTexture, _rt, _material, weightsLinear ? Pass.Linear : Pass.Falloff);
				_rt.IncrementUpdateCount();

				return _rt;
			}
		}

		private BlurShader _blurShader;

		protected override string GetDisplayShaderPath()
		{
			return BlurDirectionalFilter.CompositeShader.Path;
		}

		protected override bool DoParametersModifySource()
		{
			if (base.DoParametersModifySource())
			{
				if (_scale == 1f && _tintColor == Color.white) return false;
				return true;
			}
			return false;
		}

		protected override void OnEnable()
		{
			_blurShader = new BlurShader();
			base.OnEnable();
		}

		protected override void OnDisable()
		{
			if (_blurShader != null)
			{
				_blurShader.FreeResources();
				_blurShader = null;
			}
			base.OnDisable();
		}

		protected override float GetAlpha()
		{
			float alpha = 1f;
			if (_alphaCurve != null && _applyAlphaCurve)
			{
				if (_alphaCurve.length > 0)
				{
					alpha = _alphaCurve.Evaluate(_strength);
				}
			}
			return alpha;
		}

		protected override void GetFilterAdjustSize(ref Vector2Int leftDown, ref Vector2Int rightUp)
		{
			// TODO: make this calculation not depend on the source rectangle.  Need to do this because:
			// 1) not intutitive
			// 2) the concistent - the size of the blur will depend on the screen-size of the object
			// 3) using FilterHover component causes jittering it calls GetFilterAdjustSize() which tries to GetRect() at the wrong time..

			// Get the source rectangle size and scale the corners to work out how much it needs to grow by
			Rect size = _screenRect.GetRect();
			var texSize = new Vector2(size.width, size.height);
			if (_renderSpace == FilterRenderSpace.Canvas)
			{
				texSize *= ResolutionScalingFactor;
			}

			Vector2 centerUV = (_center + Vector2.one) * 0.5f;
			Vector2 centerTexel = centerUV * texSize;
			Vector2 p0 = ((Vector2.zero - centerTexel) * _scale) + centerTexel;
			Vector2 p1 = ((texSize - centerTexel) * _scale) + centerTexel;

			p0 = p0 - Vector2.zero;
			p1 = p1 - texSize;

			// With falloff weights the blur doesn't visibly reach as far, so we apply some heuristic logic to shrink the rectangle size
			if (_weights == BlurDirectionalWeighting.Falloff)
			{
				float w = 1f;
				if (_weightsPower > 1f)
				{
					w = (_weightsPower - 1f) / 7f;
					w = Mathf.Lerp(1f, 2.66f, w);
				}
				p0 /= w;
				p1 /= w;
			}

			p0 = new Vector2(Mathf.Abs(p0.x), Mathf.Abs(p0.y));
			p1 = new Vector2(Mathf.Abs(p1.x), Mathf.Abs(p1.y));

			p0 *= _strength;
			p1 *= _strength;

			leftDown += new Vector2Int(Mathf.RoundToInt(p0.x), Mathf.RoundToInt(p0.y));
			rightUp += new Vector2Int(Mathf.RoundToInt(p1.x), Mathf.RoundToInt(p1.y));
		}

		protected override RenderTexture RenderFilters(RenderTexture source)
		{
			if (GetAlpha() > 0f)
			{
				return _blurShader.Render(source, Mathf.Lerp(1f, _scale, _strength), (_center + Vector2.one) * 0.5f, _weights == BlurDirectionalWeighting.Linear || _weightsPower == 0f, _weightsPower, _dither * _strength * 0.1f, _side);
			}
			return null;
		}

		protected override void SetupDisplayMaterial(Texture source, Texture result)
		{
			BlurDirectionalFilter.CompositeShader.Apply(_displayMaterial, _strength, Color.Lerp(Color.white, _tintColor, _strength), Mathf.Lerp(1f, _power, _strength), Mathf.Lerp(1f, _intensity, _strength), _blend);
			base.SetupDisplayMaterial(source, result);
		}
	}
}

#endif
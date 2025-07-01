//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

#if UIFX_BETA

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityInternal = UnityEngine.Internal;

namespace ChocDino.UIFX
{
	/// <summary>
	/// </summary>
	[AddComponentMenu("UI/Chocolate Dinosaur UIFX/Filters/UIFX - Doom Melt Filter (beta)", 200)]
	public class DoomMeltFilter : FilterBase
	{
		private const string BlendShaderPath = "Hidden/ChocDino/UIFX/Blend-Doom";

		[Range(-1, 512)]
		[SerializeField] int _randomSeed = -1;

		[Range(1, 32)]
		[SerializeField] int _columnWidth = 4;

		[Range(1, 32)]
		[SerializeField] int _stepSize = 8;

		[Range(-1f, 1f)]
		[SerializeField] float _time = 0f;

		private int _textureGenHash = 0;
		private byte[] _rawBytes = new byte[1024];
		private Texture2D _timingTexture;

		static new class ShaderProp
		{
			public readonly static int TimingTex = Shader.PropertyToID("_TimingTex");
			public readonly static int Timing = Shader.PropertyToID("_Timing");
		}

		protected override string GetDisplayShaderPath()
		{
			return BlendShaderPath;
		}

		protected override bool DoParametersModifySource()
		{
			if (!base.DoParametersModifySource())
			{
				return false;
			}
			if (_time == 0.0f) return false;
			return true;
		}

		protected override void OnEnable()
		{
			_textureGenHash = 0;
			_timingTexture = new Texture2D(1024, 1, TextureFormat.Alpha8, mipChain:false, linear:true);
			_timingTexture.filterMode = FilterMode.Point;
			_timingTexture.wrapMode = TextureWrapMode.Repeat;
			base.OnEnable();
		}

		protected override void OnDisable()
		{
			ObjectHelper.Destroy(ref _timingTexture);
			base.OnDisable();
		}

		private void UpdateTimingTexture()
		{
			Random.State oldState = Random.state;
			int seed = _randomSeed;
			if (seed < 0)
			{
				seed = (int)System.DateTime.Now.Ticks;
			}
			Random.InitState(seed);

			int min = 255;
			int max = 0;
			{
				int range = _stepSize;
				int delay = 128;
				int lastDelay = 128;
				for (int i = 0; i < 1024; i++)
				{
					_rawBytes[i] = (byte)(lastDelay);
					if (i % _columnWidth == 0)
					{
						delay += Random.Range(-range, range + 1);
						delay = Mathf.Clamp(delay, 0, 255);
						lastDelay = delay;
						min = Mathf.Min(min, delay);
						max = Mathf.Max(max, delay);
					}
				}
			}
			
			// Normalize
			/*for (int i = 0; i < 1024; i++)
			{
				float delay = _rawBytes[i];
				delay = (delay - min) / (max - min);
				_rawBytes[i] = (byte)Mathf.FloorToInt(delay * 255f);
			}*/

			//Debug.Log("minmax: " + min + " " + max);

			_timingTexture.SetPixelData(_rawBytes, 0, 0);
			_timingTexture.Apply(updateMipmaps:false, makeNoLongerReadable:false);
			Random.state = oldState;
		}

		protected override void SetupDisplayMaterial(Texture source, Texture result)
		{
			_displayMaterial.SetTexture(ShaderProp.TimingTex, _timingTexture);
			_displayMaterial.SetFloat(ShaderProp.Timing, _time * _strength);
			base.SetupDisplayMaterial(source, result);
		}

		protected override void GetFilterAdjustSize(ref Vector2Int leftDown, ref Vector2Int rightUp)
		{
			// We want to extend down to the bottom of the screen
			//leftDown = new Vector2Int(0, 1+256);
			//rightUp = new Vector2Int(0, 1);
		}

		private int GetTextureGenHash()
		{
			return (_randomSeed) + (_columnWidth << 16) + (_stepSize << 24);
		}

		protected override RenderTexture RenderFilters(RenderTexture source)
		{
			int textureGenHash = GetTextureGenHash();
			if (textureGenHash != _textureGenHash)
			{
				UpdateTimingTexture();
				_textureGenHash = textureGenHash;
			}
			return source;
		}
	}
}

#endif
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Graphics
{
    /// <summary>
    /// Provides noise samples by using a pre-generated texture.
    /// </summary>
    public class NoiseSource {

        // === Fields

        Texture2D _noiseTexture;

        // === Constructors

        public NoiseSource(Texture2D noiseTexture) {
            _noiseTexture = noiseTexture;
        }

        // === Sampling

        /// <summary>
        /// Returns the noise value at given coordinates. The coordinates wrap around the texture,
        /// so NoiseAt(1.1,4.2) == NoiseAt(0.1,0.2).
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="smoothness">0-3, selects the texture channel from which we pull the values</param>
        /// <returns>the noise value in range [-1,+1]</returns>
        public float NoiseAt(float x, float y, int smoothness) {
            Color pixel = _noiseTexture.GetPixelBilinear(x, y);
            switch (smoothness) {
                case 0: return pixel.r * 2f - 1f;
                case 1: return pixel.g * 2f - 1f;
                case 2: return pixel.b * 2f - 1f;
                case 3: return pixel.a * 2f - 1f;
                default:
                    throw new ArgumentException("Smoothness must be in range [0-3].");
            }
        }

        /// <seealso cref="NoiseAt(float,float,int)"></seealso>
        [UnityEngine.Scripting.Preserve]
        public float NoiseAt(Vector2 xy, int smoothness) => NoiseAt(xy.x, xy.y, smoothness);
    }
}

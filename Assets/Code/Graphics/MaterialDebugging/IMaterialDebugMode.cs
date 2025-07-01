using UnityEngine;

namespace Awaken.TG.Graphics.MaterialDebugging {
    public interface IMaterialDebugMode {
        void Init(Renderer[] renderers);
        void Clear(Renderer[] renderers);
        
        public static readonly int ColorID = Shader.PropertyToID("_materialdebug_color");

        public static Shader GetDebugShader() {
            return Shader.Find("Hidden/materialdebug_lit");
        }
        
        public static Color GetDistinctColor(int index) {
            return index < DistinctColours.Length
                ? DistinctColours[index]
                : GetRandomColour();
        }

        static readonly Color[] DistinctColours = {
            new(1.00F, 1.00F, 0.00F), new(0.11F, 0.90F, 1.00F), new(1.00F, 0.20F, 1.00F), new(1.00F, 0.29F, 0.27F), 
            new(0.00F, 0.54F, 0.25F), new(0.00F, 0.44F, 0.65F), new(0.64F, 0.00F, 0.35F), new(1.00F, 0.86F, 0.90F), 
            new(0.48F, 0.29F, 0.00F), new(0.00F, 0.00F, 0.65F), new(0.39F, 1.00F, 0.67F), new(0.72F, 0.59F, 0.38F), 
            new(0.00F, 0.30F, 0.26F), new(0.56F, 0.69F, 1.00F), new(0.60F, 0.49F, 0.53F), new(0.35F, 0.00F, 0.03F), 
            new(0.50F, 0.59F, 0.58F), new(1.00F, 1.00F, 0.90F), new(0.11F, 0.27F, 0.00F), new(0.31F, 0.78F, 0.00F), 
            new(0.23F, 0.36F, 1.00F), new(0.29F, 0.23F, 0.33F), new(1.00F, 0.18F, 0.50F), new(0.38F, 0.38F, 0.35F), 
            new(0.73F, 0.04F, 0.00F), new(0.42F, 0.47F, 0.00F), new(0.00F, 0.76F, 0.63F), new(1.00F, 0.67F, 0.57F), 
            new(1.00F, 0.56F, 0.79F), new(0.73F, 0.01F, 0.67F), new(0.82F, 0.38F, 0.00F), new(0.87F, 0.94F, 1.00F), 
            new(0.00F, 0.00F, 0.21F), new(0.48F, 0.31F, 0.29F), new(0.63F, 0.76F, 0.60F), new(0.19F, 0.00F, 0.09F), 
            new(0.04F, 0.65F, 0.85F), new(0.00F, 0.20F, 0.29F), new(0.00F, 0.52F, 0.44F), new(0.22F, 0.13F, 0.00F), 
            new(1.00F, 0.71F, 0.00F), new(0.76F, 1.00F, 0.93F), new(0.63F, 0.47F, 0.75F), new(0.80F, 0.03F, 0.27F), 
            new(0.75F, 0.73F, 0.70F), new(0.76F, 1.00F, 0.60F), new(0.00F, 0.12F, 0.04F), new(0.00F, 0.28F, 0.61F), 
            new(0.44F, 0.00F, 0.38F), new(0.05F, 0.74F, 0.40F), new(0.93F, 0.76F, 1.00F), new(0.27F, 0.43F, 0.46F), 
            new(0.72F, 0.48F, 0.41F), new(0.48F, 0.53F, 0.63F), new(0.47F, 0.55F, 0.40F), new(0.53F, 0.33F, 0.47F), 
            new(0.98F, 0.82F, 0.62F), new(1.00F, 0.54F, 0.60F), new(0.82F, 0.34F, 0.63F), new(0.75F, 0.77F, 0.35F), 
            new(0.27F, 0.40F, 0.28F), new(0.00F, 0.53F, 0.93F), new(0.53F, 0.44F, 0.30F), new(0.20F, 0.21F, 0.18F), 
            new(0.71F, 0.66F, 0.74F), new(0.00F, 0.65F, 0.67F), new(0.27F, 0.17F, 0.17F), new(0.39F, 0.39F, 0.46F), 
            new(0.64F, 0.78F, 0.79F), new(1.00F, 0.57F, 0.25F), new(0.58F, 0.54F, 0.51F), new(0.34F, 0.33F, 0.16F), 
            new(0.00F, 1.00F, 0.81F), new(0.69F, 0.36F, 0.44F), new(0.55F, 0.82F, 1.00F), new(0.23F, 0.59F, 0.00F), 
            new(0.02F, 0.97F, 0.34F), new(0.78F, 0.63F, 0.63F), new(0.12F, 0.43F, 0.00F), new(0.47F, 0.00F, 0.84F), 
            new(0.65F, 0.46F, 0.00F), new(0.39F, 0.40F, 0.66F), new(0.63F, 0.35F, 0.22F), new(0.42F, 0.00F, 0.17F), 
            new(0.47F, 0.15F, 0.00F), new(0.84F, 0.56F, 1.00F), new(0.61F, 0.59F, 0.00F), new(0.33F, 0.62F, 0.47F), 
            new(1.00F, 0.96F, 0.62F), new(0.13F, 0.09F, 0.15F), new(0.45F, 0.25F, 0.56F), new(0.74F, 0.14F, 1.00F), 
            new(0.60F, 0.68F, 0.75F), new(0.23F, 0.14F, 0.40F), new(0.57F, 0.14F, 0.16F), new(0.36F, 0.27F, 0.20F), 
            new(0.99F, 0.91F, 0.86F), new(0.25F, 0.31F, 0.33F), new(0.00F, 0.54F, 0.64F), new(0.80F, 0.49F, 0.60F), 
            new(0.64F, 0.91F, 0.02F), new(0.20F, 0.31F, 0.45F), new(0.42F, 0.23F, 0.30F), new(0.51F, 0.67F, 0.35F), 
            new(0.00F, 0.11F, 0.12F), new(0.82F, 0.97F, 0.81F), new(0.00F, 0.29F, 0.16F), new(0.78F, 0.82F, 0.96F), 
            new(0.64F, 0.64F, 0.54F), new(0.50F, 0.42F, 0.40F), new(0.13F, 0.16F, 0.00F), new(0.75F, 0.34F, 0.31F), 
            new(0.91F, 0.19F, 0.00F), new(0.40F, 0.47F, 0.43F), new(0.85F, 0.00F, 0.49F), new(1.00F, 0.10F, 0.35F), 
            new(0.54F, 0.86F, 0.71F), new(0.12F, 0.01F, 0.00F), new(0.36F, 0.31F, 0.32F), new(0.78F, 0.58F, 0.77F), 
            new(0.20F, 0.00F, 0.20F), new(1.00F, 0.41F, 0.20F), new(0.40F, 0.88F, 0.83F), new(0.81F, 0.80F, 0.67F), 
            new(0.82F, 0.67F, 0.58F), new(0.49F, 0.83F, 0.47F), new(0.00F, 0.17F, 0.35F), new(0.48F, 0.48F, 1.00F), 
            new(0.84F, 0.56F, 0.00F), new(0.21F, 0.20F, 0.22F), new(0.47F, 0.69F, 0.63F), new(1.00F, 0.70F, 0.78F), 
            new(0.46F, 0.47F, 0.49F), new(0.51F, 0.45F, 0.58F), new(0.58F, 0.23F, 0.30F), new(0.71F, 0.96F, 1.00F), 
            new(0.82F, 0.86F, 0.84F), new(0.58F, 0.34F, 0.74F), new(0.42F, 0.44F, 0.29F), new(0.00F, 0.07F, 0.15F), 
            new(0.01F, 0.32F, 0.37F), new(0.04F, 0.64F, 0.97F), new(0.91F, 0.51F, 0.46F), new(0.86F, 0.84F, 0.87F), 
            new(0.37F, 0.74F, 0.82F), new(0.24F, 0.31F, 0.27F), new(0.49F, 0.39F, 0.02F), new(0.01F, 0.41F, 0.31F), 
            new(0.59F, 0.17F, 0.46F), new(0.55F, 0.52F, 0.27F), new(0.59F, 0.58F, 0.77F), new(0.91F, 0.45F, 0.81F), 
            new(0.85F, 0.42F, 0.47F), new(0.24F, 0.54F, 0.75F), new(0.79F, 0.51F, 0.31F), new(0.32F, 0.54F, 0.53F), 
            new(0.36F, 0.07F, 0.24F), new(0.33F, 0.51F, 0.23F), new(0.91F, 0.02F, 0.77F), new(0.00F, 0.00F, 0.37F), 
            new(0.66F, 0.45F, 0.60F), new(0.29F, 0.51F, 0.38F), new(0.35F, 0.45F, 0.54F), new(1.00F, 0.36F, 0.65F), 
            new(0.97F, 0.79F, 0.75F), new(0.39F, 0.19F, 0.15F), new(0.32F, 0.23F, 0.00F), new(0.42F, 0.58F, 0.67F), 
            new(0.32F, 0.63F, 0.35F), new(0.64F, 0.36F, 0.01F), new(0.11F, 0.09F, 0.01F), new(0.89F, 0.00F, 0.15F), 
            new(0.91F, 0.67F, 0.39F), new(0.30F, 0.38F, 0.00F), new(0.61F, 0.41F, 0.40F), new(0.39F, 0.33F, 0.48F), 
            new(0.59F, 0.59F, 0.62F), new(0.00F, 0.42F, 0.40F), new(0.22F, 0.08F, 0.02F), new(0.96F, 0.84F, 0.29F), 
            new(0.00F, 0.27F, 0.82F), new(0.00F, 0.42F, 0.19F), new(0.87F, 0.71F, 0.82F), new(0.49F, 0.40F, 0.44F), 
            new(0.62F, 0.70F, 0.64F), new(0.00F, 0.85F, 0.57F), new(0.08F, 0.63F, 0.54F), new(0.74F, 0.40F, 0.91F), 
            new(0.78F, 0.86F, 0.60F), new(0.13F, 0.23F, 0.24F), new(0.40F, 0.07F, 0.56F), new(0.42F, 0.23F, 0.39F), 
            new(0.96F, 0.88F, 1.00F), new(1.00F, 0.63F, 0.95F), new(0.80F, 0.67F, 0.21F), new(0.22F, 0.27F, 0.15F), 
            new(0.55F, 0.71F, 0.00F), new(0.47F, 0.47F, 0.41F), new(0.78F, 0.00F, 0.35F), new(0.23F, 0.00F, 0.04F), 
            new(0.78F, 0.38F, 0.25F), new(0.16F, 0.38F, 0.49F), new(0.25F, 0.14F, 0.20F), new(0.49F, 0.35F, 0.27F), 
            new(0.80F, 0.72F, 0.49F), new(0.72F, 0.51F, 0.51F), new(0.67F, 0.32F, 0.60F), new(0.71F, 0.84F, 0.76F), 
            new(0.64F, 0.52F, 0.41F), new(0.62F, 0.58F, 0.94F), new(0.65F, 0.27F, 0.44F), new(0.72F, 0.58F, 0.65F), 
            new(0.44F, 0.73F, 0.55F), new(0.00F, 0.71F, 0.20F), new(0.47F, 0.62F, 0.79F), new(0.43F, 0.50F, 0.73F), 
            new(0.58F, 0.25F, 0.00F), new(0.37F, 1.00F, 0.01F), new(0.89F, 1.00F, 0.99F), new(0.11F, 0.88F, 0.47F), 
            new(0.74F, 0.69F, 0.90F), new(0.46F, 0.57F, 0.18F), new(0.00F, 0.19F, 0.04F), new(0.00F, 0.38F, 0.80F), 
            new(0.82F, 0.00F, 0.59F), new(0.54F, 0.33F, 0.39F), new(0.16F, 0.13F, 0.11F), new(0.36F, 0.20F, 0.07F), 
            new(0.65F, 0.44F, 0.26F), new(0.54F, 0.25F, 0.18F), new(0.10F, 0.23F, 0.16F), new(0.29F, 0.29F, 0.35F), 
            new(0.66F, 0.55F, 0.52F), new(0.96F, 0.67F, 0.67F), new(0.64F, 0.95F, 0.67F), new(0.00F, 0.78F, 0.78F), 
            new(0.92F, 0.55F, 0.40F), new(0.58F, 0.54F, 0.62F), new(0.74F, 0.79F, 0.82F), new(0.62F, 0.63F, 0.39F), 
            new(0.75F, 0.28F, 0.00F), new(0.40F, 0.51F, 0.53F), new(0.51F, 0.64F, 0.52F), new(0.27F, 0.24F, 0.14F), 
            new(0.28F, 0.40F, 0.36F), new(0.23F, 0.25F, 0.00F), new(0.02F, 0.07F, 0.01F), new(0.87F, 0.98F, 0.44F), 
            new(0.53F, 0.56F, 0.49F), new(0.60F, 0.82F, 0.35F), new(0.42F, 0.56F, 0.49F), new(0.84F, 0.75F, 0.76F), 
            new(0.24F, 0.24F, 0.43F), new(0.85F, 0.24F, 0.40F), new(0.18F, 0.36F, 0.61F), new(0.42F, 0.37F, 0.27F), 
            new(0.82F, 0.36F, 0.53F), new(0.36F, 0.40F, 0.42F), new(0.00F, 0.71F, 0.50F), new(0.33F, 0.36F, 0.27F), 
            new(0.53F, 0.38F, 0.59F), new(0.21F, 0.36F, 0.15F), new(0.15F, 0.18F, 0.60F), new(0.00F, 0.80F, 1.00F), 
            new(0.40F, 0.31F, 0.38F), new(0.99F, 0.00F, 0.61F), new(0.57F, 0.54F, 0.42F), 
        };

        static Color GetRandomColour() {
            return new(Random.Range(0.05F, 0.95F), Random.Range(0.05F, 0.95F), Random.Range(0.05F, 0.95F));
        }
    }
}
Shader "Hidden/TG/TerrainHolesDebug" {
    Properties {
        [HideInInspector] _TerrainHolesTexture("Terrain Holes Texture", 2D) = "white" {}
        _HoleColor("Hole Color", Color) = (1, 0, 0, 1)
        _SolidColor("Solid Color", Color) = (0, 1, 0, 0.3)
        _Opacity("Opacity", Range(0, 1)) = 0.8
    }

    SubShader {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline" = "HDRenderPipeline" }
        LOD 100

        Pass {
            Name "TerrainHolesDebug"
            Tags { "LightMode" = "Forward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

            TEXTURE2D(_TerrainHolesTexture);
            SAMPLER(sampler_TerrainHolesTexture);

            CBUFFER_START(UnityPerMaterial)
                float4 _HoleColor;
                float4 _SolidColor;
                float _Opacity;
            CBUFFER_END

            #ifdef UNITY_INSTANCING_ENABLED
                TEXTURE2D(_TerrainHeightmapTexture);
                TEXTURE2D(_TerrainNormalmapTexture);

                CBUFFER_START(UnityTerrain)
                    float4 _TerrainHeightmapRecipSize;
                    float4 _TerrainHeightmapScale;
                CBUFFER_END

                UNITY_INSTANCING_BUFFER_START(Terrain)
                    UNITY_DEFINE_INSTANCED_PROP(float4, _TerrainPatchInstanceData)
                UNITY_INSTANCING_BUFFER_END(Terrain)
            #endif

            struct Attributes {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input) {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionWS;

                #ifdef UNITY_INSTANCING_ENABLED
                    float2 patchVertex = input.positionOS.xy;
                    float4 instanceData = UNITY_ACCESS_INSTANCED_PROP(Terrain, _TerrainPatchInstanceData);

                    float2 sampleCoords = (patchVertex.xy + instanceData.xy) * instanceData.z;
                    float height = UnpackHeightmap(_TerrainHeightmapTexture.Load(int3(sampleCoords, 0)));

                    float3 positionOS;
                    positionOS.xz = sampleCoords * _TerrainHeightmapScale.xz;
                    positionOS.y = height * _TerrainHeightmapScale.y;

                    positionWS = mul(UNITY_MATRIX_M, float4(positionOS, 1.0)).xyz;
                    output.uv = sampleCoords * _TerrainHeightmapRecipSize.zw;
                #else
                    positionWS = TransformObjectToWorld(input.positionOS);
                    output.uv = input.uv;
                #endif

                output.positionCS = TransformWorldToHClip(positionWS);
                return output;
            }

            float4 frag(Varyings input) : SV_Target {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // Sample the holes texture
                float holeMask = SAMPLE_TEXTURE2D(_TerrainHolesTexture, sampler_TerrainHolesTexture, input.uv).r;

                // holeMask: 0 = hole, 1 = solid terrain
                float4 color;
                if (holeMask < 0.5) {
                    // This is a hole - show red
                    color = _HoleColor;
                } else {
                    // This is solid terrain - show green with transparency
                    color = _SolidColor;
                }

                color.a *= _Opacity;
                return color;
            }
            ENDHLSL
        }
    }

    Fallback Off
}

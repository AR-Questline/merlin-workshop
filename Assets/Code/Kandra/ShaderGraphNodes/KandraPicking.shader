
// Use this shader as a fallback when trying to render using a BatchRendererGroup with a shader that doesn't define a ScenePickingPass or SceneSelectionPass.
Shader "Hidden/HDRP/KandraPicking"
{
    HLSLINCLUDE

    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
    #pragma editor_sync_compilation
    #pragma multi_compile_instancing
    #pragma instancing_options renderinglayer
    #pragma multi_compile DOTS_INSTANCING_ON
    //#pragma enable_d3d11_debug_symbols

    ENDHLSL

    Properties
    {
        [HideInInspector]_InstanceData("Instance data", Vector) = (0, 0, 0, 0)
    }
    SubShader
    {
        // This tags allow to use the shader replacement features
        Tags{ "RenderPipeline"="HDRenderPipeline" "RenderType" = "HDLitShader" }

        Pass
        {
            Name "ScenePickingPass"
            Tags { "LightMode" = "Picking" }

            Cull [_CullMode]

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #define SCENEPICKINGPASS
            #define SHADERPASS SHADERPASS_DEPTH_ONLY
            #define KANDRA_SKINNING
            #define ATTRIBUTES_NEED_VERTEXID

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitProperties.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/PickingSpaceTransforms.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/ShaderPass/LitDepthPass.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitData.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl"
            #include "SampleSkinBuffer.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float2 _InstanceData;
            CBUFFER_END

            #if defined(DOTS_INSTANCING_ON)
            // DOTS instancing definitions
            UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
                UNITY_DOTS_INSTANCED_PROP_OVERRIDE_SUPPORTED(float2, _InstanceData)
            UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)
            // DOTS instancing usage macros
            #define UNITY_ACCESS_HYBRID_INSTANCED_PROP(var, type) UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(type, var)
            #elif defined(UNITY_INSTANCING_ENABLED)
            // Unity instancing definitions
            UNITY_INSTANCING_BUFFER_START(SGPerInstanceData)
                UNITY_DEFINE_INSTANCED_PROP(float2, _InstanceData)
            UNITY_INSTANCING_BUFFER_END(SGPerInstanceData)
            // Unity instancing usage macros
            #define UNITY_ACCESS_HYBRID_INSTANCED_PROP(var, type) UNITY_ACCESS_INSTANCED_PROP(SGPerInstanceData, var)
            #else
            #define UNITY_ACCESS_HYBRID_INSTANCED_PROP(var, type) var
            #endif

            struct PickingAttributesMesh
            {
                uint vertexID : VERTEXID_SEMANTIC;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct PickingMeshToPS
            {
                float4 positionCS : SV_Position;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            PickingMeshToPS Vert(PickingAttributesMesh input)
            {
                PickingMeshToPS output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 positionWS = 0;
                float3 normal = 0;
                float3 tangent = 0;

                uint2 instanceData = asuint(UNITY_ACCESS_HYBRID_INSTANCED_PROP(_InstanceData, float2));
                sampleDeform(input.vertexID, instanceData, positionWS, normal, tangent);

                output.positionCS = TransformWorldToHClip(positionWS);

                return output;
            }

            void Frag(PickingMeshToPS input, out float4 outColor : SV_Target0)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                outColor = unity_SelectionID;
            }

            ENDHLSL
        }

        Pass
        {
            Name "SceneSelectionPass"
            Tags { "LightMode" = "SceneSelectionPass" }

            Cull Off

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #define SCENESELECTIONPASS
            #define SHADERPASS SHADERPASS_DEPTH_ONLY
            #define KANDRA_SKINNING
            #define ATTRIBUTES_NEED_VERTEXID
            #define UNITY_DOTS_INSTANCING_ENABLED

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitProperties.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/PickingSpaceTransforms.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/ShaderPass/LitDepthPass.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitData.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl"
            #include "SampleSkinBuffer.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float2 _InstanceData;
            CBUFFER_END

            #if defined(DOTS_INSTANCING_ON)
            // DOTS instancing definitions
            UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
                UNITY_DOTS_INSTANCED_PROP_OVERRIDE_SUPPORTED(float2, _InstanceData)
            UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)
            // DOTS instancing usage macros
            #define UNITY_ACCESS_HYBRID_INSTANCED_PROP(var, type) UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(type, var)
            #elif defined(UNITY_INSTANCING_ENABLED)
            // Unity instancing definitions
            UNITY_INSTANCING_BUFFER_START(SGPerInstanceData)
                UNITY_DEFINE_INSTANCED_PROP(float2, _InstanceData)
            UNITY_INSTANCING_BUFFER_END(SGPerInstanceData)
            // Unity instancing usage macros
            #define UNITY_ACCESS_HYBRID_INSTANCED_PROP(var, type) UNITY_ACCESS_INSTANCED_PROP(SGPerInstanceData, var)
            #else
            #define UNITY_ACCESS_HYBRID_INSTANCED_PROP(var, type) var
            #endif

            struct PickingAttributesMesh
            {
                uint vertexID : VERTEXID_SEMANTIC;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct PickingMeshToPS
            {
                float4 positionCS : SV_Position;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            PickingMeshToPS Vert(PickingAttributesMesh input)
            {
                PickingMeshToPS output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 positionWS = 0;
                float3 normal = 0;
                float3 tangent = 0;

                uint2 instanceData = asuint(UNITY_ACCESS_HYBRID_INSTANCED_PROP(_InstanceData, float2));
                sampleDeform(input.vertexID, instanceData, positionWS, normal, tangent);

                output.positionCS = TransformWorldToHClip(positionWS);

                return output;
            }

            void Frag(PickingMeshToPS input, out float4 outColor : SV_Target0)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                outColor = float4(_ObjectId, _PassValue, 1.0, 1.0);
            }

            ENDHLSL
        }
    }
}

Shader "TG/NoRender" {
    SubShader
    {
        Tags {"Queue" = "Geometry-1" }
 
        Lighting Off
        
        Pass
        {
            ZWrite Off
            ColorMask 0    
            
            HLSLPROGRAM
            #pragma multi_compile _ DOTS_INSTANCING_ON
            #pragma shader_feature KANDRA_SKINNING
            #pragma vertex vert
            #pragma fragment frag
            void vert() { }
            void frag() { }
            ENDHLSL
        }
    }
}
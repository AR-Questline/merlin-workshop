Shader "Hidden/OccludeeScreenSpaceAABB"
{
    SubShader
    {
        Lighting Off
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Fog { Mode Off }
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = v.vertex;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return fixed4(0.4, 0.0, 0.0, 1.0);
            }
            ENDCG
        }
    }
}

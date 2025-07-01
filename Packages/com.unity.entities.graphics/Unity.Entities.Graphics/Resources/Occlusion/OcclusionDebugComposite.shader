Shader "Hidden/OcclusionDebugComposite"
{
    Properties
    {
        _Depth("Depth", 2D) = "white" {}
        _Overlay("Overlay", 2D) = "white" {}
    }

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

            sampler2D _Depth;
            sampler2D _Overlay;
            float _YFlip;
            float _OnlyOverlay;
            float _OnlyDepth;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = v.vertex;
                o.uv = v.uv;
                if (_YFlip > 0.5)
                {
                    o.uv.y = 1 - o.uv.y;
                }
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                if (_OnlyOverlay > 0.5)
                {
                    return fixed4(1, 1, 1, tex2D(_Overlay, i.uv).r);
                }
                if (_OnlyDepth > 0.5)
                {
                    return fixed4(tex2D(_Depth, i.uv).rrr, 1);
                }

                {
                    fixed back = min(1.0, tex2D(_Depth, i.uv).r);
                    fixed3 fore = fixed3(1, 0, 0);
                    float alpha = tex2D(_Overlay, i.uv).r;
                    // Perform a simple alpha composite in compute, with the background alpha = 1
                    return fixed4(fore * alpha + back.rrr * (1 - alpha), 1);
                }
            }
            ENDCG
        }
    }
}

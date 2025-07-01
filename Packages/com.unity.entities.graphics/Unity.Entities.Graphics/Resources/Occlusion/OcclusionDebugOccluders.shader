Shader "Hidden/OcclusionDebugOccluders"
{
    SubShader
    {
        Lighting Off
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite On
        Cull Off
        Fog { Mode Off }
        ZTest LEqual

        Pass
        {
            Name "ForwardOnly"
            Tags { "LightMode" = "ForwardOnly" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            float4x4 _Transform;
            float _YFlip;

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color: COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color: COLOR;
                float z : TexCoord0;
            };

            v2f vert(appdata v)
            {
                v2f o;

                o.vertex = mul(_Transform, float4(v.vertex.xyz, 1));
                if (_YFlip > 0.5)
                {
                    o.vertex.y = -o.vertex.y;
                }
                o.z = v.vertex.w;
                o.color = v.color;

                return o;
            }

            struct FrameOut
            {
                fixed4 color : SV_Target;
                float depth : SV_Depth;
            };

            FrameOut frag(v2f i)
            {
                fixed4 col = i.color;

                float2 dp = normalize(float2(ddx(i.vertex.z), ddy(i.vertex.z)));
                col.rgb *= 0.5 / (abs(dp.x) + abs(dp.y));

                FrameOut result;
                result.color = col;
                result.depth = 1 / i.vertex.w;
                return result;
            }
            ENDCG
        }
    }
}

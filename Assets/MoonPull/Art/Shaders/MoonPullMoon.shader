// Glowing moon. MoonView drives _Brightness (0.08 eclipse, 1 normal, 1.8 Full Moon).
Shader "MoonPull/Moon"
{
    Properties
    {
        _Color ("Color", Color) = (0.95, 0.95, 1, 1)
        _Brightness ("Brightness", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _Brightness;

            struct v2f { float4 pos : SV_POSITION; float3 normal : TEXCOORD0; float3 viewDir : TEXCOORD1; };

            v2f vert (appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.viewDir = normalize(WorldSpaceViewDir(v.vertex));
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float rim = pow(1 - saturate(dot(normalize(i.normal), i.viewDir)), 3);
                fixed3 col = _Color.rgb * _Brightness * (0.85 + rim * 0.5);
                return fixed4(col, 1);
            }
            ENDCG
        }
    }
}

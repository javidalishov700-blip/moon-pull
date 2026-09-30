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

            struct v2f { float4 pos : SV_POSITION; float3 normal : TEXCOORD0; float3 viewDir : TEXCOORD1; float3 local : TEXCOORD2; };

            v2f vert (appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.viewDir = normalize(WorldSpaceViewDir(v.vertex));
                o.local = v.vertex.xyz;
                return o;
            }

            float hash31(float3 p) { p = frac(p * 0.3183099 + 0.1); p *= 17; return frac(p.x * p.y * p.z * (p.x + p.y + p.z)); }
            float noise3(float3 x)
            {
                float3 i = floor(x), f = frac(x);
                f = f * f * (3 - 2 * f);
                return lerp(lerp(lerp(hash31(i), hash31(i + float3(1,0,0)), f.x), lerp(hash31(i + float3(0,1,0)), hash31(i + float3(1,1,0)), f.x), f.y),
                            lerp(lerp(hash31(i + float3(0,0,1)), hash31(i + float3(1,0,1)), f.x), lerp(hash31(i + float3(0,1,1)), hash31(i + float3(1,1,1)), f.x), f.y), f.z);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 p = normalize(i.local);
                // Maria (large dark seas) and small craters give the disc a recognisable moon face.
                float maria = smoothstep(0.5, 0.7, noise3(p * 2.2 + 3.1));
                float craters = smoothstep(0.72, 0.8, noise3(p * 9 + 1.7)) * 0.5;
                float face = 1 - maria * 0.22 - craters * 0.18;
                float ndv = saturate(dot(normalize(i.normal), i.viewDir));
                float limb = 0.82 + 0.18 * ndv;                      // soft limb darkening
                float rim = pow(1 - ndv, 3);                         // thin bright edge glow
                fixed3 col = _Color.rgb * face * limb * _Brightness + rim * 0.35 * _Brightness;
                return fixed4(col, 1);
            }
            ENDCG
        }
    }
}

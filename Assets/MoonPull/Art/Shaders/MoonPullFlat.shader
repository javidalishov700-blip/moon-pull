// Cartoon (cel) surface: smooth normals, a soft two-band light ramp, a gentle highlight and a moonlit rim, plus fog.
// Used for boats, people, rocks and props. (Name kept for the generated materials.)
Shader "MoonPull/Flat"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _BaseColor ("Tint (debris)", Color) = (1, 1, 1, 1)
        _Emission ("Emission", Float) = 0
        _WetLine ("Wet Line Strength", Range(0, 1)) = 0.35
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            fixed4 _Color, _BaseColor, _LightColor0;
            float _Emission, _WetLine, _MP_WaterLevel, _MP_Dark;

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 pos : SV_POSITION; float3 worldPos : TEXCOORD0; float3 normal : TEXCOORD2; UNITY_FOG_COORDS(1) };

            v2f vert (appdata v)
            {
                v2f o;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 n = normalize(i.normal);
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                if (dot(n, viewDir) < -0.2) n = -n; // double-sided (sails, flags); smooth silhouettes keep their normals
                float3 l = normalize(_WorldSpaceLightPos0.xyz);
                float ndl = dot(n, l);
                // Cel ramp: a soft step between shadow and light, with a small lift so shadows stay colourful.
                float band = smoothstep(-0.05, 0.12, ndl) * 0.75 + smoothstep(0.55, 0.7, ndl) * 0.25;
                fixed3 albedo = _Color.rgb * _BaseColor.rgb;
                fixed3 ambient = max(UNITY_LIGHTMODEL_AMBIENT.rgb, fixed3(0.32, 0.34, 0.45));
                fixed3 col = albedo * (ambient + band * _LightColor0.rgb * 0.95) + albedo * _Emission;
                // Cartoon highlight.
                float3 h = normalize(l + viewDir);
                col += smoothstep(0.93, 0.96, dot(n, h)) * 0.22 * _LightColor0.rgb;
                // Moonlit rim so silhouettes separate from the dark sea and sky.
                float rim = smoothstep(0.55, 0.8, 1 - saturate(dot(n, viewDir)));
                col += rim * 0.35 * lerp(albedo, fixed3(0.75, 0.82, 1), 0.5) * _LightColor0.rgb;
                // Surfaces darken just below the tide line so players read water height on obstacles.
                float wet = smoothstep(_MP_WaterLevel + 0.15, _MP_WaterLevel - 0.05, i.worldPos.y) * _WetLine;
                col *= 1 - wet;
                col *= 1 - _MP_Dark * 0.6 * saturate(1 - _Emission); // lit lamps keep glowing in the dark
                fixed4 result = fixed4(col, 1);
                UNITY_APPLY_FOG(i.fogCoord, result);
                return result;
            }
            ENDCG
        }
    }
    Fallback "Unlit/Color"
}

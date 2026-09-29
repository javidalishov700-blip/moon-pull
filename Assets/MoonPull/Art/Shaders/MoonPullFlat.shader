// Flat-shaded low-poly surface: one directional light + ambient + fog, per-vertex cheap. Used for boats, rocks, props.
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
            float _Emission, _WetLine, _MP_WaterLevel;

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; float3 worldPos : TEXCOORD0; UNITY_FOG_COORDS(1) };

            v2f vert (appdata v)
            {
                v2f o;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.pos = UnityObjectToClipPos(v.vertex);
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 n = normalize(cross(ddy(i.worldPos), ddx(i.worldPos)));
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                if (dot(n, viewDir) < 0) n = -n; // winding-independent (procedural meshes, double-sided sails)
                float light = saturate(dot(n, normalize(_WorldSpaceLightPos0.xyz)));
                fixed3 albedo = _Color.rgb * _BaseColor.rgb;
                fixed3 col = albedo * (UNITY_LIGHTMODEL_AMBIENT.rgb + light * _LightColor0.rgb) + albedo * _Emission;
                // Moonlit rim so silhouettes separate from the dark sea and sky.
                col += albedo * pow(1 - saturate(dot(n, viewDir)), 3) * 0.45 * _LightColor0.rgb;
                // Surfaces darken just below the tide line so players read water height on obstacles.
                float wet = smoothstep(_MP_WaterLevel + 0.15, _MP_WaterLevel - 0.05, i.worldPos.y) * _WetLine;
                col *= 1 - wet;
                fixed4 result = fixed4(col, 1);
                UNITY_APPLY_FOG(i.fogCoord, result);
                return result;
            }
            ENDCG
        }
    }
    Fallback "Unlit/Color"
}

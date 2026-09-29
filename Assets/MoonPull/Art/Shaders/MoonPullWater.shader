// Built-in pipeline water. Vertex waves come from MoonPullWater.hlsl, the exact math WaveMath.cs uses for buoyancy.
Shader "MoonPull/Water"
{
    Properties
    {
        _ShallowColor ("Shallow", Color) = (0.45, 0.85, 0.85, 1)
        _DeepColor ("Deep", Color) = (0.10, 0.25, 0.45, 1)
        _FoamColor ("Foam", Color) = (0.95, 0.95, 1, 1)
        _FullMoonTint ("Full Moon Tint", Color) = (0.85, 0.9, 1, 1)
        _FoamHeight ("Foam Crest Height", Float) = 0.12
        _DepthRange ("Depth Range (z)", Float) = 10
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            #include "MoonPullWater.hlsl"

            fixed4 _ShallowColor, _DeepColor, _FoamColor, _FullMoonTint;
            float _FoamHeight, _DepthRange;
            fixed4 _LightColor0;

            struct appdata { float4 vertex : POSITION; };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 normal : TEXCOORD1;
                float crest : TEXCOORD2;
                UNITY_FOG_COORDS(3)
            };

            v2f vert (appdata v)
            {
                v2f o;
                float3 world = mul(unity_ObjectToWorld, v.vertex).xyz;
                float height; float3 normal;
                MoonPullWave_float(world, height, normal);
                world.y = height;
                o.worldPos = world;
                o.normal = normal;
                o.crest = height - _MP_WaterLevel;
                o.pos = mul(UNITY_MATRIX_VP, float4(world, 1));
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Faceted low-poly normal from screen derivatives.
                float3 n = normalize(cross(ddy(i.worldPos), ddx(i.worldPos)));
                if (n.y < 0) n = -n;
                float depth = saturate(i.worldPos.z / _DepthRange);
                fixed3 col = lerp(_ShallowColor.rgb, _DeepColor.rgb, depth);
                float diffuse = saturate(dot(n, normalize(_WorldSpaceLightPos0.xyz))) * 0.6 + 0.4;
                col *= diffuse;
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                float fresnel = pow(1 - saturate(dot(n, viewDir)), 4);
                col += fresnel * 0.25 * _LightColor0.rgb;
                float foam = smoothstep(_FoamHeight * 0.6, _FoamHeight, i.crest);
                col = lerp(col, _FoamColor.rgb, foam);
                col = lerp(col, col * _FullMoonTint.rgb * 1.6 + 0.12, _MP_FullMoon);
                fixed4 result = fixed4(col, 1);
                UNITY_APPLY_FOG(i.fogCoord, result);
                return result;
            }
            ENDCG
        }
    }
    Fallback "Unlit/Color"
}

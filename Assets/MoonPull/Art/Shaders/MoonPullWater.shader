// Built-in pipeline night-sea water. Vertex waves come from MoonPullWater.hlsl, the exact math WaveMath.cs uses for
// buoyancy. The fragment adds what makes it read as water: smooth analytic normals with animated ripple detail, a
// sky reflection by Fresnel, light scattering through the crests, a shimmering moon path, and broken foam only on
// the steepest wave tips.
Shader "MoonPull/Water"
{
    Properties
    {
        _ShallowColor ("Shallow / scatter", Color) = (0.20, 0.62, 0.70, 1)
        _DeepColor ("Deep", Color) = (0.03, 0.10, 0.22, 1)
        _FoamColor ("Foam", Color) = (0.92, 0.96, 1, 1)
        _FullMoonTint ("Full Moon Tint", Color) = (0.85, 0.9, 1, 1)
        _FoamHeight ("Foam Crest Height", Float) = 0.22
        _DepthRange ("Horizon Distance (z)", Float) = 45
        _RippleStrength ("Ripple Strength", Float) = 0.35
        _GlitterStrength ("Moon Glitter", Float) = 2.2
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
            float _FoamHeight, _DepthRange, _RippleStrength, _GlitterStrength;
            fixed4 _LightColor0;
            fixed4 _MP_SkyTop, _MP_SkyBottom;
            float4 _MP_MoonPos;

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

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float valueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3 - 2 * f);
                float a = hash21(i), b = hash21(i + float2(1, 0)), c = hash21(i + float2(0, 1)), d = hash21(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            // Small wind ripples: analytic slope of a few crossing sines, cheap and never repeats visibly.
            float2 rippleSlope(float2 p, float t)
            {
                float2 s = 0;
                s += float2(0.9, 0.4) * cos(dot(p, float2(0.9, 0.4)) * 2.3 + t * 1.7) * 0.9;
                s += float2(-0.5, 0.8) * cos(dot(p, float2(-0.5, 0.8)) * 3.1 + t * 2.1) * 0.6;
                s += float2(0.2, -1.0) * cos(dot(p, float2(0.2, -1.0)) * 5.3 + t * 2.9) * 0.35;
                s += float2(0.7, 0.7) * cos(dot(p, float2(0.7, 0.7)) * 8.7 + t * 3.7) * 0.2;
                return s;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float t = _MP_WaveTime;
                float3 viewVec = _WorldSpaceCameraPos - i.worldPos;
                float dist = length(viewVec);
                float3 viewDir = viewVec / dist;

                // Ripples fade with distance so the horizon stays calm instead of aliasing.
                float rippleFade = saturate(1 - dist / (_DepthRange * 1.2));
                float2 slope = rippleSlope(i.worldPos.xz, t) * _RippleStrength * rippleFade;
                float3 n = normalize(i.normal + float3(-slope.x, 0, -slope.y));

                // Body colour: deep water, lifted where light passes through the thin tops of the waves.
                float crest01 = saturate(i.crest / max(_FoamHeight, 0.01) * 0.5 + 0.5);
                float facing = saturate(dot(n, viewDir));
                fixed3 body = lerp(_DeepColor.rgb, _ShallowColor.rgb * 0.8, crest01 * crest01 * (1 - facing * 0.5));

                // Sky reflection by Fresnel, using the same gradient the sky dome draws.
                float3 r = reflect(-viewDir, n);
                fixed3 sky = lerp(_MP_SkyBottom.rgb, _MP_SkyTop.rgb, saturate(r.y * 1.4 + 0.3));
                float fresnel = 0.04 + 0.96 * pow(1 - facing, 5);
                fixed3 col = lerp(body, sky, saturate(fresnel * 1.15));

                // Soft moonlight diffuse so wave shapes stay readable.
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                col *= 0.75 + 0.35 * saturate(dot(n, lightDir));

                // Moon glitter path: sharp specular towards the moon plus sparkles that twinkle on the ripples.
                float3 toMoon = normalize(_MP_MoonPos.xyz - i.worldPos);
                float spec = pow(saturate(dot(r, toMoon)), 180);
                float wide = pow(saturate(dot(r, toMoon)), 18);
                float sparkle = step(0.93, valueNoise(i.worldPos.xz * 6 + t * 1.3)) * wide;
                float moonBright = saturate(_MP_MoonPos.w);
                col += (spec * _GlitterStrength + wide * 0.18 + sparkle * 1.4) * moonBright * _LightColor0.rgb;

                // Foam: only on the highest, steepest tips, broken up by noise so it reads as spray not paint.
                float steep = 1 - saturate(i.normal.y);
                float foamNoise = valueNoise(i.worldPos.xz * 1.7 + float2(t * 0.6, 0)) * 0.6 + valueNoise(i.worldPos.xz * 4.1 - t) * 0.4;
                float foam = smoothstep(_FoamHeight * 0.75, _FoamHeight * 1.2, i.crest + steep * 0.6) * smoothstep(0.35, 0.7, foamNoise);
                col = lerp(col, _FoamColor.rgb, foam * 0.85);

                // Fade to the sky's horizon colour with distance for depth.
                float haze = saturate((dist - _DepthRange * 0.35) / _DepthRange);
                col = lerp(col, _MP_SkyBottom.rgb * 0.9, haze * haze);

                col = lerp(col, col * _FullMoonTint.rgb * 1.5 + 0.08, _MP_FullMoon);
                fixed4 result = fixed4(col, 1);
                UNITY_APPLY_FOG(i.fogCoord, result);
                return result;
            }
            ENDCG
        }
    }
    Fallback "Unlit/Color"
}

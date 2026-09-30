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
            float _MP_Dark;
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

            // Cartoon sea, like casual mobile games: clean colour bands by wave height (deep troughs, bright teal
            // faces, pale crests), a soft white foam cap along the tops, a smooth moon path and gentle haze. No noisy
            // texture: every shape comes from the swells themselves.
            fixed4 frag (v2f i) : SV_Target
            {
                float t = _MP_WaveTime;
                float3 viewVec = _WorldSpaceCameraPos - i.worldPos;
                float dist = length(viewVec);
                float3 viewDir = viewVec / dist;
                float2 scrolled = i.worldPos.xz + float2(0, _MP_ScrollZ);

                // Height 0..1 across the current swell size.
                float amp = max(_MP_WaveAmp.x + _MP_WaveAmp.y + _MP_WaveAmp.z, 0.05);
                float h = saturate(i.crest / amp * 0.5 + 0.5);

                // Soft-banded body colour.
                float band1 = smoothstep(0.32, 0.4, h);
                float band2 = smoothstep(0.64, 0.7, h);
                fixed3 deep = _DeepColor.rgb;
                fixed3 mid = lerp(_DeepColor.rgb, _ShallowColor.rgb, 0.5);
                fixed3 top = _ShallowColor.rgb;
                fixed3 col = lerp(lerp(deep, mid, band1), top, band2);

                // Gentle two-tone light so wave faces read round.
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float ndl = dot(normalize(i.normal), lightDir);
                col *= 0.9 + 0.1 * smoothstep(-0.1, 0.4, ndl);

                // Soft sky tint at grazing angles (keeps the horizon calm).
                float facing = saturate(dot(normalize(i.normal), viewDir));
                col = lerp(col, _MP_SkyBottom.rgb, pow(1 - facing, 4) * 0.15);

                // Foam cap: a clean white rim along the highest part of each swell, with a slow wavy edge.
                float wobble = sin(scrolled.x * 0.9 + t * 1.2) * 0.03 + sin(scrolled.y * 1.3 - t) * 0.03;
                float foam = smoothstep(0.93 + wobble, 0.95 + wobble, h) * 0.8;
                // A thin second line just below, like cartoon water drawings.
                float line2 = smoothstep(0.02, 0.0, abs(h - 0.86 - wobble)) * 0.4;
                col = lerp(col, _FoamColor.rgb, saturate(foam * 0.9 + line2 * 0.5));

                // Moon path: a soft pale streak towards the moon, with a few smooth glints.
                float3 r = reflect(-viewDir, normalize(i.normal));
                float3 toMoon = normalize(_MP_MoonPos.xyz - i.worldPos);
                float m = saturate(dot(r, toMoon));
                float moonBright = saturate(_MP_MoonPos.w);
                float glint = smoothstep(0.985, 0.995, m) * (0.5 + 0.5 * sin(scrolled.x * 3 + t * 2));
                // Only out towards the horizon, so the water around the boat stays clean.
                float far = smoothstep(6, 26, dist);
                float sharp = pow(m, 400) * 1.4;
                col += (pow(m, 60) * 0.22 + glint * 0.4 + sharp) * far * moonBright * _LightColor0.rgb;

                // Fade into the sky's horizon colour with distance.
                float haze = saturate((dist - _DepthRange * 0.4) / _DepthRange);
                col = lerp(col, _MP_SkyBottom.rgb, haze * haze * 0.85);

                col = lerp(col, col * _FullMoonTint.rgb * 1.1 + 0.03, _MP_FullMoon);
                col *= 1 - _MP_Dark * 0.6; // the night deepens as the moonlight runs out
                fixed4 result = fixed4(col, 1);
                UNITY_APPLY_FOG(i.fogCoord, result);
                return result;
            }
            ENDCG
        }
    }
    Fallback "Unlit/Color"
}

// Night sky dome. Colours come from RegionAmbience via _MP_SkyTop / _MP_SkyBottom; the moon position comes from
// MoonView via _MP_MoonPos. Layers, back to front: gradient, a faint milky band, two layers of round twinkling
// stars, the moon's halo, slow drifting clouds lit from the moon's side, and far island silhouettes on the horizon.
Shader "MoonPull/Sky"
{
    Properties { }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _MP_SkyTop, _MP_SkyBottom;
            float _MP_Dark;
            float4 _MP_MoonPos;
            float _MP_FullMoon;

            struct v2f { float4 pos : SV_POSITION; float3 dir : TEXCOORD0; };

            v2f vert (float4 vertex : POSITION)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(vertex);
                o.dir = vertex.xyz;
                return o;
            }

            float hash21(float2 p)
            {
                p = frac(p * float2(233.34, 851.73));
                p += dot(p, p + 23.45);
                return frac(p.x * p.y);
            }

            float noise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f * f * (3 - 2 * f);
                return lerp(lerp(hash21(i), hash21(i + float2(1, 0)), f.x), lerp(hash21(i + float2(0, 1)), hash21(i + float2(1, 1)), f.x), f.y);
            }

            float fbm(float2 p)
            {
                float v = 0, a = 0.5;
                for (int k = 0; k < 4; k++) { v += a * noise(p); p *= 2.03; a *= 0.5; }
                return v;
            }

            // Round stars: one candidate per cell, jittered, sized and twinkling independently.
            float stars(float2 uv, float density, float size, float t)
            {
                float2 cell = floor(uv);
                float2 local = frac(uv) - 0.5;
                float h = hash21(cell);
                if (h > density) return 0;
                float2 offset = float2(hash21(cell + 7.1), hash21(cell + 3.7)) - 0.5;
                float d = length(local - offset * 0.7);
                float radius = size * (0.4 + 0.6 * hash21(cell + 1.3));
                float twinkle = 0.65 + 0.35 * sin(t * (1.5 + h * 4) + h * 40);
                return smoothstep(radius, radius * 0.2, d) * twinkle;
            }

            // Cartoon night sky: a smooth two-colour gradient with a warm glow on the horizon, a few big soft stars,
            // a clean moon halo and puffy rounded clouds. No noise textures, so it reads calm and friendly.
            float puff(float2 p, float2 c, float2 size)
            {
                // A cloud is a few overlapping circles on a flat base.
                float2 q = (p - c) / size;
                float d = min(min(length(q - float2(-0.55, 0)) - 0.45, length(q - float2(0.1, 0.18)) - 0.6),
                              min(length(q - float2(0.7, 0.02)) - 0.42, length(q - float2(0.35, -0.05)) - 0.45));
                d = max(d, -q.y - 0.3); // flat bottom
                return smoothstep(0.04, -0.04, d);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float t = _Time.y;
                float up = saturate(d.y);

                fixed3 col = lerp(_MP_SkyBottom.rgb, _MP_SkyTop.rgb, smoothstep(0.0, 0.7, up));
                col = lerp(col, _MP_SkyBottom.rgb * 1.15 + fixed3(0.06, 0.03, 0.0), exp(-up * 14) * 0.6); // horizon glow

                float2 uv = float2(atan2(d.x, d.z) * 3.2, d.y * 5.5);
                float starMask = smoothstep(0.12, 0.4, d.y);
                col += stars(uv * 10, 0.28, 0.1, t) * starMask * 0.9;

                float3 moonDir = normalize(_MP_MoonPos.xyz - _WorldSpaceCameraPos);
                float m = saturate(dot(d, moonDir));
                float moonBright = saturate(_MP_MoonPos.w) * (1 + _MP_FullMoon * 0.5);
                col += (pow(m, 40) * 0.25 + pow(m, 400) * 0.35) * moonBright * fixed3(0.95, 0.95, 1.0);

                // Puffy clouds drifting slowly along the lower sky.
                float2 cp = float2(atan2(d.x, d.z) + t * 0.004, d.y);
                float c = 0;
                c = max(c, puff(cp, float2(-0.9, 0.16), float2(0.22, 0.07)));
                c = max(c, puff(cp, float2(-0.2, 0.26), float2(0.16, 0.05)));
                c = max(c, puff(cp, float2(0.45, 0.13), float2(0.26, 0.08)));
                c = max(c, puff(cp, float2(1.2, 0.22), float2(0.2, 0.06)));
                c = max(c, puff(cp, float2(2.3, 0.18), float2(0.24, 0.07)));
                c = max(c, puff(cp, float2(-1.8, 0.2), float2(0.2, 0.06)));
                fixed3 cloudCol = lerp(_MP_SkyBottom.rgb, fixed3(0.92, 0.94, 1.0), 0.5 + 0.3 * pow(m, 4));
                col = lerp(col, cloudCol, c * 0.85);

                col *= 1 - _MP_Dark * 0.6; // the night deepens as the moonlight runs out
                return fixed4(col, 1);
            }
            ENDCG
        }
    }
}

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

            fixed4 frag (v2f i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float t = _Time.y;
                float up = saturate(d.y);

                // Gradient with a warmer glow hugging the horizon.
                fixed3 col = lerp(_MP_SkyBottom.rgb, _MP_SkyTop.rgb, saturate(pow(up, 0.6) * 1.2));
                col += _MP_SkyBottom.rgb * 0.35 * exp(-up * 9);

                // Spherical-ish mapping for the star layers (avoids pinching at the zenith for our view range).
                float2 uv = float2(atan2(d.x, d.z) * 3.2, d.y * 5.5);

                // Milky band: faint, diagonal, broken up by noise.
                float band = exp(-pow((d.y - d.x * 0.35 - 0.45) * 4, 2)) * fbm(uv * 1.3 + 4);
                col += band * 0.12 * float3(0.8, 0.8, 1.0);

                float starMask = smoothstep(0.02, 0.25, d.y);
                float s = stars(uv * 18, 0.35, 0.09, t) + stars(uv * 42 + 11, 0.5, 0.12, t * 1.3) * 0.55;
                col += s * starMask * float3(0.95, 0.95, 1.0);

                // Moon halo: wide soft glow plus a tighter bright ring. Direction from camera to the moon.
                float3 moonDir = normalize(_MP_MoonPos.xyz - _WorldSpaceCameraPos);
                float m = saturate(dot(d, moonDir));
                float moonBright = saturate(_MP_MoonPos.w) * (1 + _MP_FullMoon * 0.8);
                col += (pow(m, 60) * 0.35 + pow(m, 600) * 0.6 + pow(m, 8) * 0.08) * moonBright * float3(0.9, 0.92, 1.0);

                // Clouds: two drifting fbm layers in a band above the horizon, rim-lit on the moon side.
                float2 cuv = float2(atan2(d.x, d.z) * 2.2 + t * 0.012, d.y * 7);
                float cloud = smoothstep(0.52, 0.8, fbm(cuv * float2(1, 2.2))) * smoothstep(0.03, 0.15, d.y) * smoothstep(0.55, 0.25, d.y);
                float lit = pow(m, 6) * moonBright;
                fixed3 cloudCol = lerp(_MP_SkyBottom.rgb * 0.55, _MP_SkyBottom.rgb * 1.2 + 0.1, lit);
                col = lerp(col, cloudCol, cloud * 0.75);

                // Far islands: soft dark silhouettes sitting on the horizon line.
                float ridge = 0.03 * fbm(float2(atan2(d.x, d.z) * 6, 1.7)) * smoothstep(0.55, 0.8, noise(float2(atan2(d.x, d.z) * 1.5, 9)));
                float island = smoothstep(ridge, ridge - 0.004, d.y) * smoothstep(-0.02, 0.0, d.y);
                col = lerp(col, _MP_SkyTop.rgb * 0.55, island * 0.85);

                return fixed4(col, 1);
            }
            ENDCG
        }
    }
}

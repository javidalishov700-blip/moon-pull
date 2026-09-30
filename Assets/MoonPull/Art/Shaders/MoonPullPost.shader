// Moon Pull's mobile post stack (built-in pipeline, driven by MoonPullPost.cs): a soft-knee bloom made from a
// dual-filter downsample/upsample chain, then one composite pass with bloom, subtle edge chromatic aberration,
// saturation/contrast, split toning (cool shadows, warm highlights) and a vignette. UI is drawn after it.
Shader "MoonPull/Post"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
    }

    CGINCLUDE
    #include "UnityCG.cginc"

    sampler2D _MainTex;
    float4 _MainTex_TexelSize;
    sampler2D _BloomTex;
    float4 _Threshold;          // x: threshold, y: knee, z: 2 * knee, w: 0.25 / knee
    float _BloomIntensity;
    float _Vignette;
    float _Chromatic;
    float _Saturation;
    float _Contrast;
    float4 _ShadowTint;
    float4 _HighlightTint;

    struct v2f
    {
        float4 pos : SV_POSITION;
        float2 uv : TEXCOORD0;
    };

    v2f vert (appdata_img v)
    {
        v2f o;
        o.pos = UnityObjectToClipPos(v.vertex);
        o.uv = v.texcoord;
        return o;
    }

    half3 Box4 (float2 uv, float delta)
    {
        float4 o = _MainTex_TexelSize.xyxy * float4(-delta, -delta, delta, delta);
        half3 s = tex2D(_MainTex, uv + o.xy).rgb + tex2D(_MainTex, uv + o.zy).rgb
                + tex2D(_MainTex, uv + o.xw).rgb + tex2D(_MainTex, uv + o.zw).rgb;
        return s * 0.25;
    }

    half3 Prefilter (half3 c)
    {
        half brightness = max(c.r, max(c.g, c.b));
        half soft = clamp(brightness - _Threshold.x + _Threshold.y, 0, _Threshold.z);
        soft = soft * soft * _Threshold.w;
        half contribution = max(soft, brightness - _Threshold.x) / max(brightness, 0.00001);
        return c * contribution;
    }
    ENDCG

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass // 0: prefilter + first downsample
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            half4 frag (v2f i) : SV_Target { return half4(Prefilter(Box4(i.uv, 1)), 1); }
            ENDCG
        }

        Pass // 1: downsample
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            half4 frag (v2f i) : SV_Target { return half4(Box4(i.uv, 1), 1); }
            ENDCG
        }

        Pass // 2: upsample, added onto the larger level
        {
            Blend One One
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            half4 frag (v2f i) : SV_Target { return half4(Box4(i.uv, 0.5), 1); }
            ENDCG
        }

        Pass // 3: composite and grade
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            half4 frag (v2f i) : SV_Target
            {
                float2 d = i.uv - 0.5;
                float2 ca = d * dot(d, d) * _Chromatic;
                half3 c;
                c.r = tex2D(_MainTex, i.uv - ca).r;
                c.g = tex2D(_MainTex, i.uv).g;
                c.b = tex2D(_MainTex, i.uv + ca).b;

                float2 bloomUV = i.uv;
                #if UNITY_UV_STARTS_AT_TOP
                if (_MainTex_TexelSize.y < 0)
                    bloomUV.y = 1 - bloomUV.y;
                #endif
                c += tex2D(_BloomTex, bloomUV).rgb * _BloomIntensity;

                half luma = dot(c, half3(0.2126, 0.7152, 0.0722));
                c = lerp(luma.xxx, c, _Saturation);
                c = (c - 0.5) * _Contrast + 0.5;
                c *= lerp(_ShadowTint.rgb, _HighlightTint.rgb, saturate(luma * 1.4));
                c *= saturate(1 - _Vignette * dot(d, d) * 2.2);
                return half4(saturate(c), 1);
            }
            ENDCG
        }
    }
    Fallback Off
}

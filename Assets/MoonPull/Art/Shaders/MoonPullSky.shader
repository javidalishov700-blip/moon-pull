// Gradient night sky with sparse stars. Colors come from RegionAmbience via _MP_SkyTop / _MP_SkyBottom.
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

            struct v2f { float4 pos : SV_POSITION; float3 dir : TEXCOORD0; };

            v2f vert (float4 vertex : POSITION)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(vertex);
                o.dir = vertex.xyz;
                return o;
            }

            float hash(float3 p) { return frac(sin(dot(p, float3(12.9898, 78.233, 37.719))) * 43758.5453); }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 d = normalize(i.dir);
                fixed3 col = lerp(_MP_SkyBottom.rgb, _MP_SkyTop.rgb, saturate(d.y * 1.4 + 0.3));
                float3 cell = floor(d * 180);
                float star = step(0.992, hash(cell)) * saturate(d.y * 2);
                col += star * 0.8;
                return fixed4(col, 1);
            }
            ENDCG
        }
    }
}

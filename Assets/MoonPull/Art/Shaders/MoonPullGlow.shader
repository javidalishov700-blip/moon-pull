// Soft additive glow for halos, beams and lamp light: brightest where the sphere faces the camera, fading to
// nothing at the rim, so a sphere reads as a round bloom instead of a flat grey disc.
Shader "MoonPull/Glow"
{
    Properties
    {
        _Color ("Color", Color) = (1, 0.9, 0.6, 0.3)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend One One
        ZWrite Off
        Cull Back
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;

            struct v2f { float4 pos : SV_POSITION; float3 normal : TEXCOORD0; float3 view : TEXCOORD1; };

            v2f vert (float4 vertex : POSITION, float3 normal : NORMAL)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(vertex);
                o.normal = UnityObjectToWorldNormal(normal);
                o.view = WorldSpaceViewDir(vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float facing = saturate(dot(normalize(i.normal), normalize(i.view)));
                float glow = pow(facing, 3.0);
                return fixed4(_Color.rgb * _Color.a * glow * 2.0, 1);
            }
            ENDCG
        }
    }
}

Shader "Uptime/PacketBorderFX"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Sprite Tint", Color) = (1,1,1,1)

        _Confidence01 ("Confidence", Range(0,1)) = 0
        _Anomaly01 ("Anomaly", Range(0,1)) = 0

        _Alpha ("Base Alpha", Range(0,1)) = 0.55
        _Emission ("Emission", Float) = 3.5

        _NoiseScale ("Noise Scale", Float) = 42
        _NoiseSpeed ("Noise Speed", Float) = 10
        _FragmentStrength ("Fragment Strength", Float) = 1.0
        _FragmentThreshold ("Fragment Threshold", Range(0,1)) = 0.72

        _PulseStrength ("Pulse Strength", Float) = 0.18
        _PulseSpeed ("Pulse Speed", Float) = 3
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha One

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _Color;

            float _Confidence01;
            float _Anomaly01;
            float _Alpha;
            float _Emission;
            float _NoiseScale;
            float _NoiseSpeed;
            float _FragmentStrength;
            float _FragmentThreshold;
            float _PulseStrength;
            float _PulseSpeed;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color * _Color;
                o.uv = v.texcoord;
                return o;
            }

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float mask = tex2D(_MainTex, i.uv).a;

                // Hard cutoff: don't render soft/empty sprite pixels.
                if (mask <= 0.25)
                    discard;

                float c = saturate(_Confidence01);
                float a = saturate(_Anomaly01);

                float2 cell = floor(i.uv * _NoiseScale);
                float t = floor(_Time.y * _NoiseSpeed);

                float n = hash21(cell + t);
                float n2 = hash21(cell * 1.7 + t * 0.37);

                // Chunky pixel blocks instead of hairlines.
                float2 blockGrid = floor(i.uv * float2(22.0, 22.0));
                float blockNoise = hash21(blockGrid + t);

                // Sparse blocks.
                float blockOn = step(lerp(0.992, 0.78, a), blockNoise);

                // Make blocks rectangular / chunky.
                float2 blockLocal = frac(i.uv * float2(22.0, 22.0));
                float blockShape =
                    step(0.18, blockLocal.x) *
                    step(blockLocal.x, 0.82) *
                    step(0.18, blockLocal.y) *
                    step(blockLocal.y, 0.82);

                float glitch = blockOn * blockShape;

                // Very subtle baseline crawl.
                float crawl = step(0.985, n2) * lerp(0.15, 0.7, a);

                // No continuous ring.
                float intensity = 0;
                intensity += glitch * _FragmentStrength * lerp(0.25, 1.0, a);
                intensity += crawl * 0.35 * lerp(0.2, 1.0, c);

                float3 rgb = i.color.rgb * _Emission * intensity;

                return fixed4(rgb, saturate(intensity));
            }
            ENDCG
        }
    }
}
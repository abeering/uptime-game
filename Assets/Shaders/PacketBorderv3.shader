Shader "Uptime/PacketBorderGlowV3"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Sprite Tint", Color) = (1,1,1,1)

        _Confidence01 ("Confidence", Range(0,1)) = 0
        _Anomaly01 ("Anomaly", Range(0,1)) = 0

        _EmissionMin ("Emission Min", Float) = 1.0
        _EmissionMax ("Emission Max", Float) = 3.0

        _PulseStrength ("Pulse Strength", Float) = 0.10
        _PulseSpeed ("Pulse Speed", Float) = 3.0

        _NoiseStrength ("Digital Noise Strength", Float) = 0.22
        _NoiseScaleFine ("Noise Scale Fine", Float) = 180.0
        _NoiseScaleBlock ("Noise Scale Block", Float) = 44.0
        _NoiseSpeedFine ("Noise Speed Fine", Float) = 18.0
        _NoiseSpeedBlock ("Noise Speed Block", Float) = 7.0

        _GlitchStrength ("Glitch Strength", Float) = 1.8
        _GlitchScale ("Glitch Scale", Float) = 42.0
        _GlitchSpeed ("Glitch Speed", Float) = 10.0
        _GlitchAlphaCut ("Glitch Alpha Cut", Range(0,1)) = 0.22

        _GhostStrength ("Ghost Strength", Float) = 0.9
        _GhostOffset ("Ghost Offset", Float) = 0.010

        _CrawlStrength ("Edge Crawl Strength", Float) = 0.35
        _CrawlScale ("Edge Crawl Scale", Float) = 85.0
        _CrawlSpeed ("Edge Crawl Speed", Float) = 9.0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

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

            float _EmissionMin;
            float _EmissionMax;

            float _PulseStrength;
            float _PulseSpeed;

            float _NoiseStrength;
            float _NoiseScaleFine;
            float _NoiseScaleBlock;
            float _NoiseSpeedFine;
            float _NoiseSpeedBlock;

            float _GlitchStrength;
            float _GlitchScale;
            float _GlitchSpeed;
            float _GlitchAlphaCut;

            float _GhostStrength;
            float _GhostOffset;

            float _CrawlStrength;
            float _CrawlScale;
            float _CrawlSpeed;

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
                float4 tex = tex2D(_MainTex, i.uv);
                float alpha = tex.a * i.color.a;

                if (alpha <= 0.001)
                    discard;

                float c = saturate(_Confidence01);
                float a = saturate(_Anomaly01);

                float cPow = pow(c, 1.25);
                float aPow = pow(a, 1.10);

                float3 baseColor = i.color.rgb;

                // Confidence makes the existing PacketView border color more energized,
                // but does NOT push it toward white.
                float emission = lerp(_EmissionMin, _EmissionMax, cPow);

                // Chunky CRT/digital noise: subtle baseline, stronger with anomaly.
                float fineTime = floor(_Time.y * _NoiseSpeedFine);
                float blockTime = floor(_Time.y * _NoiseSpeedBlock);

                float fineNoise = hash21(floor(i.uv * _NoiseScaleFine) + fineTime);
                float blockNoise = hash21(floor(i.uv * _NoiseScaleBlock) + blockTime);

                float digitalNoise =
                    (fineNoise - 0.5) * 0.55 +
                    (blockNoise - 0.5) * 0.85;

                float noiseAmount = _NoiseStrength * (0.18 + aPow * 0.82);

                // Slow colored pulse, like an energized CRT signal.
                float pulseNoise = hash21(floor(i.uv * 18.0) + floor(_Time.y * 2.0));
                float pulse =
                    1.0 +
                    sin(_Time.y * _PulseSpeed + pulseNoise * 6.28318)
                    * _PulseStrength
                    * lerp(0.25, 1.0, cPow);

                float3 rgb = baseColor * emission * pulse;
                rgb *= 1.0 + digitalNoise * noiseAmount;

                // Edge crawl: thin colored line activity running around/through border pixels.
                float crawlX = sin(i.uv.x * _CrawlScale + _Time.y * _CrawlSpeed);
                float crawlY = sin(i.uv.y * (_CrawlScale * 0.73) - _Time.y * (_CrawlSpeed * 0.61));
                float crawl = smoothstep(0.78, 1.0, max(crawlX, crawlY));
                crawl *= _CrawlStrength * (0.20 + cPow * 0.45 + aPow * 0.60);

                rgb += baseColor * crawl;

                // Segment-based glitch: not shiny, just pixel instability.
                float2 glitchCell = floor(i.uv * _GlitchScale);
                float glitchTime = floor(_Time.y * _GlitchSpeed);
                float glitchNoise = hash21(glitchCell + glitchTime);

                float glitchGate = step(lerp(0.995, 0.72, aPow), glitchNoise);

                float barX = step(0.86, frac(i.uv.x * (_GlitchScale * 1.7) + glitchTime * 0.17));
                float barY = step(0.90, frac(i.uv.y * (_GlitchScale * 1.2) - glitchTime * 0.11));

                float glitch = glitchGate * max(barX, barY);

                rgb += baseColor * glitch * _GlitchStrength * (0.12 + aPow);

                // Ghost samples create a tiny warped duplicate edge near the border.
                float ghostA =
                    tex2D(_MainTex, i.uv + float2(_GhostOffset, 0)).a +
                    tex2D(_MainTex, i.uv + float2(-_GhostOffset, 0)).a +
                    tex2D(_MainTex, i.uv + float2(0, _GhostOffset)).a +
                    tex2D(_MainTex, i.uv + float2(0, -_GhostOffset)).a;

                ghostA = saturate(ghostA * 0.35 - alpha);
                ghostA *= _GhostStrength * (0.08 + aPow * 0.92);

                rgb += baseColor * ghostA;

                // Alpha behavior: slight living density change, stronger anomaly bites.
                alpha += crawl * 0.08;
                alpha += ghostA * 0.22;
                alpha *= 1.0 + digitalNoise * noiseAmount * 0.10;
                alpha *= 1.0 - glitch * _GlitchAlphaCut * (0.15 + aPow * 0.85);
                alpha = saturate(alpha);

                return fixed4(rgb, alpha);
            }
            ENDCG
        }
    }
}
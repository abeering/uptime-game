Shader "Uptime/PacketBorderGlowV2"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Sprite Tint", Color) = (1,1,1,1)

        _Confidence01 ("Confidence", Range(0,1)) = 0
        _Anomaly01 ("Anomaly", Range(0,1)) = 0

        _EmissionMin ("Emission Min", Float) = 1.0
        _EmissionMax ("Emission Max", Float) = 5.5

        _PulseStrength ("Pulse Strength", Float) = 0.35
        _PulseSpeed ("Pulse Speed", Float) = 4.0

        _SheenStrength ("Sheen Strength", Float) = 2.0
        _SheenSpeed ("Sheen Speed", Float) = 1.25
        _SheenWidth ("Sheen Width", Range(0.01, 0.5)) = 0.12

        _EdgeHotStrength ("Edge Hot Strength", Float) = 2.5

        _CrackleStrength ("Crackle Strength", Float) = 3.0
        _CrackleScale ("Crackle Scale", Float) = 55.0
        _CrackleSpeed ("Crackle Speed", Float) = 10.0
        _CrackleCut ("Crackle Alpha Cut", Range(0,1)) = 0.35

        _GhostStrength ("Ghost Strength", Float) = 1.6
        _GhostOffset ("Ghost Offset", Float) = 0.012

        _ScanlineStrength ("Scanline Strength", Float) = 0.35
        _ScanlineScale ("Scanline Scale", Float) = 95.0
        _ScanlineSpeed ("Scanline Speed", Float) = 18.0
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

            float _SheenStrength;
            float _SheenSpeed;
            float _SheenWidth;

            float _EdgeHotStrength;

            float _CrackleStrength;
            float _CrackleScale;
            float _CrackleSpeed;
            float _CrackleCut;

            float _GhostStrength;
            float _GhostOffset;

            float _ScanlineStrength;
            float _ScanlineScale;
            float _ScanlineSpeed;

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

            float band(float x, float center, float width)
            {
                float d = abs(frac(x) - center);
                return smoothstep(width, 0.0, d);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float4 tex = tex2D(_MainTex, i.uv);
                float alpha = tex.a * i.color.a;

                if (alpha <= 0.001)
                    discard;

                float c = saturate(_Confidence01);
                float a = saturate(_Anomaly01);

                float cPow = pow(c, 1.35);
                float aPow = pow(a, 1.15);

                float3 baseColor = i.color.rgb;

                // Main confidence emission.
                float emission = lerp(_EmissionMin, _EmissionMax, cPow);

                // Strong living pulse.
                float pulse = 1.0 + sin(_Time.y * _PulseSpeed + i.uv.x * 5.0) * _PulseStrength * cPow;

                // Moving diagonal digital sheen.
                float sheenCoord = i.uv.x * 0.9 + i.uv.y * 0.55 - _Time.y * _SheenSpeed;
                float sheen = band(sheenCoord, 0.5, _SheenWidth);
                sheen *= _SheenStrength * lerp(0.25, 1.0, cPow);

                // Scanline shimmer.
                float scanline = sin((i.uv.y * _ScanlineScale) + _Time.y * _ScanlineSpeed);
                scanline = smoothstep(0.55, 1.0, scanline);
                scanline *= _ScanlineStrength * lerp(0.25, 1.0, cPow);

                // Blocky segment crackle.
                float2 segmentUv = floor(i.uv * _CrackleScale);
                float timeStep = floor(_Time.y * _CrackleSpeed);
                float noise = hash21(segmentUv + timeStep);

                float crackleGate = step(lerp(0.985, 0.62, aPow), noise);

                // Thin horizontal/vertical digital streak fragments.
                float streakX = step(0.86, frac(i.uv.x * _CrackleScale + timeStep * 0.37));
                float streakY = step(0.90, frac(i.uv.y * (_CrackleScale * 0.6) - timeStep * 0.21));
                float crackle = crackleGate * max(streakX, streakY);

                // Ghosted offset border samples, only visible with anomaly.
                float ghostA =
                    tex2D(_MainTex, i.uv + float2(_GhostOffset, 0)).a +
                    tex2D(_MainTex, i.uv + float2(-_GhostOffset, 0)).a +
                    tex2D(_MainTex, i.uv + float2(0, _GhostOffset)).a +
                    tex2D(_MainTex, i.uv + float2(0, -_GhostOffset)).a;

                ghostA = saturate(ghostA * 0.35 - alpha);
                ghostA *= aPow * _GhostStrength;

                // Hot edge based on ring alpha.
                float hotEdge = smoothstep(0.1, 0.95, alpha) * _EdgeHotStrength * cPow;

                float3 rgb = baseColor * emission * pulse;

                rgb += baseColor * sheen;
                rgb += baseColor * scanline;
                rgb += baseColor * hotEdge;
                rgb += baseColor * crackle * _CrackleStrength * aPow;
                rgb += baseColor * ghostA;

                // Push toward white at high confidence so it feels hot/energized.
                // float whiteHot = saturate((sheen + hotEdge * 0.25 + crackle * aPow) * 0.35);
                // rgb = lerp(rgb, float3(1,1,1) * length(baseColor), whiteHot * cPow);

                // Alpha instability.
                alpha += sheen * 0.18;
                alpha += ghostA * 0.35;
                alpha -= crackle * aPow * _CrackleCut;
                alpha = saturate(alpha);

                return fixed4(rgb, alpha);
            }
            ENDCG
        }
    }
}
Shader "Uptime/ConsoleChaosGlitchOverlay"
{
    Properties
    {
        _MainTex ("Noise Texture", 2D) = "white" {}

        [Header(Base)]
        _Opacity ("Opacity", Range(0,1)) = 0.75
        _Intensity ("Intensity", Range(0,1)) = 1.0
        _ChaosAmount ("Chaos Amount", Range(0,2)) = 1.0
        _Seed ("Seed", Float) = 37.1

        _BandJumpRate ("Band Jump Rate", Range(0.1,20)) = 2.4
        _ColorJumpRate ("Color Jump Rate", Range(0.1,20)) = 4.2
        _BlackoutJumpRate ("Blackout Jump Rate", Range(0.1,20)) = 1.7

        _BandJitterChance ("Band Jitter Chance", Range(0,1)) = 0.32
        _BlockJitterChance ("Block Jitter Chance", Range(0,1)) = 0.48

        [Header(Block Timing)]
        _BlockWindowRate ("Block Window Rate", Range(0.1,10)) = 1.15
        _BlockPauseChance ("Block Pause Chance", Range(0,1)) = 0.30
        _BlockSingleChance ("Block Single Chance", Range(0,1)) = 0.18
        _BlockBurstSlowRate ("Block Burst Slow Rate", Range(0.1,20)) = 2.2
        _BlockBurstFastRate ("Block Burst Fast Rate", Range(0.1,40)) = 8.0

        [Header(Color)]
        _BaseTint ("Base Tint", Color) = (0.2, 1.0, 0.85, 1.0)
        _GlitchTint1 ("Glitch Tint 1", Color) = (1.0, 0.1, 0.8, 1.0)
        _GlitchTint2 ("Glitch Tint 2", Color) = (1.0, 0.95, 0.1, 1.0)
        _GlitchTint3 ("Glitch Tint 3", Color) = (0.15, 0.35, 1.0, 1.0)
        _Saturation ("Saturation", Range(0,2)) = 1.25
        _ChromaticAberration ("Chromatic Aberration", Range(0,0.25)) = 0.06

        [Header(Bands)]
        _BandCount ("Band Count", Range(1,16)) = 8
        _BandDensity ("Band Density", Range(0,3)) = 1.0
        _BandMinHeight ("Min Height", Range(0.002,0.2)) = 0.01
        _BandMaxHeight ("Max Height", Range(0.005,0.4)) = 0.18
        _BandJitter ("Jitter", Range(0,2)) = 1.1
        _BandDisplacement ("Displacement", Range(0,0.5)) = 0.16
        _BandChaos ("Band Chaos", Range(0,2)) = 1.2
        _OpaqueBandChance ("Opaque Band Chance", Range(0,1)) = 0.20

        [Header(Blocks)]
        _BlockDensity ("Block Density", Range(0,3)) = 0.85
        _BlockCountMultiplier ("Block Count Multiplier", Range(0.25,6)) = 2.8
        _BlockMinWidth ("Min Width", Range(1,128)) = 2
        _BlockMaxWidth ("Max Width", Range(1,256)) = 56
        _BlockMinHeight ("Min Height", Range(1,64)) = 1
        _BlockMaxHeight ("Max Height", Range(1,128)) = 48
        _BlockChaos ("Block Chaos", Range(0,2)) = 1.25
        _OpaqueBlockChance ("Opaque Block Chance", Range(0,1)) = 0.35

        [Header(Output)]
        _OverallIntensity ("Overall Intensity", Range(0,3)) = 0.9
        _MaxBrightness ("Max Brightness", Range(0.5,3)) = 1.5
        _HardCutThreshold ("Hard Cut Threshold", Range(0,1)) = 0.38

        [Header(Background)]
        _BackgroundNoise ("Background Noise", Range(0,1)) = 0.05
        _NoiseScrollSpeed ("Noise Scroll Speed", Range(-5,5)) = 0.45
        _ScanlineStrength ("Scanline Strength", Range(0,1)) = 0.08
        _ScanlineDensity ("Scanline Density", Range(20,500)) = 230
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
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

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 uv       : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            float _Opacity;
            float _Intensity;
            float _ChaosAmount;
            float _Seed;

            float _BandJumpRate;
            float _ColorJumpRate;
            float _BlackoutJumpRate;

            float _BandJitterChance;
            float _BlockJitterChance;

            float _BlockWindowRate;
            float _BlockPauseChance;
            float _BlockSingleChance;
            float _BlockBurstSlowRate;
            float _BlockBurstFastRate;

            fixed4 _BaseTint;
            fixed4 _GlitchTint1;
            fixed4 _GlitchTint2;
            fixed4 _GlitchTint3;
            float _Saturation;
            float _ChromaticAberration;

            float _BandCount;
            float _BandDensity;
            float _BandMinHeight;
            float _BandMaxHeight;
            float _BandJitter;
            float _BandDisplacement;
            float _BandChaos;
            float _OpaqueBandChance;

            float _BlockDensity;
            float _BlockCountMultiplier;
            float _BlockMinWidth;
            float _BlockMaxWidth;
            float _BlockMinHeight;
            float _BlockMaxHeight;
            float _BlockChaos;
            float _OpaqueBlockChance;

            float _OverallIntensity;
            float _MaxBrightness;
            float _HardCutThreshold;

            float _BackgroundNoise;
            float _NoiseScrollSpeed;
            float _ScanlineStrength;
            float _ScanlineDensity;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color;
                return o;
            }

            float hash21(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            float3 SaturateColor(float3 c, float s)
            {
                float l = dot(c, float3(0.299, 0.587, 0.114));
                return lerp(float3(l, l, l), c, s);
            }

            float JumpClock(float t, float rate, float seed)
            {
                return floor(t * max(rate, 0.001) + seed);
            }

            float ChoosePalette(float r)
            {
                if (r < 0.25) return 0.0;
                if (r < 0.50) return 1.0;
                if (r < 0.75) return 2.0;
                return 3.0;
            }

            float3 PaletteColor(float idx)
            {
                if (idx < 0.5) return _BaseTint.rgb;
                if (idx < 1.5) return _GlitchTint1.rgb;
                if (idx < 2.5) return _GlitchTint2.rgb;
                return _GlitchTint3.rgb;
            }

            // Returns a stepped clock for blocks plus an activity multiplier.
            float ResolveBlockClock(float t, float seed, out float activityMul)
            {
                float windowClock = floor(t * max(_BlockWindowRate, 0.001) + seed);
                float windowPhase = frac(t * max(_BlockWindowRate, 0.001) + seed);

                float modeRand = hash21(float2(windowClock, seed + 17.0));
                float pauseCut = _BlockPauseChance;
                float singleCut = _BlockPauseChance + _BlockSingleChance;
                float slowCut = lerp(singleCut, 1.0, 0.5);

                float localClock = 0.0;
                activityMul = 1.0;

                // pause
                if (modeRand < pauseCut)
                {
                    localClock = windowClock * 0.173;
                    activityMul = 0.08;
                }
                // single-pop: one quick update near the start of window, then hold
                else if (modeRand < singleCut)
                {
                    float popGate = step(0.16, windowPhase);
                    localClock = floor((windowClock * 13.17) + popGate);
                    activityMul = lerp(0.55, 1.0, popGate);
                }
                // slow burst
                else if (modeRand < slowCut)
                {
                    localClock = floor((windowClock * 29.31) + windowPhase * _BlockBurstSlowRate);
                    activityMul = 0.85;
                }
                // fast burst
                else
                {
                    localClock = floor((windowClock * 43.73) + windowPhase * _BlockBurstFastRate);
                    activityMul = 1.0;
                }

                return localClock;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float t = _Time.y;

                float bandClock = JumpClock(t, _BandJumpRate, _Seed * 1.13);
                float colorClock = JumpClock(t, _ColorJumpRate, _Seed * 2.31);
                float blackoutClock = JumpClock(t, _BlackoutJumpRate, _Seed * 2.87);

                float blockActivity = 1.0;
                float blockClock = ResolveBlockClock(t, _Seed * 1.79, blockActivity);

                // Background: still slightly fluid.
                float2 bgUV = uv * 2.3 + float2(0.0, t * _NoiseScrollSpeed * 0.07);
                float bgNoiseA = tex2D(_MainTex, bgUV).r;
                float bgNoiseB = tex2D(_MainTex, uv * 1.15 + float2(_Seed * 0.013, -t * 0.031)).r;
                float bgNoise = lerp(bgNoiseA, bgNoiseB, 0.45);

                float scan = 0.5 + 0.5 * sin((uv.y * _ScanlineDensity + t * 1.6) * 6.28318);
                float scanlineMask = lerp(1.0, scan, _ScanlineStrength);

                float baseAlpha = bgNoise * _BackgroundNoise * scanlineMask;

                float alphaAccum = baseAlpha;
                float opaqueAccum = 0.0;
                float brightnessAccum = 0.0;
                float3 colorAccum = _BaseTint.rgb * (0.18 + bgNoise * 0.35);

                // Bands
                for (int bandIter = 0; bandIter < 8; bandIter++)
                {
                    if (bandIter >= (int)_BandCount) break;

                    float bandId = (float)bandIter + floor(bandClock * 0.37);

                    float r1 = hash21(float2(bandId, bandClock + _Seed));
                    float r2 = hash21(float2(bandId + 19.1, bandClock * 1.7 + 17.13));
                    float r3 = hash21(float2(bandId + 51.7, bandClock + 4.0));
                    float r4 = hash21(float2(bandId + 77.3, bandClock * 1.31 + 11.0));

                    float activeThreshold = 1.0 - saturate(_BandDensity * 0.55);
                    float active = step(activeThreshold, r1);
                    if (active < 0.5) continue;

                    float centerY = r2;
                    float height = lerp(_BandMinHeight, _BandMaxHeight, pow(r3, 1.0 - 0.45 * _BandChaos));

                    float bandMask = smoothstep(height, 0.0, abs(uv.y - centerY));

                    float jitter = (r4 - 0.5) * _BandJitter * 0.03;
                    float displacedMask = smoothstep(height, 0.0, abs((uv.y + jitter) - centerY));
                    bandMask = max(bandMask, displacedMask);

                    float bandJitterGate = step(1.0 - _BandJitterChance, hash21(float2(bandId + 141.0, bandClock + 3.0)));
                    float bandPulse = lerp(1.0, 0.45 + 0.55 * hash21(float2(bandId + 155.0, bandClock + 9.0)), bandJitterGate);
                    bandMask *= bandPulse;

                    float displacement = (hash21(float2(bandId + 101.3, bandClock)) - 0.5) * _BandDisplacement * (0.5 + _ChaosAmount);
                    float2 bandSampleUV = uv + float2(displacement, 0.0);

                    float bandNoise = tex2D(_MainTex, bandSampleUV * (2.0 + r1 * 3.0) + float2(_Seed * 0.01, r4 * 13.0)).r;

                    float bandPalette = ChoosePalette(hash21(float2(bandId + 201.0, colorClock + 3.1)));
                    float3 bandColor = PaletteColor(bandPalette);
                    bandColor = SaturateColor(bandColor, _Saturation);

                    float bandAlpha = bandMask * (0.18 + bandNoise * 0.75) * _Intensity;
                    alphaAccum += bandAlpha * 0.75;

                    float bandOpaque = step(1.0 - _OpaqueBandChance, hash21(float2(bandId + 301.0, blackoutClock + 7.7)));
                    opaqueAccum = max(opaqueAccum, bandMask * bandOpaque * (0.35 + 0.65 * _Intensity));

                    colorAccum += bandColor * bandMask * (0.25 + bandNoise * 0.85);
                    brightnessAccum += bandMask * (0.35 + bandNoise);
                }

                // Blocks
                for (int k = 0; k < 12; k++)
                {
                    float blockId = (float)k + floor(blockClock * 0.83);

                    float br1 = hash21(float2(blockId + 9.1, blockClock + _Seed));
                    float br2 = hash21(float2(blockId + 21.7, blockClock * 1.17));
                    float br3 = hash21(float2(blockId + 33.3, blockClock * 1.37));
                    float br4 = hash21(float2(blockId + 47.9, blockClock * 1.57));
                    float br5 = hash21(float2(blockId + 59.5, blockClock * 1.91));
                    float br6 = hash21(float2(blockId + 74.2, blockClock * 2.23));

                    float activeThreshold = 1.0 - saturate(_BlockDensity * 0.45);
                    float active = step(activeThreshold, br1);
                    if (active < 0.5) continue;

                    float centerX = br2;
                    float centerY = br3;

                    float widthCells = lerp(_BlockMinWidth, _BlockMaxWidth, pow(br4, 0.65));
                    float heightCells = lerp(_BlockMinHeight, _BlockMaxHeight, pow(br5, 0.72));

                    float width = widthCells / 256.0 * _BlockCountMultiplier;
                    float height = heightCells / 256.0;

                    float chunky = step(0.76, br6);
                    width *= lerp(1.0, 2.3, chunky);
                    height *= lerp(1.0, 1.8, chunky);

                    float thinChance = hash21(float2(blockId + 90.0, blockClock + 1.0));
                    if (thinChance > 0.82)
                    {
                        height *= 0.28;
                    }

                    float xMask = step(abs(uv.x - centerX), width * 0.5);
                    float yMask = step(abs(uv.y - centerY), height * 0.5);
                    float blockMask = xMask * yMask;

                    if (blockMask < 0.5) continue;

                    float blockJitterGate = step(1.0 - _BlockJitterChance, hash21(float2(blockId + 111.0, blockClock + 5.0)));
                    float blockFlicker = lerp(1.0, 0.35 + 0.65 * hash21(float2(blockId + 123.0, blockClock + 8.0)), blockJitterGate);
                    blockMask *= blockFlicker;

                    float localNoise = tex2D(_MainTex, uv * (3.0 + br2 * 4.0) + float2(br4 * 7.0, br5 * 9.0)).r;

                    float paletteIdx = ChoosePalette(hash21(float2(blockId + 130.0, colorClock + 11.0)));
                    float3 blockColor = PaletteColor(paletteIdx);
                    blockColor = SaturateColor(blockColor, _Saturation);

                    float blockAlpha = (0.35 + localNoise * 0.65) * _Intensity * (0.6 + _BlockChaos * 0.6);
                    blockAlpha *= blockActivity;
                    alphaAccum += blockMask * blockAlpha;

                    float opaqueBlock = step(1.0 - _OpaqueBlockChance, hash21(float2(blockId + 170.0, blackoutClock + 15.0)));
                    opaqueAccum = max(opaqueAccum, blockMask * opaqueBlock * blockActivity);

                    colorAccum += blockColor * blockMask * (0.65 + localNoise * 0.85) * blockActivity;
                    brightnessAccum += blockMask * (0.4 + localNoise) * blockActivity;
                }

                float cutLine = floor((uv.y + blackoutClock * 0.013) * 140.0);
                float cutRand = hash21(float2(cutLine, blackoutClock + _Seed * 0.7));
                float hardCut = step(_HardCutThreshold, cutRand) * step(0.975, hash21(float2(cutLine + 13.0, blackoutClock + 9.0)));
                opaqueAccum = max(opaqueAccum, hardCut * 0.85);

                float ca = _ChromaticAberration * (0.3 + 0.7 * _ChaosAmount);
                float chromaR = tex2D(_MainTex, uv + float2(ca, 0.0)).r;
                float chromaG = tex2D(_MainTex, uv).r;
                float chromaB = tex2D(_MainTex, uv - float2(ca, 0.0)).r;
                float3 chromaNoise = float3(chromaR, chromaG, chromaB);

                float brightness = saturate(brightnessAccum * 0.12 + 0.35 + bgNoise * 0.25) * _MaxBrightness;
                float3 finalColor = colorAccum * (0.45 + brightness) + chromaNoise * 0.22;
                finalColor = SaturateColor(finalColor, _Saturation);

                float alpha = saturate(alphaAccum * _OverallIntensity);
                alpha = max(alpha, opaqueAccum);
                alpha *= _Opacity * (0.35 + _Intensity * 0.9);
                alpha = saturate(alpha);

                float blackoutMask = step(0.5, opaqueAccum) * step(0.7, hash21(float2(blackoutClock + 401.0, floor(uv.y * 37.0))));
                finalColor = lerp(finalColor, float3(0.0, 0.0, 0.0), blackoutMask * 0.9);

                if (alpha < 0.001)
                    return fixed4(0, 0, 0, 0);

                return fixed4(saturate(finalColor), alpha) * i.color;
            }
            ENDCG
        }
    }
}
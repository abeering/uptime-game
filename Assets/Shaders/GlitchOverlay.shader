Shader "UI/ConsoleGlitchOverlay"
{
    Properties
    {
        _MainTex ("Noise Texture", 2D) = "white" {}
        _TintColor ("Tint Color", Color) = (0.35, 1.0, 0.85, 1.0)

        _Opacity ("Opacity", Range(0,1)) = 0.22
        _Intensity ("Intensity", Range(0,1)) = 0.55

        _NoiseTiling ("Noise Tiling", Float) = 2.5
        _NoiseScrollY ("Noise Scroll Y", Float) = 0.6

        _BackgroundNoiseStrength ("Background Noise Strength", Range(0,1)) = 0.18
        _ScanlineStrength ("Scanline Strength", Range(0,1)) = 0.12
        _ScanlineDensity ("Scanline Density", Float) = 180.0

        _BandStrength ("Band Strength", Range(0,1)) = 0.85
        _BandCount ("Band Count", Float) = 10.0
        _BandSpeed ("Band Speed", Float) = 2.6
        _BandThicknessMin ("Band Thickness Min", Range(0.001,0.2)) = 0.025
        _BandThicknessMax ("Band Thickness Max", Range(0.001,0.3)) = 0.09

        _BlockStrength ("Block Strength", Range(0,1)) = 0.8
        _BlockGridX ("Block Grid X", Float) = 28.0
        _BlockGridY ("Block Grid Y", Float) = 16.0
        _BlockFlickerSpeed ("Block Flicker Speed", Float) = 8.0

        _DropoutStrength ("Dropout Strength", Range(0,1)) = 0.65
        _DropoutSpeed ("Dropout Speed", Float) = 12.0

        _BrightBandBoost ("Bright Band Boost", Range(0,2)) = 0.75
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

            fixed4 _TintColor;
            float _Opacity;
            float _Intensity;

            float _NoiseTiling;
            float _NoiseScrollY;

            float _BackgroundNoiseStrength;
            float _ScanlineStrength;
            float _ScanlineDensity;

            float _BandStrength;
            float _BandCount;
            float _BandSpeed;
            float _BandThicknessMin;
            float _BandThicknessMax;

            float _BlockStrength;
            float _BlockGridX;
            float _BlockGridY;
            float _BlockFlickerSpeed;

            float _DropoutStrength;
            float _DropoutSpeed;

            float _BrightBandBoost;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color;
                return o;
            }

            float hash11(float p)
            {
                p = frac(p * 0.1031);
                p *= p + 33.33;
                p *= p + p;
                return frac(p);
            }

            float hash21(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float t = _Time.y;
                float2 uv = i.uv;

                // Background animated grime
                float2 noiseUV = uv * _NoiseTiling + float2(0.0, t * _NoiseScrollY);
                float noiseA = tex2D(_MainTex, noiseUV).r;
                float noiseB = tex2D(_MainTex, uv * (_NoiseTiling * 0.65) + float2(0.0, -t * 0.23)).r;
                float bgNoise = lerp(noiseA, noiseB, 0.35);

                // Scanlines
                float scan = 0.5 + 0.5 * sin((uv.y * _ScanlineDensity + t * 2.0) * 6.28318);
                float scanlines = lerp(1.0, scan, _ScanlineStrength);

                // Big horizontal corruption bands
                float bandField = uv.y * _BandCount;
                float bandIndex = floor(bandField);
                float bandLocal = frac(bandField);

                float bandPhase = floor(t * _BandSpeed);
                float bandRand = hash21(float2(bandIndex, bandPhase));
                float bandActive = step(0.55 - _BandStrength * 0.35, bandRand);

                float thickness = lerp(_BandThicknessMin, _BandThicknessMax, hash21(float2(bandIndex, 77.0 + bandPhase)));
                float center = hash21(float2(bandIndex, 123.0 + bandPhase));
                float bandMask = smoothstep(thickness, 0.0, abs(bandLocal - center));
                bandMask *= bandActive;

                // Inner block fragments
                float2 grid = float2(_BlockGridX, _BlockGridY);
                float2 cell = floor(uv * grid);

                float blockRand = hash21(cell + floor(t * _BlockFlickerSpeed));
                float blockMask = step(1.0 - (_BlockStrength * 0.55), blockRand);

                // Make blocks strongly favor active bands
                float blockInBands = blockMask * saturate(bandMask * 3.0 + 0.15);

                // Additional dropout lines
                float dropoutRow = floor((uv.y + t * 0.03) * 120.0);
                float dropoutRand = hash21(float2(dropoutRow, floor(t * _DropoutSpeed)));
                float dropoutMask = step(1.0 - (_DropoutStrength * 0.25), dropoutRand);

                // Wide band slabs + fragmented cells
                float slabAlpha = bandMask * (0.45 + _BandStrength * 0.55);
                float blockAlpha = blockInBands * (0.35 + _BlockStrength * 0.65);
                float dropoutAlpha = dropoutMask * bandMask * 0.75;

                // Persistent mild dirty layer underneath
                float backgroundAlpha = bgNoise * _BackgroundNoiseStrength * 0.5;

                float alpha = 0.0;
                alpha += backgroundAlpha;
                alpha += slabAlpha;
                alpha += blockAlpha;
                alpha += dropoutAlpha;

                alpha *= scanlines;
                alpha *= _Opacity;
                alpha *= (0.5 + _Intensity * 1.25);

                // Bright corrupted bars
                float brightBand = saturate(bandMask * (0.6 + blockMask * 0.8));
                float brightness = 0.55 + bgNoise * 0.35 + brightBand * _BrightBandBoost;

                fixed3 color = _TintColor.rgb * brightness;

                return fixed4(color, saturate(alpha) * _TintColor.a) * i.color;
            }
            ENDCG
        }
    }
}
Shader "Uptime/PacketBorderGlow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _Confidence01 ("Confidence", Range(0,1)) = 0
        _Anomaly01 ("Anomaly", Range(0,1)) = 0

        _EmissionMin ("Emission Min", Float) = 0.9
        _EmissionMax ("Emission Max", Float) = 2.0

        _GlowStrength ("Glow Strength", Float) = 0.65
        _ShimmerStrength ("Shimmer Strength", Float) = 0.045
        _ShimmerSpeed ("Shimmer Speed", Float) = 7.0

        _CrackleStrength ("Crackle Strength", Float) = 0.55
        _CrackleScale ("Crackle Scale", Float) = 85.0
        _CrackleSpeed ("Crackle Speed", Float) = 18.0

        _AlphaCut ("Alpha Crackle Cut", Float) = 0.12
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

            float _GlowStrength;
            float _ShimmerStrength;
            float _ShimmerSpeed;

            float _CrackleStrength;
            float _CrackleScale;
            float _CrackleSpeed;

            float _AlphaCut;

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
                float2 local : TEXCOORD1;
            };

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color * _Color;
                o.uv = v.texcoord;
                o.local = v.texcoord * 2.0 - 1.0;
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
                fixed4 tex = tex2D(_MainTex, i.uv);
                float alpha = tex.a * i.color.a;

                // Your PacketView border color remains the source of truth.
                float3 baseColor = i.color.rgb;

                float c = pow(saturate(_Confidence01), 1.45);
                float a = saturate(_Anomaly01);

                float emission = lerp(_EmissionMin, _EmissionMax, c);

                // Tiny always-on shimmer so even normal packets feel alive.
                float shimmer =
                    sin(_Time.y * _ShimmerSpeed + i.uv.x * 31.0 + i.uv.y * 17.0)
                    * _ShimmerStrength;

                // Blocky crackle pattern, animated in time.
                float2 crackleUv = floor(i.uv * _CrackleScale);
                float crackleNoise = hash21(crackleUv + floor(_Time.y * _CrackleSpeed));

                // Sparse threshold. More anomaly = more sparks, but still not constant.
                float crackleThreshold = lerp(0.985, 0.82, a);
                float crackle = step(crackleThreshold, crackleNoise);

                // Keep crackle mostly on visible ring pixels.
                float borderMask = smoothstep(0.05, 0.75, alpha);

                float glow = alpha * _GlowStrength * c;
                float crackleBoost = crackle * borderMask * a * _CrackleStrength;

                float3 rgb = baseColor;
                rgb *= emission;
                rgb *= 1.0 + shimmer;
                rgb += baseColor * glow;
                rgb += baseColor * crackleBoost;

                // Very small alpha instability. Do not destroy the silhouette.
                alpha *= 1.0 - (crackle * a * _AlphaCut);

                return fixed4(rgb, alpha);
            }
            ENDCG
        }
    }
}
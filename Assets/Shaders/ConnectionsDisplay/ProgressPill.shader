Shader "Uptime/Connections/DisplayProgressPill"
{
    Properties
    {
        [PerRendererData] _MainTex ("UI Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _FillColor ("Fill", Color) = (0.025,0.075,0.1,1)
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float4 shape : TEXCOORD0;
                float4 corner : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float4 shape : TEXCOORD0;
                float4 corner : TEXCOORD1;
                float2 localPosition : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            fixed4 _Color;
            fixed4 _FillColor;
            float4 _ClipRect;

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.localPosition = v.vertex.xy;
                o.shape = v.shape;
                o.corner = v.corner;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float radius = i.corner.x;
                float2 offset = abs(i.shape.xy) - i.shape.zw + radius;
                float distanceToEdge = length(max(offset, 0.0))
                    + min(max(offset.x, offset.y), 0.0) - radius;
                float pixelWidth = max(fwidth(distanceToEdge), 0.0001);
                float outer = saturate(0.5 - distanceToEdge / pixelWidth);
                float inner = saturate(0.5 - (distanceToEdge + i.corner.y) / pixelWidth);
                float work = saturate(i.corner.z);
                float interiorMask = step(i.shape.x + i.shape.z, work * i.shape.z * 2);
                // Clockwise angular border reveal, starting at twelve o'clock.
                float angle = frac(atan2(i.shape.x / i.shape.z, i.shape.y / i.shape.w) / 6.2831853 + 1);
                float perimeterMask = work <= 0 ? 0 : work >= .9999 ? 1 : step(angle, work);
                fixed4 fill = lerp(_FillColor, fixed4(i.color.rgb * .38, 1), interiorMask * (1 - i.corner.w));
                fixed4 outline = lerp(i.color * fixed4(.28,.28,.28,1), i.color,
                    i.corner.w > .5 ? perimeterMask : 1);
                fixed4 result = lerp(outline, fill, inner);
                result.a = i.color.a * outer;
                #ifdef UNITY_UI_CLIP_RECT
                result.a *= UnityGet2DClipping(i.localPosition, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(result.a - 0.001);
                #endif
                return result;
            }
            ENDCG
        }
    }
}

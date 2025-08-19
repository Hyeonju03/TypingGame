Shader "UI/RoundedRect"
{
    Properties
    {
        _Color ("Tint (ignore if UseGraphicColor=Off)", Color) = (1,1,1,1)
        _FillColor   ("Fill Color",   Color) = (0.965, 0.757, 0.357, 1)
        _BorderColor ("Border Color", Color) = (0.722, 0.518, 0.180, 1)
        _Radius  ("Corner Radius (px)", Float) = 24
        _Border  ("Border Width (px)", Float)  = 6
        _UseGraphicColor ("Use Image Color Tint", Float) = 0
        [HideInInspector]_RectSize("RectSize", Vector) = (200,100,0,0)
        [HideInInspector]_MainTex("Sprite", 2D) = "white" {}
        [HideInInspector]_ClipRect("ClipRect", Vector) = (-32767,-32767,32767,32767)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Stencil { Ref 1 Comp Always Pass Keep Fail Keep ZFail Keep }
        Cull Off ZWrite Off Lighting Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t { float4 vertex:POSITION; float2 texcoord:TEXCOORD0; fixed4 color:COLOR; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; fixed4 col:COLOR; float2 local:TEXCOORD1; };

            sampler2D _MainTex;
            float4 _ClipRect;
            fixed4 _Color, _FillColor, _BorderColor;
            float2 _RectSize;
            float _Radius, _Border, _UseGraphicColor;

            v2f vert (appdata_t v){ v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.texcoord; o.col=v.color; o.local=v.vertex.xy; return o; }

            float sdRoundRect(float2 p, float2 b, float r){
                float2 q = abs(p) - (b - r);
                return length(max(q,0)) + min(max(q.x,q.y),0) - r;
            }

            fixed4 frag (v2f i):SV_Target
            {
                float2 halfSize = _RectSize * 0.5;
                float2 p = i.local;

                float R = clamp(_Radius, 0, min(halfSize.x, halfSize.y)-0.5);
                float B = clamp(_Border, 0, R);

                float distOuter = sdRoundRect(p, halfSize, R);
                float distInner = sdRoundRect(p, halfSize - B, max(R - B, 0));
                float aa = fwidth(distOuter) * 1.25;

                fixed4 fillCol   = (_UseGraphicColor>0.5)? _FillColor*_Color   : _FillColor;
                fixed4 borderCol = (_UseGraphicColor>0.5)? _BorderColor*_Color : _BorderColor;

                float aOuter  = saturate(0.5 - distOuter/aa);
                float onBorder= saturate(0.5 - distOuter/aa) * (1.0 - saturate(0.5 - (-distInner)/aa));
                float inside  = step(distInner, 0.0);

                fixed4 col = 0;
                col = lerp(col, borderCol, onBorder);
                col = lerp(col, fillCol, inside);
                col.a *= UnityGet2DClipping(i.local, _ClipRect);
                return col * aOuter;
            }
            ENDCG
        }
    }
}

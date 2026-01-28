Shader "Custom/TransparentHoleCircle"
{
    Properties
    {
        _Color ("Background Color", Color) = (0,0.5,1,1)
        _Center ("Center (UV)", Vector) = (0.5, 0.5, 0, 0)
        _Radius ("Radius (0-1)", Range(0,1)) = 0.25
        _Softness ("Soft Edge", Range(0,0.5)) = 0.02
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appv { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 vertex : SV_POSITION; };

            float4 _Color;
            float4 _Center;
            float _Radius;
            float _Softness;

            v2f vert(appv v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // UV in [0,1]
                float2 uv = i.uv;
                float2 center = _Center.xy;
                float d = distance(uv, center);

                // a = 0 inside circle (transparent), 1 outside (opaque)
                float a = smoothstep(_Radius - _Softness, _Radius + _Softness, d);

                // Output background color with alpha = a
                fixed4 col = fixed4(_Color.rgb, a);
                return col;
            }
            ENDHLSL
        }
    }
    FallBack "Unlit/Transparent"
}

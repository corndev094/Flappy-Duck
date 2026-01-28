Shader "Custom/TextureHoleWithSoftEdge"
{
    Properties
    {
        _Color ("Background Color", Color) = (0,0.5,1,1)
        _MainTex ("Hole Texture", 2D) = "white" {}
        _Center ("Center (UV)", Vector) = (0.5,0.5,0,0)
        _Scale ("Scale", Range(0,20)) = 0.25
        _Softness ("Soft Edge", Range(0,0.5)) = 0.02
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
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

            sampler2D _MainTex;
            float4 _Color;
            float4 _Center;
            float _Scale;
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
                float2 uv = i.uv;
                float2 center = _Center.xy;

                // UV relative to center, scaled
                float2 rel = (uv - center) / _Scale + 0.5;

                // Sample texture
                fixed4 texCol = tex2D(_MainTex, rel);

                // Soft edge: alpha giảm dần ở rìa texture
                float alpha = texCol.a;
                if(_Softness > 0)
                {
                    alpha = smoothstep(0, _Softness, alpha); // alpha 0->1 ở viền
                    alpha = 1 - alpha; // đảo để texture là transparent
                }
                else
                {
                    alpha = 1 - alpha;
                }

                return fixed4(_Color.rgb, alpha);
            }
            ENDHLSL
        }
    }
    FallBack "Unlit/Transparent"
}

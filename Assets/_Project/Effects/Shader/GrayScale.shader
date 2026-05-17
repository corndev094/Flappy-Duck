Shader "Custom/GrayscaleTransparent"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Mask ("Local Mask (Optional)", 2D) = "white" {}
        _MaskIntensity ("Mask Intensity", Range(0, 1)) = 1.0
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
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
            #pragma multi_compile _ UNITY_UI_ALPHACLIP
            
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            
            sampler2D _Mask;
            float4 _Mask_ST;
            float _MaskIntensity;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color; // Giữ nguyên color từ Image component
                
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // 1. Sample main texture (chuẩn Unity UI)
                half4 color = tex2D(_MainTex, IN.texcoord) * IN.color;
                
                // 2. Convert to Grayscale
                half gray = dot(color.rgb, half3(0.299, 0.587, 0.114));
                color.rgb = half3(gray, gray, gray);

                // 3. Apply Local Mask (CHỈ khi có cường độ > 0)
                // Dùng lerp để an toàn: nếu intensity = 1 thì dùng mask.r, nếu = 0 thì giữ nguyên 1.0
                if (_MaskIntensity > 0.001)
                {
                    fixed4 mask = tex2D(_Mask, TRANSFORM_TEX(IN.texcoord, _Mask));
                    color.a *= lerp(1.0, mask.r, _MaskIntensity);
                }

                // 4. Unity UI Mask/RectMask2D Clipping
                // Chỉ áp dụng nếu _ClipRect có kích thước hợp lệ (tránh bug transparent khi không có Mask parent)
                if (_ClipRect.z > _ClipRect.x && _ClipRect.w > _ClipRect.y)
                {
                    color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                }

                // 5. Alpha clip optimization (chỉ clip khi alpha thực sự quá nhỏ)
                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
    FallBack "UI/Default"
}
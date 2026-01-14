Shader "Glaucoma/Overlay"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _GazeCenter ("Gaze Center UV", Vector) = (0.5, 0.5, 0, 0)
        _Scale ("Field Scale", Float) = 1.0
        _EyeAspect ("Eye Aspect (Auto or Manual)", Float) = 1.0
    }
    SubShader
    {
        // Draw LAST on top of everything
        Tags { "Queue"="Overlay" }
        
        // No culling or depth
        Cull Off ZWrite Off ZTest Always
        // Multiply Blending: DstColor = DstColor * SrcColor
        // White (1) in shader keeps bg same. Black (0) makes bg black.
        Blend DstColor Zero 

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float4 _GazeCenter;
            float _Scale;
            float _EyeAspect;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Correct Aspect Ratio and Scale
                // Default Unity VR per-eye aspect is often near 1.0, but screens are 16:9
                
                // [FIX] We REMOVE the auto-detect logic because it interferes when user wants exactly 1.0.
                // In VR, ScreenParams can return 2x width (two eyes), causing elliptical distortion.
                float aspect = _EyeAspect;

                float2 offset = i.uv - _GazeCenter.xy;
                
                // Fix Aspect: Scale X to match Y visual magnitude
                offset.x *= aspect;
                
                // Fix Size: Inverse Scale
                offset *= (1.0 / _Scale);

                // Re-center
                float2 texUV = offset + float2(0.5, 0.5);

                // Hard Boundary Check: If we look outside the texture, it is BLIND (Black)
                if (texUV.x < 0.0 || texUV.x > 1.0 || texUV.y < 0.0 || texUV.y > 1.0)
                {
                     return fixed4(0, 0, 0, 1);
                }

                fixed4 col = tex2D(_MainTex, texUV);
                
                // Apply Tint
                col *= _Color;
                
                return col;
            }
            ENDCG
        }
    }
}

Shader "Medical/CataractSimulation"
{
    Properties
    {
        _BlurSize ("Blur Size (Acuity Loss)", Range(0, 10)) = 2.0
        _Contrast ("Contrast (logMAR)", Range(0, 1.5)) = 1.0
        _Brightness ("Brightness", Range(0, 2)) = 1.0
        _Tint ("Tint (Nuclear Color)", Color) = (1, 1, 1, 1)
        _OverlayColor ("Glare Overlay", Color) = (1, 1, 1, 0) // Alpha controls strength
    }
    SubShader
    {
        // Transparent overlay queue to render on top
        Tags { "Queue"="Overlay" "RenderType"="Transparent" }
        ZTest Always
        ZWrite Off
        Cull Off

        GrabPass
        {
            "_BackgroundTexture"
        }

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
                float4 grabPos : TEXCOORD1;
            };

            sampler2D _BackgroundTexture;
            float4 _BackgroundTexture_TexelSize;
            
            float _BlurSize;
            float _Contrast;
            float _Brightness;
            fixed4 _Tint;
            fixed4 _OverlayColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.grabPos = ComputeGrabScreenPos(o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 1. Blur Effect (Simple 5-tap Gaussian approximation)
                // Clinical Note: 20/80 vision means inability to resolve high frequencies.
                // We simulate this by averaging neighbor pixels.
                
                float2 grabUV = i.grabPos.xy / i.grabPos.w;
                float2 stride = _BackgroundTexture_TexelSize.xy * _BlurSize;

                fixed4 col = tex2D(_BackgroundTexture, grabUV) * 0.2270270270;
                
                // Sample 4 cardinal directions
                col += tex2D(_BackgroundTexture, grabUV + float2(stride.x, 0)) * 0.1945945946;
                col += tex2D(_BackgroundTexture, grabUV - float2(stride.x, 0)) * 0.1945945946;
                col += tex2D(_BackgroundTexture, grabUV + float2(0, stride.y)) * 0.1945945946;
                col += tex2D(_BackgroundTexture, grabUV - float2(0, stride.y)) * 0.1945945946;
                
                // Sample 4 diagonals (optional for smoother blur, skipped for performance)
                // If _BlurSize is large, this might look blocky. For VR, low tap count is safer for FPS.

                // 2. Tint & Brightness (Nuclear Sclerosis)
                col *= _Tint;
                col.rgb *= _Brightness;

                // 3. Contrast Loss (Pelli-Robson / logMAR)
                // Standard Contrast Formula: (Color - 0.5) * Contrast + 0.5
                col.rgb = (col.rgb - 0.5) * _Contrast + 0.5;

                // 4. Glare / Bloom Overlay
                // Simulates light scattering in the lens (Straylight)
                // We add a flat "milky" layer based on _OverlayColor alpha
                col.rgb = lerp(col.rgb, _OverlayColor.rgb, _OverlayColor.a);
                
                return col;
            }
            ENDCG
        }
    }
}

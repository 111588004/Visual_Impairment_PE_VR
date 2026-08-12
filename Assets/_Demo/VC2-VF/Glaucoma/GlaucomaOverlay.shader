Shader "Glaucoma/Overlay"
{
    Properties
    {
        _MainTex ("Sensitivity Map", 2D) = "black" {} // Default to black to avoid 'white square' artifact if missing
        _Color ("Tint", Color) = (1,1,1,1)
        _GazeCenter ("Gaze Center UV", Vector) = (0.5, 0.5, 0, 0)
        _Scale ("Field Scale", Float) = 1.0
        _EyeAspect ("Eye Aspect", Float) = 1.0
        
        _BlurSize ("Blur Size", Range(0.0, 0.05)) = 0.01
        _PeripheralDarkness ("Peripheral Darkness", Range(0.0, 1.0)) = 0.2
        _Desaturation ("Peripheral Desaturation", Range(0.0, 1.0)) = 0.5
        
        _ReferenceFOV ("Reference FOV (Internal)", Float) = 110.0
        [IntegerRange] _DebugMode ("Debug Mode (0=Off, 4=Gaussian)", Range(0,4)) = 0
        
        _GlobalContrast ("Global Contrast", Range(0.0, 2.0)) = 1.0
        _GlobalBrightness ("Global Brightness", Range(0.0, 2.0)) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+100" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha // Normal Alpha Blending for the overlay itself

        Pass
        {
            Name "GlaucomaBlurURP"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 screenPos : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // URP Texture Declarations
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            // Global texture from Render Feature
            TEXTURE2D(_GlaucomaBlurTex); 

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                
                // Legacy Single Gaze (Fallback)
                float4 _GazeCenter; 
                
                // Binocular Gaze
                float4 _GazeCenterLeft;
                float4 _GazeCenterRight;
                
                float _Scale;
                float _EyeAspect;
                float _BlurSize;
                float _PeripheralDarkness;
                float _Desaturation;
                float _ReferenceFOV;
                float _DebugMode;
                float _GlobalContrast;
                float _GlobalBrightness;
            CBUFFER_END

            v2f vert (appdata v)
            {
                v2f o;
                
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.screenPos = ComputeScreenPos(o.vertex);
                return o;
            }
            
            float3 Greyscale(float3 color)
            {
                float gray = dot(color, float3(0.299, 0.587, 0.114));
                return float3(gray, gray, gray);
            }



            // Simple pseudo-random function
            float rand(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }

            half4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
            
                // 1. Calculate Mask/Sensitivity Logic
                float aspect = _EyeAspect;
                
                // --- Binocular Logic ---
                // Determine which gaze center to use based on Stereo Eye Index
                // unity_StereoEyeIndex: 0 = Left, 1 = Right (usually)
                float2 currentGaze = _GazeCenter.xy;
                if (unity_StereoEyeIndex == 0) currentGaze = _GazeCenterLeft.xy;
                else if (unity_StereoEyeIndex == 1) currentGaze = _GazeCenterRight.xy;
                if (length(currentGaze) < 0.001) currentGaze = _GazeCenter.xy;
                
                // 1.2 Unified Tangent-Space Mapping
                // To be exact, screen UV distance is linear in tan(angle).
                // Screen radius 0.5 (half-width) corresponds to half-FOV.
                float halfFovRad = _ReferenceFOV * 0.5 * 3.14159 / 180.0;
                float maxTan = tan(halfFovRad);
                
                float2 offset = i.uv - currentGaze;
                
                // Apply eye aspect to compensate for SBS or wide viewports
                // If the eye viewport is square, aspect should be 1.0. 
                // If it's SBS (2:1), aspect for currentGaze=0.25 is 2.0.
                offset.x *= aspect;
                offset *= (1.0 / _Scale);

                // Use the radius directly to sample the 1:1 mapped texture
                // texture radius 1.0 = angle half-fov.
                // screen UV distance 0.5 = angle half-fov (if scaled correctly)
                float2 maskUV = offset + float2(0.5, 0.5);
                
                half sensitivity = 0.0;
                if (maskUV.x >= 0.0 && maskUV.x <= 1.0 && maskUV.y >= 0.0 && maskUV.y <= 1.0)
                {
                    sensitivity = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, maskUV).r;
                }

                // 2. Prepare Screen UV
                float2 screenUV = i.screenPos.xy / i.screenPos.w;

                // 3. Sample Scene Color (Sharp)
                half3 sharpCol = SampleSceneColor(screenUV);

                // --- MODIFIED: Handle Mode 1 and 2 with Global Contrast ---
                if (_DebugMode == 1 || _DebugMode == 2)
                {
                    // Apply Global Contrast/Brightness to sharpCol
                    sharpCol = (sharpCol - 0.5) * _GlobalContrast + 0.5;
                    sharpCol *= _GlobalBrightness;
                    
                    if (_DebugMode == 1) 
                    {
                        // In Mode 1, we want black periphery and sharp center + contrast.
                        // We must return opaque to overwrite the original scene and apply contrast correctly.
                        half3 maskColor = lerp(half3(0,0,0), sharpCol, sensitivity);
                        return half4(maskColor, 1.0);
                    }
                    if (_DebugMode == 2) return half4(sharpCol, 1.0); // Full Sharp + Contrast
                }

                // 4. Sample Scene Color (Blurred Peripheral Vision)
                // IMPROVED BLUR V8: Ultra-High Sample Golden Spiral
                // User requested maximum confidence limit for smoothness.
                // 80 samples provides extremely high quality, suitable for PC/Demo.
                // Note: This matches "Ultra" quality settings in post-processing stacks.
                
                half3 blurCol = 0;
                float totalWeight = 0;
                
                const int SAMPLE_COUNT = 80; // "Ultra" Quality (Restored)
                const float GOLDEN_ANGLE = 2.39996323; 
                
                float screenAspect = _ScreenParams.x / _ScreenParams.y;
                
                for (int j = 0; j < SAMPLE_COUNT; j++)
                {
                    float theta = j * GOLDEN_ANGLE;
                    float r = sqrt((float)j) / sqrt((float)SAMPLE_COUNT);
                    r *= _BlurSize;
                    
                    float2 dir = float2(cos(theta), sin(theta));
                    
                    // Aspect correction
                    float2 offset = dir * r;
                    offset.y *= screenAspect;
                    
                    float2 sampleUV = screenUV + offset;
                    
                    blurCol += SampleSceneColor(sampleUV).rgb;
                    totalWeight += 1.0;
                }
                blurCol /= totalWeight;
                
                // 5. Apply Peripheral Effects
                half3 grayBlur = Greyscale(blurCol);
                blurCol = lerp(blurCol, grayBlur, _Desaturation);
                blurCol *= (1.0 - _PeripheralDarkness);
                
                // 6. DEBUG MODES
                // Mode 1 & 2 handled early for performance
                if (_DebugMode == 3) return half4(blurCol, 1); // Internal Golden Spiral

                // Mode 4: Experimental Gaussian Blur (Render Feature)
                // Samples the global texture generated by GlaucomaBlurFeature.cs
                // Mode 4: "Pseudo-Gaussian" (Mip-Map + Jitter Fallback)
                // Uses texture LOD to get cheap blur, then jitters to hide blockiness.
                if (_DebugMode == 4)
                {
                // Mode 4: High-Quality Dithered Bokeh (Corrected Math)
                // Fixed radius calculation to prevent sampling outside meaningful UV range.
                if (_DebugMode == 4)
                {
                    float3 sum = 0;
                    float totalWeight = 0;
                    
                    float rnd = rand(screenUV + _Time.x);
                    float angleOffset = rnd * 6.28; 
                    float goldenAngle = 2.39996;
                    
                    // CORRECTION:
                    // Max sqrt(j) for 32 samples is ~5.65.
                    // We want max radius to be roughly proportional to _BlurSize.
                    // 0.15 * (8.0 / 5.65) ~= 0.21.
                    float radiusScale = _BlurSize * 0.2; 
                    
                    // 32 Samples (Restored)
                    for (int j = 0; j < 32; j++)
                    {
                        float theta = j * goldenAngle + angleOffset;
                        float r = sqrt((float)j) * radiusScale;

                        float2 uvOffset = float2(cos(theta), sin(theta)) * r * float2(1.0, _EyeAspect);
                        half3 col = SampleSceneColor(screenUV + uvOffset).rgb;
                        
                        // Weighting: slightly reduced at edges to soften "hard circle" effect
                        // But mostly uniform for Bokeh look
                        float w = 1.0; 
                        
                        sum += col * w;
                        totalWeight += w;
                    }
                    
                    half3 blurColor = sum / totalWeight;

                    // Apply Glaucoma Effects
                    half3 grayColor = Greyscale(blurColor);
                    blurColor = lerp(blurColor, grayColor, _Desaturation); 
                    blurColor *= (1.0 - (_PeripheralDarkness * 0.8)); // Restored darkness slightly

                    half3 finalColor = lerp(blurColor, sharpCol, sensitivity);

                    // Apply Global Desensitization
                    finalColor.rgb = (finalColor.rgb - 0.5) * _GlobalContrast + 0.5;
                    finalColor.rgb *= _GlobalBrightness;

                    return half4(finalColor, 1.0);
                }
                }
                
                // 7. Final Mix
                half3 finalColor = lerp(blurCol, sharpCol, sensitivity);

                // 8. Global Desensitization (Contrast & Brightness)
                // Affects the whole screen including the tunnel vision center
                finalColor.rgb = (finalColor.rgb - 0.5) * _GlobalContrast + 0.5;
                finalColor.rgb *= _GlobalBrightness;

                return half4(finalColor, 1.0);
            }

            ENDHLSL
        }
    }
}

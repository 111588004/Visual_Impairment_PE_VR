Shader "Hidden/GlaucomaBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BlurSize ("Blur Size", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        ZTest Always ZWrite Off Cull Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

        struct Attributes
        {
            float4 positionOS : POSITION;
            float2 uv         : TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 texcoord   : TEXCOORD0;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        Varyings Vert(Attributes input)
        {
            Varyings output;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

            // Use standard mesh transform - safer for Blitter's Quad mesh
            output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
            output.texcoord   = input.uv;
            return output;
        }

        TEXTURE2D(_BlitTexture);
        SAMPLER(sampler_LinearClamp);
        float4 _BlitTexture_TexelSize;
        float _BlurSize;

        half4 GaussianBlur(Varyings input, float2 dir)
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            return half4(1, 0, 0, 1); // DEBUG: RED
        }
        ENDHLSL

        Pass
        {
            Name "Horizontal"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragHorizontal
            
            half4 FragHorizontal(Varyings input) : SV_Target
            {
                return GaussianBlur(input, float2(1.0, 0.0));
            }
            ENDHLSL
        }

        Pass
        {
            Name "Vertical"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragVertical

            half4 FragVertical(Varyings input) : SV_Target
            {
                return GaussianBlur(input, float2(0.0, 1.0));
            }
            ENDHLSL
        }
    }
}

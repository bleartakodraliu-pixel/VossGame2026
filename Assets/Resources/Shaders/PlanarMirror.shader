Shader "Voss/Planar Mirror"
{
    Properties
    {
        _ReflectionTex ("Reflection", 2D) = "black" {}
        _BaseColor ("Tint", Color) = (0.93, 0.96, 1, 1)
        _EmissionColor ("Glow", Color) = (0, 0, 0, 0)
        _ReflectionEnabled ("Reflection Visible", Float) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Back
            ZWrite On
            ZTest LEqual
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_ReflectionTex);
            SAMPLER(sampler_ReflectionTex);
            CBUFFER_START(UnityPerMaterial)
                float4x4 _ReflectionVP;
                half4 _BaseColor;
                half4 _EmissionColor;
                float _ReflectionEnabled;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float4 reflectionCS : TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 world = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(world);
                output.reflectionCS = mul(_ReflectionVP, float4(world, 1));
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.reflectionCS.xy / max(input.reflectionCS.w, 0.0001) * 0.5 + 0.5;
                half3 reflected = SAMPLE_TEXTURE2D(_ReflectionTex, sampler_ReflectionTex, saturate(uv)).rgb;
                half3 surface = lerp(half3(0.012, 0.015, 0.02), reflected * _BaseColor.rgb, _ReflectionEnabled);
                return half4(surface + _EmissionColor.rgb * 0.12h, 1);
            }
            ENDHLSL
        }
    }
}

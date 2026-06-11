// LC-style retro outline (docs/art/lofi-uplift-and-outline-research.md):
// depth + normals Roberts-cross edge detection as a URP fullscreen pass.
// Outline color is civic blue-black #0A0F14, NOT pure black (style-lock v2 identity).
Shader "BlackCommission/LcOutline"
{
    Properties
    {
        _OutlineColor("Outline Color", Color) = (0.0392, 0.0588, 0.0784, 1)
        _DepthThreshold("Depth Threshold", Float) = 1.5
        _NormalThreshold("Normal Threshold", Float) = 0.4
        _OutlineStrength("Outline Strength", Range(0, 1)) = 0.85
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off Cull Off ZTest Always

        Pass
        {
            Name "LcOutline"

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            #pragma vertex Vert
            #pragma fragment Frag

            float4 _OutlineColor;
            float _DepthThreshold;
            float _NormalThreshold;
            float _OutlineStrength;

            float LinearDepthAt(float2 uv)
            {
                return LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                // _ScaledScreenParams tracks the (low-res) render target, so the outline
                // stays one chunky texel wide regardless of the URP render scale.
                float2 texel = 1.0 / _ScaledScreenParams.xy;

                half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                // Roberts cross taps (two diagonals).
                float2 uvA = uv + texel * float2(-1, -1);
                float2 uvB = uv + texel * float2( 1,  1);
                float2 uvC = uv + texel * float2(-1,  1);
                float2 uvD = uv + texel * float2( 1, -1);

                // Depth edge, normalized by center depth so far walls don't over-edge.
                float centerDepth = LinearDepthAt(uv);
                float dAB = LinearDepthAt(uvA) - LinearDepthAt(uvB);
                float dCD = LinearDepthAt(uvC) - LinearDepthAt(uvD);
                float depthEdge = sqrt(dAB * dAB + dCD * dCD) / max(centerDepth, 0.05);
                float depthFactor = step(_DepthThreshold * 0.01, depthEdge);

                // Normal edge.
                float3 nAB = SampleSceneNormals(uvA) - SampleSceneNormals(uvB);
                float3 nCD = SampleSceneNormals(uvC) - SampleSceneNormals(uvD);
                float normalEdge = sqrt(dot(nAB, nAB) + dot(nCD, nCD));
                float normalFactor = step(_NormalThreshold, normalEdge);

                float edge = max(depthFactor, normalFactor) * _OutlineStrength;
                return half4(lerp(color.rgb, _OutlineColor.rgb, edge), color.a);
            }
            ENDHLSL
        }
    }
    Fallback Off
}

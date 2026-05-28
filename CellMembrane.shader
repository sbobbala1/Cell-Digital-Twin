// CellMembrane.shader
// URP Lit shader with Fresnel rim, dissolve, and opacity.
// Use this as a fallback if you can't open Shader Graph.
// In Shader Graph: connect the same parameters (see property list below).
//
// Properties driven at runtime by CellMembraneController.cs:
//   _RimColor        (Color)   — rim/Fresnel colour
//   _RimPower        (Float)   — Fresnel falloff exponent (1.5–4)
//   _DissolveAmount  (Float)   — 0 = intact, 0.9 = nearly dissolved
//   _Opacity         (Float)   — overall alpha

Shader "CellTwin/CellMembrane"
{
    Properties
    {
        _BaseColor       ("Base color",    Color)  = (0.08, 0.08, 0.06, 0.6)
        _RimColor        ("Rim color",     Color)  = (0.2, 0.75, 0.6, 1.0)
        _RimPower        ("Rim power",     Range(0.5,8)) = 3.0
        _DissolveAmount  ("Dissolve",      Range(0,1))   = 0.0
        _DissolveTex     ("Dissolve tex",  2D) = "white" {}
        _Opacity         ("Opacity",       Range(0,1))   = 0.75
        _NormalMap       ("Normal map",    2D) = "bump"  {}
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent"
               "RenderPipeline"="UniversalPipeline" }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_DissolveTex); SAMPLER(sampler_DissolveTex);
            TEXTURE2D(_NormalMap);   SAMPLER(sampler_NormalMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _RimColor;
                float  _RimPower;
                float  _DissolveAmount;
                float  _Opacity;
                float4 _DissolveTex_ST;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
                float3 viewDirWS  : TEXCOORD1;
                float2 uv         : TEXCOORD2;
                float  fogFactor  : TEXCOORD3;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs vpi = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = vpi.positionCS;
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                OUT.viewDirWS  = GetWorldSpaceViewDir(vpi.positionWS);
                OUT.uv         = TRANSFORM_TEX(IN.uv, _DissolveTex);
                OUT.fogFactor  = ComputeFogFactor(vpi.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Dissolve — clip pixels below threshold
                float dissolveNoise = SAMPLE_TEXTURE2D(_DissolveTex,
                                          sampler_DissolveTex, IN.uv).r;
                clip(dissolveNoise - _DissolveAmount);

                // Fresnel rim
                float3 N = normalize(IN.normalWS);
                float3 V = normalize(IN.viewDirWS);
                float  fresnel = pow(1.0 - saturate(dot(N, V)), _RimPower);

                half4 col = lerp(_BaseColor, _RimColor, fresnel);
                col.a = _Opacity * lerp(0.3, 1.0, fresnel);

                // Fog
                col.rgb = MixFog(col.rgb, IN.fogFactor);
                return col;
            }
            ENDHLSL
        }
    }
}

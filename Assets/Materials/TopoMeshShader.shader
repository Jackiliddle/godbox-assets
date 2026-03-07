Shader "Custom/TopoMeshShader"
{
    Properties
    {
        _WaterColor ("Water Color (Low)", Color) = (0.1, 0.4, 0.8, 1)
        _SandColor ("Sand Color (Mid)", Color) = (0.9, 0.8, 0.6, 1)
        _SnowColor ("Snow Color (High)", Color) = (1.0, 1.0, 1.0, 1)
        _MinHeight ("Water Height Level", Float) = 0.0 
        _MaxHeight ("Snow Height Level", Float) = 0.2 
        _Steps ("Number of Terraces", Float) = 12.0
        _LineThickness ("Contour Line Thickness", Range(0.0, 0.5)) = 0.1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            // Bring in the URP lighting and math libraries
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _WaterColor;
                float4 _SandColor;
                float4 _SnowColor;
                float _MinHeight;
                float _MaxHeight;
                float _Steps;
                float _LineThickness;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                // Convert object position to world and screen space
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 1. The Topographic Math
                float h = saturate((input.positionWS.y - _MinHeight) / (_MaxHeight - _MinHeight));
                float hScaled = h * _Steps;
                float steppedH = floor(hScaled) / _Steps;

                // 2. Calculate the colors based on height
                half4 finalColor = lerp(
                    lerp(_WaterColor, _SandColor, steppedH * 2.0),
                    lerp(_SandColor, _SnowColor, (steppedH - 0.5) * 2.0),
                    step(0.5, steppedH)
                );

                // 3. Draw the black contour lines
                if (frac(hScaled) < _LineThickness)
                {
                    finalColor = half4(0, 0, 0, 1);
                }

                // 4. Calculate URP Lighting (Shadows & Sun)
                Light mainLight = GetMainLight();
                half NdotL = saturate(dot(normalize(input.normalWS), normalize(mainLight.direction)));
                
                half3 ambient = half3(0.4, 0.4, 0.4); // Base light so shadows aren't pitch black
                half3 diffuse = mainLight.color * NdotL;
                
                // Apply the light to our topographic color
                finalColor.rgb *= (ambient + diffuse);

                return finalColor;
            }
            ENDHLSL
        }
    }
}
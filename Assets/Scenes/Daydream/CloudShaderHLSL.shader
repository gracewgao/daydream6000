Shader "Custom/CloudShaderHLSL"
{
    Properties
    {
        _SDFTex ("SDF Texture", 3D) = "white" {}
        _MainTex ("Texture", 2D) = "white" {}
        _BlueNoise ("Blue Noise Texture", 2D) = "white" {}
        _NoiseScale ("Noise Scale", Float) = 1.0
        _CloudDensity ("Cloud Density", Range(0, 2)) = 1.0
        _FadeStart ("Fade Start", Range(0.0, 1.0)) = 0.3
        _FadeEnd ("Fade End", Range(0.0, 1.0)) = 0.5
        _DebugSDF ("Debug SDF", Integer) = 0
        _SDFStrength ("SDF Strength", Range(1, 5)) = 3.0
        _NoiseStrength ("Noise Strength", Range(0, 1)) = 0.3
        _CloudSpeed ("Cloud Speed", Range(0, 2)) = 0.5
    }
    
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"
            #define SHADERPASS SHADERPASS_FORWARD_UNLIT
            
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 objectPos : TEXCOORD1;
                float3 objectSpaceViewDir : TEXCOORD2;
                float3 objectSpaceLightDir : TEXCOORD3;
                float4 positionNDC : TEXCOORD4;
            };

            TEXTURE2D(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;

            TEXTURE2D(_BlueNoise);
            SAMPLER(sampler_BlueNoise);

            TEXTURE3D(_SDFTex);
            SAMPLER(sampler_SDFTex);

            float _NoiseScale;
            float _CloudDensity;

            float _FadeStart;
            float _FadeEnd;

            float _SDFStrength;
            float _NoiseStrength;

            float _CloudSpeed;
            int _DebugSDF;
            
            #define MAX_STEPS 80
            #define MARCH_SIZE 0.16
            
            float sampleSDF(float3 p) {
                float3 uv = p + 0.5;
                return SAMPLE_TEXTURE3D(_SDFTex, sampler_SDFTex, uv).r;
            }

            // hash function for procedurally-generated noise
            float hash(float3 p) {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            // procedurally-generated noise (instead of sampling from a noise texture)
            float noise(float3 x) {
                float3 p = floor(x);
                float3 f = frac(x);
                f = f * f * (3.0 - 2.0 * f);
                
                float n = p.x + p.y * 157.0 + 113.0 * p.z;
                return lerp(
                    lerp(
                        lerp(hash(p + float3(0, 0, 0)), hash(p + float3(1, 0, 0)), f.x),
                        lerp(hash(p + float3(0, 1, 0)), hash(p + float3(1, 1, 0)), f.x),
                        f.y
                    ),
                    lerp(
                        lerp(hash(p + float3(0, 0, 1)), hash(p + float3(1, 0, 1)), f.x),
                        lerp(hash(p + float3(0, 1, 1)), hash(p + float3(1, 1, 1)), f.x),
                        f.y
                    ),
                    f.z
                ) * 2.0 - 1.0;
            }

            float fbm(float3 p) {
                float3 q = p + _Time.y * _CloudSpeed * float3(1.0, -0.2, -1.0);

                float f = 0.0;
                float scale = 0.5;
                float factor = 2.02;

                for (int i = 0; i < 6; i++) {
                    f += scale * noise(q * _NoiseScale);
                    q *= factor;
                    factor += 0.21;
                    scale *= 0.5;
                }

                return f;
            }

            float scene(float3 p)
            {
                float distance = sampleSDF(p);
                float f = fbm(p);

                float len = length(p);
                float falloff = 1.0 - smoothstep(_FadeStart, _FadeEnd, len * len);

                float baseShape = -distance * _SDFStrength;
                float noise = f * _NoiseStrength;
    
                float blend = smoothstep(-0.15, 0.25, baseShape);
                float cloudShape = baseShape + noise * blend;

                cloudShape += pow(noise, 2.0) * 0.2;

                return cloudShape * _CloudDensity * falloff;
            }

            float4 raymarch(float3 rayOrigin, float3 rayDirection, float3 lightDirection, float offset) {
                if (_DebugSDF != 0) {
                    float depth = 0.0;
                    for (int i = 0; i < MAX_STEPS; i++)
                    {
                        float3 p = rayOrigin + depth * rayDirection;
                        float dist = sampleSDF(p);
            
                        if (dist < 0.001) {
                            // We hit the surface - return solid color
                            return float4(1.0, 1.0, 1.0, 1.0);
                        }
                        depth += max(0.01, abs(dist));
                    }
                    return float4(0.0, 0.0, 0.0, 0.0);
                }

                float depth = 0.0;
                depth += MARCH_SIZE * offset;
                float3 p = rayOrigin + depth * rayDirection;
                
                float4 res = float4(0.0, 0.0, 0.0, 0.0);
                
                for (int i = 0; i < MAX_STEPS; i++)
                {
                    float density = scene(p);
                    
                    if (density > 0.0)
                    {
                        float diffuse = clamp((scene(p) - scene(p + 0.3 * lightDirection)) / 0.3, 0.0, 1.0);
                        float3 lin = float3(0.60, 0.60, 0.75) * 1.1 + 0.8 * float3(1.0, 0.6, 0.3) * diffuse;
                        float4 color = float4(lerp(float3(1.0, 1.0, 1.0), float3(0.0, 0.0, 0.0), density), density);
                        color.rgb *= lin;
                        color.rgb *= color.a;

                        float transmittance = 1.0 - res.a;
                        res += color * transmittance;
                    }
                    
                    depth += MARCH_SIZE;
                    p = rayOrigin + depth * rayDirection;
                }
                
                return res;
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv.xy * _MainTex_ST.xy + _MainTex_ST.zw;
                
                // Keep the object position as-is without scaling
                output.objectPos = input.positionOS.xyz;
                
                // Get object space view direction
                float3 objectSpaceViewPos = mul(UNITY_MATRIX_I_M, float4(_WorldSpaceCameraPos.xyz, 1.0)).xyz;
                output.objectSpaceViewDir = normalize(objectSpaceViewPos - input.positionOS.xyz);
                
                // Get light direction
                output.objectSpaceLightDir = normalize(mul((float3x3)UNITY_MATRIX_I_M, -_DirectionalLightDatas[0].forward.xyz));
                
                // Compute NDC position for screen UV calculation
                output.positionNDC = output.positionCS * 0.5f;
                output.positionNDC.xy = float2(output.positionNDC.x, output.positionNDC.y * _ProjectionParams.x) + output.positionNDC.w;
                output.positionNDC.zw = output.positionCS.zw;
                
                return output;
            }
            
            float4 frag(Varyings input) : SV_Target
            {
                // Use object space view position
                float3 ro = mul(UNITY_MATRIX_I_M, float4(_WorldSpaceCameraPos.xyz, 1.0)).xyz;
                float3 rd = normalize(input.objectPos - ro);

                float2 screenUV = input.positionNDC.xy / input.positionNDC.w;
                
                float timeOffset = frac(_Time.y * 60);
                float blueNoise = SAMPLE_TEXTURE2D(_BlueNoise, sampler_BlueNoise, input.positionCS.xy / 1024.0).r;
                float offset = frac(blueNoise * 2.4 + timeOffset);

                float4 res = raymarch(ro, rd, input.objectSpaceLightDir, offset);

                // Sample background colour using NDC-derived UVs to get rid of dark ring
                float3 backgroundColor = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, screenUV).rgb;
                float3 finalColor = backgroundColor * (1.0 - res.a) + res.rgb;
                
                return float4(finalColor, res.a);
            }
            ENDHLSL
        }
    }
}
Shader "Custom/CloudShader"
{
    Properties
    {
        _SDFTex ("SDF Texture", 3D) = "white" {}                // defines the shape of the cloud
        _BlueNoise ("Blue Noise Texture", 2D) = "white" {}      // defines the dithering pattern
        _NoiseScale ("Noise Scale", Float) = 1.0                // scales the noise pattern
        _CloudDensity ("Cloud Density", Range(0, 2)) = 1.0      // controls the density of the cloud
        _FadeStart ("Fade Start", Range(0.0, 1.0)) = 0.3        // start of the fade
        [Toggle]_DebugSDF ("Debug SDF", Integer) = 0            // enables debug mode for the SDF
        _SDFStrength ("SDF Strength", Range(1, 5)) = 3.0        // controls how closely the shape of the cloud follows the shape of the SDF
        _NoiseStrength ("Noise Strength", Range(0, 1)) = 0.3    // controls noise strength
        _NoiseGain ("Noise Gain", Range(0, 1)) = 0.612          // controls noise gain
        _NoiseLacunarity ("Noise Lacunarity", Range(1, 5)) = 2.920
        _NoiseBias ("Noise Bias", Range(0.5, 3.0)) = 0.7
        _NoiseRange ("Noise Range", Range(0.5, 5.0)) = 1.0
        _CloudSpeed ("Cloud Speed", Range(0, 5)) = 0.5          // controls speed of cloud animation
        _SunDirection ("Sun Direction", Vector) = (1,0,0)       // controls direction of sun (as a unit vector in absolute world coordinates)
    }
    
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

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
                float4 screenPos : TEXCOORD1;
                float3 objectSpaceViewDir : TEXCOORD2;
                float3 objectPos : TEXCOORD3;
                float4 grabPos : TEXCOORD4;
            };

            UNITY_DECLARE_SCREENSPACE_TEXTURE(_CameraOpaqueTexture);

            sampler2D _BlueNoise;

            sampler3D _SDFTex;

            float _NoiseScale;
            float _CloudDensity;

            float _FadeStart;

            float _SDFStrength;

            float _NoiseStrength;
            float _NoiseGain;
            float _NoiseLacunarity;
            float _NoiseBias;
            float _NoiseRange;

            float _CloudSpeed;
            bool _DebugSDF;

            float3 _SunDirection;
            
            #define MAX_STEPS 100
            #define MARCH_SIZE 0.1
            
            float sampleSDF(float3 p) {
                float3 uv = p + 0.5;
                return tex3D(_SDFTex, uv).r;
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
                ) * 2.0 * _NoiseRange - _NoiseRange + _NoiseBias; // apply custom range and bias to noise
            }

            float3 getNoiseAnimationDirection() {
                float3 viewDir = normalize(_WorldSpaceCameraPos - unity_ObjectToWorld[3].xyz);
                float3 up = abs(viewDir.y) < 0.999 ? float3(0,1,0) : float3(1,0,0);
                return normalize(cross(viewDir, up));
            }

            float fbm(float3 p) {
                float3 noiseAnimationDirection = getNoiseAnimationDirection();
                float3 q = p + _Time.x * _CloudSpeed * noiseAnimationDirection;

                float f = 0.0;
                float amplitude = 0.5;
                float frequency = 1.0;
                int octaves = 6;

                for (int i = 0; i < octaves; i++) {
                    f += amplitude * noise(q * _NoiseScale * frequency);
                    frequency *= _NoiseLacunarity;
                    amplitude *= _NoiseGain;
                }

              return clamp(f, -10.0, 10.0);
            }

            float scene(float3 p)
            {
                float distance = sampleSDF(p);
                float f = fbm(p);

                float falloff = 1.0 - smoothstep(_FadeStart, 1.0, length(p));
                float baseShape = -distance * _SDFStrength;

                float noise = (f - _NoiseBias) * _NoiseStrength;
                float blend = smoothstep(-0.15, 0.25, baseShape);
                
                float cloudShape = max(0.0, baseShape + noise * blend);

                return cloudShape * _CloudDensity * falloff;
            }

            float4 raymarch(float3 rayOrigin, float3 rayDirection, float offset)
            {
                if (_DebugSDF) {
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

                float3 sunDirection = normalize(_SunDirection);
                
                float4 res = float4(0.0, 0.0, 0.0, 0.0);
                
                for (int i = 0; i < MAX_STEPS; i++)
                {
                    float density = scene(p);
                    
                    if (density > 0.0)
                    {
                        float diffuse = clamp((scene(p) - scene(p + 0.3 * sunDirection)) / 0.3, 0.0, 1.0);
                        float3 lin = float3(0.60, 0.60, 0.75) * 1.1 + 0.8 * float3(1.0, 0.6, 0.3) * diffuse;
                        float visualDensity = 1.0 - pow(1.0 - min(density, 1.0), 0.5); // Compress the density curve
                        float4 color = float4(lerp(float3(1.0, 1.0, 1.0), float3(0.5, 0.5, 0.5), visualDensity), density);
                        color.rgb *= lin;
                        color.rgb *= color.a;
                        res += color * (1.0 - res.a);
                    }
                    
                    depth += MARCH_SIZE;
                    p = rayOrigin + depth * rayDirection;
                }
                
                return res;
            }
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.screenPos = ComputeScreenPos(o.vertex);
                o.grabPos = ComputeGrabScreenPos(o.vertex);

                float3 objectSpaceViewPos = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1.0)).xyz;
                o.objectSpaceViewDir = normalize(objectSpaceViewPos - v.vertex.xyz);
                o.objectPos = v.vertex.xyz;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                float3 ro = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1.0)).xyz;
                float3 rd = normalize(i.objectPos - ro);

                float timeOffset = frac(_Time.y * 60);
                float blueNoise = tex2D(_BlueNoise, i.screenPos.xy / i.screenPos.w * _ScreenParams.xy / 1024.0).r;
                float offset = frac(blueNoise + timeOffset);

                float4 res = raymarch(ro, rd, offset);
                
                // Create a softer edge falloff & apply to alpha 
                res.a *= smoothstep(0.0, 0.15, res.a);
                
                // Sample background colour & blend with cloud colour
                float2 grabUV = i.grabPos.xy / i.grabPos.w;
                float3 backgroundColor = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_CameraOpaqueTexture, grabUV).rgb;
                float3 cloudColor = res.a > 0.0001 ? res.rgb / res.a : float3(1,1,1);
                
                float3 finalColor = lerp(backgroundColor, cloudColor, res.a);
                
                return float4(finalColor, res.a);
            }
            ENDCG
        }
    }
}

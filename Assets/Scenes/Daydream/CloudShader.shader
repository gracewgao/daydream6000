Shader "Custom/CloudShader"
{
    Properties
    {
        _SDFTex ("SDF Texture", 3D) = "white" {}                // defines the shape of the cloud
        _BlueNoise ("Blue Noise Texture", 2D) = "white" {}      // defines the dithering pattern
        _NoiseScale ("Noise Scale", Float) = 1.0                // scales the noise pattern
        _CloudDensity ("Cloud Density", Range(0, 6)) = 1.0      // controls the density of the cloud
        [Toggle]_DebugSDF ("Debug SDF", Integer) = 0            // enables debug mode for the SDF
        _SDFStrength ("SDF Strength", Range(1, 5)) = 3.0        // controls how closely the shape of the cloud follows the shape of the SDF
        _NoiseStrength ("Noise Strength", Range(0, 1)) = 0.3    // controls noise strength
        _NoiseGain ("Noise Gain", Range(0, 1)) = 0.612          // controls noise gain
        _NoiseLacunarity ("Noise Lacunarity", Range(1, 5)) = 2.920
        _NoiseBias ("Noise Bias", Range(0.5, 3.0)) = 0.7
        _NoiseRange ("Noise Range", Range(0.5, 5.0)) = 1.0
        _CloudFatness ("Cloud Fatness", Range(0, 0.5)) = 0.1    // controls how fat the clouds are
        _AnimationSpeed ("Animation Speed", Range(0, 10)) = 0.5   // controls speed of cloud animation
        _BoundingBoxSize ("Bounding Box Size", Float) = 1.0     // controls the side length of the bounding box
        _SunDirection ("Sun Direction", Vector) = (1,0,0)       // controls direction of sun (as a unit vector in absolute world coordinates)
        _LightColor ("Light Color", Color) = (1,1,1,1)          // color of the directional light
        _ShadowStrength ("Shadow Strength", Range(0.1, 3.0)) = 1.0 // controls how dark the shadows get
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

            float _SDFStrength;

            float _NoiseStrength;
            float _NoiseGain;
            float _NoiseLacunarity;
            float _NoiseBias;
            float _NoiseRange;
            float _CloudFatness;

            float _AnimationSpeed;
            bool _DebugSDF;
            
            float _BoundingBoxSize;

            float3 _SunDirection;
            float3 _GlobalSunDirection;
            float4 _LightColor;
            float _ShadowStrength;
            
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

            float fbm(float3 p) {
                float3 q = p + _Time.x * _AnimationSpeed;

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

            // Calculate the base shape from SDF
            float calculateBaseShape(float3 p)
            {
                float distance = sampleSDF(p);
                
                // Calculate distance from center using a smoother metric
                // This avoids the angular/cubic look from using min() on axis distances
                float centerDist = length(p);
                
                // Calculate the maximum possible distance dynamically
                // We use the fact that in normalized coordinates, the center is at (0,0,0)
                // and the furthest point would be at a corner of the bounding box
                // The bounding box size is controlled by _BoundingBoxSize (default=1.0)
                float normalizedDist = centerDist / (length(float3(0.5, 0.5, 0.5)) * _BoundingBoxSize);
                
                // Invert so it's 1 at center, 0 at edges
                float centerFactor = 1.0 - normalizedDist;
                
                // Apply smoothstep to control the falloff and create a more natural thickness gradient
                // Use the CloudFatness property to control the amount of thickness
                float fatFactor = smoothstep(0.0, 0.7, centerFactor) * _CloudFatness;
                
                // Apply the thickness by reducing the distance value
                // This effectively expands the cloud shape
                return -(distance - fatFactor) * _SDFStrength;
            }
            
            // Calculate the final cloud shape with noise
            float calculateCloudShape(float baseShape, float noiseValue)
            {
                float noise = (noiseValue - _NoiseBias) * _NoiseStrength;
                float blend = smoothstep(-0.15, 0.25, baseShape);
                
                return max(0.0, baseShape + noise * blend);
            }
            
            // Calculate a boundary falloff factor to ensure clouds fade out at the edges
            float calculateBoundaryFalloff(float3 p)
            {
                // Calculate distance from center as a percentage of the bounding box
                // We use absolute values for each axis to create a box-shaped falloff
                float3 absPos = abs(p);
                
                // Find the maximum component distance to create a box-shaped boundary
                float maxDist = max(max(absPos.x, absPos.y), absPos.z);
                
                // Calculate how close we are to the edge (0 = at center, 1 = at edge)
                // The 0.5 represents the normalized distance to the edge of the box
                float edgeDistance = maxDist / 0.5;
                
                // Create a smooth falloff that starts at 0.8 of the distance to the edge
                // This ensures clouds fade out before reaching the actual boundary
                return smoothstep(1.0, 0.8, edgeDistance);
            }
            
            float scene(float3 p)
            {
                float baseShape = calculateBaseShape(p);
                float noiseValue = fbm(p);
                float cloudShape = calculateCloudShape(baseShape, noiseValue);
                
                // Apply the boundary falloff to ensure clouds fade out at the edges
                float boundaryFalloff = calculateBoundaryFalloff(p);
                
                return cloudShape * _CloudDensity * boundaryFalloff;
            }

            // Calculate cloud color at a specific point in the cloud
            float4 calculateCloudColor(float3 p, float density, float3 sunDirection)
            {
                // Multi-sample shadow calculation for softer shadows
                // Take multiple samples along the light direction and average them
                float lightSampleDistance = 0.6; // Base distance for light sampling
                float diffuse = 0.0;
                
                // Number of samples for soft shadows
                const int shadowSamples = 4;
                
                // Take multiple samples with gradually increasing distances
                for (int i = 0; i < shadowSamples; i++) {
                    // Use progressively larger steps for each sample
                    float stepSize = lightSampleDistance * (0.5 + float(i) / float(shadowSamples));
                    float samplePoint = (scene(p) - scene(p + stepSize * sunDirection)) / stepSize;
                    
                    // Weight closer samples more heavily for a more natural falloff
                    float weight = 1.0 - (float(i) / float(shadowSamples)) * 0.5;
                    diffuse += clamp(samplePoint, 0.0, 1.0) * weight;
                }
                
                // Normalize the weighted sum
                diffuse /= shadowSamples * 0.75; // Adjust divisor to maintain overall brightness
                diffuse *= _ShadowStrength; // Apply shadow strength
                
                // Increased ambient term for softer shadows
                float ambient = 0.25; // Increased from 0.15 for softer shadows
                float lightFactor = ambient + diffuse * (1.0 - ambient);
                
                // Add bluish-purple tinge to shadows
                float3 shadowColor = float3(0.46, 0.43, 0.61); // Bluish-purple color
                float3 sunlitColor = _LightColor.rgb;
                
                // Blend between shadow color and sunlit color based on light factor
                // More shadow = more bluish-purple
                float3 lin = lerp(shadowColor, sunlitColor, lightFactor);
                
                // Density visualization with light-dependent base color
                float visualDensity = 1.0 - pow(1.0 - min(density, 1.0), 0.5); // Compress the density curve
                // Base cloud color now depends on light intensity
                // Using slightly warmer and darker colors for a more natural look
                float3 brightCloudColor = float3(0.75, 0.73, 0.7); // Slightly warmer white (was 0.8, 0.8, 0.8)
                float3 darkCloudColor = float3(0.37, 0.35, 0.33); // Slightly warmer dark gray (was 0.4, 0.4, 0.4)
                float3 baseCloudColor = lerp(brightCloudColor, darkCloudColor, visualDensity);
                float4 color = float4(baseCloudColor, density);
                
                // Apply lighting
                color.rgb *= lin;
                color.rgb *= color.a;
                
                return color;
            }
            
            // Calculate sunset color based on sun position and height
            float3 calculateSunsetColor(float3 globalSunDir, float3 baseLightColor)
            {
                // Calculate sun height (dot product with global up vector)
                float sunHeight = dot(globalSunDir, float3(0, 1, 0));
                
                // Create two sunset factors for different angle ranges
                // Mid-angle sunset (orange tones) - active when sun is at medium-low height
                float orangeSunsetFactor = 1.0 - smoothstep(0.2, 0.5, sunHeight);
                orangeSunsetFactor *= smoothstep(0.0, 0.2, sunHeight); // Fade out when sun gets too low
                
                // Low-angle sunset (red tones) - active when sun is very low on horizon
                float redSunsetFactor = 1.0 - smoothstep(0.0, 0.25, sunHeight);
                
                // Create sunset colors
                float3 orangeSunsetColor = float3(1.2, 0.9, 0.5); // Less saturated warm orange
                float3 redSunsetColor = float3(1.4, 0.5, 0.2); // Deep red-orange
                
                // Apply the directional light's color with multi-stage sunset
                float3 adjustedColor = baseLightColor;
                // Apply orange sunset color at medium-low angles
                adjustedColor = lerp(adjustedColor, adjustedColor * orangeSunsetColor, orangeSunsetFactor);
                // Apply red sunset color at very low angles
                adjustedColor = lerp(adjustedColor, adjustedColor * redSunsetColor, redSunsetFactor);
                
                // Apply additional darkening when sun becomes lower in the sky
                float sunVisibility = smoothstep(-0.1, 0.05, sunHeight);
                adjustedColor *= max(0.2, sunVisibility);
                
                return adjustedColor;
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

                // Use the object-space sun direction for shadows
                float3 sunDirection = normalize(_SunDirection);
                
                float4 res = float4(0.0, 0.0, 0.0, 0.0);
                
                for (int i = 0; i < MAX_STEPS; i++)
                {
                    float density = scene(p);
                    
                    if (density > 0.0)
                    {
                        float4 color = calculateCloudColor(p, density, sunDirection);
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
                
                float3 globalSunDir = normalize(_GlobalSunDirection);
                
                float timeOffset = frac(_Time.y * 60);
                float blueNoise = tex2D(_BlueNoise, i.screenPos.xy / i.screenPos.w * _ScreenParams.xy / 1024.0).r;
                float offset = frac(blueNoise + timeOffset);

                float4 res = raymarch(ro, rd, offset);
                
                // Get the sunset-adjusted light color
                float3 adjustedLightColor = calculateSunsetColor(globalSunDir, _LightColor.rgb);
                
                res.rgb *= adjustedLightColor;
                
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

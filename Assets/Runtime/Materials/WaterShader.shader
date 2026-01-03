Shader "Custom/WaterShader"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (0.2, 0.5, 0.7, 1)
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}

        [Header(Wave Settings)]
        _WaveSpeed("Wave Speed", Range(0.1, 2.0)) = 0.5
        _WaveHeight("Wave Height", Range(0.01, 0.5)) = 0.1
        _WaveFrequency("Wave Frequency", Range(1.0, 10.0)) = 3.0

        [Header(Ripple Settings)]
        _RippleSpeed("Ripple Speed", Range(0.1, 3.0)) = 1.0
        _RippleScale("Ripple Scale", Range(0.5, 5.0)) = 2.0
        _RippleStrength("Ripple Strength", Range(0.01, 0.2)) = 0.05

        [Header(PS2 Effects)]
        _VertexSnap("Vertex Snap Intensity", Range(0, 50)) = 20
        _ColorSteps("Color Quantization Steps", Range(2, 32)) = 16
        [Toggle] _EnableAffineTex("Enable Affine Texture Mapping", Float) = 1
        _AffineIntensity("Affine Intensity", Range(0, 1)) = 1.0
        [Toggle] _EnableDithering("Enable Ordered Dithering", Float) = 1
        _DitherIntensity("Dither Intensity", Range(0, 0.1)) = 0.03

        [Header(Dual Layer Scrolling)]
        [Toggle] _EnableDualLayer("Enable Dual Layer", Float) = 1
        _Layer2Map("Layer 2 Map", 2D) = "white" {}
        _Layer1ScrollSpeed("Layer 1 Scroll Speed", Vector) = (0.05, 0.02, 0, 0)
        _Layer2ScrollSpeed("Layer 2 Scroll Speed", Vector) = (-0.03, 0.04, 0, 0)
        _LayerBlend("Layer Blend", Range(0, 1)) = 0.5

        [Header(Height Shading)]
        [Toggle] _EnableHeightShading("Enable Height Shading", Float) = 1
        _HeightShadingIntensity("Height Shading Intensity", Range(0, 1)) = 0.3

        [Header(Transparency)]
        _Alpha("Water Alpha", Range(0.1, 1.0)) = 0.7
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                // === AFFINE TEXTURE MAPPING ===
                // For affine mapping, we pass UV * W (affineUV.xy) and W (affineUV.z)
                // PS2 hardware linearly interpolated UV coordinates without perspective correction
                // By multiplying UVs by clip.w in vertex shader and dividing in fragment,
                // we simulate this perspective-incorrect interpolation
                float3 affineUV1 : TEXCOORD0;      // Layer 1: xy = uv * w, z = w
                float3 affineUV2 : TEXCOORD1;      // Layer 2: xy = uv * w, z = w
                float2 standardUV1 : TEXCOORD2;   // Standard perspective-correct UVs (fallback)
                float2 standardUV2 : TEXCOORD3;   // Standard perspective-correct UVs (fallback)
                float3 worldPos : TEXCOORD4;
                float4 screenPos : TEXCOORD5;     // For dithering based on screen position
                float waveHeight : TEXCOORD6;     // Wave displacement for height-based shading
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_Layer2Map);
            SAMPLER(sampler_Layer2Map);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float4 _BaseMap_ST;
                float4 _Layer2Map_ST;

                float _WaveSpeed;
                float _WaveHeight;
                float _WaveFrequency;

                float _RippleSpeed;
                float _RippleScale;
                float _RippleStrength;

                float _VertexSnap;
                float _ColorSteps;
                float _EnableAffineTex;
                float _AffineIntensity;
                float _EnableDithering;
                float _DitherIntensity;

                float _EnableDualLayer;
                float4 _Layer1ScrollSpeed;
                float4 _Layer2ScrollSpeed;
                float _LayerBlend;

                float _EnableHeightShading;
                float _HeightShadingIntensity;

                float _Alpha;
            CBUFFER_END

            // === 4x4 BAYER DITHERING MATRIX ===
            // Classic ordered dithering pattern used in retro graphics
            // Values normalized to 0-1 range, offset by -0.5 to center around zero
            // This creates the characteristic crosshatch/stipple pattern of PS2 era
            static const float4x4 BAYER_MATRIX = float4x4(
                 0.0/16.0,  8.0/16.0,  2.0/16.0, 10.0/16.0,
                12.0/16.0,  4.0/16.0, 14.0/16.0,  6.0/16.0,
                 3.0/16.0, 11.0/16.0,  1.0/16.0,  9.0/16.0,
                15.0/16.0,  7.0/16.0, 13.0/16.0,  5.0/16.0
            );

            // Helper function to get dither value from Bayer matrix
            float GetDitherValue(float2 screenPos)
            {
                // Get pixel coordinates and wrap to 4x4 pattern
                int2 pixelCoord = int2(fmod(screenPos, 4.0));
                // Sample the Bayer matrix - offset by 0.5 to center the dither effect
                return BAYER_MATRIX[pixelCoord.x][pixelCoord.y] - 0.5;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                // Transform to world space
                float3 worldPos = TransformObjectToWorld(IN.positionOS.xyz);

                // === WAVE ANIMATION (calm, slow primary waves) ===
                float waveTime = _Time.y * _WaveSpeed;
                // Primary wave along X axis
                float wave1 = sin(worldPos.x * _WaveFrequency + waveTime) * _WaveHeight;
                // Secondary wave along Z axis with offset frequency for variety
                float wave2 = sin(worldPos.z * _WaveFrequency * 0.8 + waveTime * 1.3) * _WaveHeight * 0.5;

                // === RIPPLE ANIMATION (smaller secondary waves) ===
                float rippleTime = _Time.y * _RippleSpeed;
                // First ripple layer
                float ripple = sin((worldPos.x + worldPos.z) * _RippleScale + rippleTime) * _RippleStrength;
                // Second ripple layer at different scale and phase
                ripple += sin((worldPos.x - worldPos.z) * _RippleScale * 1.5 + rippleTime * 0.7) * _RippleStrength * 0.5;

                // === TOTAL WAVE DISPLACEMENT ===
                // Calculate combined wave height for height-based shading
                // This value is passed to fragment shader to modulate brightness
                float totalWaveHeight = wave1 + wave2 + ripple;

                // Apply vertical displacement (all waves combined)
                worldPos.y += totalWaveHeight;

                // === PS2 VERTEX SNAPPING ===
                // Transform to clip space
                float4 clipPos = TransformWorldToHClip(worldPos);

                // Snap vertices to grid (simulates PS2 fixed-point precision)
                float snapScale = _VertexSnap;
                clipPos.xyz = floor(clipPos.xyz * snapScale + 0.5) / snapScale;

                OUT.positionHCS = clipPos;

                // === DUAL-LAYER UV SCROLLING ===
                // Layer 1 UV with independent scroll direction
                float2 uv1 = TRANSFORM_TEX(IN.uv, _BaseMap);
                uv1 += _Time.y * _Layer1ScrollSpeed.xy;

                // Layer 2 UV with different scroll direction for visual variety
                float2 uv2 = TRANSFORM_TEX(IN.uv, _Layer2Map);
                uv2 += _Time.y * _Layer2ScrollSpeed.xy;

                // Store standard (perspective-correct) UVs
                OUT.standardUV1 = uv1;
                OUT.standardUV2 = uv2;

                // === AFFINE TEXTURE MAPPING SETUP ===
                // PS2 hardware used affine (linear) texture mapping instead of perspective-correct
                // This caused textures to warp and swim on surfaces, especially at angles
                //
                // How it works:
                // - Modern GPUs interpolate UV/W and 1/W, then compute UV = (UV/W) / (1/W) per pixel
                // - PS2 just linearly interpolated UV directly, ignoring perspective
                // - We simulate this by:
                //   1. Multiplying UV by W (clip.w) in vertex shader
                //   2. Passing W alongside the UVs
                //   3. Dividing UV*W by W in fragment shader AFTER interpolation
                // - The linear interpolation of UV*W and W creates the characteristic warping

                float w = clipPos.w;
                OUT.affineUV1 = float3(uv1 * w, w);
                OUT.affineUV2 = float3(uv2 * w, w);

                // === SCREEN POSITION FOR DITHERING ===
                // ComputeScreenPos gives us normalized screen coordinates
                OUT.screenPos = ComputeScreenPos(clipPos);

                // Pass world position for fragment shader effects
                OUT.worldPos = worldPos;

                // === WAVE HEIGHT FOR HEIGHT-BASED SHADING ===
                // Normalize wave height to approximately -1 to 1 range
                // The maximum theoretical displacement is _WaveHeight * 1.5 + _RippleStrength * 1.5
                // We divide by _WaveHeight to get a normalized value relative to wave settings
                // This makes the shading intensity independent of wave height settings
                float maxWaveAmplitude = _WaveHeight * 1.5 + _RippleStrength * 1.5;
                OUT.waveHeight = (maxWaveAmplitude > 0.001) ? (totalWaveHeight / maxWaveAmplitude) : 0.0;

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // === AFFINE VS PERSPECTIVE-CORRECT UV SELECTION ===
                // Reconstruct UVs based on affine mapping toggle
                float2 uv1, uv2;

                if (_EnableAffineTex > 0.5)
                {
                    // Affine mapping: divide the interpolated UV*W by interpolated W
                    // This recreates the PS2's perspective-incorrect texture mapping
                    // The warping is most visible on large polygons viewed at angles
                    float2 affineUV1 = IN.affineUV1.xy / IN.affineUV1.z;
                    float2 affineUV2 = IN.affineUV2.xy / IN.affineUV2.z;

                    // Blend between affine and correct based on intensity
                    uv1 = lerp(IN.standardUV1, affineUV1, _AffineIntensity);
                    uv2 = lerp(IN.standardUV2, affineUV2, _AffineIntensity);
                }
                else
                {
                    // Standard perspective-correct mapping
                    uv1 = IN.standardUV1;
                    uv2 = IN.standardUV2;
                }

                // === DUAL-LAYER TEXTURE SAMPLING ===
                half4 layer1Color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv1);
                half4 layer2Color = SAMPLE_TEXTURE2D(_Layer2Map, sampler_Layer2Map, uv2);

                // Blend layers based on toggle and blend factor
                half4 texColor;
                if (_EnableDualLayer > 0.5)
                {
                    // Blend between two layers for more complex water surface
                    // This simulates multiple reflective/refractive layers
                    texColor = lerp(layer1Color, layer2Color, _LayerBlend);
                }
                else
                {
                    texColor = layer1Color;
                }

                half4 color = texColor * _BaseColor;

                // === ORDERED DITHERING (4x4 BAYER PATTERN) ===
                // PS2 had limited color precision (16-bit framebuffer typically)
                // Ordered dithering was used to simulate more colors than available
                // This creates the characteristic stipple pattern visible in PS2 games
                if (_EnableDithering > 0.5)
                {
                    // Convert screen position to pixel coordinates
                    // We use the actual screen pixel position for stable dithering
                    float2 screenPixelPos = IN.screenPos.xy / IN.screenPos.w * _ScreenParams.xy;

                    // Get dither threshold from Bayer matrix
                    float ditherValue = GetDitherValue(screenPixelPos);

                    // Apply dither as color offset before quantization
                    // This spreads quantization errors in an ordered pattern
                    color.rgb += ditherValue * _DitherIntensity;
                }

                // === HEIGHT-BASED BRIGHTNESS MODULATION ===
                // Simulates realistic water lighting where:
                // - Wave peaks catch more light (brighter)
                // - Wave valleys are shadowed (darker)
                // This aligns visual appearance with actual geometry displacement
                // Very PS2-authentic: simple vertex-based shading was common on PS2
                if (_EnableHeightShading > 0.5)
                {
                    // IN.waveHeight is normalized to approximately -1 to 1 range
                    // Positive values = peaks (brighter), negative = valleys (darker)
                    // We scale by intensity and add to color
                    // Using 0.5 as base multiplier so intensity 1.0 gives +/- 0.5 brightness
                    float heightBrightness = IN.waveHeight * _HeightShadingIntensity * 0.5;
                    color.rgb += heightBrightness;
                    // Clamp to prevent over-brightening or going negative
                    color.rgb = saturate(color.rgb);
                }

                // === PS2 COLOR QUANTIZATION ===
                // Posterize colors to simulate limited color palette
                // Applied AFTER dithering and height shading so both affect the quantization bands
                color.rgb = floor(color.rgb * _ColorSteps) / _ColorSteps;

                // === SIMPLE FAKE SPECULAR (PS2-style vertex-lit appearance) ===
                // Calculate view direction
                float3 viewDir = normalize(_WorldSpaceCameraPos - IN.worldPos);
                // Simple fresnel effect (edge brightness boost)
                float fresnel = pow(1.0 - saturate(dot(float3(0, 1, 0), viewDir)), 2.0);
                color.rgb += fresnel * 0.15;

                // === ALPHA TRANSPARENCY ===
                color.a = _Alpha;

                return color;
            }
            ENDHLSL
        }
    }
}

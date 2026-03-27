Shader "Custom/staticblocks"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        
        // 效果开关和透明度
        _EffectOpacity ("Effect Opacity", Range(0, 1)) = 0.5
        _EffectBlend ("Effect Blend Mode", Range(0, 1)) = 0.5
        
        // 邪恶紫色基础色
        _EvilPurple ("Evil Purple Base", Color) = (0.8, 0.2, 1.0, 1)
        _EvilDark ("Evil Dark", Color) = (0.4, 0.0, 0.6, 1)
        
        // 闪烁控制
        _ShimmerSpeed ("Shimmer Speed", Float) = 2.0
        _ShimmerScale ("Shimmer Scale", Float) = 3.0
        _ShimmerIntensity ("Shimmer Intensity", Range(0, 2)) = 0.8
        
        // 扭曲效果
        _DistortAmount ("Distortion Amount", Range(0, 0.2)) = 0.05
        _DistortSpeed ("Distortion Speed", Float) = 1.5
        
        // 边缘暗角
        _VignettePower ("Vignette Power", Range(0, 5)) = 1.5
        
        // 效果空间选择
        [KeywordEnum(UV, Screen, World)] _EffectSpace ("Effect Space", Float) = 1
    }
    
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ PIXELSNAP_ON
            #pragma multi_compile _EFFECTSPACE_UV _EFFECTSPACE_SCREEN _EFFECTSPACE_WORLD
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPos : TEXCOORD1;
                float4 screenPos : TEXCOORD2;
            };

            fixed4 _Color;
            sampler2D _MainTex;
            
            // 自定义变量
            fixed4 _EvilPurple;
            fixed4 _EvilDark;
            float _ShimmerSpeed;
            float _ShimmerScale;
            float _ShimmerIntensity;
            float _DistortAmount;
            float _DistortSpeed;
            float _VignettePower;
            float _EffectOpacity;
            float _EffectBlend;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                OUT.worldPos = mul(unity_ObjectToWorld, IN.vertex);
                OUT.screenPos = ComputeScreenPos(OUT.vertex);
                
                #ifdef PIXELSNAP_ON
                OUT.vertex = UnityPixelSnap(OUT.vertex);
                #endif

                return OUT;
            }

            // 获取效果空间的UV坐标
            float2 GetEffectUV(v2f IN)
            {
                #if defined(_EFFECTSPACE_UV)
                    return IN.texcoord;
                    
                #elif defined(_EFFECTSPACE_SCREEN)
                    float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
                    screenUV.x *= _ScreenParams.x / _ScreenParams.y;
                    return screenUV * 2.5; // 缩放效果
                    
                #elif defined(_EFFECTSPACE_WORLD)
                    return IN.worldPos.xy * 0.8;
                #endif
            }

            // 生成效果层颜色（纯效果，不混合原图）
            fixed3 GenerateEffectLayer(float2 effectSpace, float2 originalUv, float time)
            {
                // 边缘减弱
                float edgeMask = 1.0;
                float2 borderDist = min(originalUv, 1.0 - originalUv);
                float minBorder = min(borderDist.x, borderDist.y);
                if (minBorder < 0.1)
                {
                    edgeMask = min(minBorder * 10.0, 1.0);
                }
                
                // 扭曲效果
                float2 distortedUv = effectSpace;
                float distortX = sin(effectSpace.y * 10 + time * _DistortSpeed) * _DistortAmount * edgeMask;
                float distortY = cos(effectSpace.x * 8 + time * _DistortSpeed * 1.3) * _DistortAmount * edgeMask;
                distortedUv = effectSpace + float2(distortX, distortY);
                
                // 邪恶图案
                float evilPattern1 = sin(distortedUv.x * _ShimmerScale + time * _ShimmerSpeed) * 
                                     cos(distortedUv.y * (_ShimmerScale * 1.2) - time * 1.2);
                float evilPattern2 = sin((distortedUv.x + distortedUv.y) * 15 + time * 1.8);
                float evilPattern = (evilPattern1 + evilPattern2) * 0.5;
                float evilFactor = (evilPattern * 0.5 + 0.5);
                
                // 基础色
                fixed3 baseColor = lerp(_EvilDark.rgb, _EvilPurple.rgb, evilFactor);
                
                // 闪烁
                float shimmer1 = sin(distortedUv.x * _ShimmerScale + time * _ShimmerSpeed);
                float shimmer2 = cos(distortedUv.y * _ShimmerScale * 1.5 - time * _ShimmerSpeed * 0.8);
                float shimmer = (shimmer1 + shimmer2) * 0.5;
                shimmer = abs(shimmer);
                shimmer = pow(shimmer, 1.5);
                
                fixed3 shimmerColor = fixed3(0.8, 0.3, 1.0);
                fixed3 effectColor = baseColor + shimmer * _ShimmerIntensity * shimmerColor;
                
                // 暗角
                float2 centerVec = originalUv - 0.5;
                float vignette = 1.0 - length(centerVec) * 1.2;
                vignette = saturate(vignette);
                vignette = pow(vignette, _VignettePower);
                effectColor *= vignette;
                
                // 边缘透明度减弱
                effectColor *= edgeMask;
                
                return saturate(effectColor);
            }

            // 混合模式函数
            fixed3 BlendMode(fixed3 base, fixed3 layer, float blendType)
            {
                // 叠加模式 (Overlay)
                fixed3 overlay = layer * (base + (2.0 * base * (1.0 - base)));
                
                // 强光模式 (Hard Light)
                fixed3 hardLight = layer < 0.5 ? 
                    2.0 * layer * base : 
                    1.0 - 2.0 * (1.0 - layer) * (1.0 - base);
                
                // 线性减淡 (更亮的叠加效果)
                fixed3 linearDodge = base + layer;
                
                // 根据blendType混合叠加和强光
                fixed3 blended = lerp(overlay, hardLight, blendType);
                
                // 混合一点线性减淡让效果更亮
                blended = lerp(blended, linearDodge, 0.3);
                
                return blended;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // 原始纹理（保持不变）
                float2 uv = IN.texcoord;
                fixed4 originalColor = tex2D(_MainTex, uv) * IN.color;
                
                if (originalColor.a < 0.01)
                {
                    return fixed4(0, 0, 0, 0);
                }
                
                float time = _Time.y;
                
                // 获取效果空间坐标
                float2 effectSpace = GetEffectUV(IN);
                
                // 生成效果层颜色（纯效果，不含原图）
                fixed3 effectColor = GenerateEffectLayer(effectSpace, uv, time);
                
                // 根据原图亮度调整效果强度（让效果更自然地附着在物体上）
                float originalBrightness = dot(originalColor.rgb, float3(0.299, 0.587, 0.114));
                float effectIntensity = _EffectOpacity * (0.6 + originalBrightness * 0.4);
                
                // 混合模式：效果层叠加到原图层上
                fixed3 finalRgb;
                
                if (_EffectBlend < 0.3)
                {
                    // 添加模式 (Additive) - 纯叠加，最亮
                    finalRgb = originalColor.rgb + effectColor * effectIntensity;
                }
                else if (_EffectBlend < 0.6)
                {
                    // 屏幕模式 (Screen) - 更柔和的叠加
                    finalRgb = 1.0 - (1.0 - originalColor.rgb) * (1.0 - effectColor * effectIntensity);
                }
                else
                {
                    // 混合模式 - 使用更丰富的混合
                    finalRgb = BlendMode(originalColor.rgb, effectColor, (_EffectBlend - 0.6) / 0.4);
                    finalRgb = lerp(finalRgb, originalColor.rgb + effectColor * effectIntensity, 0.5);
                }
                
                // 保留原始颜色的饱和度，只增加一点紫色氛围
                finalRgb = lerp(finalRgb, originalColor.rgb, 0.3);
                
                // 确保颜色不过曝
                finalRgb = min(finalRgb, 1.0);
                
                return fixed4(finalRgb * originalColor.a, originalColor.a);
            }
            ENDCG
        }
    }
    Fallback "Sprites/Default"
}
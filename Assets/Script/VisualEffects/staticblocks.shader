
Shader "Custom/staticblocks"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        
        // 效果开关和透明度
        _EffectOpacity ("Effect Opacity", Range(0, 1)) = 0.7 // 效果叠加透明度
        _EffectBlend ("Effect Blend Mode", Range(0, 1)) = 0.5 // 0: 叠加, 1: 强光
        
        // 邪恶紫色基础色
        _EvilPurple ("Evil Purple Base", Color) = (0.6, 0.1, 0.8, 1)
        _EvilDark ("Evil Dark", Color) = (0.2, 0.0, 0.3, 1)
        
        // 闪烁控制
        _ShimmerSpeed ("Shimmer Speed", Float) = 2.0
        _ShimmerScale ("Shimmer Scale", Float) = 3.0
        _ShimmerIntensity ("Shimmer Intensity", Range(0, 2)) = 0.8
        
        // 扭曲效果
        _DistortAmount ("Distortion Amount", Range(0, 0.2)) = 0.05
        _DistortSpeed ("Distortion Speed", Float) = 1.5
        
        // 边缘暗角 (只影响效果层)
        _VignettePower ("Vignette Power", Range(0, 5)) = 2.0
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
            };

            fixed4 _Color;
            sampler2D _MainTex;
            float4 _MainTex_ST;
            
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
                
                #ifdef PIXELSNAP_ON
                OUT.vertex = UnityPixelSnap (OUT.vertex);
                #endif

                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // 基础纹理采样 - 使用原始UV，不扭曲，保证原图完整
                float2 uv = IN.texcoord;
                fixed4 originalTex = tex2D(_MainTex, uv) * IN.color;
                
                // 如果原图alpha为0，直接返回透明
                if (originalTex.a < 0.01)
                {
                    return fixed4(0, 0, 0, 0);
                }
                
                // 时间变量
                float t = _Time.y;
                
                // 扭曲UV - 只用于效果层，不用于采样原图
                float2 effectUv = uv;
                
                // 只在有效区域内应用扭曲（避免边缘拉伸造成空缺）
                // 边缘区域减少扭曲，保持效果覆盖完整
                float edgeMask = 1.0;
                float2 borderDist = min(uv, 1.0 - uv);
                float minBorder = min(borderDist.x, borderDist.y);
                if (minBorder < 0.1)
                {
                    // 靠近边缘时减弱扭曲
                    edgeMask = minBorder * 10.0;
                }
                
                float distortX = sin(uv.y * 10 + t * _DistortSpeed) * _DistortAmount * edgeMask;
                float distortY = cos(uv.x * 8 + t * _DistortSpeed * 1.3) * _DistortAmount * edgeMask;
                effectUv = uv + float2(distortX, distortY);
                
                // 确保效果UV在有效范围内（clamp防止采样到空白）
                effectUv = clamp(effectUv, 0.001, 0.999);
                
                // 创建邪恶的紫色调效果
                float evilPattern1 = sin(effectUv.x * 10 + t) * cos(effectUv.y * 12 - t * 1.2);
                float evilPattern2 = sin((effectUv.x + effectUv.y) * 15 + t * 1.8);
                float evilPattern = (evilPattern1 + evilPattern2) * 0.5;
                float evilFactor = (evilPattern * 0.5 + 0.5);
                
                // 基础邪恶色
                fixed3 evilBase = lerp(_EvilDark.rgb, _EvilPurple.rgb, evilFactor);
                
                // 闪烁效果
                float shimmer1 = sin(effectUv.x * _ShimmerScale + t * _ShimmerSpeed);
                float shimmer2 = cos(effectUv.y * _ShimmerScale * 1.5 - t * _ShimmerSpeed * 0.8);
                float shimmer = (shimmer1 + shimmer2) * 0.5;
                shimmer = abs(shimmer);
                shimmer = pow(shimmer, 1.5);
                
                fixed3 shimmerColor = fixed3(0.7, 0.3, 1.0);
                fixed3 effectRgb = evilBase + shimmer * _ShimmerIntensity * shimmerColor;
                
                // 暗角效果 (只影响效果层)
                float2 centerVec = uv - 0.5;
                float vignette = 1.0 - length(centerVec) * 1.2;
                vignette = saturate(vignette);
                vignette = pow(vignette, _VignettePower);
                effectRgb *= vignette;
                
                // 确保效果颜色饱和度
                effectRgb = saturate(effectRgb);
                
                // 根据原图亮度调整效果强度，让效果在原图区域更自然
                float originalGray = dot(originalTex.rgb, float3(0.299, 0.587, 0.114));
                float effectMask = originalGray * 0.8 + 0.2; // 让暗部也有一定效果
                
                // 混合模式：根据_EffectBlend在叠加和强光之间混合
                // 叠加模式 (Overlay)
                float3 overlay = effectRgb * (originalTex.rgb + (2.0 * originalTex.rgb * (1.0 - originalTex.rgb)));
                
                // 强光模式 (Hard Light)
                float3 hardLight = effectRgb < 0.5 ? 
                    2.0 * effectRgb * originalTex.rgb : 
                    1.0 - 2.0 * (1.0 - effectRgb) * (1.0 - originalTex.rgb);
                
                // 根据_EffectBlend混合两种模式
                float3 blendedEffect = lerp(overlay, hardLight, _EffectBlend);
                
                // 最终颜色 = 原图 + 效果 * 透明度 * 遮罩
                float3 finalRgb = originalTex.rgb + blendedEffect * _EffectOpacity * effectMask;
                
                // 保持原图的饱和度，但稍微增强紫色调
                finalRgb = lerp(finalRgb, (finalRgb * 0.7 + effectRgb * 0.3), 0.3);
                
                // 确保颜色范围正确
                finalRgb = saturate(finalRgb);
                
                // 保持原图的alpha
                float finalAlpha = originalTex.a;
                
                return fixed4(finalRgb * finalAlpha, finalAlpha);
            }
            ENDCG
        }
    }
    Fallback "Sprites/Default"
}
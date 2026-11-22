// 画面全体を白黒にし、マスク（_CR_MaskTex）が白い所だけ元の色に戻す合成シェーダー。
// ColorRestoreFeature から全画面で使われる。直接マテリアルに設定する必要はない。
Shader "Hidden/LastTower/ColorRestoreComposite"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off Blend Off

        Pass
        {
            Name "ColorRestoreComposite"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D(_CR_MaskTex);

            float  _CR_GrayAmount;      // 白黒の強さ (0=元の色, 1=完全な白黒)
            float  _CR_GlobalRestore;   // 画面全体の色の戻り具合 (0..1)
            float4 _CR_GrayTint;        // 白黒部分の色味（セピアなど）
            float  _CR_Contrast;        // 白黒部分のコントラスト
            float  _CR_Brightness;      // 白黒部分の明るさ
            float4 _CR_EdgeColor;       // 色が戻る境目の光の色
            float  _CR_EdgeIntensity;   // 境目の光の強さ
            float  _CR_Invert;          // 1 なら逆（カラーの世界がマスク部分だけ白黒になる）

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                half4 src = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                half2 maskRG = SAMPLE_TEXTURE2D(_CR_MaskTex, sampler_LinearClamp, uv).rg;
                half mask = maskRG.r;   // 色の戻り具合
                half front = maskRG.g;  // 広がっている先端

                half restore = saturate(max(mask, _CR_GlobalRestore));
                restore = lerp(restore, 1.0h - restore, _CR_Invert);

                half luma = dot(src.rgb, half3(0.299h, 0.587h, 0.114h));
                luma = saturate((luma - 0.5h) * _CR_Contrast + 0.5h) * _CR_Brightness;
                half3 gray = luma * _CR_GrayTint.rgb;
                half3 mono = lerp(src.rgb, gray, _CR_GrayAmount);

                half3 col = lerp(mono, src.rgb, restore);

                // 色が広がっている先端だけ少し光らせる
                col += _CR_EdgeColor.rgb * front * _CR_EdgeIntensity * (1.0h - _CR_GlobalRestore);

                return half4(col, src.a);
            }
            ENDHLSL
        }
    }
}

Shader "CustomRP/Lit"
{
    Properties
    {
        _BaseMap("Texture", 2D) = "white" {}
        _BaseColor("Color", Color) = (1.0, 1.0, 1.0, 1.0)
        //透明度测试的阈值
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        [Toggle(_CLIPPING)] _Clipping("Alpha Clipping", Float) = 0
        [Toggle(_PREMULTIPLY_ALPHA)] _PremultiplyAlpha("Premultiply Alpha", Float) = 0
        
        //金属度和光滑度
        _Metallic("Metallic", Range(0, 1)) = 0
        _Smoothness("Smoothness", Range(0, 1)) = 0.5
        //设置混合模式
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 0
        //默认写入深度缓冲区
        [Enum(Off, 0, On, 1)] _ZWrite("Z Write", Float) = 1
      
    }


    SubShader
    {

        Pass
        {
            //光照模式定义
            Tags
            {
                "LightMode" = "CustomLit"
            }
            //定义混合模式
            Blend[_SrcBlend][_DstBlend]
            //是否写入深度
            ZWrite[_ZWrite]
        HLSLPROGRAM
            #pragma target 3.5
           //使用shader feature 声明一个toggle开关对应的_CLIpping关键字
            #pragma shader_feature _CLIPPING
            #pragma shader_feature _PREMULTIPLY_ALPHA
            //_代表默认 _DIRECTIONAL_PCF2
            #pragma multi_compile _ _DIRECTIONAL_PCF3 _DIRECTIONAL_PCF5 _DIRECTIONAL_PCF7
            #pragma multi_compile_instancing


            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment
            #include "LitPass.hlsl"
        ENDHLSL
        }

        Pass
        {
            Tags
            {
                "LightMode" = "ShadowCaster"
            }
            ColorMask 0

        HLSLPROGRAM
            #pragma target 3.5
           //使用shader feature 声明一个toggle开关对应的_CLIpping关键字
            #pragma shader_feature _CLIPPING
            #pragma multi_compile_instancing
            #pragma vertex ShadowCasterPassVertex
            #pragma fragment ShadowCasterPassFragment
            #include "ShadowCasterPass.hlsl"
        ENDHLSL

        }
    }
    CustomEditor "CustomShaderGUI"
}

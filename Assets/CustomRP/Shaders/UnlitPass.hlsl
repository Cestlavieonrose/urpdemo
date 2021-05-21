﻿
#ifndef CUSTOM_UNLIT_PASS_INCLUDED
#define CUSTOM_UNLIT_PASS_INCLUDED

//允许多次include同个文件
#include "../ShaderLibrary/Common.hlsl"
//并不是所有版本的opengl（比方说opengl es 2.0）都支持cbuffer 所有需要在宏里定义
// CBUFFER_START(UnityPerMaterial)
//         float4 _BaseColor;
// CBUFFER_END
//
//定义一张2D纹理，并使用SAMPLER(sampler+纹理名) 这个宏为该纹理指定一个采样器
TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);
UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
//提供纹理的缩放和平移
UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)
UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)


//用作顶点函数的输入参数
struct Attributes
{
    float3 positionOS:POSITION;
    float2 baseUV:TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

//用作片段函数的输入参数
struct Varyings
{
    float4 positionCS:SV_POSITION;
    float2 baseUV:VAR_BASE_UV;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

//顶点函数
Varyings UnlitPassVertex(Attributes input)
{
    Varyings outputs;
    //用来提取顶点输入结构体中的渲染对象的索引，并将其存储到Attributes的UNITY_VERTEX_INPUT_INSTANCE_ID中
    UNITY_SETUP_INSTANCE_ID(input);
    //将对象位置和索引输出 若Varyings定义了UNITY_VERTEX_INPUT_INSTANCE_ID，则进行复制
    UNITY_TRANSFER_INSTANCE_ID(input, outputs);
    float3 positionWS = TransformObjectToWorld(input.positionOS);
    float4 positionVP = TransformWorldToHClip(positionWS);
    outputs.positionCS = positionVP;

    //计算缩放和偏移后的UV坐标
    float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseMap_ST);
    outputs.baseUV = input.baseUV*baseST.xy + baseST.zw;
    return outputs;
}

//片元函数
float4 UnlitPassFragment(Varyings input):SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    float4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.baseUV);

    //访问获取材质的颜色属性
    float4 c = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor)*baseMap;
#if defined(_CLIPPING)
    //透明度低于阈值的偏远进行舍弃
    clip(c.a - UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Cutoff));
#endif
    return c;
}

#endif


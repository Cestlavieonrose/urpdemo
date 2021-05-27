#ifndef CUSTOM_ShadowCasterPass_PASS_INCLUDED
#define CUSTOM_ShadowCasterPass_PASS_INCLUDED
//允许多次include同个文件
#include "../ShaderLibrary/Common.hlsl"

//定义一张2D纹理，并使用SAMPLER(sampler+纹理名) 这个宏为该纹理指定一个采样器
TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

//支持instancing的cbuff
UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
//提供纹理的缩放和平移
UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)
UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)


//用作顶点函数的输入参数
struct Attributes
{
    float3 positionOS : POSITION;
	float2 baseUV : TEXCOORD0;
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
Varyings ShadowCasterPassVertex(Attributes input)
{
    Varyings outputs;
    //用来提取顶点输入结构体中的渲染对象的索引，并将其存储到Attributes的UNITY_VERTEX_INPUT_INSTANCE_ID中
    UNITY_SETUP_INSTANCE_ID(input);
    //将对象位置和索引输出 若Varyings定义了UNITY_VERTEX_INPUT_INSTANCE_ID，则进行复制
    UNITY_TRANSFER_INSTANCE_ID(input, outputs);
    float3 positionWS = TransformObjectToWorld(input.positionOS);
    float4 positionVP = TransformWorldToHClip(positionWS);
    outputs.positionCS = positionVP;

//解决阴影深度因为近截面裁剪掉而出现却角或者镂空的问题
    #if UNITY_REVERSED_Z
        outputs.positionCS.z = min( outputs.positionCS.z,  outputs.positionCS.w*UNITY_NEAR_CLIP_VALUE);
    #else
        outputs.positionCS.z = max( outputs.positionCS.z,  outputs.positionCS.w*UNITY_NEAR_CLIP_VALUE);
    #endif

    //计算缩放和偏移后的UV坐标
    float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseMap_ST);
    outputs.baseUV = input.baseUV*baseST.xy + baseST.zw;
    return outputs;
}

//片元函数
void ShadowCasterPassFragment(Varyings input)
{
    UNITY_SETUP_INSTANCE_ID(input);
    float4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.baseUV);

    //访问获取材质的颜色属性
    float4 c = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor)*baseMap;

#if defined(_SHADOWS_CLIP)
	//透明度低于阈值的片元进行舍弃
	clip(c.a - UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Cutoff));
#elif defined(_SHADOWS_DITHER)
	//计算抖动值
	float dither = InterleavedGradientNoise(input.positionCS.xy, 0);
	clip(c.a - dither);
#endif

}

#endif


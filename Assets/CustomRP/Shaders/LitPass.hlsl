#ifndef CUSTOM_LIT_PASS_INCLUDED
#define CUSTOM_LIT_PASS_INCLUDED
//允许多次include同个文件
#include "../ShaderLibrary/Common.hlsl"
#include "../ShaderLibrary/Surface.hlsl"
#include "../ShaderLibrary/Light.hlsl"
#include "../ShaderLibrary/Lighting.hlsl"

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
	//表面法线
	float3 normalOS : NORMAL;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

//用作片段函数的输入参数
struct Varyings
{
    float4 positionCS:SV_POSITION;
    float2 baseUV:VAR_BASE_UV;
    //世界法线
    float3 normalWS:VAR_NORMAL;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

//顶点函数
Varyings LitPassVertex(Attributes input)
{
    Varyings outputs;
    //用来提取顶点输入结构体中的渲染对象的索引，并将其存储到Attributes的UNITY_VERTEX_INPUT_INSTANCE_ID中
    UNITY_SETUP_INSTANCE_ID(input);
    //将对象位置和索引输出 若Varyings定义了UNITY_VERTEX_INPUT_INSTANCE_ID，则进行复制
    UNITY_TRANSFER_INSTANCE_ID(input, outputs);
    float3 positionWS = TransformObjectToWorld(input.positionOS);
    //计算世界空间的法线
    outputs.normalWS = TransformObjectToWorldNormal(input.normalOS);

    float4 positionVP = TransformWorldToHClip(positionWS);
    outputs.positionCS = positionVP;

    //计算缩放和偏移后的UV坐标
    float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseMap_ST);
    outputs.baseUV = input.baseUV*baseST.xy + baseST.zw;
    return outputs;
}

//片元函数
float4 LitPassFragment(Varyings input):SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    float4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.baseUV);

    //访问获取材质的颜色属性
    float4 c = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor)*baseMap;
#if defined(_CLIPPING)
    //透明度低于阈值的偏远进行舍弃
    clip(c.a - UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Cutoff));
#endif

    Surface surface;
    surface.normal = normalize(input.normalWS);
    surface.color = c.rgb;
    surface.alpha = c.a;
    //通过表面属性计算最终光照结果
    float3 color = GetLighting(surface);

    return float4(color, surface.alpha);
}

#endif


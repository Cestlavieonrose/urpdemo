
#ifndef CUSTOM_UNLIT_PASS_INCLUDED
#define CUSTOM_UNLIT_PASS_INCLUDED

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
	outputs.baseUV = TransformBaseUV(input.baseUV);
    return outputs;
}

//片元函数
float4 UnlitPassFragment(Varyings input):SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);

    float4 c = GetBase(input.baseUV);
#if defined(_CLIPPING)
    //透明度低于阈值的片元进行舍弃
	clip(c.a - GetCutoff(input.baseUV));
#endif
    return c;
}

#endif


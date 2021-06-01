//unity 标准输入库
#ifndef CUSTOM_SURFACE_INCLUDED
#define CUSTOM_SURFACE_INCLUDED

struct Surface
{
    //表面位置
    float3 position;
    float3 normal;
    float3 color;
    float alpha;
    float metallic;
    float smoothness;
    float3 viewDirection;
    //表面深度
    float depth;
    //阴影抖动属性
    float dither;
    //菲涅尔反射强度
    float fresnelStrength;
};

#endif
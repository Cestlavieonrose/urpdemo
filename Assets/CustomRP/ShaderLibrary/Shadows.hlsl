//阴影数据相关库
#ifndef CUSTOM_SHADOWS_INCLUDED
#define CUSTOM_SHADOWS_INCLUDED

#define MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT 4
//阴影图集
TEXTURE2D_SHADOW(_DirectionalShadowAtlas);

#define SHADOW_SAMPLER sampler_linear_clamp_compare
SAMPLER_CMP(SHADOW_SAMPLER);

CBUFFER_START(_CustomShadows)
//阴影转换矩阵
float4x4 _DirectionalShadowMatrices[MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT];
CBUFFER_END

//阴影数据信息
struct DirectionalShadowData
{
    float strength;
    int tileIndex;
};

//采样阴影图集
float SampleDirectionalShadowAtalas(float3 positionSTD)
{
    return SAMPLE_TEXTURE2D_SHADOW(_DirectionalShadowAtlas, SHADOW_SAMPLER, positionSTD);
}

//计算阴影衰减
float GetDirectionalShadowAttenuation(DirectionalShadowData directional, Surface surfaceWS) {
    if (directional.strength<=0.0)
    {
        return 1.0;
    }
	//通过阴影转换矩阵和表面位置得到在阴影纹理(图块)空间的位置，然后对图集进行采样 
	float3 positionSTS = mul(_DirectionalShadowMatrices[directional.tileIndex], float4(surfaceWS.position, 1.0)).xyz;
	float shadow = SampleDirectionalShadowAtalas(positionSTS);
	return lerp(1.0, shadow, directional.strength);
}
#endif
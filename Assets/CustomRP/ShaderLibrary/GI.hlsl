//GI 全局照明相关库
#ifndef CUSTOM_GI_INCLUDED
#define CUSTOM_GI_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"
TEXTURE2D(unity_Lightmap);
SAMPLER(samplerunity_Lightmap);

//采样光照贴图
float3 SamplerLightMap(float2 lightmapUV)
{
	#if defined(LIGHTMAP_ON)
		return SampleSingleLightmap(
			TEXTURE2D_ARGS(unity_Lightmap, samplerunity_Lightmap),
			lightmapUV,
			float4(1.0,1.0,0.0,0.0),
#if defined(UNITY_LIGHTMAP_FULL_HDR)
			false,
#else
			true,
#endif
			float4(LIGHTMAP_HDR_MULTIPLIER,LIGHTMAP_HDR_EXPONENT, 0.0, 0.0)
		);
	#else
		return 0.0;
	#endif
}

//BRDF属性
struct GI {
    //漫反射
	float3 diffuse;
};

GI GetGI(float2 lightmapUV)
{
	GI gi;
	gi.diffuse = SamplerLightMap(lightmapUV);
	return gi;
}

//当需要渲染光照贴图的对象时
#if defined(LIGHTMAP_ON)
#define GI_ATTRIBUTE_DATA float2 lightmapUV:TEXCOORD1;
#define GI_VARYINGSDATA float2 lightmapUV:VAR_LIGHT_MAP_UV;
#define TRANSFER_GI_DATA(input, output) output.lightmapUV = input.lightmapUV*unity_LightmapST.xy + unity_LightmapST.zw;
#define GI_FRAGMENT_DATA(input) input.lightmapUV
#else
//否则这些宏都应该为空
#define GI_ATTRIBUTE_DATA 
#define GI_VARYINGSDATA 
#define TRANSFER_GI_DATA(input, output) 
#define GI_FRAGMENT_DATA(input) 0.0

#endif




#endif
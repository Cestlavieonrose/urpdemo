//灯光数据相关库
#ifndef CUSTOM_LIGHT_INCLUDED
#define CUSTOM_LIGHT_INCLUDED

#define MAX_DIRECTIONAL_LIGHT_COUNT 4
#define MAX_OTHER_LIGHT_COUNT 64

CBUFFER_START(_CustomLight)
	int _DirectionalLightCount;
	//定向光源颜色、方向、阴影等数据
    float4 _DirectionalLightColors[MAX_DIRECTIONAL_LIGHT_COUNT];
    float4 _DirectionalLightDirections[MAX_DIRECTIONAL_LIGHT_COUNT];
	//阴影数据
	float4 _DirectionalLightShadowData[MAX_DIRECTIONAL_LIGHT_COUNT];

	//非定向光源属性
	int _OtherLightCount;
	float4 _OtherLightColors[MAX_OTHER_LIGHT_COUNT];
	float4 _OtherLightPosition[MAX_OTHER_LIGHT_COUNT];
	float4 _OtherLightDirections[MAX_OTHER_LIGHT_COUNT];
	float4 _OtherLightSpotAngles[MAX_OTHER_LIGHT_COUNT];
	float4 _OtherLightShadowData[MAX_OTHER_LIGHT_COUNT];
CBUFFER_END

int GetOtherLightCount()
{
	return _OtherLightCount;
}

//灯光的属性
struct Light {
	//颜色
	float3 color;
	//方向
	float3 direction;
	//衰减
	float attenuation;
};

//获取方向光源的数量
int GetDirectionalLightCount() {
	return _DirectionalLightCount;
}

//获取方向光的阴影数据
DirectionalShadowData GetDirectionalShadowData(int lightIndex, ShadowData shadowData) {
	DirectionalShadowData data;
	data.strength = _DirectionalLightShadowData[lightIndex].x * shadowData.strength;
	data.tileIndex = _DirectionalLightShadowData[lightIndex].y + shadowData.cascadeIndex;
	//获取灯光的法线偏差值
	data.normalBias = _DirectionalLightShadowData[lightIndex].z;
	data.shadowMaskChannel = _DirectionalLightShadowData[lightIndex].w;
	return data;
}

//获取目标索引定向光的属性
Light GetDirectionalLight (int index,Surface surfaceWS, ShadowData shadowData) {
	Light light;
	light.color = _DirectionalLightColors[index].rgb;
	light.direction = _DirectionalLightDirections[index].xyz;
	//得到阴影数据
	DirectionalShadowData dirShadowData = GetDirectionalShadowData(index, shadowData);
	//得到阴影衰减
	light.attenuation = GetDirectionalShadowAttenuation(dirShadowData, shadowData, surfaceWS);
	//light.attenuation = shadowData.cascadeIndex%4; //查看球形包围盒的范围
	return light;
}


OtherShadowData GetOtherShadowData(int lightIndex) {
	OtherShadowData data;
	data.strength = _OtherLightShadowData[lightIndex].x;
	data.shadowMaskChannel = _OtherLightShadowData[lightIndex].w;
	return data;
}

//获取指定索引的非定向光源数据
Light GetOtherLight(int index, Surface surfaceWS, ShadowData shadowData)
{
	Light light;
	light.color = _OtherLightColors[index];
	float3 ray = _OtherLightPosition[index].xyz - surfaceWS.position;
	light.direction = normalize(ray);
	//光照强度随距离衰减
	float distanceSqr = max(dot(ray, ray), 0.00001);
	float4 spotAngles = _OtherLightSpotAngles[index];
	//到一定距离后就衰减为0
	float rangeAtten = Square(saturate(1.0-Square(distanceSqr*_OtherLightPosition[index].w)));
	//得到聚光灯衰
	float SpotAtten = Square(saturate(dot(_OtherLightDirections[index].xyz, light.direction)*spotAngles.x + spotAngles.y));
	OtherShadowData otherShadowData = GetOtherShadowData(index);
	light.attenuation = GetOtherShadowAttenuation(otherShadowData, shadowData, surfaceWS)*SpotAtten*rangeAtten/distanceSqr;
	return light;
}


#endif
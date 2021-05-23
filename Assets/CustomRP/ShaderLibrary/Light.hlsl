//灯光数据相关库
#ifndef CUSTOM_LIGHT_INCLUDED
#define CUSTOM_LIGHT_INCLUDED

#define MAX_DIRECTIONAL_LIGHT_COUNT 4
//方向光的数据 获取场景中默认的那盏方向光的灯光数据
CBUFFER_START(_CustomLight)
    int _DirectionalLightCount;
    //定向光源颜色、方向、阴影等数据
    float4 _DirectionalLightColors[MAX_DIRECTIONAL_LIGHT_COUNT];
    float4 _DirectionalLightDirections[MAX_DIRECTIONAL_LIGHT_COUNT];
    float2 _DirectionalLightShadowData[MAX_DIRECTIONAL_LIGHT_COUNT];
CBUFFER_END

//灯光的属性
struct Light {
	//颜色
	float3 color;
	//方向
	float3 direction;

    float attenuation;
};


//获取方向光数量
int GetDirectionalLightCount()
{
    return _DirectionalLightCount;
}

//获取方向光的阴影数据
DirectionalShadowData GetDirectionalShadowData(int lightIndex) {
	DirectionalShadowData data;
	data.strength = _DirectionalLightShadowData[lightIndex].x;
	data.tileIndex = _DirectionalLightShadowData[lightIndex].y;
	return data;
}

//获取指定索引的方向光数据
Light GetDirectionalLight(int index, Surface surface)
{
    Light light;
    light.color = _DirectionalLightColors[index];
    light.direction = _DirectionalLightDirections[index];
    //得到阴影数据
    DirectionalShadowData shadowData = GetDirectionalShadowData(index);
    //得到阴影衰减
    light.attenuation = GetDirectionalShadowAttenuation(shadowData, surface);
    return light;
}



#endif
//计算光照相关库
#ifndef CUSTOM_LIGHTING_INCLUDED
#define CUSTOM_LIGHTING_INCLUDED


//计算入射光照
float3 IncomingLight(Surface surface, Light light)
{
    return saturate(dot(surface.normal, light.direction))*light.color;
}

//入射光照乘以BRDF的漫反射部分，得到最终的照明
float3 GetLighting(Surface surface, BRDF brdf, Light light)
{
    return IncomingLight(surface, light)*brdf.diffuse;
}

//根据物体表面信息获取最终光照结果
float3 GetLighting(Surface surface, BRDF brdf)
{
    //可见方向光的照明结果进行累加得到最终照明结果
    float3 coulor = 0.0;
    for (int i=0; i< GetDirectionalLightCount(); i++)
    {
        coulor += GetLighting(surface, brdf, GetDirectionalLight(i));
    }
    return coulor;
}
#endif
//计算光照相关库
#ifndef CUSTOM_LIGHTING_INCLUDED
#define CUSTOM_LIGHTING_INCLUDED


//计算入射光照
float3 IncomingLight(Surface surface, Light light)
{
    return saturate(dot(surface.normal, light.direction))*light.color;
}

//入射光照乘以表面颜色，得到最终的照明颜色
float3 GetLighting(Surface surface, Light light)
{
    return IncomingLight(surface, light)*surface.color;
}

//根据物体表面信息获取最终光照结果
float3 GetLighting(Surface surface)
{
    return GetLighting(surface, GetDirectionalLight());
}
#endif
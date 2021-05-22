//计算光照相关库
#ifndef CUSTOM_LIGHTING_INCLUDED
#define CUSTOM_LIGHTING_INCLUDED


float SpecularStrength(Surface surface, BRDF brdf, Light light)
{
    float3 h = SafeNormalize(light.direction + surface.viewDirection);
    float nh2 = Square(saturate(dot(surface.normal, h)));
    float lh2 = Square(saturate(dot(light.direction, h)));
    float r2 = Square(brdf.roughness);
    float d2 = Square(nh2*(r2-1.0) + 1.00001);
    float normalization = brdf.roughness*4.0 + 2.0;
    return r2/(d2*max(0.1, lh2)*normalization);

}

float3 DirectBRDF(Surface surface, BRDF brdf, Light light)
{
    return SpecularStrength(surface, brdf, light)*brdf.specular + brdf.diffuse;
}

//计算入射光照
float3 IncomingLight(Surface surface, Light light)
{
    return saturate(dot(surface.normal, light.direction))*light.color;
}

//入射光照乘以BRDF的漫反射部分，得到最终的照明
float3 GetLighting(Surface surface, BRDF brdf, Light light)
{
    return IncomingLight(surface, light)*DirectBRDF(surface, brdf, light);
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
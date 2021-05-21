//BRDF相关库
#ifndef CUSTOM_BRDF_INCLUDED
#define CUSTOM_BRDF_INCLUDED

struct BRDF
{
    float3 diffuse;
    float3 specular;
    float roughness;
};

//电解质的反射率平均约为0.04
#define MIN_REFLECTIVITY 0.04
float OneMinusReflectivity(float metallic)
{
    float range = 1.0 - MIN_REFLECTIVITY;
    return range - metallic*range;
}


//获取给定表面的BRDF数据
BRDF GetBRDF(Surface surface)
{
    BRDF brdf;
    float onMinusReflectivity = OneMinusReflectivity(surface.metallic);
    brdf.diffuse = surface.color * onMinusReflectivity;

    brdf.specular = lerp(MIN_REFLECTIVITY, surface.color, surface.metallic);
    brdf.roughness = 1.0;
    return brdf;
}

#endif
//unity 标准输入库
#ifndef CUSTOM_UNITY_INPUT_INCLUDED
#define CUSTOM_UNITY_INPUT_INCLUDED

CBUFFER_START(UnityPerDraw)

//定义一个从模型空间转换到世界空间的转换矩阵
float4x4 unity_ObjectToWorld;
float4x4 unity_WorldToObject;
float4 unity_LODFade;
//这个矩阵包含一些在这里我们不需要转换信息
real4 unity_WorldTransformParams;
float4 unity_LightmapST;
float4 unity_DynamicLightmapST;
CBUFFER_END

//定义一个从世界空间转换到裁剪空间的矩阵
float4x4 unity_MatrixV;
float4x4 unity_MatrixVP;
float4x4 glstate_matrix_projection;
float3 _WorldSpaceCameraPos;

#endif
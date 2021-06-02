﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Shadows
{
    const string bufferName = "Shadows";
    CommandBuffer buffer = new CommandBuffer
    {
        name = bufferName
    };

    ScriptableRenderContext context;
    CullingResults cullingResults;
    ShadowSettings shadowSetting;

    //可投射阴影的定向光源数量
    const int maxShadowedDirectionalLightCount = 4;

    //最大级联数量
    const int maxCascades = 4;
    
    //定向光的阴影数据
    struct ShadowedDirectionalLight
    {
        public int visibleLightIndex;
        //斜度比例偏差值
        public float slopeScaleBias;
        //阴影视锥体进裁剪平面偏移
        public float nearPlaneOffset;
    }
    //存储可投射阴影的定向光源的数据
    ShadowedDirectionalLight[] ShadowedDirectionalLights = new ShadowedDirectionalLight[maxShadowedDirectionalLightCount];
    //存储可投射阴影的平行光数量
    int ShadowedDirectionalLightCount;
    static int dirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas");
    static int dirShadowMatricesId = Shader.PropertyToID("_DirectionalShadowMatrices");

    static int cascadeCountId = Shader.PropertyToID("_CascadeCount");
    static int cascadeCullingSpheresId = Shader.PropertyToID("_CascadeCullingSpheres");

    static int shadowDistanceId = Shader.PropertyToID("_ShadowDistance");

    static int shadowDistanceFadeId = Shader.PropertyToID("_ShadowDistanceFade");

    static int shadowAtlasSizeId = Shader.PropertyToID("_ShadowAtlasSize");


    static Vector4[] cascadeCullingSpheres = new Vector4[maxCascades];

    //存储光源的阴影转换矩阵
    static Matrix4x4[] dirShadowMatrices = new Matrix4x4[maxShadowedDirectionalLightCount* maxCascades];
    public void Setup(
        ScriptableRenderContext context, 
        CullingResults cullingResults, 
        ShadowSettings shadowSetting)
    {
        this.context = context;
        this.cullingResults = cullingResults;
        this.shadowSetting = shadowSetting;
        ShadowedDirectionalLightCount = 0;
    }

    void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    //存储可见光的阴影数据
    public Vector3 ReserveDirectionalShadows(Light light, int visibleLightIndex)
    {
       if (ShadowedDirectionalLightCount < maxShadowedDirectionalLightCount && light.shadows != LightShadows.None && light.shadowStrength > 0f)
		{
            float maskChannel = -1;
            //如果使用了ShadowMask
            LightBakingOutput lightBaking = light.bakingOutput;
			if (lightBaking.lightmapBakeType == LightmapBakeType.Mixed && lightBaking.mixedLightingMode == MixedLightingMode.Shadowmask)
			{
				useShadowMask = true;
                //得到光源的阴影蒙版通道索引
                maskChannel = lightBaking.occlusionMaskChannel;
            }
            if (!cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b ))
            {
                return new Vector4(-light.shadowStrength, 0f, 0f, maskChannel);
            }
            ShadowedDirectionalLights[ShadowedDirectionalLightCount] = new ShadowedDirectionalLight{ visibleLightIndex = visibleLightIndex,slopeScaleBias = light.shadowBias, 
				nearPlaneOffset = light.shadowNearPlane };
            //返回阴影强度、阴影图块的偏移索引、法线偏差、阴影蒙版通道索引
            return new Vector4(light.shadowStrength, shadowSetting.directional.cascadeCount * ShadowedDirectionalLightCount++, light.shadowNormalBias, maskChannel);
        }
		return new Vector4(0f, 0f, 0f, -1f);
    }
    bool useShadowMask = false;
    
    //存储其他类型光源的阴影
    public Vector4 ReserveOtherShadows(Light light, int visibleLightIndex)
    {
        if (light.shadows != LightShadows.None && light.shadowStrength > 0f)
        {
            LightBakingOutput lightBaking = light.bakingOutput;
            if (lightBaking.lightmapBakeType == LightmapBakeType.Mixed && lightBaking.mixedLightingMode == MixedLightingMode.Shadowmask)
            {
                useShadowMask = true;
                return new Vector4(light.shadowStrength, 0f, 0f, lightBaking.occlusionMaskChannel);
            }
        }
        return new Vector4(0f, 0f, 0f, -1f);
    }

    //渲染阴影
    public void Render()
    {
        if (ShadowedDirectionalLightCount > 0)
        {
            RenderDirectionalShadows();
        }
    }
    //集联数据
    static int cascadeDataId = Shader.PropertyToID("_CascadeData");
    static Vector4[] cascadeData = new Vector4[maxCascades];

    //级联混合模式
    static string[] cascadeBlendKeywords = {
        "_CASCADE_BLEND_SOFT",
        "_CASCADE_BLEND_DITHER"
    };

    void SetCascadeData(int index, Vector4 cullingSphere, float tileSize)
    {
        //包围球半径除以阴影图块大小=近似纹素大小
        float texelSize = 2f * cullingSphere.w / tileSize;
        
        float filterSize = texelSize * ((float)shadowSetting.directional.filter + 1f);
        //防止PCF算法后出现的阴影瑕疵
        cullingSphere.w -= filterSize;
        //得到半径的平方值
        cullingSphere.w *= cullingSphere.w;
        cascadeCullingSpheres[index] = cullingSphere;
        cascadeData[index] = new Vector4(1f / cullingSphere.w, filterSize * 1.4142136f);
    }

    //渲染定向光阴影
    void RenderDirectionalShadows()
    {
        //创建RT， 并指定为阴影贴图
        int atlasSize = (int)shadowSetting.directional.atlasSize;
        buffer.GetTemporaryRT(dirShadowAtlasId, atlasSize, atlasSize, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        //指定渲染数据存储到RT中
        buffer.SetRenderTarget(dirShadowAtlasId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        //清除深度缓冲
        buffer.ClearRenderTarget(true, false, Color.clear);
        buffer.BeginSample(bufferName);
        ExecuteBuffer();

        int tiles = ShadowedDirectionalLightCount * shadowSetting.directional.cascadeCount;

        int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
        int tileSize = atlasSize/split;
        //遍历所有方向光渲染阴影
        for (int i = 0; i < ShadowedDirectionalLightCount; i++)
        {
            RenderDirectionalShadows(i, split, tileSize);
        }
        //将级联数量和包围球数据发送到GPU
        buffer.SetGlobalInt(cascadeCountId, shadowSetting.directional.cascadeCount);
        buffer.SetGlobalVectorArray(cascadeCullingSpheresId, cascadeCullingSpheres);
        buffer.SetGlobalVectorArray(cascadeDataId, cascadeData);
        buffer.SetGlobalMatrixArray(dirShadowMatricesId, dirShadowMatrices);
        buffer.SetGlobalFloat(shadowDistanceId, shadowSetting.MaxDistance);
        //阴影过渡距离发送到GPU
        float f = 1f - shadowSetting.directional.cascadeFade;
        buffer.SetGlobalVector(shadowDistanceFadeId, new Vector4(1f/shadowSetting.MaxDistance, 
                                    1f/shadowSetting.distanceFade,
                                    1f/(1f-f*f)));
        //设置PCF shader宏
        SetKeywords(directionalFilterKeywords, (int)shadowSetting.directional.filter - 1);
        SetKeywords(cascadeBlendKeywords, (int)shadowSetting.directional.cascadeBlend - 1);
        //传递图集大小和文素大小
        buffer.SetGlobalVector(shadowAtlasSizeId, new Vector4(atlasSize, 1f / atlasSize));
        buffer.EndSample(bufferName);
        ExecuteBuffer();
    }

      //渲染单个方向光阴影
    void RenderDirectionalShadows(int index, int split, int tileSize)
    {
        ShadowedDirectionalLight light = ShadowedDirectionalLights[index];
        ShadowDrawingSettings settings = new ShadowDrawingSettings(cullingResults, light.visibleLightIndex);
        //得到级联阴影贴图需要的参数
        int cascadeCount = shadowSetting.directional.cascadeCount;
        int tileOffset = index * cascadeCount;
        Vector3 ratios = shadowSetting.directional.CascadeRatios;
        float cullingFactor = Mathf.Max(0f, 0.8f - shadowSetting.directional.cascadeFade);
        for (int i = 0; i < cascadeCount; i++)
        {
            cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(light.visibleLightIndex,
                i, cascadeCount, ratios, tileSize, light.nearPlaneOffset, out Matrix4x4 viewMatrix,
            out Matrix4x4 projectionMatrix, out ShadowSplitData splitData);
            //得到第一个光源的包围球数据
            if (index == 0)
            {
                Vector4 cullingSpheres = splitData.cullingSphere;
                SetCascadeData(i, cullingSpheres, tileSize);      
            }
            //剔除被小的级联所包含的渲染实体，防止多个级联之间重复渲染
            splitData.shadowCascadeBlendCullingFactor = cullingFactor;
            settings.splitData = splitData;
            //调整图块索引，它等于光源的图块偏移加上级联的索引
            int tileIndex = tileOffset + i;
            dirShadowMatrices[tileIndex] = ConvertToAtalasMatrix(projectionMatrix * viewMatrix, SetTileViewport(tileIndex, split, tileSize), split);
            buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);

            //设置深度偏差
            //  buffer.SetGlobalDepthBias(0f, 3f);
            //设置斜度比例偏差值
            buffer.SetGlobalDepthBias(0f, light.slopeScaleBias);
            ExecuteBuffer();
            //只渲染"LightMode" = "ShadowCaster"的通道
            context.DrawShadows(ref settings);
            buffer.SetGlobalDepthBias(0f, 0f);
        }
    }

    //释放渲染纹理
    public void Cleanup()
    {
        buffer.ReleaseTemporaryRT(dirShadowAtlasId);
        ExecuteBuffer();
    }

    //调整渲染视口来渲染单个图块
    Vector2 SetTileViewport(int index, int split, float tileSize)
    {
        //计算索引图块的偏移位置
        Vector2 offset = new Vector2(index%split, index/split);
        //设置渲染视口，拆分成多个图块
        buffer.SetViewport(new Rect(offset.x*tileSize, offset.y*tileSize, tileSize, tileSize));
        return offset;
    }

    //返回一个从世界空间到阴影图块空间的转换空间
    Matrix4x4 ConvertToAtalasMatrix(Matrix4x4 m, Vector2 offset, int split)
    {
        //如果使用了反向Zbuffer
        if (SystemInfo.usesReversedZBuffer)
        {
            m.m20 = -m.m20;
            m.m21 = -m.m21;
            m.m22 = -m.m22;
            m.m23 = -m.m23;
        }
        //设置矩阵坐标
        float scale = 1f / split;
        m.m00 = (0.5f * (m.m00 + m.m30) + offset.x * m.m30) * scale;
        m.m01 = (0.5f * (m.m01 + m.m31) + offset.x * m.m31) * scale;
        m.m02 = (0.5f * (m.m02 + m.m32) + offset.x * m.m32) * scale;
        m.m03 = (0.5f * (m.m03 + m.m33) + offset.x * m.m33) * scale;
        m.m10 = (0.5f * (m.m10 + m.m30) + offset.y * m.m30) * scale;
        m.m11 = (0.5f * (m.m11 + m.m31) + offset.y * m.m31) * scale;
        m.m12 = (0.5f * (m.m12 + m.m32) + offset.y * m.m32) * scale;
        m.m13 = (0.5f * (m.m13 + m.m33) + offset.y * m.m33) * scale;
        m.m20 = 0.5f * (m.m20 + m.m30);
        m.m21 = 0.5f * (m.m21 + m.m31);
        m.m22 = 0.5f * (m.m22 + m.m32);
        m.m23 = 0.5f * (m.m23 + m.m33);
        return m;

    }

    //PCF滤波模式
    static string[] directionalFilterKeywords =
    {
        "_DIRECTIONAL_PCF3",
        "_DIRECTIONAL_PCF5",
        "_DIRECTIONAL_PCF7",
    };

    /// <summary>
    /// 设置关键字
    /// </summary>
    /// <param name="keywords"></param>
    /// <param name="enabledIndex"></param>
    void SetKeywords(string[] keywords, int enabledIndex)
    {
        // int enabledIndex = (int)settings.directional.filter - 1;
        for (int i = 0; i < keywords.Length; i++)
        {
            if (i == enabledIndex)
            {
                buffer.EnableShaderKeyword(keywords[i]);
            }
            else
            {
                buffer.DisableShaderKeyword(keywords[i]);
            }
        }
    }
}

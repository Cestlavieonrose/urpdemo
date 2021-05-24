using System.Collections;
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
    public Vector2 ReserveDirectionalShadows(Light light, int visibleLightIndex)
    {
        //存储可见光源的索引，前提是开启了阴影投射，且强度不为0
        if (ShadowedDirectionalLightCount < maxShadowedDirectionalLightCount && light.shadows != LightShadows.None && light.shadowStrength > 0f
            //还需要加一个判断，就是是否在最大投影范围内
			&& cullingResults.GetShadowCasterBounds(visibleLightIndex,out Bounds b))
        {
            ShadowedDirectionalLights[ShadowedDirectionalLightCount] = new ShadowedDirectionalLight
            {
                visibleLightIndex = visibleLightIndex
            };
            return new Vector2(light.shadowStrength, shadowSetting.directional.cascadeCount * ShadowedDirectionalLightCount++);
        }
        return Vector2.zero;
    }

    //渲染阴影
    public void Render()
    {
        if (ShadowedDirectionalLightCount > 0)
        {
            RenderDirectionalShadows();
        }
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
        buffer.SetGlobalMatrixArray(dirShadowMatricesId, dirShadowMatrices);
        buffer.SetGlobalFloat(shadowDistanceId, shadowSetting.MaxDistance);
        //阴影过渡距离发送到GPU
        float f = 1f - shadowSetting.directional.cascadeFade;
        buffer.SetGlobalVector(shadowDistanceFadeId, new Vector4(1f/shadowSetting.MaxDistance, 
        1f/shadowSetting.distanceFade,
        1f/(1f-f*f)));
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

        for (int i = 0; i < cascadeCount; i++)
        {
            cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(light.visibleLightIndex,
                i, cascadeCount, ratios, tileSize, 0f, out Matrix4x4 viewMatrix,
            out Matrix4x4 projectionMatrix, out ShadowSplitData splitData);
            //得到第一个光源的包围球数据
            if (index == 0)
            {
                Vector4 cullingSpheres = splitData.cullingSphere;
                cullingSpheres.w *= cullingSpheres.w;
                cascadeCullingSpheres[i] = cullingSpheres;
                
            }
            settings.splitData = splitData;
            //调整图块索引，它等于光源的图块偏移加上级联的索引
            int tileIndex = tileOffset + i;
            dirShadowMatrices[tileIndex] = ConvertToAtalasMatrix(projectionMatrix * viewMatrix, SetTileViewport(tileIndex, split, tileSize), split);
            buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
            ExecuteBuffer();
            //只渲染"LightMode" = "ShadowCaster"的通道
            context.DrawShadows(ref settings);
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
}

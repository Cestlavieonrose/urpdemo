using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

//灯光管理类
public class Lighting
{
    //定义其他类型光源的数量
    const int maxOtherLightCount = 64;
    static int otherLightCountId = Shader.PropertyToID("_OtherLightCount");
    static int otherLightColorsId = Shader.PropertyToID("_OtherLightColors");
    static int otherLightPositionsId = Shader.PropertyToID("_OtherLightPosition");
    static int otherLightDirsId = Shader.PropertyToID("_OtherLightDirections");
    static int otherLightSpotAnglesId = Shader.PropertyToID("_OtherLightSpotAngles");
    //存储其他类型光源的颜色和位置数据
    static Vector4[] otherLightColors = new Vector4[maxOtherLightCount];
    static Vector4[] otherLightPositions = new Vector4[maxOtherLightCount];
    static Vector4[] otherLightDirections = new Vector4[maxOtherLightCount];
    static Vector4[] otherLightSpotAngles = new Vector4[maxOtherLightCount];

    const string bufferName = "Lighting";
    CommandBuffer buffer = new CommandBuffer
    { 
        name = bufferName
    };

    //限制最大可见平行光数量为4
    const int maxDirLightCount = 4;

    static int dirLightCountId = Shader.PropertyToID("_DirectionalLightCount");
    static int dirLightColorsId = Shader.PropertyToID("_DirectionalLightColors");
    static int dirLightDirectionsId = Shader.PropertyToID("_DirectionalLightDirections");
    static int dirLightShadowDataId = Shader.PropertyToID("_DirectionalLightShadowData");
    //存储定向光的颜色和方向
    static Vector4[] dirLightColors = new Vector4[maxDirLightCount];
    static Vector4[] dirLightDirections = new Vector4[maxDirLightCount];
    //存储定向光的阴影数据
    static Vector4[] dirLightShadowData = new Vector4[maxDirLightCount];
    //存储相机剔除后的结果
    CullingResults cullingResults;

    Shadows shadows = new Shadows();

    public void Setup(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings shadowSetting)
    {
        this.cullingResults = cullingResults;
        buffer.BeginSample(bufferName);
        //传递阴影数据
        shadows.Setup(context, cullingResults, shadowSetting);
        //存储并发送所有光源数据
        SetupLights();
        //渲染阴影
        shadows.Render();
        buffer.EndSample(bufferName);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    //将可见光的光照颜色和方向存储到数组
    void SetupDirectionalLight(int index, ref VisibleLight visibleLight)
    {
        dirLightColors[index] = visibleLight.finalColor;//已经应用了光照强度
        dirLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
        //存储阴影数据
        dirLightShadowData[index] = shadows.ReserveDirectionalShadows(visibleLight.light, index);
    }

    //将场景主光源的光照颜色和方向传递到GPU
    void SetupLights()
    {
        //Light light = RenderSettings.sun;
        //灯光的颜色我们在乘上光强作为最终颜色
        //buffer.SetGlobalVector(dirLightColorId, light.color.linear * light.intensity);
        //buffer.SetGlobalVector(dirLightDirectionId, -light.transform.forward);
        //见到的所有可见光
        NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;
        int dirLightCount = 0, otherLightCount = 0;
        for (int i = 0; i < visibleLights.Length; i++)
        {
            VisibleLight visibleLight = visibleLights[i];
            switch(visibleLight.lightType)
            {
                case LightType.Directional:
                    if (dirLightCount < maxDirLightCount)
                    {
                        //VisibleLight结构很大，我们改为传递引用不是传递值，这样不生成副本
                        SetupDirectionalLight(dirLightCount++, ref visibleLight);
                    }
                    break;
                case LightType.Point:
                    if (otherLightCount < maxOtherLightCount)
                    {
                        SetupPointLight(otherLightCount++, ref visibleLight);
                    }
                    break;
                case LightType.Spot:
                    if (otherLightCount < maxOtherLightCount)
                    {
                        SetupSpotLight(otherLightCount++, ref visibleLight);
                    }
                    break;
            }
            
        }

        buffer.SetGlobalInt(dirLightCountId, dirLightCount);
        if (dirLightCount>0)
        {
            buffer.SetGlobalVectorArray(dirLightColorsId, dirLightColors);
            buffer.SetGlobalVectorArray(dirLightDirectionsId, dirLightDirections);
            buffer.SetGlobalVectorArray(dirLightShadowDataId, dirLightShadowData);
        }

        buffer.SetGlobalInt(otherLightCountId, otherLightCount);
        if (otherLightCount>0)
        {
            buffer.SetGlobalVectorArray(otherLightColorsId, otherLightColors);
            buffer.SetGlobalVectorArray(otherLightPositionsId, otherLightPositions);
            buffer.SetGlobalVectorArray(otherLightDirsId, otherLightDirections);
            buffer.SetGlobalVectorArray(otherLightSpotAnglesId, otherLightSpotAngles);
            
        }
        
    }

    void SetupPointLight(int index, ref VisibleLight light)
    {
        otherLightColors[index] = light.finalColor;
        Vector4 position = light.localToWorldMatrix.GetColumn(3);
        position.w = 1.0f/Mathf.Max(light.range*light.range, 0.00001f);
        otherLightPositions[index] = position;
        otherLightSpotAngles[index] = new Vector4(0f, 1f);
    }

    void SetupSpotLight(int index, ref VisibleLight light)
    {
        otherLightColors[index] = light.finalColor;
        Vector4 position = light.localToWorldMatrix.GetColumn(3);
        position.w = 1.0f / Mathf.Max(light.range * light.range, 0.00001f);
        otherLightPositions[index] = position;
        //本地到世界转换矩阵的第三列求反得到光照方向
        otherLightDirections[index] = -light.localToWorldMatrix.GetColumn(2);
        //Debug.Log("SetupSpotLight:" + index + " dir:" + otherLightDirections[index]);
        Light l = light.light;
        float innerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * l.innerSpotAngle);
        float outerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * light.spotAngle);
        float angleRangeInv = 1f / Mathf.Max(innerCos - outerCos, 0.001f);
        otherLightSpotAngles[index] = new Vector4(angleRangeInv, -outerCos * angleRangeInv);
    }

    //释放阴影贴图
    public void Cleanup()
    {
        shadows.Cleanup();
    }
}

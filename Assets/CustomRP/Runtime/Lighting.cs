using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class Lighting
{
    //存储相机剔除后的结果
    CullingResults cullingResults;
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
    //存储可见光颜色和方向
    static Vector4[] dirLightColors = new Vector4[maxDirLightCount];
    static Vector4[] dirLightDirections = new Vector4[maxDirLightCount];


    public void Setup(ScriptableRenderContext context, CullingResults cullingResults)
    {
        this.cullingResults = cullingResults;
        buffer.BeginSample(bufferName);
        //发送光源数据
        SetupLights();
        buffer.EndSample(bufferName);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    //将可见光的光照颜色和方向存储到数组
    void SetupDirectionalLight(int index, ref VisibleLight visibleLight)
    {
        dirLightColors[index] = visibleLight.finalColor;//已经应用了光照强度
        dirLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);

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
        int dirLightCount = 0;
        for (int i = 0; i < visibleLights.Length; i++)
        {
            VisibleLight visibleLight = visibleLights[i];
            //如果是方向光 我们才进行数据存储
            if (visibleLight.lightType == LightType.Directional)
            {
                //VisibleLight结构很大，我们改为传递引用不是传递值，这样不生成副本
                SetupDirectionalLight(dirLightCount++, ref visibleLight);
                if (dirLightCount >= maxDirLightCount)
                {
                    break;
                }
            }
        }

        buffer.SetGlobalInt(dirLightCountId, dirLightCount);
        buffer.SetGlobalVectorArray(dirLightColorsId, dirLightColors);
        buffer.SetGlobalVectorArray(dirLightDirectionsId, dirLightDirections);
    }
}

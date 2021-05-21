using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CustomRenderPipeline : RenderPipeline
{
    CameraRenderer cameraRenderer = new CameraRenderer();
    bool useDynamicBatching, useGPUInstancing;
    //测试SRP合批启用
    public CustomRenderPipeline(bool useDynamicBatching, bool useGPUInstancing, bool useSRPBatcher)
    {
        //设置合批使用状态
        this.useDynamicBatching = useDynamicBatching;
        this.useGPUInstancing = useGPUInstancing;
        GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;
        //灯光使用线性强度
        GraphicsSettings.lightsUseLinearIntensity = true;
    }

    //Unity 每一帧都会调用这个方法进行画面渲染 该方法是SRP的入口
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        foreach(Camera camera in cameras)
        {
            cameraRenderer.Render(context, camera, useDynamicBatching, useGPUInstancing);
        }

    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
////相机渲染管理类：单独控制每个相机的渲染
public partial class CameraRenderer
{
    ScriptableRenderContext context;
    Camera camera;
    const string bufferName = "Render Camera";

    CommandBuffer buffer = new CommandBuffer
    {
        name = bufferName
    };

    //存储相机剔除后的结果
    CullingResults cullingResults;
    //哪个shader的哪个Pass进行渲染
    static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");
    static ShaderTagId litShaderTagId = new ShaderTagId("CustomLit");
    //光照实例
    Lighting lighting = new Lighting();
    
    //自定义的相机渲染类
    public void Render(ScriptableRenderContext context, Camera camera, bool useDynamicBatching, 
                    bool useGPUInstancing, ShadowSettings shadowSetting)
    {
        this.context = context;
        this.camera = camera;
        //设置相机名字作为commandbuff的名字
        PrepareBuffer();
        //再game视图绘制的几何体也绘制到Scene视图中
        PrepareForSceneWindow();

        if (!Cull(shadowSetting.MaxDistance))
        {
            return;
        }
        buffer.BeginSample(SampleName);
        ExecuteBuffer();
        //光源数据和阴影数据发送到GPU计算光照
        lighting.Setup(context, cullingResults, shadowSetting);
        buffer.EndSample(SampleName);

        Setup();
        //绘制几何体
        DrawVisibleGeometry(useDynamicBatching, useGPUInstancing);
        //绘制SRP不支持的着色器类型
        DrawUnsupportedShaders();
        //绘制Gizmos
        DrawGizmos();
        lighting.Cleanup();
        Submit();
    }

    //绘制可见物
    void DrawVisibleGeometry(bool useDynamicBatching, bool useGPUInstancing)
    {
        //设置绘制顺序和指定渲染相机
        SortingSettings sortingSettings = new SortingSettings(camera)
        {
            //不透明对象的经典排序模式
            criteria = SortingCriteria.CommonOpaque
        };
        //设置渲染的shader pass和排序模式
        var drawingSettings = new DrawingSettings(unlitShaderTagId, sortingSettings)
        {
            //设置渲染时批处理的使用状态
            enableDynamicBatching = useDynamicBatching,
            enableInstancing = useGPUInstancing,
            //对每个烘焙了光照信息对对象发送光照贴图信息
            perObjectData = PerObjectData.Lightmaps | PerObjectData.LightProbe | PerObjectData.LightProbeProxyVolume | PerObjectData.ReflectionProbes
        };
        //渲染CustomList表示的pass块
        drawingSettings.SetShaderPassName(1, litShaderTagId);
        //设置哪些类型的渲染队列可以被绘制 这里只绘制不透明物体
        var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
        //1. 绘制不透明物体
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);

        //2. 绘制天空盒
        context.DrawSkybox(camera);

        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        drawingSettings.sortingSettings = sortingSettings;
        //只绘制RenderQueue为transparent的透明的物体
        filteringSettings.renderQueueRange = RenderQueueRange.transparent;
        //3. 绘制透明物体
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
    }

    //提交缓冲区渲染命令
    void Submit()
    {
        buffer.EndSample(SampleName);
        ExecuteBuffer();
        context.Submit();
    }

     //设置相机的属性和矩阵
    void Setup()
    {
        //设置VP矩阵
        context.SetupCameraProperties(camera);
        //得到相机的clear flags
        CameraClearFlags flags = camera.clearFlags;
        //设置相机清楚状态
        buffer.ClearRenderTarget(flags <= CameraClearFlags.Depth, flags == CameraClearFlags.Color, 
            flags == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.clear);
        buffer.BeginSample(SampleName);  
        ExecuteBuffer();
    }
    //执行缓冲区命令
    void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    //裁剪
    bool Cull(float maxShadowDistance)
    {
        ScriptableCullingParameters p;
        //得到需要进行剔除检查的所有物体
        if (camera.TryGetCullingParameters(out p))
        {
            //得到的最大距离和相机的farPanel进行比较，取最小的那个作为阴影距离
            p.shadowDistance = Mathf.Min(maxShadowDistance, camera.farClipPlane);
            //正式剔除
            cullingResults = context.Cull(ref p);
            return true;
        }
        return false;
    }

    

}

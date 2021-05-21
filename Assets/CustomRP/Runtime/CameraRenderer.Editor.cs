using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

public partial class CameraRenderer
{

    partial void DrawUnsupportedShaders();
    partial void DrawGizmos();
    partial void PrepareForSceneWindow();
    partial void PrepareBuffer();

#if UNITY_EDITOR
    //SRP不支持的着色器标签类型
    static ShaderTagId[] legacyShaderTagIds =
    {
        new ShaderTagId("Always"),
        new ShaderTagId("ForwardBase"),
        new ShaderTagId("PrepassBase"),
        new ShaderTagId("Vertex"),
        new ShaderTagId("VertexLMRGBM"),
        new ShaderTagId("VertexLM")
    };

    //绘制成使用错误材质的粉红颜色
    static Material errorMat;

    //绘制SRP不支持的着色器类型
    partial void DrawUnsupportedShaders()
    {
        //不支持的shaderTag类型我们使用错误的材质专用shader来渲染
        if (errorMat == null)
            errorMat = new Material(Shader.Find("Hidden/InternalErrorShader"));

        //数组第一个元素用来构造DrawingSettings对象的时候设置
        DrawingSettings drawingSettings = new DrawingSettings(legacyShaderTagIds[0], new SortingSettings(camera))
        {
            overrideMaterial = errorMat
        };
        for (int i=1; i<legacyShaderTagIds.Length; i++)
        {
            //遍历数组逐个设置着色器的PassName， 从i=1开始
            drawingSettings.SetShaderPassName(i, legacyShaderTagIds[i]);
        }
        //使用默认设置即可，反正画出来的都是不支持的
        FilteringSettings filteringSettings = FilteringSettings.defaultValue;
        //绘制不支持的ShaderTag类型的物体
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
    }

    //绘制DrawGizmos
    partial void DrawGizmos()
    {
        
        if (Handles.ShouldRenderGizmos())
        {
            //后处理前还是后绘制gizmos
            context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
            context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
        }
    }

    //再game视图绘制的几何体也绘制到Scene试图中
    partial void PrepareForSceneWindow()
    {
        if (camera.cameraType == CameraType.SceneView)
        {
            //如果切换到Scene视图，调用此方法完成绘制
            ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
        }
    }
    string SampleName { get; set; }
    //使用相机名字去设置命令缓冲区的名字
    partial void PrepareBuffer()
    {
        //设置一下只有再编辑器模式下才分配内存
        Profiler.BeginSample("Editor Only");
        buffer.name = SampleName = camera.name;
        Profiler.EndSample();
    }
#else
    const string SampleName = buffName;
#endif
}

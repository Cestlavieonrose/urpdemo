using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
/// <summary>
/// 自定义渲染管线资产
/// </summary>
//该标签会在你在Project下右键->Asset/Create菜单中添加一个新的子菜单
[CreateAssetMenu(menuName ="Rendering/CreateCustomRenderPipeline")]
public class CustomRenderPipelineAsset : RenderPipelineAsset
{
    //设置批处理启用状态
    [SerializeField]
    bool useDynamicBatching = true, useGPUInstancing = true, useSRPBatcher = true;
    //HDR设置
    [SerializeField]
    bool allowHDR = true;
    //是否使用逐对象光照
    [SerializeField]
    bool useLightsPerObject = true;
    //阴影配置
    [SerializeField]
    ShadowSettings shadows = default;
    //后处理配置
    [SerializeField]
    PostFXSettings postFXSettings = default;
    
    //重写抽象方法，需要返回一个RenderPipeline实例对象
    protected override RenderPipeline CreatePipeline()
    {
        return new CustomRenderPipeline(allowHDR, useDynamicBatching, useGPUInstancing, useSRPBatcher, useLightsPerObject, shadows, postFXSettings);
    }
}

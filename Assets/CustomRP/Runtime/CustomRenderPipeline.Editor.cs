using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Rendering;
using Lightmapping = UnityEngine.Experimental.GlobalIllumination.Lightmapping;

public partial class CustomRenderPipeline:RenderPipeline
{

    partial void InitializeForEditor();

#if UNITY_EDITOR
    static Lightmapping.RequestLightsDelegate lightsDelegate = 
        (Light[] lights, NativeArray<LightDataGI> output) => {
            LightDataGI lightData = new LightDataGI();
            for (int i = 0; i < lights.Length; i++)
            {
                Light light = lights[i];
                switch (light.type)
                {
                    case UnityEngine.LightType.Directional:
                        var directionalLight = new DirectionalLight();
                        lightData.Init(ref directionalLight);
                        break;
                    case UnityEngine.LightType.Point:
                        var pointLight = new PointLight();
                        LightmapperUtils.Extract(light, ref pointLight);
                        lightData.Init(ref pointLight);
                        break;
                    case UnityEngine.LightType.Spot:
                        var spotLight = new SpotLight();
                        LightmapperUtils.Extract(light, ref spotLight);
                        lightData.Init(ref spotLight);
                        break;
                    case UnityEngine.LightType.Area:
                        var rectLight = new RectangleLight();
                        LightmapperUtils.Extract(light, ref rectLight);
                        rectLight.mode = LightMode.Baked;
                        lightData.Init(ref rectLight);
                        break;
                    default:
                        lightData.InitNoBake(light.GetInstanceID());
                        break;
                }
                lightData.falloff = FalloffType.InverseSquared;
                output[i] = lightData;
            }
        };

    partial void InitializeForEditor()
    {
        Lightmapping.SetDelegate(lightsDelegate);
    }

    //清理和重置委托
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Lightmapping.ResetDelegate();

    }
#endif
}

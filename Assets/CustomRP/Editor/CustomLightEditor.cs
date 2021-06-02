using UnityEngine;
using UnityEditor;

///扩展灯光属性面板
[CanEditMultipleObjects]
[CustomEditorForRenderPipeline(typeof(Light), typeof(CustomRenderPipelineAsset))]
public class CustomLightEditor : LightEditor 
{
	//重写灯光属性面板
	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();
        //如果是聚光灯面板
		if (!settings.lightType.hasMultipleDifferentValues &&(LightType)settings.lightType.enumValueIndex == LightType.Spot)
		{
            //绘制一个调节内外聚光角度滑块
			settings.DrawInnerAndOuterSpotAngle();
			settings.ApplyModifiedProperties();
		}
	}
}

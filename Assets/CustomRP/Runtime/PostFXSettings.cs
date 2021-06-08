using UnityEngine;
/// <summary>
/// 后处理特效栈的配置
/// </summary>
[CreateAssetMenu(menuName = "Rendering/Custom Post FX Settings")]
public class PostFXSettings : ScriptableObject 
{
    [SerializeField]
    Shader shader = default;

	[System.NonSerialized]
	Material material;
	[System.Serializable]
	public struct BloomSettings
	{
        //模糊迭代次数
		[Range(0f, 16f)]
		public int maxIterations;
        //下采样纹理尺寸下限
		[Min(1f)]
		public int downscaleLimit;
        //双三次滤波上采样
		public bool bicubicUpsampling;
        //阈值
		[Min(0f)]
		public float threshold;
        //阈值拐点
		[Range(0f, 1f)]
		public float thresholdKnee;
        //Bloom强度
		[Min(0f)]
		public float intensity;
	}

	[SerializeField]
	BloomSettings bloom = default;

	public BloomSettings Bloom => bloom;
	public Material Material
	{
		get
		{
			if (material == null && shader != null)
			{
				material = new Material(shader);
				material.hideFlags = HideFlags.HideAndDontSave;
			}
			return material;
		}
	}
}

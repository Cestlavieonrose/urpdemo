using UnityEngine;
using UnityEngine.Rendering;
/// <summary>
/// 后处理效果管理类
/// </summary>
public partial class PostFXStack
{
	const string bufferName = "Post FX";
	int bloomBucibicUpsamplingId = Shader.PropertyToID("_BloomBicubicUpsampling");
	int bloomPrefilterId = Shader.PropertyToID("_BloomPrefilter");
	int bloomThresholdId = Shader.PropertyToID("_BloomThreshold");
	int bloomIntensityId = Shader.PropertyToID("_BloomIntensity");
	int fxSourceId = Shader.PropertyToID("_PostFXSource");
	int fxSource2Id = Shader.PropertyToID("_PostFXSource2");
	CommandBuffer buffer = new CommandBuffer
	{
		name = bufferName
	};
	ScriptableRenderContext context;
	Camera camera;
	PostFXSettings settings;
	//最大纹理金字塔级别
	const int maxBloomPyramidLevels = 16;
	//纹理标识符
	int bloomPyramidId;
    //每个枚举值对应一个后处理着色器Pass
	enum Pass
	{
		BloomHorizontal,
		BloomVertical,
		BloomCombine,
		BloomPrefilter,
		Copy
	}
	//判断后效栈是否激活
	public bool IsActive => settings != null;
    //在构造方法中获取纹理标识符，且只跟踪第一个标识符即可
	public PostFXStack()
	{
		bloomPyramidId = Shader.PropertyToID("_BloomPyramid0");
		for (int i = 1; i < maxBloomPyramidLevels * 2; i++)
		{
			Shader.PropertyToID("_BloomPyramid" + i);
		}
	}
    //初始化设置
	public void Setup(ScriptableRenderContext context, Camera camera, PostFXSettings settings)
	{
		this.context = context;
		this.camera = camera;
		this.settings = camera.cameraType <= CameraType.SceneView ? settings : null;
		ApplySceneViewState();
	}
    /// <summary>
    /// 将源数据绘制到指定渲染目标中
    /// </summary>
    /// <param name="from">源标识符</param>
    /// <param name="to">目标标识符</param>
    /// <param name="pass">通道序号</param>
	void Draw(RenderTargetIdentifier from, RenderTargetIdentifier to, Pass pass)
	{
		buffer.SetGlobalTexture(fxSourceId, from);
		buffer.SetRenderTarget(to, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        //绘制三角形
		buffer.DrawProcedural(Matrix4x4.identity, settings.Material, (int)pass,MeshTopology.Triangles, 3);
	}
     /// <summary>
    /// 渲染后处理特效
    /// </summary>
    /// <param name="sourceId"></param>
	public void Render(int sourceId)
	{
		//Draw(sourceId, BuiltinRenderTextureType.CameraTarget, Pass.Copy);
		DoBloom(sourceId);
		context.ExecuteCommandBuffer(buffer);
		buffer.Clear();
	}

    /// <summary>
    /// 渲染Bloom
    /// </summary>
    /// <param name="sourceId"></param>
    /// <returns></returns>
	void DoBloom(int sourceId)
	{
		buffer.BeginSample("Bloom");
		PostFXSettings.BloomSettings bloom = settings.Bloom;
		int width = camera.pixelWidth / 2, height = camera.pixelHeight / 2;
		if (bloom.maxIterations == 0 || bloom.intensity <= 0f || height < bloom.downscaleLimit * 2 || width < bloom.downscaleLimit * 2)
		{
			Draw(sourceId, BuiltinRenderTextureType.CameraTarget, Pass.Copy);
			buffer.EndSample("Bloom");
			return;
		}
		//发送阈值和相关数据
		Vector4 threshold;
		threshold.x = Mathf.GammaToLinearSpace(bloom.threshold);
		threshold.y = threshold.x * bloom.thresholdKnee;
		threshold.z = 2f * threshold.y;
		threshold.w = 0.25f / (threshold.y + 0.00001f);
		threshold.y -= threshold.x;
		buffer.SetGlobalVector(bloomThresholdId, threshold);

		RenderTextureFormat format = RenderTextureFormat.Default;
		buffer.GetTemporaryRT(bloomPrefilterId, width, height, 0, FilterMode.Bilinear, format);
		Draw(sourceId, bloomPrefilterId, Pass.BloomPrefilter);
		width /= 2;
		height /= 2;

		int fromId = bloomPrefilterId;
		int toId = bloomPyramidId + 1;
		int i;
        //逐步下采样
		for (i = 0; i < bloom.maxIterations; i++)
		{
			if (height < bloom.downscaleLimit || width < bloom.downscaleLimit)
			{
				break;
			}
			int midId = toId - 1;
			buffer.GetTemporaryRT(midId, width, height, 0, FilterMode.Bilinear, format);
			buffer.GetTemporaryRT(toId, width, height, 0, FilterMode.Bilinear, format);
			Draw(fromId, midId, Pass.BloomHorizontal);
			Draw(midId, toId, Pass.BloomVertical);
			fromId = toId;
			toId += 2;
			width /= 2;
			height /= 2;
		}
		buffer.ReleaseTemporaryRT(bloomPrefilterId);
		buffer.SetGlobalFloat(bloomBucibicUpsamplingId, bloom.bicubicUpsampling ? 1f : 0f);
		buffer.SetGlobalFloat(bloomIntensityId, 1f);
        //逐步上采样
		if (i > 1)
		{
			buffer.ReleaseTemporaryRT(fromId - 1);
			toId -= 5;
			for (i -= 1; i > 0; i--)
			{
				buffer.SetGlobalTexture(fxSource2Id, toId + 1);
				Draw(fromId, toId, Pass.BloomCombine);
				buffer.ReleaseTemporaryRT(fromId);
				buffer.ReleaseTemporaryRT(toId + 1);
				fromId = toId;
				toId -= 2;
			}
        }
        else
        {
			buffer.ReleaseTemporaryRT(bloomPyramidId);
		}
		buffer.SetGlobalFloat(bloomIntensityId, bloom.intensity);
		buffer.SetGlobalTexture(fxSource2Id, sourceId);
		Draw(fromId, BuiltinRenderTextureType.CameraTarget, Pass.BloomCombine);
		buffer.ReleaseTemporaryRT(fromId);
		buffer.EndSample("Bloom");
	}
}
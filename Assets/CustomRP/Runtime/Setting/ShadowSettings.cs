using UnityEngine;

//阴影属性设置
[System.Serializable]
public class ShadowSettings
{
    //阴影最大距离
    [Min(0f)]
    public float MaxDistance = 100f;
    //阴影贴图大小
    public enum TextureSize
    {
        _256=256,
        _512=512,
        _1024=1024,
        _2048=2048,
        _4096=4096,
        _8192=8192,
    }

    [System.Serializable]
    public struct Directional
    {
        public TextureSize atlasSize;
        //级联数量
        [Range(1, 4)]
        public int cascadeCount;
        //级联比例
        [Range(0f, 1f)]
        public float cascadeRatio1, cascadeRatio2, cascadeRatio3;
        public Vector3 CascadeRatios => new Vector3(cascadeRatio1, cascadeRatio2, cascadeRatio3);
    }

    //默认尺寸为1024
    public Directional directional = new Directional
    {
        atlasSize = TextureSize._1024,
        cascadeCount = 4,
        cascadeRatio1 = 0.1f,
        cascadeRatio2 = 0.25f,
        cascadeRatio3 = 0.5f,
        
    };    

    
}



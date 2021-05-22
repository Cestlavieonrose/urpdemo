using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//阴影属性设置
[System.Serializable]
public class ShadowSetting
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
    }

    //默认尺寸为1024
    public Directional directional = new Directional
    {
        atlasSize = TextureSize._1024
    };
    
}



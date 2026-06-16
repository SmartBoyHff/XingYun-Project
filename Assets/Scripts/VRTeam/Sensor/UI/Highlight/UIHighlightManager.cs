using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UIHighlightManager", menuName = "UI/UIHighlight Manager")]
public class UIHighlightManager : ScriptableObject
{
    [Tooltip("全局颜色序列（至少2个）")]
    public Color[] colors = new Color[]
      {
        Color.white,
        Color.red,
        Color.blue,
        Color.green
      };

    [Tooltip("全局渐变速度")]
    [Range(0.1f, 10f)]
    public float speed = 1f;
}

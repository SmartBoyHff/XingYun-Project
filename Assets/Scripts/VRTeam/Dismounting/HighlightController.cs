using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HighlightController : MonoBehaviour
{
     public void SetHighlightTargets(List<GameObject> targets)
    {
        // 清除旧轮廓光
        // 给targets添加轮廓光效果
        Debug.Log($"高亮 {targets.Count} 个物体");
    }

    public void ClearHighlight()
    {
        // 清除所有轮廓光
    }
}

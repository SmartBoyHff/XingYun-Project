using System.Collections.Generic;
using UnityEngine;

// ============================================================
// 文件名：VR4InteractionLayerSettings
// 模块：模块4 - 维护保养
// 功能：维护保养自定义交互层名称配置，用于编辑器中显示可自由增删的交互层级。
// 创建日期：2026-05-5
// 最后更新：2026-05-5
// ============================================================

namespace VRHelmet.VRTeam.Maintenance
{
    /// <summary>
    /// 维护保养自定义交互层配置资产。
    ///
    /// 【功能说明】
    /// 1. 存储 VR4InteractionLayer 在 Inspector 中显示的层级名称。
    /// 2. 限制最多 30 个自定义交互层，避免超过 int 掩码可用范围。
    /// 3. 在名称为空或列表为空时自动补齐默认层级，保证编辑器绘制稳定。
    ///
    /// 【依赖组件】
    /// - VR4InteractionLayer：维护保养流程使用的自定义交互层掩码。
    /// - VR4InteractionLayerDrawer：Inspector 中绘制掩码字段的编辑器脚本。
    /// </summary>
    [CreateAssetMenu(fileName = "VR4InteractionLayerSettings", menuName = "VR4/Maintenance/Interaction Layer Settings")]
    public class VR4InteractionLayerSettings : ScriptableObject
    {
        public const int MaxLayerCount = VR4InteractionLayer.MaxLayerCount;

        [SerializeField] private List<string> layerNames = new List<string>
        {
            "默认",
            "可交互",
            "不可交互"
        };

        public IReadOnlyList<string> LayerNames => layerNames;

        private void OnValidate()
        {
            EnsureValidLayerNames();
        }

        public string[] GetLayerNameArray()
        {
            EnsureValidLayerNames();
            return layerNames.ToArray();
        }

        private void EnsureValidLayerNames()
        {
            if (layerNames == null)
            {
                layerNames = new List<string>();
            }

            if (layerNames.Count == 0)
            {
                layerNames.Add("默认");
                layerNames.Add("可交互");
                layerNames.Add("不可交互");
            }

            while (layerNames.Count > MaxLayerCount)
            {
                layerNames.RemoveAt(layerNames.Count - 1);
            }

            for (int i = 0; i < layerNames.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(layerNames[i]))
                {
                    layerNames[i] = $"Layer {i}";
                }
            }
        }
    }
}

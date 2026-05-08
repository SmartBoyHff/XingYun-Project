using System;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
// 文件名：VR4_ExperimentBtn
// 模块：模块4 - 维护保养
// 功能：工作单入口按钮，负责解析步骤区间与题表索引并启动对应流程。
// 创建日期：2026-05-5
// 最后更新：2026-05-5
// ============================================================

namespace VRHelmet.VRTeam.Maintenance
{
    /// <summary>
    /// 工作单按钮。点击后启动指定步骤区间，并把对应题表索引传给答题管理器。
    /// </summary>
    /// <summary>
    /// VR4_ExperimentBtn 类型说明
    /// 
    /// 【功能说明】
    /// 1. 属于模块4 - 维护保养中的 VR4_ExperimentBtn 类型。
    /// 2. 负责解析步骤区间与题表索引并启动工作单流程。
    /// 3. 与维护保养流程中的步骤数据、交互逻辑或 UI 显示协同工作。
    /// 
    /// 【依赖组件】
    /// - Unity 组件体系。
    /// - VRHelmet.VRTeam.Maintenance 维护保养流程脚本。
    /// </summary>
    public class VR4_ExperimentBtn : MonoBehaviour
    {
        #region ==========Field==========
        /// <summary>
        /// 当前工作单物体上的按钮组件。未手动指定时会自动从当前物体获取。
        /// </summary>
        public Button button;

        /// <summary>
        /// 点击该工作单后要执行的步骤区间。示例：填 "1-4" 会执行 oStepList[1] 到 oStepList[4]，填 "3" 只执行 oStepList[3]。
        /// </summary>
        public string index = "0";

        /// <summary>
        /// 点击该工作单后要打开的题表索引，对应 VR4_TopicManager.topicTables。
        /// </summary>
        public int topicTableIndex;
        #endregion

        #region ==========Unity Method==========
        private void Start()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (button != null)
            {
                button.onClick.AddListener(OnBtnClicked);
            }
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(OnBtnClicked);
            }
        }
        #endregion

        #region ==========Logic==========
        private void OnBtnClicked()
        {
            if (!TryParseStepRange(index, out int startIndex, out int endIndex))
            {
                Debug.LogError($"{name} 的步骤区间 index 配置无效: {index}");
                return;
            }

            if (VR4_UIManager.HasInstance)
            {
                VR4_UIManager.Instance.StartWorkOrder(startIndex, endIndex, topicTableIndex);
            }
        }

        private bool TryParseStepRange(string rangeText, out int startIndex, out int endIndex)
        {
            startIndex = 0;
            endIndex = 0;

            if (string.IsNullOrWhiteSpace(rangeText))
            {
                return false;
            }

            string normalizedText = rangeText.Trim().Replace(" ", string.Empty);
            char[] separators = { '-', '~', '～', '—', '－' };
            string[] parts = normalizedText.Split(separators, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 1 && int.TryParse(parts[0], out startIndex))
            {
                endIndex = startIndex;
                return true;
            }

            if (parts.Length == 2 &&
                int.TryParse(parts[0], out startIndex) &&
                int.TryParse(parts[1], out endIndex))
            {
                if (startIndex > endIndex)
                {
                    int temp = startIndex;
                    startIndex = endIndex;
                    endIndex = temp;
                }

                return true;
            }

            return false;
        }
        #endregion

        #region ==========API==========
        #endregion
    }
}

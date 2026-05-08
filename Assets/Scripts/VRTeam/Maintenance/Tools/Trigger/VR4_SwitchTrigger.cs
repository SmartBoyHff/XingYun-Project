using System;
using UnityEngine;

// ============================================================
// 文件名：SwitchTrigger
// 模块：模块4 - 维护保养
// 功能：开关任务触发器，负责监听开关开闭事件并通知流程任务完成。
// 创建日期：2026-05-5
// 最后更新：2026-05-5
// ============================================================

namespace VRHelmet.VRTeam.Maintenance
{
    /// <summary>
    /// SwitchTrigger 类型说明
    /// 
    /// 【功能说明】
    /// 1. 属于模块4 - 维护保养中的 SwitchTrigger 类型。
    /// 2. 负责监听开关开闭并触发任务完成。
    /// 3. 与维护保养流程中的步骤数据、交互逻辑或 UI 显示协同工作。
    /// 
    /// 【依赖组件】
    /// - Unity 组件体系。
    /// - VRHelmet.VRTeam.Maintenance 维护保养流程脚本。
    /// </summary>
    public class VR4_SwitchTrigger : MonoBehaviour
    {
        #region ==========Field==========
        /// <summary>
        /// 负责实际拉杆交互和值变化事件的开关组件。
        /// </summary>
        public VR4_SwitchObject pullRod;

        /// <summary>
        /// 开关打开时触发的任务完成回调。
        /// </summary>
        public Action OnSwitchOpenCompleted;

        /// <summary>
        /// 开关闭合时触发的任务完成回调。
        /// </summary>
        public Action OnSwitchCloseCompleted;

        /// <summary>
        /// 特殊旋转容器打开时需要移动到的目标位姿。
        /// </summary>
        public Transform goatPos;

        Vector3 originalPosition;
        Quaternion originalRotation;
        bool hasOriginalPose;
        #endregion

        #region ==========Unity Method==========
        private void OnEnable()
        {
            if (pullRod == null) return;

            pullRod.enabled = true;
            pullRod.onLeverActivate.AddListener(Open);
            pullRod.onLeverDeactivate.AddListener(Close);

            if (!hasOriginalPose)
            {
                originalPosition = transform.position;
                originalRotation = transform.rotation;
                hasOriginalPose = true;
            }
        }

        private void OnDisable()
        {
            if (pullRod == null) return;

            pullRod.enabled = false;
            pullRod.onLeverActivate.RemoveListener(Open);
            pullRod.onLeverDeactivate.RemoveListener(Close);
        }
        #endregion

        #region ==========Logic==========
        void Open()
        {
            if (this.name == "大旋转容器")
            {
                if (goatPos != null)
                {
                    this.transform.rotation = goatPos.rotation;
                    this.transform.position = goatPos.position;
                }
            }
            OnSwitchOpenCompleted?.Invoke();
            this.enabled = false;
        }

        void Close()
        {
            if (this.name == "大旋转容器")
            {
                if (hasOriginalPose)
                {
                    this.transform.rotation = originalRotation;
                    this.transform.position = originalPosition;
                }
            }
            OnSwitchCloseCompleted?.Invoke();
            this.enabled = false;
        }
        #endregion

        #region ==========API==========
        #endregion
    }
}

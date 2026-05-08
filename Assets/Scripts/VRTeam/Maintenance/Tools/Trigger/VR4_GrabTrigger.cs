using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System;

// ============================================================
// 文件名：GrabTrigger
// 模块：模块4 - 维护保养
// 功能：抓取放置任务触发器，负责检测物体释放到目标 Socket 后完成任务。
// 创建日期：2026-05-5
// 最后更新：2026-05-5
// ============================================================

namespace VRHelmet.VRTeam.Maintenance
{
    /// <summary>
    /// GrabTrigger 类型说明
    /// 
    /// 【功能说明】
    /// 1. 属于模块4 - 维护保养中的 GrabTrigger 类型。
    /// 2. 负责检测物体释放到目标 Socket 后完成任务。
    /// 3. 与维护保养流程中的步骤数据、交互逻辑或 UI 显示协同工作。
    /// 
    /// 【依赖组件】
    /// - Unity 组件体系。
    /// - VRHelmet.VRTeam.Maintenance 维护保养流程脚本。
    /// </summary>
    public class VR4_GrabTrigger : MonoBehaviour
    {
        #region ==========Field==========
        /// <summary>
        /// 正确放置物体时需要进入的目标 Socket。
        /// </summary>
        public XRSocketInteractor targetSocket;

        /// <summary>
        /// 物体被正确放入目标 Socket 后触发的完成回调。
        /// </summary>
        public Action OnCorrectPlacement;

        private XRGrabInteractable grabInteractable;
        #endregion

        #region ==========Unity Method==========
        void Awake()
        {
            grabInteractable = GetComponent<XRGrabInteractable>();
        }

        void OnEnable()
        {
            if (grabInteractable != null)
            {
                grabInteractable.selectExited.AddListener(OnReleaseEvent);
            }
        }

        void OnDisable()
        {
            if (grabInteractable != null)
            {
                grabInteractable.selectExited.RemoveListener(OnReleaseEvent);
            }
        }
        #endregion

        #region ==========Logic==========
        void OnReleaseEvent(SelectExitEventArgs args)
        {
            if (targetSocket != null && targetSocket.interactablesSelected.Count > 0 && targetSocket.GetOldestInteractableSelected().transform.gameObject == gameObject)
            {
                this.enabled = false;
                OnCorrectPlacement?.Invoke();
            }
        }
        #endregion

        #region ==========API==========
        #endregion
    }
}

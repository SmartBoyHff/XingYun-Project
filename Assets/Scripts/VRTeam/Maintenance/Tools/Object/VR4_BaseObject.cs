using System;
using UnityEngine;
using UnityEngine.Events;

// ============================================================
// 文件名：BaseObject
// 模块：模块4 - 维护保养
// 功能：维护保养交互物体基类，统一步骤完成锁、完成事件和自定义任务接入。
// 创建日期：2026-05-5
// 最后更新：2026-05-5
// ============================================================

namespace VRHelmet.VRTeam.Maintenance
{
    /// <summary>
    /// 可作为步骤完成源的交互物体接口。
    /// 任何自定义物体只要在合适时机调用 CompleteStep，就可以统一触发本地事件或桥接到 VR4_ExperimentManager。
    /// </summary>
    /// <summary>
    /// IVR4StepCompletionSource 类型说明
    /// 
    /// 【功能说明】
    /// 1. 属于模块4 - 维护保养中的 IVR4StepCompletionSource 类型。
    /// 2. 负责统一步骤完成锁、完成事件和自定义任务接入。
    /// 3. 与维护保养流程中的步骤数据、交互逻辑或 UI 显示协同工作。
    /// 
    /// 【依赖组件】
    /// - Unity 组件体系。
    /// - VRHelmet.VRTeam.Maintenance 维护保养流程脚本。
    /// </summary>
    public interface IVR4StepCompletionSource
    {
        #region ==========API==========
        /// <summary>
        /// 当前交互物体是否已经触发过步骤完成，防止同一步骤被重复提交。
        /// </summary>
        bool IsStepCompleted { get; }

        /// <summary>
        /// 完成步骤时触发的 UnityEvent，可在 Inspector 中自由绑定 UI、音效、提示或其它自定义逻辑。
        /// </summary>
        UnityEvent OnStepCompleted { get; }

        /// <summary>
        /// 主动完成当前交互步骤。
        /// </summary>
        void CompleteStep();

        /// <summary>
        /// 重置完成锁，让该物体可以重新参与下一次步骤流程。
        /// </summary>
        void ResetStepCompletion();
        #endregion
    }

    /// <summary>
    /// Maintenance 交互物体通用基类。
    /// 统一封装步骤完成锁和自定义完成事件，ShakeObject、CollisionObject、胎压表等特殊物体可以直接继承。
    /// </summary>
    /// <summary>
    /// BaseObject 类型说明
    /// 
    /// 【功能说明】
    /// 1. 属于模块4 - 维护保养中的 BaseObject 类型。
    /// 2. 负责统一步骤完成锁、完成事件和自定义任务接入。
    /// 3. 与维护保养流程中的步骤数据、交互逻辑或 UI 显示协同工作。
    /// 
    /// 【依赖组件】
    /// - Unity 组件体系。
    /// - VRHelmet.VRTeam.Maintenance 维护保养流程脚本。
    /// </summary>
    public class VR4_BaseObject : MonoBehaviour, IVR4StepCompletionSource
    {
        #region ==========Field==========
        [Header("Step Completion")]
        /// <summary>
        /// 步骤完成后的自定义扩展事件。
        /// VR4_ExperimentManager 的 BaseObjectTaskHandler 会监听该事件，并在触发后完成当前步骤。
        /// 也可以在 Inspector 中继续绑定音效、UI、提示等额外行为。
        /// </summary>
        public UnityEvent onStepCompleted = new UnityEvent();

        /// <summary>
        /// 步骤完成锁，避免一次交互连续触发多个完成回调。
        /// </summary>
        [SerializeField] private bool isStepCompleted = false;

        /// <summary>
        /// 当前物体是否已经完成本轮步骤。
        /// </summary>
        public bool IsStepCompleted => isStepCompleted;

        /// <summary>
        /// 对外暴露的完成事件引用，供接口统一访问。
        /// </summary>
        public UnityEvent OnStepCompleted => onStepCompleted;
        #endregion

        #region ==========Unity Method==========
        #endregion

        #region ==========Logic==========
        /// <summary>
        /// 完成步骤并兼容旧任务回调。
        /// 旧回调用于 ShakeObject.OnShakeCompleted、CollisionObject.OnCollisionCompleted 这类已被旧任务处理器监听的事件。
        /// </summary>
        /// <param name="legacyCompletedCallback">旧系统任务完成回调，可为空。</param>
        protected void CompleteStep(Action legacyCompletedCallback)
        {
            if (isStepCompleted)
            {
                return;
            }

            isStepCompleted = true;
            legacyCompletedCallback?.Invoke();
            onStepCompleted?.Invoke();
        }
        #endregion

        #region ==========API==========
        /// <summary>
        /// 完成当前步骤。
        /// 自定义交互脚本通常只需要在条件满足时调用此方法即可。
        /// </summary>
        public void CompleteStep()
        {
            CompleteStep(null);
        }

        /// <summary>
        /// 重置步骤完成锁。
        /// 在重新开始任务、重新抓取或重置物体时调用。
        /// </summary>
        public void ResetStepCompletion()
        {
            isStepCompleted = false;
        }
        #endregion
    }

    /// <summary>
    /// RotatableObject 的统一完成源适配器。
    /// 不修改 RotatableObject 本体，通过监听 onValueChange 在目标值范围内触发 CompleteStep。
    /// </summary>
    /// <summary>
    /// VR4_RotatableStepCompletionAdapter 类型说明
    /// 
    /// 【功能说明】
    /// 1. 属于模块4 - 维护保养中的 VR4_RotatableStepCompletionAdapter 类型。
    /// 2. 负责统一步骤完成锁、完成事件和自定义任务接入。
    /// 3. 与维护保养流程中的步骤数据、交互逻辑或 UI 显示协同工作。
    /// 
    /// 【依赖组件】
    /// - Unity 组件体系。
    /// - VRHelmet.VRTeam.Maintenance 维护保养流程脚本。
    /// </summary>
    public class VR4_RotatableStepCompletionAdapter : VR4_BaseObject
    {
        #region ==========Field==========
        /// <summary>
        /// 需要监听的旋钮对象。为空时会在当前物体上自动获取。
        /// </summary>
        public VR4_RotatableObject rotatableObject;

        /// <summary>
        /// 旋钮完成目标值，范围为 0 到 1。
        /// </summary>
        [Range(0f, 1f)] public float targetValue = 1f;

        /// <summary>
        /// 允许的目标误差。
        /// </summary>
        [Range(0f, 1f)] public float tolerance = 0.05f;
        #endregion

        #region ==========Unity Method==========
        private void Reset()
        {
            rotatableObject = GetComponent<VR4_RotatableObject>();
        }

        private void OnEnable()
        {
            if (rotatableObject != null)
            {
                rotatableObject.onValueChange.AddListener(OnRotatableValueChanged);
            }
        }

        private void OnDisable()
        {
            if (rotatableObject != null)
            {
                rotatableObject.onValueChange.RemoveListener(OnRotatableValueChanged);
            }
        }
        #endregion

        #region ==========Logic==========
        private void OnRotatableValueChanged(float value)
        {
            if (Mathf.Abs(value - targetValue) <= tolerance)
            {
                CompleteStep();
            }
        }
        #endregion

        #region ==========API==========
        #endregion
    }

    /// <summary>
    /// SwitchObject 的统一完成源适配器。
    /// 不修改 SwitchObject 本体，通过监听开关开/关事件触发 CompleteStep。
    /// </summary>
    /// <summary>
    /// VR4_SwitchStepCompletionAdapter 类型说明
    /// 
    /// 【功能说明】
    /// 1. 属于模块4 - 维护保养中的 VR4_SwitchStepCompletionAdapter 类型。
    /// 2. 负责统一步骤完成锁、完成事件和自定义任务接入。
    /// 3. 与维护保养流程中的步骤数据、交互逻辑或 UI 显示协同工作。
    /// 
    /// 【依赖组件】
    /// - Unity 组件体系。
    /// - VRHelmet.VRTeam.Maintenance 维护保养流程脚本。
    /// </summary>
    public class VR4_SwitchStepCompletionAdapter : VR4_BaseObject
    {
        #region ==========Field==========
        /// <summary>
        /// 需要监听的开关对象。为空时会在当前物体上自动获取。
        /// </summary>
        public VR4_SwitchObject switchObject;

        /// <summary>
        /// 为 true 时开关打开完成步骤；为 false 时开关闭合完成步骤。
        /// </summary>
        public bool completeOnOpen = true;
        #endregion

        #region ==========Unity Method==========
        private void Reset()
        {
            switchObject = GetComponent<VR4_SwitchObject>();
        }

        private void OnEnable()
        {
            if (switchObject == null)
            {
                return;
            }

            switchObject.onLeverActivate.AddListener(OnSwitchOpened);
            switchObject.onLeverDeactivate.AddListener(OnSwitchClosed);
        }

        private void OnDisable()
        {
            if (switchObject == null)
            {
                return;
            }

            switchObject.onLeverActivate.RemoveListener(OnSwitchOpened);
            switchObject.onLeverDeactivate.RemoveListener(OnSwitchClosed);
        }
        #endregion

        #region ==========Logic==========
        private void OnSwitchOpened()
        {
            if (completeOnOpen)
            {
                CompleteStep();
            }
        }

        private void OnSwitchClosed()
        {
            if (!completeOnOpen)
            {
                CompleteStep();
            }
        }
        #endregion

        #region ==========API==========
        #endregion
    }
}

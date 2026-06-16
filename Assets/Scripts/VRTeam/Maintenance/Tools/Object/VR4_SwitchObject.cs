using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

// ============================================================
// 文件名：VR_SwitchObject
// 模块：模块4 - 维护保养
// 功能：XR 开关交互物体，负责根据手柄交互驱动开关值与开关事件。
// 创建日期：2026-05-5
// 最后更新：2026-05-5
// ============================================================

namespace VRHelmet.VRTeam.Maintenance
{
    /// <summary>
    /// 开关类。
    /// </summary>
    /// <summary>
    /// VR_SwitchObject 类型说明
    /// 
    /// 【功能说明】
    /// 1. 属于模块4 - 维护保养中的 VR_SwitchObject 类型。
    /// 2. 负责根据手柄交互驱动开关值与开关事件。
    /// 3. 与维护保养流程中的步骤数据、交互逻辑或 UI 显示协同工作。
    /// 
    /// 【依赖组件】
    /// - Unity 组件体系。
    /// - VRHelmet.VRTeam.Maintenance 维护保养流程脚本。
    /// </summary>
    public class VR4_SwitchObject : XRBaseInteractable
    {
        #region ==========Field==========
        const float k_LeverDeadZone = 0.1f;

        [SerializeField]
        [Tooltip("The object that is visually grabbed and manipulated")]
        Transform m_Handle = null;

        [SerializeField]
        [Tooltip("The value of the lever")]
        bool m_Value = false;

        [SerializeField]
        [Tooltip("If enabled, the lever will snap to the value position when released")]
        bool m_LockToValue;

        [SerializeField]
        [Tooltip("If enabled, selecting this switch with an XR Ray Interactor will immediately set the switch to the on value")]
        bool m_OpenOnRaySelect;

        [SerializeField]
        [Tooltip("Local axis used to rotate and evaluate the lever")]
        SwitchLocalAxis m_LocalAxis = SwitchLocalAxis.X;

        [SerializeField]
        [Tooltip("Angle of the lever in the 'on' position")]
        [Range(-90.0f, 90.0f)]
        float m_MaxAngle = 90.0f;

        [SerializeField]
        [Tooltip("Angle of the lever in the 'off' position")]
        [Range(-90.0f, 90.0f)]
        float m_MinAngle = -90.0f;

        [SerializeField]
        [Tooltip("Events to trigger when the lever activates")]
        UnityEvent m_OnLeverActivate = new UnityEvent();

        [SerializeField]
        [Tooltip("Events to trigger when the lever deactivates")]
        UnityEvent m_OnLeverDeactivate = new UnityEvent();

        [Header("Switch Trigger")]
        public Action OnSwitchOpenCompleted;
        public Action OnSwitchCloseCompleted;
        public Action OnSwitchTargetCompleted;
        public bool targetSwitchOpen;
        public bool closeAfterOpenBeforeComplete;

        IXRSelectInteractor m_Interactor;
        VR4_BaseObject permissionSource;
        VR4_FunctionalComponents functionalPermissionSource;
        bool hasReachedTargetBeforeReturn;
        bool isRayInstantSelect;

        /// <summary>
        /// 可视化拉杆手柄。
        /// </summary>
        public Transform handle
        {
            get => m_Handle;
            set => m_Handle = value;
        }

        /// <summary>
        /// 拉杆当前开关值。
        /// </summary>
        public bool value
        {
            get => m_Value;
            set => SetValue(value, true);
        }

        /// <summary>
        /// 释放后是否吸附到当前开关值对应角度。
        /// </summary>
        public bool lockToValue
        {
            get => m_LockToValue;
            set => m_LockToValue = value;
        }

        public bool openOnRaySelect
        {
            get => m_OpenOnRaySelect;
            set => m_OpenOnRaySelect = value;
        }

        public SwitchLocalAxis localAxis
        {
            get => m_LocalAxis;
            set => m_LocalAxis = value;
        }

        /// <summary>
        /// 开启状态对应角度。
        /// </summary>
        public float maxAngle
        {
            get => m_MaxAngle;
            set => m_MaxAngle = value;
        }

        /// <summary>
        /// 关闭状态对应角度。
        /// </summary>
        public float minAngle
        {
            get => m_MinAngle;
            set => m_MinAngle = value;
        }

        /// <summary>
        /// 拉杆切换到开启状态时触发的事件。
        /// </summary>
        public UnityEvent onLeverActivate => m_OnLeverActivate;

        /// <summary>
        /// 拉杆切换到关闭状态时触发的事件。
        /// </summary>
        public UnityEvent onLeverDeactivate => m_OnLeverDeactivate;
        #endregion

        #region ==========Unity Method==========
        void Start()
        {
            SetValue(m_Value, true);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            selectEntered.RemoveListener(StartGrab);
            selectEntered.AddListener(StartGrab);
        }

        protected override void OnDisable()
        {
            selectEntered.RemoveListener(StartGrab);
            base.OnDisable();
        }

        void OnDrawGizmosSelected()
        {
            var angleStartPoint = transform.position;

            if (m_Handle != null)
                angleStartPoint = m_Handle.position;

            const float k_AngleLength = 0.25f;

            var angleMaxPoint = angleStartPoint + transform.TransformDirection(GetAxisRotation(m_MaxAngle) * GetGizmoDirection()) * k_AngleLength;
            var angleMinPoint = angleStartPoint + transform.TransformDirection(GetAxisRotation(m_MinAngle) * GetGizmoDirection()) * k_AngleLength;

            Gizmos.color = Color.green;
            Gizmos.DrawLine(angleStartPoint, angleMaxPoint);

            Gizmos.color = Color.red;
            Gizmos.DrawLine(angleStartPoint, angleMinPoint);
        }

        void OnValidate()
        {
            SetHandleAngle(m_Value ? m_MaxAngle : m_MinAngle);
        }
        #endregion

        #region ==========Logic==========
        VR4_BaseObject GetPermissionSource()
        {
            if (permissionSource == null)
            {
                permissionSource = GetComponent<VR4_BaseObject>();
            }

            return permissionSource;
        }

        bool CanInteractByStepLayer(IXRInteractor interactor, string actionName)
        {
            VR4_BaseObject source = GetPermissionSource();
            if (source != null)
            {
                return source.CanInteract(interactor as XRBaseInteractor, actionName);
            }

            if (functionalPermissionSource == null)
            {
                functionalPermissionSource = GetComponent<VR4_FunctionalComponents>();
            }

            return functionalPermissionSource == null || functionalPermissionSource.IsInteractorAllowed(interactor, actionName);
        }

        void StartGrab(SelectEnterEventArgs args)
        {
            isRayInstantSelect = m_OpenOnRaySelect && args.interactorObject is XRRayInteractor;

            if (isRayInstantSelect)
            {
                m_Interactor = null;
                SetValue(!m_Value, true);
                return;
            }

            m_Interactor = args.interactorObject;
        }

        Vector3 GetLookDirection()
        {
            Vector3 direction = m_Interactor.GetAttachTransform(this).position - m_Handle.position;
            direction = transform.InverseTransformDirection(direction);

            switch (m_LocalAxis)
            {
                case SwitchLocalAxis.X:
                    direction.x = 0;
                    break;
                case SwitchLocalAxis.Y:
                    direction.y = 0;
                    break;
                case SwitchLocalAxis.Z:
                    direction.z = 0;
                    break;
            }

            return direction.normalized;
        }

        float GetLookAngle(Vector3 lookDirection)
        {
            switch (m_LocalAxis)
            {
                case SwitchLocalAxis.Y:
                    return Mathf.Atan2(lookDirection.x, lookDirection.z) * Mathf.Rad2Deg;
                case SwitchLocalAxis.Z:
                    return Mathf.Atan2(-lookDirection.x, lookDirection.y) * Mathf.Rad2Deg;
                default:
                    return Mathf.Atan2(lookDirection.z, lookDirection.y) * Mathf.Rad2Deg;
            }
        }

        void UpdateValue()
        {
            var lookDirection = GetLookDirection();
            var lookAngle = GetLookAngle(lookDirection);

            if (m_MinAngle < m_MaxAngle)
                lookAngle = Mathf.Clamp(lookAngle, m_MinAngle, m_MaxAngle);
            else
                lookAngle = Mathf.Clamp(lookAngle, m_MaxAngle, m_MinAngle);

            var maxAngleDistance = Mathf.Abs(m_MaxAngle - lookAngle);
            var minAngleDistance = Mathf.Abs(m_MinAngle - lookAngle);

            if (m_Value)
                maxAngleDistance *= (1.0f - k_LeverDeadZone);
            else
                minAngleDistance *= (1.0f - k_LeverDeadZone);

            var newValue = (maxAngleDistance < minAngleDistance);

            SetHandleAngle(lookAngle);

            SetValue(newValue);
        }

        void SetValue(bool isOn, bool forceRotation = false)
        {
            if (m_Value == isOn)
            {
                if (forceRotation)
                    SetHandleAngle(m_Value ? m_MaxAngle : m_MinAngle);

                return;
            }

            m_Value = isOn;

            if (m_Value)
            {
                m_OnLeverActivate.Invoke();
                HandleSwitchOpened();
            }
            else
            {
                m_OnLeverDeactivate.Invoke();
                HandleSwitchClosed();
            }

            if (m_LockToValue || forceRotation)
                SetHandleAngle(m_Value ? m_MaxAngle : m_MinAngle);

            HandleSwitchTargetStateChanged();
        }

        void SetHandleAngle(float angle)
        {
            if (m_Handle != null)
                m_Handle.localRotation = GetAxisRotation(angle);
        }

        Quaternion GetAxisRotation(float angle)
        {
            switch (m_LocalAxis)
            {
                case SwitchLocalAxis.Y:
                    return Quaternion.Euler(0.0f, angle, 0.0f);
                case SwitchLocalAxis.Z:
                    return Quaternion.Euler(0.0f, 0.0f, angle);
                default:
                    return Quaternion.Euler(angle, 0.0f, 0.0f);
            }
        }

        Vector3 GetGizmoDirection()
        {
            return m_LocalAxis == SwitchLocalAxis.Y ? Vector3.forward : Vector3.up;
        }
        #endregion

        #region ==========Switch Trigger==========
        void HandleSwitchOpened()
        {
            OnSwitchOpenCompleted?.Invoke();
        }

        void HandleSwitchClosed()
        {
            OnSwitchCloseCompleted?.Invoke();
        }

        void HandleSwitchTargetStateChanged()
        {
            if (!closeAfterOpenBeforeComplete)
            {
                if (m_Value == targetSwitchOpen)
                {
                    OnSwitchTargetCompleted?.Invoke();
                }

                return;
            }

            if (!hasReachedTargetBeforeReturn)
            {
                if (m_Value == targetSwitchOpen)
                {
                    hasReachedTargetBeforeReturn = true;
                }

                return;
            }

            if (m_Value == !targetSwitchOpen)
            {
                OnSwitchTargetCompleted?.Invoke();
            }
        }
        #endregion

        #region ==========API==========
        public void ConfigureSwitchTarget(bool targetOpen, bool closeAfterOpenBefore)
        {
            targetSwitchOpen = targetOpen;
            closeAfterOpenBeforeComplete = closeAfterOpenBefore;
            hasReachedTargetBeforeReturn = false;
        }

        public void ClearSwitchTarget()
        {
            OnSwitchTargetCompleted = null;
            targetSwitchOpen = false;
            closeAfterOpenBeforeComplete = false;
            hasReachedTargetBeforeReturn = false;
        }

        public override bool IsHoverableBy(IXRHoverInteractor interactor)
        {
            return base.IsHoverableBy(interactor) && CanInteractByStepLayer(interactor, "Hover");
        }

        public override bool IsSelectableBy(IXRSelectInteractor interactor)
        {
            return base.IsSelectableBy(interactor) && CanInteractByStepLayer(interactor, "Select");
        }

        /// <summary>
        /// 根据 XR Interaction Toolkit 更新阶段处理拉杆交互。
        /// </summary>
        /// <param name="updatePhase">当前交互更新阶段。</param>
        public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            base.ProcessInteractable(updatePhase);

            if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
            {
                if (isSelected && !isRayInstantSelect && m_Interactor != null)
                {
                    UpdateValue();
                }
            }
        }
        #endregion
    }

    public enum SwitchLocalAxis
    {
        X,
        Y,
        Z
    }
}

using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

// ============================================================
// 文件名：VR6_CameraOrbit
// 模块：模块6 - 三体式飞行汽车展示
// 功能：制摄像机从不同角度展示三体式飞行汽车控
// 创建日期：2026-04-28
// 最后更新：2026-04-29
// ============================================================

namespace VRHelmet.VRTeam.Manufacturing.VehicleTest.ThreeBodyCar
{
    public class VR6_CameraOrbit : MonoBehaviour
    {
        /// <summary>
        /// VR视角目标环绕控制器
        /// 
        /// 【功能说明】
        /// 1. 维护一个Target目标物体，支持运行时动态切换
        /// 2. Target改变后立即刷新头显视角，使其朝向目标
        /// 3. 通过PICO手柄摇杆输入，控制视角绕目标旋转
        /// 4. 约束俯仰角范围，避免视角翻转或过度抬头低头
        /// 
        /// 【依赖组件】
        /// - XROrigin：用于移动/旋转XR原点以驱动头显视角
        /// - Camera（XROrigin内部）：读取当前头显位置与朝向
        /// - Unity XR InputDevice：读取左右手柄 primary2DAxis 摇杆输入
        /// </summary>
        [Header("XR")]
        [SerializeField] private InputActionReference primary2DAxisAction;
        [SerializeField] private InputActionReference triggerBtn;
        [SerializeField] private InputActionReference grabBtn;
        [SerializeField] private InputActionReference aBtn;

        private InputAction cachedAxisAction;
        private InputAction cachedTriggerAction;
        private InputAction cachedGrabAction;
        private InputAction cachedAAction;

        [Header("Target")]
        [SerializeField] private Transform target;
        [SerializeField] private float deadZone = 0.15f;

        [Header("Target Move")]
        [SerializeField] private float targetMoveSpeed = 0.2f;
        [SerializeField] private float targetTurnSpeed = 90f;

        private Vector3 targetInitialPosition;
        private Quaternion targetInitialRotation;
        private Transform movingTarget;

        [Header("VR6 Modules")]
        [SerializeField] private Transform DiPan;
        [SerializeField] private Transform FeiXingQi;
        [SerializeField] private Transform JiCang;

        [Header("Play Image")]
        [SerializeField] private GameObject playImage;

        [Header("FeiXingQi Takeoff")]
        [SerializeField] private float feiXingQiLiftSpeed = 0.1f;

        [Header("Auto Rotate Parts")]
        [SerializeField] private List<VR6_AxisRotateConstraint> feiXingQiBladeRotators = new List<VR6_AxisRotateConstraint>();
        [SerializeField] private List<VR6_AxisRotateConstraint> carWheelRotators = new List<VR6_AxisRotateConstraint>();

        private enum SelectedModule
        {
            None,
            DiPan,
            FeiXingQi,
            JiCang
        }

        private SelectedModule selectedModule = SelectedModule.None;

        private Transform hiddenModule;
        private Transform cameraTransform;

        private bool isTargetMoving;

        private void Awake()
        {
            cachedAxisAction = primary2DAxisAction != null ? primary2DAxisAction.action : null;
            cachedTriggerAction = triggerBtn != null ? triggerBtn.action : null;
            cachedGrabAction = grabBtn != null ? grabBtn.action : null;
            cachedAAction = aBtn != null ? aBtn.action : null;

            if (target != null)
            {
                movingTarget = target;
                targetInitialPosition = target.position;
                targetInitialRotation = target.rotation;
            }

            StopAllAutoRotators();
        }

        private void OnEnable()
        {
            cachedAxisAction?.Enable();
            cachedTriggerAction?.Enable();
            cachedGrabAction?.Enable();
            cachedAAction?.Enable();
        }

        private void OnDisable()
        {
            cachedAxisAction?.Disable();
            cachedTriggerAction?.Disable();
            cachedGrabAction?.Disable();
            cachedAAction?.Disable();
        }

        private void Update()
        {
            TryStopAndResetByGrab();
            UpdateTargetMove();
        }

        private void TryStopAndResetByGrab()
        {
            if (cachedGrabAction == null)
            {
                return;
            }

            if (!cachedGrabAction.WasPressedThisFrame())
            {
                return;
            }

            ResetMovingTargetPosition();
        }


        public void ActivateCarMode()
        {
            selectedModule = SelectedModule.DiPan;
            HideModule(FeiXingQi);
            StartTargetMove();

            SetPlayImageActive(true);
            SetRotatorsAutoRotate(carWheelRotators, true);
            SetRotatorsAutoRotate(feiXingQiBladeRotators, false);
        }

        public void ActivateFlightMode()
        {
            selectedModule = SelectedModule.FeiXingQi;
            HideModule(DiPan);
            StartTargetMove();

            SetPlayImageActive(true);
            SetRotatorsAutoRotate(feiXingQiBladeRotators, true);
            SetRotatorsAutoRotate(carWheelRotators, false);
        }

        public void ActivateCabinMode()
        {
            selectedModule = SelectedModule.JiCang;
            RestoreHiddenModule();
            StartTargetMove();

            SetPlayImageActive(false);
            StopAllAutoRotators();
        }

        private void TryResetByGrab()
        {
            if (cachedGrabAction == null)
            {
                return;
            }

            if (!cachedGrabAction.WasPressedThisFrame())
            {
                return;
            }

            ResetMovingTargetPosition();
        }

        private void StartTargetMove()
        {
            if (target == null)
            {
                return;
            }

            movingTarget = target;
            isTargetMoving = true;
        }

        private void HideModule(Transform module)
        {
            RestoreHiddenModule();

            if (module == null)
            {
                return;
            }

            hiddenModule = module;
            hiddenModule.gameObject.SetActive(false);
        }

        private void RestoreHiddenModule()
        {
            if (hiddenModule != null)
            {
                hiddenModule.gameObject.SetActive(true);
                hiddenModule = null;
            }
        }

        private void UpdateTargetMove()
        {
            if (!isTargetMoving || movingTarget == null)
            {
                return;
            }

            Vector3 targetPositionBefore = movingTarget.position;

            Vector2 axis = cachedAxisAction != null ? cachedAxisAction.ReadValue<Vector2>() : Vector2.zero;

            float moveInput = Mathf.Abs(axis.y) > deadZone ? axis.y : 0f;
            float turnInput = Mathf.Abs(axis.x) > deadZone ? axis.x : 0f;

            if (Mathf.Abs(turnInput) > 0f)
            {
                movingTarget.Rotate(0f, turnInput * targetTurnSpeed * Time.deltaTime, 0f, Space.World);
            }

            if (Mathf.Abs(moveInput) > 0f)
            {
                Vector3 deltaMove = movingTarget.forward * moveInput * targetMoveSpeed * Time.deltaTime;
                movingTarget.position += deltaMove;
            }

            if (selectedModule == SelectedModule.FeiXingQi && cachedTriggerAction != null && cachedTriggerAction.IsPressed())
            {
                movingTarget.position += Vector3.up * feiXingQiLiftSpeed * Time.deltaTime;
            }
        }

        private void ResetMovingTargetPosition()
        {
            if (target != null)
            {
                target.position = targetInitialPosition;
                target.rotation = targetInitialRotation;
            }

            movingTarget = target;
            isTargetMoving = false;

            RestoreHiddenModule();
            StopAllAutoRotators();
            SetPlayImageActive(false);

            selectedModule = SelectedModule.None;
        }

        private void SetPlayImageActive(bool active)
        {
            if (playImage != null)
            {
                playImage.SetActive(active);
            }
        }

        private void StopAllAutoRotators()
        {
            SetRotatorsAutoRotate(feiXingQiBladeRotators, false);
            SetRotatorsAutoRotate(carWheelRotators, false);
        }

        private void SetRotatorsAutoRotate(List<VR6_AxisRotateConstraint> rotators, bool enable)
        {
            if (rotators == null)
            {
                return;
            }

            for (int i = 0; i < rotators.Count; i++)
            {
                if (rotators[i] != null)
                {
                    rotators[i].SetAutoRotate(enable);
                }
            }
        }
    }
}
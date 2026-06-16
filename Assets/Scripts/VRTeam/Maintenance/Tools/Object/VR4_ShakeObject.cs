using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// ============================================================
// 文件名：ShakeObject
// 模块：模块4 - 维护保养
// 功能：摇晃交互物体，负责统计有效摇晃次数并在达标后完成步骤。
// 创建日期：2026-05-5
// 最后更新：2026-05-5
// ============================================================

namespace VRHelmet.VRTeam.Maintenance
{
    /// <summary>
    /// ShakeObject 类型说明
    /// 
    /// 【功能说明】
    /// 1. 属于模块4 - 维护保养中的 ShakeObject 类型。
    /// 2. 负责统计有效摇晃次数并在达标后完成步骤。
    /// 3. 与维护保养流程中的步骤数据、交互逻辑或 UI 显示协同工作。
    /// 
    /// 【依赖组件】
    /// - Unity 组件体系。
    /// - VRHelmet.VRTeam.Maintenance 维护保养流程脚本。
    /// </summary>
    public class VR4_ShakeObject : VR4_BaseObject
    {
        #region ==========Field==========
        /// <summary>
        /// 完成摇晃任务所需的有效计数。
        /// </summary>
        public int targetCount = 4;

        /// <summary>
        /// 计为一次反向摇晃的最小角度。
        /// </summary>
        [Range(-30f, 0f)] public float minAngle = -25f;

        /// <summary>
        /// 计为一次正向摇晃的最大角度。
        /// </summary>
        [Range(0f, 30f)] public float maxAngle = 25f;

        [SerializeField] int successCount = 0;
        private int lastState = 0;
        private bool isShaking = false;

        private Quaternion initialRotation;
        private Coroutine shakeCoroutine;

        /// <summary>
        /// 摇晃次数达标后触发的完成回调。
        /// </summary>
        public System.Action OnShakeCompleted; // 摇晃完成时触发的事件

        /// <summary>
        /// 当前物体的抓取交互组件。
        /// </summary>
        public XRGrabInteractable grabInteractable; // 用于抓取交互的组件

        [SerializeField] bool isValid = false;
        #endregion

        #region ==========Unity Method==========
        void Start()
        {
            initialRotation = transform.rotation;
        }

        private void OnEnable()
        {
            if (grabInteractable != null)
            {
                grabInteractable.selectEntered.RemoveListener(OnGrabbed);
                grabInteractable.selectExited.RemoveListener(OnReleased);
                grabInteractable.selectEntered.AddListener(OnGrabbed);
                grabInteractable.selectExited.AddListener(OnReleased);
            }
        }

        private void OnDisable()
        {
            if (grabInteractable != null)
            {
                grabInteractable.selectEntered.RemoveListener(OnGrabbed);
                grabInteractable.selectExited.RemoveListener(OnReleased);
            }

            StopShaking();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Holo") && !isShaking)
            {
                
                StartShaking();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Holo"))
            {
                StopShaking();
            }
        }
        #endregion

        #region ==========Logic==========
        void OnGrabbed(SelectEnterEventArgs args)
        {
            ResetStepCompletion();
            ResetShakeState();
        }

        void OnReleased(SelectExitEventArgs args)
        {
            StopShaking();
            transform.rotation = initialRotation;
        }

        private void StartShaking()
        {
            if (isShaking) return;

            isShaking = true;
            shakeCoroutine = StartCoroutine(ShakeRoutine());
        }

        private void StopShaking()
        {
            isShaking = false;

            if (shakeCoroutine != null)
            {
                StopCoroutine(shakeCoroutine);
                shakeCoroutine = null;
            }
        }


        void ResetShakeState()
        {
            successCount = 0;
            lastState = 0;
            StopShaking();
        }

        IEnumerator ShakeRoutine()
        {
            while (successCount < targetCount)
            {

                // 获取当前x轴旋转（本地欧拉角）
                float currentX = transform.eulerAngles.z;

                if (IsHoldingVertically())
                {
                    // 检测穿越-5和5度
                    CheckRotation();
                }
                else
                {
                    VR4_UIManager.ShowTip("");
                }
                yield return null;
            }

            //完成步骤
            if (successCount >= targetCount)
            {
                CompleteStep(OnShakeCompleted);
                OutputValidity("打孔完成");
            }

            // 恢复原始旋转
            transform.rotation = initialRotation;
            shakeCoroutine = null;
            ResetShakeState();
        }

        /// <summary>
        /// 检测是否垂直拿握打孔器
        /// </summary>
        /// <param name="maxTiltAngle">最大倾斜角度，默认70度</param>
        /// <returns>如果倾斜角度小于等于指定角度，返回true</returns>
        private bool IsHoldingVertically()
        {
            // 获取当前本地欧拉角
            float localEuler = transform.localEulerAngles.x;

            // 获取X轴和Z轴的旋转角度（转换为-180到180范围）
            float xAngle = NormalizeAngle(localEuler);

            float angleDifference = Mathf.Abs(xAngle - -90f);

            // 检查倾斜角度是否在允许范围内
            return angleDifference <= 30;
        }

        /// <summary>
        /// 将角度规范化到-180到180范围
        /// </summary>
        private float NormalizeAngle(float angle)
        {
            angle %= 360f;
            if (angle > 180f) angle -= 360f;
            if (angle < -180f) angle += 360f;
            return angle;
        }

        void CheckRotation()
        {
            // 获取当前x轴旋转（本地欧拉角）
            float curAngle = transform.localEulerAngles.z;

            // 修正角度到[-180,180]
            //if (curAngle > 180f) curAngle -= 360f;
            NormalizeAngle(curAngle);

            if (curAngle <= minAngle && lastState != -1)
            {
                successCount++;
                lastState = -1;
            }
            else if (curAngle >= maxAngle && lastState != 1)
            {
                successCount++;
                lastState = 1;
            }
            else if (curAngle > minAngle && curAngle < maxAngle)
            {
                lastState = 0;
            }
        }

        void OutputValidity(string info)
        {
            if (!isValid) return;
            Debug.Log(info);
        }
        #endregion

        #region ==========API==========
        #endregion
    }
}

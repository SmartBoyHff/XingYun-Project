using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ============================================================
// 文件名：VR6_AxisRotateConstraint
// 模块：模块6 - 三体式飞行汽车展示
// 功能：车轮、螺旋桨等部件单轴旋转
// 创建日期：2026-04-28
// 最后更新：2026-04-29
// ============================================================

namespace VRHelmet.VRTeam.Manufacturing.VehicleTest.ThreeBodyCar
{
    public class VR6_AxisRotateConstraint : MonoBehaviour
    {
        /// <summary>
        /// 轮子单轴旋转约束器
        /// 
        /// 【功能说明】
        /// 1. 允许轮子仅绕预选本地轴（X/Y/Z）旋转
        /// 2. 每帧锁定其余两个轴的旋转分量，防止偏转
        /// 3. 提供绝对角度设置与增量旋转接口，便于外部驱动
        /// 4. 支持重绑定基准姿态，适配运行时重新校准
        /// 
        /// 【依赖组件】
        /// - Transform：读写本地旋转并应用单轴约束
        /// - （可选）外部驱动脚本：调用 SetAxisAngle/AddAxisAngle 控制转动
        /// </summary>

        [Header("允许旋转的本地轴")]
        [SerializeField] private Axis freeAxis = Axis.X;

        [Header("初始角度偏移(度)")]
        [SerializeField] private float initialAngleOffset = 0f;

        [Header("自动旋转")]
        [SerializeField] private bool autoRotateOnStart = false;
        [SerializeField] private float autoRotateSpeedDegPerSec = 180f; // 度/秒

        // 记录初始姿态，用来保持另外两个轴不变
        private Quaternion baseLocalRotation;

        // 当前沿自由轴的累计角度
        private float axisAngle;

        private bool autoRotating;

        private void Awake()
        {
            baseLocalRotation = transform.localRotation;
            axisAngle = initialAngleOffset;
            autoRotating = autoRotateOnStart;
            ApplyRotation();
        }

        private void Update()
        {
            if (autoRotating)
            {
                axisAngle += autoRotateSpeedDegPerSec * Time.deltaTime;
            }
        }

        private void LateUpdate()
        {
            // 即使有外部脚本/物理修改了旋转，这里也会在每帧末强制约束回单轴
            ApplyRotation();
        }

        /// <summary>
        /// 外部可调用：设置轮子沿自由轴的绝对角度(度)
        /// </summary>
        public void SetAxisAngle(float angleDeg)
        {
            axisAngle = angleDeg;
            ApplyRotation();
        }

        /// <summary>
        /// 外部可调用：在当前角度基础上增量旋转(度)
        /// </summary>
        public void AddAxisAngle(float deltaDeg)
        {
            axisAngle += deltaDeg;
            ApplyRotation();
        }

        /// <summary>
        /// 外部可调用：重置基准姿态（例如你手动摆好轮子后调用）
        /// </summary>
        public void RebindCurrentAsBase()
        {
            baseLocalRotation = transform.localRotation;
            axisAngle = 0f;
            ApplyRotation();
        }

        /// <summary>
        /// 开启/关闭自动旋转
        /// </summary>
        public void SetAutoRotate(bool enable)
        {
            autoRotating = enable;
        }

        /// <summary>
        /// 切换自动旋转开关（返回切换后状态）
        /// </summary>
        public bool ToggleAutoRotate()
        {
            autoRotating = !autoRotating;
            return autoRotating;
        }

        private void ApplyRotation()
        {
            Vector3 localAxis = GetLocalAxisVector(freeAxis);
            Quaternion spin = Quaternion.AngleAxis(axisAngle, localAxis);
            transform.localRotation = baseLocalRotation * spin;
        }

        private static Vector3 GetLocalAxisVector(Axis axis)
        {
            switch (axis)
            {
                case Axis.X: return Vector3.right;
                case Axis.Y: return Vector3.up;
                default: return Vector3.forward;
            }
        }


    }

    public enum Axis
    {
        X, Y, Z
    }
}
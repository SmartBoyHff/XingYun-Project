using System;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

namespace UnityEngine.XR.Content.Interaction
{
    /// <summary>
    /// An interactable knob that follows the rotation of the interactor
    /// </summary>
    public class XRKnob : XRBaseInteractable
    {
        /// <summary>
        /// An interactable knob that follows the rotation of the interactor
        /// </summary>
      
            const float k_ModeSwitchDeadZone = 0.1f; // Prevents rapid switching between the different rotation tracking modes

            public enum SliderAxis
            {
                X,
                Y,
                Z
            }
            [SerializeField]
            [Tooltip("选择滑块移动的轴")]
            private SliderAxis m_SliderAxis = SliderAxis.Z;

            [SerializeField]
            [Tooltip("辅助线的缩放比例")]
            private float m_GizmoScale = 1.0f;

            [SerializeField]
            [Tooltip("射线抓取位置更改")]
            private Transform m_H;
            /// <summary>
            /// 辅助线的缩放比例
            /// </summary>
            public float gizmoScale
            {
                get => m_GizmoScale;
                set => m_GizmoScale = Mathf.Max(0.1f, value); // 限制最小缩放为0.1
            }

            /// <summary>
            /// Helper class used to track rotations that can go beyond 180 degrees while minimizing accumulation error
            /// </summary>
            struct TrackedRotation
            {
                /// <summary>
                /// The anchor rotation we calculate an offset from
                /// </summary>
                float m_BaseAngle;

                /// <summary>
                /// The target rotate we calculate the offset to
                /// </summary>
                float m_CurrentOffset;

                /// <summary>
                /// Any previous offsets we've added in
                /// </summary>
                float m_AccumulatedAngle;

                /// <summary>
                /// The total rotation that occurred from when this rotation started being tracked
                /// </summary>
                public float totalOffset => m_AccumulatedAngle + m_CurrentOffset;

                /// <summary>
                /// Resets the tracked rotation so that total offset returns 0
                /// </summary>
                public void Reset()
                {
                    m_BaseAngle = 0.0f;
                    m_CurrentOffset = 0.0f;
                    m_AccumulatedAngle = 0.0f;
                }


                /// <summary>
                /// Sets a new anchor rotation while maintaining any previously accumulated offset
                /// </summary>
                /// <param name="direction">The XZ vector used to calculate a rotation angle</param>
                public void SetBaseFromVector(Vector3 direction)
                {
                    // Update any accumulated angle
                    m_AccumulatedAngle += m_CurrentOffset;

                    // Now set a new base angle
                    m_BaseAngle = Mathf.Atan2(direction.z, direction.x) * Mathf.Rad2Deg;
                    m_CurrentOffset = 0.0f;
                }

                public void SetTargetFromVector(Vector3 direction)
                {
                    // Set the target angle
                    var targetAngle = Mathf.Atan2(direction.z, direction.x) * Mathf.Rad2Deg;

                    // Return the offset
                    m_CurrentOffset = ShortestAngleDistance(m_BaseAngle, targetAngle, 360.0f);

                    // If the offset is greater than 90 degrees, we update the base so we can rotate beyond 180 degrees
                    if (Mathf.Abs(m_CurrentOffset) > 90.0f)
                    {
                        m_BaseAngle = targetAngle;
                        m_AccumulatedAngle += m_CurrentOffset;
                        m_CurrentOffset = 0.0f;
                    }
                }
            }

            [Serializable]
            public class ValueChangeEvent : UnityEvent<float> { }

            [SerializeField]
            [Tooltip("旋转对象")]
            Transform m_Handle = null;

            [SerializeField]
            [Tooltip("The value of the knob")]
            [Range(0.0f, 1.0f)]
            float m_Value = 0.5f;

            [SerializeField]
            [Tooltip("Whether this knob's rotation should be clamped by the angle limits")]
            bool m_ClampedMotion = true;

            [SerializeField]
            [Tooltip("Rotation of the knob at value '1'")]
            float m_MaxAngle = 90.0f;

            [SerializeField]
            [Tooltip("Rotation of the knob at value '0'")]
            float m_MinAngle = -90.0f;

            [SerializeField]
            [Tooltip("Angle increments to support, if greater than '0'")]
            float m_AngleIncrement = 0.0f;

            [SerializeField]
            [Tooltip("The position of the interactor controls rotation when outside this radius")]
            float m_PositionTrackedRadius = 0.1f;

            [SerializeField]
            [Tooltip("How much controller rotation ")]
            float m_TwistSensitivity = 1.5f;

            [SerializeField]
            [Tooltip("Events to trigger when the knob is rotated")]
            ValueChangeEvent m_OnValueChange = new ValueChangeEvent();

            IXRSelectInteractor m_Interactor;

            bool m_PositionDriven = false;
            bool m_UpVectorDriven = false;

            TrackedRotation m_PositionAngles = new TrackedRotation();
            TrackedRotation m_UpVectorAngles = new TrackedRotation();
            TrackedRotation m_ForwardVectorAngles = new TrackedRotation();

            float m_BaseKnobRotation = 0.0f;

            /// <summary>
            /// The object that is visually grabbed and manipulated
            /// </summary>
            public Transform rotH
            {
                get => m_H;
                set => m_H = value;
            }

            /// <summary>
            /// The object that is visually grabbed and manipulated
            /// </summary>
            public Transform handle
            {
                get => m_Handle;
                set => m_Handle = value;
            }

            /// <summary>
            /// The value of the knob
            /// </summary>
            public float value
            {
                get => m_Value;
                set
                {
                    SetValue(value);
                    SetKnobRotation(ValueToRotation());
                }
            }

            /// <summary>
            /// Whether this knob's rotation should be clamped by the angle limits
            /// </summary>
            public bool clampedMotion
            {
                get => m_ClampedMotion;
                set => m_ClampedMotion = value;
            }


            /// <summary>
            /// Rotation of the knob at value '1'
            /// </summary>
            public float maxAngle
            {
                get => m_MaxAngle;
                set => m_MaxAngle = value;
            }

            /// <summary>
            /// Rotation of the knob at value '0'
            /// </summary>
            public float minAngle
            {
                get => m_MinAngle;
                set => m_MinAngle = value;
            }

            /// <summary>
            /// The position of the interactor controls rotation when outside this radius
            /// </summary>
            public float positionTrackedRadius
            {
                get => m_PositionTrackedRadius;
                set => m_PositionTrackedRadius = value;
            }

            /// <summary>
            /// Events to trigger when the knob is rotated
            /// </summary>
            public ValueChangeEvent onValueChange => m_OnValueChange;

            public SliderAxis sliderAxis
            {
                get => m_SliderAxis;
                set
                {
                    m_SliderAxis = value;
                    // 保存当前非目标轴的旋转值

                    SetKnobRotation(ValueToRotation());
                }
            }

            void Start()
            {
                SetValue(m_Value);
                SetKnobRotation(ValueToRotation());
            }

            protected override void OnEnable()
            {
                base.OnEnable();
                selectEntered.AddListener(StartGrab);
                selectExited.AddListener(EndGrab);
            }

            protected override void OnDisable()
            {
                selectEntered.RemoveListener(StartGrab);
                selectExited.RemoveListener(EndGrab);
                base.OnDisable();
            }

            void StartGrab(SelectEnterEventArgs args)
            {
                m_Interactor = args.interactorObject;

                m_PositionAngles.Reset();
                m_UpVectorAngles.Reset();
                m_ForwardVectorAngles.Reset();

                UpdateBaseKnobRotation();
                UpdateRotation(true);
            }

            void EndGrab(SelectExitEventArgs args)
            {
                m_Interactor = null;
            }

            public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
            {
                base.ProcessInteractable(updatePhase);

                if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
                {
                    if (isSelected)
                    {
                        UpdateRotation();
                    }
                }
            }

            void UpdateRotation(bool freshCheck = false)
            {
            var interactorTransform = m_Interactor.GetAttachTransform(this);
            var localOffset = transform.InverseTransformVector(interactorTransform.position - m_Handle.position);

            // ---- 根据旋转轴将偏移投影到相应平面 ----
            Vector3 projOffset;
            switch (m_SliderAxis)
            {
                case SliderAxis.X:
                    projOffset = new Vector3(0, localOffset.y, localOffset.z);
                    break;
                case SliderAxis.Y:
                    projOffset = new Vector3(localOffset.x, 0, localOffset.z);
                    break;
                case SliderAxis.Z:
                    projOffset = new Vector3(localOffset.x, localOffset.y, 0);
                    break;
                default:
                    projOffset = localOffset; // fallback (should not happen)
                    break;
            }

            // 半径和归一化使用投影向量
            var radiusOffset = projOffset.magnitude;
            if (radiusOffset > Mathf.Epsilon)
                projOffset.Normalize();
            else
                projOffset = Vector3.zero;

            // 保留原有的控制器方向跟踪（不依赖 m_SliderAxis，维持原逻辑）
            var localForward = transform.InverseTransformDirection(interactorTransform.forward);
            var localY = Math.Abs(localForward.y);
            localForward.y = 0.0f;
            localForward.Normalize();

            var localUp = transform.InverseTransformDirection(interactorTransform.up);
            localUp.y = 0.0f;
            localUp.Normalize();

            // ---- 位置驱动判断（使用投影半径） ----
            if (m_PositionDriven && !freshCheck)
                radiusOffset *= (1.0f + k_ModeSwitchDeadZone);

            if (radiusOffset >= m_PositionTrackedRadius)
            {
                if (!m_PositionDriven || freshCheck)
                {
                    m_PositionAngles.SetBaseFromVector(projOffset); // 传入投影方向
                    m_PositionDriven = true;
                }
            }
            else
                m_PositionDriven = false;

            // 方向驱动判断（不变）
            if (!freshCheck)
            {
                if (!m_UpVectorDriven)
                    localY *= (1.0f - (k_ModeSwitchDeadZone * 0.5f));
                else
                    localY *= (1.0f + (k_ModeSwitchDeadZone * 0.5f));
            }

            if (localY > 0.707f)
            {
                if (!m_UpVectorDriven || freshCheck)
                {
                    m_UpVectorAngles.SetBaseFromVector(localUp);
                    m_UpVectorDriven = true;
                }
            }
            else
            {
                if (m_UpVectorDriven || freshCheck)
                {
                    m_ForwardVectorAngles.SetBaseFromVector(localForward);
                    m_UpVectorDriven = false;
                }
            }

            // 更新角度（位置驱动使用投影方向）
            if (m_PositionDriven)
                m_PositionAngles.SetTargetFromVector(projOffset);

            if (m_UpVectorDriven)
                m_UpVectorAngles.SetTargetFromVector(localUp);
            else
                m_ForwardVectorAngles.SetTargetFromVector(localForward);

            // 计算总旋转
            var knobRotation = m_BaseKnobRotation
                               - ((m_UpVectorAngles.totalOffset + m_ForwardVectorAngles.totalOffset) * m_TwistSensitivity)
                               - m_PositionAngles.totalOffset;

            if (m_ClampedMotion)
                knobRotation = Mathf.Clamp(knobRotation, m_MinAngle, m_MaxAngle);

            SetKnobRotation(knobRotation);
            var knobValue = (knobRotation - m_MinAngle) / (m_MaxAngle - m_MinAngle);
            SetValue(knobValue);
        }

            void SetKnobRotation(float angle)
            {
                if (m_AngleIncrement > 0)
                {
                    var normalizeAngle = angle - m_MinAngle;
                    angle = (Mathf.Round(normalizeAngle / m_AngleIncrement) * m_AngleIncrement) + m_MinAngle;
                }

                if (m_Handle != null)
                {
                    // 保留当前旋转的其他轴角度，只修改目标轴
                    Vector3 currentRotation = m_Handle.localEulerAngles;
                    switch (m_SliderAxis)
                    {
                        case SliderAxis.X:
                            currentRotation.x = angle;
                            break;
                        case SliderAxis.Y:
                            currentRotation.y = angle;
                            break;
                        case SliderAxis.Z:
                            currentRotation.z = angle;
                            break;
                    }
                    m_Handle.localEulerAngles = currentRotation;
                }
            }

            void SetValue(float value)
            {
                if (m_ClampedMotion)
                    value = Mathf.Clamp01(value);

                if (m_AngleIncrement > 0)
                {
                    var angleRange = m_MaxAngle - m_MinAngle;
                    var angle = Mathf.Lerp(0.0f, angleRange, value);
                    angle = Mathf.Round(angle / m_AngleIncrement) * m_AngleIncrement;
                    value = Mathf.InverseLerp(0.0f, angleRange, angle);
                }

                m_Value = value;
                m_OnValueChange.Invoke(m_Value);
            }

            float ValueToRotation()
            {
                return m_ClampedMotion ? Mathf.Lerp(m_MinAngle, m_MaxAngle, m_Value) : Mathf.LerpUnclamped(m_MinAngle, m_MaxAngle, m_Value);
            }

            void UpdateBaseKnobRotation()
            {
                m_BaseKnobRotation = Mathf.LerpUnclamped(m_MinAngle, m_MaxAngle, m_Value);
            }

            static float ShortestAngleDistance(float start, float end, float max)
            {
                var angleDelta = end - start;
                var angleSign = Mathf.Sign(angleDelta);

                angleDelta = Math.Abs(angleDelta) % max;
                if (angleDelta > (max * 0.5f))
                    angleDelta = -(max - angleDelta);

                return angleDelta * angleSign;
            }

            void OnDrawGizmosSelected()
            {

                const int k_CircleSegments = 16;
                const float k_SegmentRatio = 1.0f / k_CircleSegments;

                if (m_PositionTrackedRadius <= Mathf.Epsilon)
                    return;

                var circleCenter = transform.position;
                if (m_Handle != null)
                    circleCenter = m_Handle.position;

                // 根据选择的旋转轴确定圆所在的平面（垂直于旋转轴）
                Vector3 circleX, circleY;
                switch (m_SliderAxis)
                {
                    case SliderAxis.X:
                        // 旋转轴为X时，圆在YZ平面
                        circleX = transform.up;       // Y方向
                        circleY = transform.forward;  // Z方向
                        break;
                    case SliderAxis.Y:
                        // 旋转轴为Y时，圆在XZ平面（默认）
                        circleX = transform.right;    // X方向
                        circleY = transform.forward;  // Z方向
                        break;
                    case SliderAxis.Z:
                        // 旋转轴为Z时，圆在XY平面
                        circleX = transform.right;    // X方向
                        circleY = transform.up;       // Y方向
                        break;
                    default:
                        circleX = transform.right;
                        circleY = transform.forward;
                        break;
                }

                Gizmos.color = Color.green;
                var segmentCounter = 0;
                while (segmentCounter < k_CircleSegments)
                {
                    var startAngle = (float)segmentCounter * k_SegmentRatio * 2.0f * Mathf.PI;
                    segmentCounter++;
                    var endAngle = (float)segmentCounter * k_SegmentRatio * 2.0f * Mathf.PI;

                    // 计算当前平面上的圆轮廓线，应用缩放比例
                    float scaledRadius = m_PositionTrackedRadius * m_GizmoScale;
                    Gizmos.DrawLine(
                        circleCenter + (Mathf.Cos(startAngle) * circleX + Mathf.Sin(startAngle) * circleY) * scaledRadius,
                        circleCenter + (Mathf.Cos(endAngle) * circleX + Mathf.Sin(endAngle) * circleY) * scaledRadius
                    );
                }

                // 绘制旋转轴指示线，长度应用缩放比例
                Vector3 axisDirection = m_SliderAxis switch
                {
                    SliderAxis.X => transform.right,
                    SliderAxis.Y => transform.up,
                    SliderAxis.Z => transform.forward,
                    _ => transform.up
                };
                Gizmos.color = Color.red; // 红色表示旋转轴
                Gizmos.DrawLine(circleCenter, circleCenter + axisDirection * m_PositionTrackedRadius * 1.2f * m_GizmoScale);
            }

            void OnValidate()
            {
                if (m_ClampedMotion)
                    m_Value = Mathf.Clamp01(m_Value);

                if (m_MinAngle > m_MaxAngle)
                    m_MinAngle = m_MaxAngle;

                SetKnobRotation(ValueToRotation());
            }
            public override Transform GetAttachTransform(IXRInteractor interactor)
            {
                if (m_H == null)
                {
                    return m_Handle.transform;
                }
                else
                {
                    return m_H.transform;
                }

            }
        }
    }


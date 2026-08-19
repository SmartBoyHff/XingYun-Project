using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System;
using UnityEngine.Events;

namespace UnityEngine.XR.Content.Interaction
{


    public class XRSlider_2 : XRBaseInteractable
    {
        public enum SliderAxis
        {
            X,
            Y,
            Z
        }

        [Serializable]
        public class ValueChangeEvent : UnityEvent<float> { }
        [SerializeField]
        [Tooltip("选择滑块移动的轴")]
        private SliderAxis m_SliderAxis = SliderAxis.Z;

        [SerializeField]
        [Tooltip("The object that is visually grabbed and manipulated")]
        private Transform m_Handle = null;

        [SerializeField]
        [Tooltip("The value of the slider")]
        [Range(0.0f, 1.0f)]
        private float m_Value = 0.5f;

        [SerializeField]
        [Tooltip("The offset of the slider at value '1' from handle's original position")]
        private float m_MaxOffset = 0.5f;

        [SerializeField]
        [Tooltip("The offset of the slider at value '0' from handle's original position")]
        private float m_MinOffset = -0.5f;

        [SerializeField]
        [Tooltip("Events to trigger when the slider is moved")]
        private ValueChangeEvent m_OnValueChange = new ValueChangeEvent();
        [SerializeField]
        private bool m_HandlePos;
        private IXRSelectInteractor m_Interactor;
        // 记录手柄初始的局部坐标
        private Vector3 m_HandPosition;

        /// <summary>
        /// 滑块的值
        /// </summary>
        public float value
        {
            get => m_Value;
            set
            {
                SetValue(value);
                SetSliderPosition(value);
            }
        }


        /// <summary>
        /// 滑块值改变时触发的事件
        /// </summary>
        public ValueChangeEvent onValueChange => m_OnValueChange;

        /// <summary>
        /// 选择滑块移动的轴
        /// </summary>
        public SliderAxis sliderAxis
        {
            get => m_SliderAxis;
            set
            {
                m_SliderAxis = value;
                // 轴改变后，重新设置滑块位置和值
                UpdateValueFromPosition();
                SetSliderPosition(m_Value);
            }
        }

        private void Start()
        {
            UpdateValueFromPosition();

            SetSliderPosition(m_Value);
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

        private void StartGrab(SelectEnterEventArgs args)
        {
            m_Interactor = args.interactorObject;
            UpdateSliderPosition();
        }

        private void EndGrab(SelectExitEventArgs args)
        {
            m_Interactor = null;
        }

        public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            base.ProcessInteractable(updatePhase);

            if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic && isSelected)
            {
                UpdateSliderPosition();
            }
        }

        private void UpdateSliderPosition()
        {
            if (m_Interactor == null || m_Handle == null) return;

            // 将交互器附加变换的世界坐标转换为当前滑块对象的局部坐标
            Vector3 localPosition = transform.InverseTransformPoint(m_Interactor.GetAttachTransform(this).position);
            float axisValue = 0f;

            // 根据选择的轴获取对应的值
            switch (m_SliderAxis)
            {
                case SliderAxis.X:
                    axisValue = localPosition.x;
                    break;
                case SliderAxis.Y:
                    axisValue = localPosition.y;
                    break;
                case SliderAxis.Z:
                    axisValue = localPosition.z;
                    break;
            }

            // 计算滑块值，基于手柄初始位置的偏移
            float minPos = m_HandPosition[(int)m_SliderAxis] + m_MinOffset;
            float maxPos = m_HandPosition[(int)m_SliderAxis] + m_MaxOffset;
            float sliderValue = Mathf.Clamp01((axisValue - minPos) / (maxPos - minPos));

            SetValue(sliderValue);
            SetSliderPosition(sliderValue);
        }

        private void SetSliderPosition(float value)
        {
            if (m_Handle == null) return;

            Vector3 handlePos = m_Handle.localPosition;
            float targetPos = Mathf.Lerp(
                m_HandPosition[(int)m_SliderAxis] + m_MinOffset,
                m_HandPosition[(int)m_SliderAxis] + m_MaxOffset,
                value
            );

            // 根据选择的轴设置对应坐标
            switch (m_SliderAxis)
            {
                case SliderAxis.X:
                    handlePos.x = targetPos;
                    break;
                case SliderAxis.Y:
                    handlePos.y = targetPos;
                    break;
                case SliderAxis.Z:
                    handlePos.z = targetPos;
                    break;
            }

            m_Handle.localPosition = handlePos;
            //Debug.Log($"Value={value}, 轴={m_SliderAxis}, 局部坐标: →{m_Handle.localPosition}, 世界坐标: {m_Handle.position}");
        }

        private void SetValue(float value)
        {
            m_Value = value;
            m_OnValueChange.Invoke(m_Value);
        }

        private void OnDrawGizmosSelected()
        {
            if (m_Handle == null) return;

            float minPos = m_HandPosition[(int)m_SliderAxis] + m_MinOffset;
            float maxPos = m_HandPosition[(int)m_SliderAxis] + m_MaxOffset;
            Vector3 sliderMinPoint = transform.TransformPoint(GetAxisVector(minPos));
            Vector3 sliderMaxPoint = transform.TransformPoint(GetAxisVector(maxPos));

            Gizmos.color = Color.green;
            Gizmos.DrawLine(sliderMinPoint, sliderMaxPoint);
        }

        // 根据轴和位置值生成对应的局部坐标向量
        private Vector3 GetAxisVector(float positionValue)
        {
            Vector3 vector = Vector3.zero;
            vector[(int)m_SliderAxis] = positionValue;
            return vector;
        }

        private void OnValidate()
        {
            if (m_Handle != null && !m_HandlePos)
            {
                m_HandPosition = m_Handle.localPosition;
                Debug.Log(m_Handle.localPosition);
                UpdateValueFromPosition();
            }
            SetSliderPosition(m_Value);
        }
        private void UpdateValueFromPosition()
        {
            if (m_Handle == null) return;

            float currentPos = m_Handle.localPosition[(int)m_SliderAxis];
            float minPos = m_HandPosition[(int)m_SliderAxis] + m_MinOffset;
            float maxPos = m_HandPosition[(int)m_SliderAxis] + m_MaxOffset;

            // 计算并设置值
            m_Value = Mathf.Clamp01((currentPos - minPos) / (maxPos - minPos));
        }
    }
}


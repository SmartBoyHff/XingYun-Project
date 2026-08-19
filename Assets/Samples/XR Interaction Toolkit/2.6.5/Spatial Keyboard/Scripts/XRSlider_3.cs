using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEditor;
using System.Runtime.InteropServices;

public class XRSlider_3 : XRBaseInteractable
{
    [Serializable]
    public class ValueChangeEvent : UnityEvent<float> { }
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
    [Tooltip("The value of the slider")]
    [Range(0.0f, 1.0f)]
    float m_Value = 0.5f;

    [SerializeField]
    [Tooltip("最大")]
    float m_MaxPosition = 0.5f;

    [SerializeField]
    [Tooltip("最小")]
    float m_MinPosition = -0.5f;

    [SerializeField]
    ValueChangeEvent m_OnValueChange = new ValueChangeEvent();

    IXRSelectInteractor m_Interactor;
    Vector3 m_InitialPosition;
    Vector3 m_InitialInteractorPosition;
    bool m_IsGrabbing = false;
    float m_SmoothingFactor = 0.2f;
    float m_LastValue = 0.5f;
    private Vector3 m_HandPosition;
    [SerializeField]
    private bool m_HandlePos;

    /// <summary>
    /// The value of the slider
    /// </summary>
    public float value
    {
        get => m_Value;
        set
        {
            SetValue(value);
            
            SetSelfPosition(value);
        }
    }

    /// <summary>
    /// Events to trigger when the slider is moved
    /// </summary>
    public ValueChangeEvent onValueChange => m_OnValueChange;

    void Start()
    {
        SetValue(m_Value);
        SetSelfPosition(m_Value);
        m_LastValue = m_Value;
    }
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
            UpdateSelfPosition();
            SetSelfPosition(m_Value);
        }
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
        m_InitialPosition = transform.localPosition;
        m_InitialInteractorPosition = m_Interactor.GetAttachTransform(this).position;
        m_IsGrabbing = true;
        m_LastValue = m_Value;
    }

    void EndGrab(SelectExitEventArgs args)
    {
        m_Interactor = null;
        m_IsGrabbing = false;
    }

    public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
    {
        base.ProcessInteractable(updatePhase);

        if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
        {
            if (isSelected && m_IsGrabbing)
            {
                UpdateSelfPosition();
            }
        }
    }

    void UpdateSelfPosition()
    {
        if (m_Interactor == null)
            return;

      
        Vector3 currentInteractorPosition = m_Interactor.GetAttachTransform(this).position;

      
        Vector3 interactorDelta = transform.InverseTransformPoint(currentInteractorPosition) -
                                  transform.InverseTransformPoint(m_InitialInteractorPosition);


        float deltaZ = 0;
           
        switch (m_SliderAxis)
        {
            case SliderAxis.X:
                deltaZ = interactorDelta.x;
                break;
            case SliderAxis.Y:
                deltaZ= interactorDelta.y;
                break;
            case SliderAxis.Z:
                deltaZ= interactorDelta.z;
                break;
        }



        float newSliderValue = Mathf.Clamp01(
            (m_InitialPosition.z + deltaZ - m_MinPosition) / (m_MaxPosition - m_MinPosition)
        );

      
        float smoothedValue = Mathf.Lerp(m_LastValue, newSliderValue, m_SmoothingFactor);

      
        if (Mathf.Abs(smoothedValue - m_Value) > 0.005f)
        {
            SetValue(smoothedValue);
            SetSelfPosition(smoothedValue);
            m_LastValue = smoothedValue;
        }
    }

    void SetSelfPosition(float value)
    {
        var pos = transform.localPosition;
        float targetPos = Mathf.Lerp(
                m_HandPosition[(int)m_SliderAxis] + m_MinPosition,
                m_HandPosition[(int)m_SliderAxis] + m_MaxPosition,
                value
            );
        switch (m_SliderAxis)
        {
            case SliderAxis.X:
                pos.x = targetPos;
                break;
            case SliderAxis.Y:
                pos.y = targetPos;
                break;
            case SliderAxis.Z:
                pos.z = targetPos;
                break;
        }
        transform.localPosition = pos;
    }

    void SetValue(float value)
    {
        m_Value = value;
        m_OnValueChange.Invoke(m_Value);
    }

    void OnDrawGizmosSelected()
    {
        float minPos = m_HandPosition[(int)m_SliderAxis] + m_MinPosition;
        float maxPos = m_HandPosition[(int)m_SliderAxis] + m_MaxPosition;
        var sliderMinPoint = transform.TransformPoint(GetAxisVector(minPos));
        var sliderMaxPoint = transform.TransformPoint(GetAxisVector(maxPos));

        Gizmos.color = Color.green;
        Gizmos.DrawLine(sliderMinPoint, sliderMaxPoint);

        // Draw current position
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.position, 0.02f);
    }
    private Vector3 GetAxisVector(float positionValue)
    {
        Vector3 vector = Vector3.zero;
        vector[(int)m_SliderAxis] = positionValue;
        return vector;
    }

    void OnValidate()
    {
        if (!m_HandlePos)
        {
            m_HandPosition = transform.localPosition;
            UpdateValueFromPosition();
        }
        else
        {
          
        }
        SetSelfPosition(m_Value);
    }
    private void UpdateValueFromPosition()
    {
        

        float currentPos = transform.localPosition[(int)m_SliderAxis];
        float minPos = m_HandPosition[(int)m_SliderAxis] + m_MinPosition;
        float maxPos = m_HandPosition[(int)m_SliderAxis] + m_MaxPosition;

        // 计算并设置值
        m_Value = Mathf.Clamp01((currentPos - minPos) / (maxPos - minPos));
    }
}

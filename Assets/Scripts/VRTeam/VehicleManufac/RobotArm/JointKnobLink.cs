using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Content.Interaction;

public class JointKnobLink : MonoBehaviour
{
    public XRKnob knob;             // 对应的旋钮
    [SerializeField] private Transform jointTransform; // 关节 Transform
    [SerializeField] private Vector3 rotationAxis = Vector3.right; // 旋转轴
    [SerializeField] private float minAngle = -90f;
    [SerializeField] private float maxAngle = 90f;
    [SerializeField] private int armIndex = 0;

    private void OnEnable()
    {
        if (knob != null)
            knob.onValueChange.AddListener(SetJointAngleFromKnob);
    }

    private void OnDisable()
    {
        if (knob != null)
            knob.onValueChange.RemoveListener(SetJointAngleFromKnob);
      
    }

    private void SetJointAngleFromKnob(float knobValue)
    {
        var recorder = ActionRecorder.GetRecorder(armIndex);
        if (recorder != null && recorder.IsPlaying)
            return;   // 回放中不响应旋钮

        float angle = Mathf.Lerp(minAngle, maxAngle, knobValue);
        jointTransform.localRotation = Quaternion.AngleAxis(angle, rotationAxis);   
    }


}

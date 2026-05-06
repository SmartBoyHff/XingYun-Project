using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class StepInteractable : MonoBehaviour
{
     [SerializeField] private string stepID; // 对应StepData的stepID，用于验证是否在当前步骤可交互
    public UnityEvent OnCorrectInteraction; // 验证通过后的额外操作（例如播放动画）

    public void TryInteract()
    {
        D_StepManager stepManager = D_StepManager.Instance;
        if (stepManager == null) return;

        D_StepData currentStep = stepManager.GetCurrentStep();
        if (currentStep == null || currentStep.stepID != stepID)
        {
            Debug.Log("当前步骤不能操作此物体");
            return;
        }

        if (stepManager.ValidateStepAction(out string msg))
        {
            // 验证通过，执行操作
            OnCorrectInteraction?.Invoke();
            stepManager.CompleteStep();
        }
        else
        {
            Debug.Log(msg); // 实际项目中应显示在VR UI上
        }
    }
}

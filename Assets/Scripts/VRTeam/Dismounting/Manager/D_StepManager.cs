using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class D_StepManager : MonoBehaviour
{
    public static D_StepManager Instance { get; private set; }

    [SerializeField] private List<D_StepData> steps;          // 直接在Inspector中编辑
    public int CurrentStepIndex { get; private set; } = 0;

    public UnityEvent<int> OnStepChanged;

    [Header("教学模式组件")]
    [SerializeField] private HighlightController highlightController;
    [SerializeField] private D_StepUIManager stepUIManager;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        GoToStep(0);
    }

    public D_StepData GetCurrentStep()
    {
        if (steps == null || steps.Count == 0) return null;
        if (CurrentStepIndex >= steps.Count) return null;
        return steps[CurrentStepIndex];
    }

    public bool ValidateStepAction(out string message)
    {
        message = string.Empty;
        D_StepData step = GetCurrentStep();
        if (step == null) return true;

        if (step.requiredProtection != ProtectionType.None &&
            D_ProtectionManager.Instance.CurrentProtection != step.requiredProtection)
        {
            message = $"当前需要装备：{step.requiredProtection}";
            return false;
        }

        if (step.requiredItemIDs.Count > 0)
        {
            string currentTool = D_GrabManager.Instance.CurrentGrabbedItemID;
            if (string.IsNullOrEmpty(currentTool) || !step.requiredItemIDs.Contains(currentTool))
            {
                message = "当前使用的工具不正确";
                return false;
            }
        }

        return true;
    }

    public void CompleteStep()
    {
        if (steps == null || steps.Count == 0) return;
        if (CurrentStepIndex >= steps.Count - 1)
        {
            Debug.Log("所有步骤已完成");
            return;
        }
        GoToStep(CurrentStepIndex + 1);
    }

    public void SkipStep()
    {
        if (steps == null || steps.Count == 0) return;
        if (CurrentStepIndex >= steps.Count - 1)
        {
            Debug.Log("已是最后一步，无法跳过");
            return;
        }
        GoToStep(CurrentStepIndex + 1);
    }

    private void GoToStep(int index)
    {
        CurrentStepIndex = Mathf.Clamp(index, 0, steps.Count - 1);
        Debug.Log($"进入步骤 {CurrentStepIndex}: {GetCurrentStep()?.description}");

        stepUIManager?.RefreshUI(GetCurrentStep(), CurrentStepIndex, steps.Count);

        if (D_GameManager.Instance != null && D_GameManager.Instance.IsTeachingMode && highlightController != null)
        {
            List<GameObject> targets = FindStepTargets(GetCurrentStep());
            highlightController.SetHighlightTargets(targets);
        }

        OnStepChanged?.Invoke(CurrentStepIndex);
    }

    private List<GameObject> FindStepTargets(D_StepData step)
    {
        List<GameObject> targets = new List<GameObject>();
        // 实际根据requiredItemIDs查找场景中对象，此处留空
        return targets;
    }
}

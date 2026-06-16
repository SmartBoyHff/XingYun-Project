using EPOOutline;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    [Header("步骤配置")]
    public TutorialStepSO[] steps;
    private int currentStepIndex = 0;

    [Header("文本")]
    /// <summary>
    /// 全步骤排列
    /// </summary>
    public GameObject stepTextPrefab;          // 文本预制体（含 Text/TextMeshProUGUI）
    public Transform stepTextContainer;        // 父容器（如一个垂直布局的 Panel）
    /// <summary>
    /// 单步骤提示
    /// </summary>
    public TextMeshProUGUI stepText;//获取文本组件
    public Color inProgressTextColor = Color.white;   // 进行中步骤颜色
    public Color completedTextColor = Color.gray;     // 已完成步骤颜色
    public Color pendingTextColor = new Color(1, 1, 1, 0.5f); // 未开始步骤颜色（可选）

    private List<TextMeshProUGUI> stepTexts = new List<TextMeshProUGUI>(); // 存储生成的文本组件
    private List<int> textOriginalIndices = new List<int>();   // 每个文本对应的 steps 索引
    private List<int> textCompleteThresholds = new List<int>(); // 完成阈值
    [Header("通用按钮")]
    public Button nextButton;

    //[Header("多语言")]
    //[SerializeField] private MonoBehaviour localizationProvider;
    //private ILocalizationProvider loc;

    [Header("分数")]
    private int score = 0;
    public int Score => score;

   
    private AudioSource audioSource;
    private bool canProceed = true;
    private Button currentStepSpecificButton;
    public bool isExam=false;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        if (nextButton != null)
            nextButton.onClick.AddListener(OnGenericNextClicked);

        //if (localizationProvider != null)
        //    loc = localizationProvider as ILocalizationProvider;

        // 清空容器并生成所有步骤文本
        GenerateStepTexts();
    }

    private void Start()
    {
        ExecuteStep(0);
    }

    /// <summary> 根据 steps 生成对应数量的文本对象 </summary>
    private void GenerateStepTexts()
    {
        // 清空容器
        foreach (Transform child in stepTextContainer)
            Destroy(child.gameObject);
        stepTexts.Clear();
        textOriginalIndices.Clear();
        textCompleteThresholds.Clear();

        if (stepTextPrefab == null || stepTextContainer == null) return;

        // 找出所有需要显示文本的步骤索引
        List<int> nonEmptyIndices = new List<int>();
        for (int i = 0; i < steps.Length; i++)
        {
            string text = GetStepText(steps[i]);
            if (!string.IsNullOrEmpty(text))
                nonEmptyIndices.Add(i);
        }

        if (nonEmptyIndices.Count == 0) return;

        // 计算每个文本的完成阈值（下一个有文本步骤的索引，无则 steps.Length）
        for (int i = 0; i < nonEmptyIndices.Count; i++)
        {
            int origIdx = nonEmptyIndices[i];
            int threshold = (i + 1 < nonEmptyIndices.Count) ? nonEmptyIndices[i + 1] : steps.Length;

            GameObject obj = Instantiate(stepTextPrefab, stepTextContainer);
            TextMeshProUGUI txt = obj.GetComponent<TextMeshProUGUI>();
            if (txt == null)
            {
                Debug.LogWarning("stepTextPrefab 缺少 TextMeshProUGUI 组件");
                continue;
            }

            txt.text = GetStepText(steps[origIdx]);
            txt.color = pendingTextColor; // 初始全部未开始
            txt.enabled = true;
            stepTexts.Add(txt);
            textOriginalIndices.Add(origIdx);
            textCompleteThresholds.Add(threshold);
        }
    }

    /// <summary> 刷新所有文本的颜色（根据 currentStepIndex） </summary>
    private void RefreshStepTextsColor()
    {
        // 当前步骤索引可能超出数组（教程结束时）
        int effectiveCurrent = Mathf.Min(currentStepIndex, steps.Length);

        for (int i = 0; i < stepTexts.Count; i++)
        {
            int origIdx = textOriginalIndices[i];
            int threshold = textCompleteThresholds[i];

            if (effectiveCurrent >= threshold)
                stepTexts[i].color = completedTextColor;
            else if (effectiveCurrent >= origIdx)
                stepTexts[i].color = inProgressTextColor;
            else
                stepTexts[i].color = pendingTextColor;
        }
    }

    // --- 按钮与流程控制 ---
    private void OnGenericNextClicked()
    {
        if (!canProceed)
          steps[currentStepIndex].dependencies[0].externalScript.SkipCurrentStep();
       else
          AdvanceToNextStep(true);
    }

    private void OnStepNextButtonClicked()
    {
        if (!canProceed) return;
        score += 1;
        AdvanceToNextStep(false);
    }

    private void AdvanceToNextStep(bool isSkip)
    {
        var step = steps[currentStepIndex];
        
        // 如果是跳过，且当前步骤有专属按钮，则触发按钮事件
        if (isSkip && currentStepIndex < steps.Length)
        {
            if (step.next != null)
            {
                // 临时移除 TutorialManager 的监听，避免重复加分/推进
                step.next.onClick.RemoveListener(OnStepNextButtonClicked);
                DisableAllButton();
                // 触发按钮上所有其他监听
                step.next.onClick.Invoke();
                // 重新添加回监听
                step.next.onClick.AddListener(OnStepNextButtonClicked);

                // 触发 TutorialStepSO 中的自定义跳过事件
                step.onSkip?.Invoke();
            }
        }
        // 原有清理
        UnbindSpecificButton();
        DisableAllHighlights();

        currentStepIndex++;
        if (currentStepIndex < steps.Length)
            ExecuteStep(currentStepIndex);
        else
            EndTutorial();
    }

    private void ExecuteStep(int index)
    {
        if (index < 0 || index >= steps.Length)
        {
            EndTutorial();
            return;
        }
        

        var step = steps[index];

        // 1. 物体显示/隐藏
        foreach (var obj in step.objectsToShow) if (obj) obj.SetActive(true);
        foreach (var obj in step.objectsToHide) if (obj) obj.SetActive(false);

        // 2. 高亮
        DisableAllHighlights();
        if(!isExam)
        foreach (var obj in step.objectsToHighlight)
        {
            if (!obj) continue;
            var highlight = obj.GetComponentInChildren<UIHighlight>(true);
            if (highlight) { highlight.gameObject.SetActive(true); activeHighlights.Add(highlight); }
            var outline = obj.GetComponent<Outlinable>();
            if (outline) { outline.enabled = true; activeOutlines.Add(outline); }
        }

        // 3. 刷新文本颜色（当前步骤变成进行中颜色，之前的步骤自动变为完成色）
        RefreshStepTextsColor();
        if(!string.IsNullOrEmpty(step.stepText))
            stepText.text = $"当前步骤：\n<color=#FFFFFF>{step.stepText}</color>";


        // 4. 语音
        if (step.voiceClip && audioSource)
        {
            audioSource.clip = step.voiceClip;
            audioSource.Play();
        }

        // 5. 专属按钮绑定
        if (step.next != null)
        {
            currentStepSpecificButton = step.next;
            currentStepSpecificButton.onClick.AddListener(OnStepNextButtonClicked);
        }
        DisableAllButton();
        foreach (var b in step.nexts)
        {
            if(b!=null)
            { 
                b.onClick.AddListener(OnStepNextButtonClicked);
                activeButtons.Add(b);
            }
        }
        // 6. 流程控制
        if (step.autoProceed)
        {
            StartCoroutine(WaitForExternalConditions(index));
        }
        else
        {
            canProceed = true;
            if (step.next == null)
            {
                if (step.waitForVoice && audioSource && audioSource.clip != null)
                    StartCoroutine(WaitVoiceThenAutoProceed());
                else
                    AdvanceToNextStep(false);
            }
        }
        if(step.waitTime!=0)
        {
            StartCoroutine(WaittingTime(step.waitTime));
        }
    }

    private string GetStepText(TutorialStepSO step)
    {
        //if (loc != null && !string.IsNullOrEmpty(step.textKey))
        //    return loc.GetText(step.textKey);
        return step.stepText;
    }
    private IEnumerator WaittingTime(float t)
    {
        yield return new WaitForSeconds(t);
        OnGenericNextClicked();
    }

    private IEnumerator WaitForExternalConditions(int stepIndex)
    {
        canProceed = false;
        var step = steps[stepIndex];
        foreach (var dep in steps[stepIndex].dependencies)
        {
            if (dep.externalScript == null) continue;
            yield return new WaitUntil(() => dep.externalScript.Index >= dep.targetStepIndex);
        }
        if (step.waitForVoice && audioSource && audioSource.clip != null)
            yield return new WaitWhile(() => audioSource.isPlaying);
        AdvanceToNextStep(true);
    }

    private IEnumerator WaitVoiceThenAutoProceed()
    {
        canProceed = false;
        yield return new WaitWhile(() => audioSource.isPlaying);
        AdvanceToNextStep(true);
    }

    private void UnbindSpecificButton()
    {
        if (currentStepSpecificButton != null)
        {
            currentStepSpecificButton.onClick.RemoveListener(OnStepNextButtonClicked);
            currentStepSpecificButton = null;
        }
     
    }

    // 高亮列表
    private List<UIHighlight> activeHighlights = new List<UIHighlight>();
    private List<Outlinable> activeOutlines = new List<Outlinable>();
    private void DisableAllHighlights()
    {
        foreach (var h in activeHighlights) if (h) h.gameObject.SetActive(false);
        foreach (var o in activeOutlines) if (o) o.enabled = false;
        activeHighlights.Clear();
        activeOutlines.Clear();
    }
    //按钮列表
    private List<Button> activeButtons = new List<Button>();
    private void DisableAllButton()
    {
        foreach (var b in activeButtons)if(b)b.onClick.RemoveListener(OnStepNextButtonClicked);
        activeButtons.Clear();
    }
    private void EndTutorial()
    {
        UnbindSpecificButton();
        DisableAllHighlights();
        canProceed = false;
        nextButton?.gameObject.SetActive(false);
        Debug.Log($"引导结束，总分：{score}");
    }
}


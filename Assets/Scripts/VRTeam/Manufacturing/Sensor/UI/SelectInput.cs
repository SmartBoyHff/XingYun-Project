using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;



[Serializable]
public class InputContent
{
    public string[] strings;
    public string[] description;
    public int Correct;
}
public class SelectInput : MonoBehaviour
{

    [SerializeField] private List<Toggle> optionToggles;       // 选项 Toggle 列表（建议至少4个）
    [SerializeField] private TMP_InputField targetInputField;  // 需要填写的输入框
    [SerializeField] private TMP_Text text;                    // 正确/错误提示文本
    public ExamUIManager exam;                                 // 考核管理器引用
    private string[] optionPrefixes; 
    // 动态生成的题目列表（顺序 = 所有步骤的 startCommand 和 endCommand 依次排列）
    private List<InputContent> dynamicInputContents = new List<InputContent>();
    private int previousExamIndex = -1;                        // 上一次记录的 exam.Index

    // 可配置的选项数量（每个题目包含正确命令+干扰项，总数建议不超过 Toggle 数量）
    [SerializeField] private int optionsPerQuestion = 4;

    // 可选：自定义干扰命令池（留空则自动从所有命令中提取）
    [SerializeField] private List<string> customDistractors;

    private void Start()
    {
        if (exam == null)
            exam = FindObjectOfType<ExamUIManager>();

        if (exam == null)
        {
            Debug.LogError("SelectInput: 未找到 ExamUIManager！");
            return;
        }
        optionPrefixes = GeneratePrefixes(optionToggles.Count);
        BuildQuestionsFromExamSteps();   // 根据考核步骤动态生成题目
        previousExamIndex = exam.Index;
        RefreshUI();
    }

    private string[] GeneratePrefixes(int count)
    {
        string[] prefixes = new string[count];
        for (int i = 0; i < count; i++)
        {
            prefixes[i] = ((char)('A' + i)).ToString();
        }
        return prefixes;
    }

    private void Update()
    {
        if (exam != null && exam.Index != previousExamIndex)
        {
            previousExamIndex = exam.Index;
         
        }
    }

    /// <summary>
    /// 从 ExamUIManager 的 Steps 中读取所有命令，按顺序生成题目
    /// </summary>
    private void BuildQuestionsFromExamSteps()
    {
        dynamicInputContents.Clear();
        var steps = exam.Steps;
        if (steps == null || steps.Count == 0)
        {
            Debug.LogWarning("SelectInput: 考核步骤为空，无法生成题目！");
            return;
        }

        // 1. 收集所有可用的命令（作为干扰项池）
        HashSet<string> allCommands = new HashSet<string>();
        foreach (var step in steps)
        {
            if (!string.IsNullOrEmpty(step.startCommand))
                allCommands.Add(step.startCommand);
            if (!string.IsNullOrEmpty(step.endCommand))
                allCommands.Add(step.endCommand);
        }
        if (customDistractors != null)
        {
            foreach (var cmd in customDistractors)
                if (!string.IsNullOrEmpty(cmd))
                    allCommands.Add(cmd);
        }
        allCommands.Remove("");

        // 2. 为每个步骤的 startCommand 和 endCommand 分别创建题目
        foreach (var step in steps)
        {
            // 处理开始命令（显示文本 = 命令本身）
            if (!string.IsNullOrEmpty(step.startCommand))
            {
                var content = CreateInputContentForCommand(
                    correctCmd: step.startCommand,
                    displayText: step.startCommand,   // 开始命令没有特殊显示名，直接用命令
                    allCommands: allCommands,
                    totalOptions: optionsPerQuestion
                );
                dynamicInputContents.Add(content);
            }

            // 处理结束命令（显示文本使用 endName，若为空则回退到 endCommand）
            if (!string.IsNullOrEmpty(step.endCommand))
            {
                string displayForEnd = !string.IsNullOrEmpty(step.endName) ? step.endName : step.endCommand;
                var content = CreateInputContentForCommand(
                    correctCmd: step.endCommand,
                    displayText: displayForEnd,
                    allCommands: allCommands,
                    totalOptions: optionsPerQuestion
                );
                dynamicInputContents.Add(content);
            }
        }
    }

    /// <summary>
    /// 为单个命令创建一个 InputContent，包含随机干扰选项
    /// </summary>
    /// <param name="correctCmd">正确命令（实际填入输入框的值）</param>
    /// <param name="displayText">正确选项在UI上显示的文字</param>
    /// <param name="allCommands">全局命令池（干扰项来源）</param>
    /// <param name="totalOptions">总共需要的选项数量（含正确命令）</param>
    private InputContent CreateInputContentForCommand(string correctCmd, string displayText, HashSet<string> allCommands, int totalOptions)
    {
        // 准备候选干扰项（排除正确命令本身）
        List<string> candidates = allCommands.Where(cmd => cmd != correctCmd).ToList();
        int needDistractors = totalOptions - 1;
        List<string> selectedDistractors = new List<string>();

        if (candidates.Count >= needDistractors)
        {
            selectedDistractors = candidates.OrderBy(x => Guid.NewGuid()).Take(needDistractors).ToList();
        }
        else
        {
            selectedDistractors.AddRange(candidates);
            while (selectedDistractors.Count < needDistractors)
                selectedDistractors.Add("(无效命令)");
        }

        // 构建选项列表：正确命令 + 干扰项
        List<string> optionCommands = new List<string> { correctCmd };
        optionCommands.AddRange(selectedDistractors);

        // 随机打乱选项顺序
        var shuffled = optionCommands.OrderBy(x => Guid.NewGuid()).ToList();
        int correctIndex = shuffled.IndexOf(correctCmd);

        // 生成显示文本数组（description）：
        // - 对于正确选项，使用传入的 displayText
        // - 对于干扰项，显示其命令本身（因为干扰项没有额外的显示名）
        string[] descriptions = new string[shuffled.Count];
        for (int i = 0; i < shuffled.Count; i++)
        {
            if (i == correctIndex)
                descriptions[i] = displayText;
            else
                descriptions[i] = shuffled[i];   // 干扰项显示命令原文
        }

        InputContent content = new InputContent
        {
            strings = shuffled.ToArray(),   // 实际命令
            description = descriptions,      // UI显示文本
            Correct = correctIndex
        };

        return content;
    }

    /// <summary>
    /// 刷新 UI：根据当前的 exam.Index 显示对应的题目和选项
    /// </summary>
    public void RefreshUI()
    {
        if (targetInputField != null)
            targetInputField.text = "";
        if (text != null)
            text.text = "";

        if (dynamicInputContents == null || dynamicInputContents.Count == 0)
            return;

        int currentIdx = exam.Index;
        if (currentIdx < 0 || currentIdx >= dynamicInputContents.Count)
            return;

        InputContent content = dynamicInputContents[currentIdx];
        if (content.strings == null)
            return;

        bool isExamMode = exam.isExam;

        for (int i = 0; i < optionToggles.Count; i++)
        {
            Toggle toggle = optionToggles[i];
            if (toggle == null) continue;

            var label = toggle.GetComponentInChildren<TMP_Text>();
            if (label == null) continue;

            if (i < content.strings.Length)
            {
                // 显示文本：优先使用 description，若没有则使用命令本身
                string displayText = (content.description != null && i < content.description.Length && !string.IsNullOrEmpty(content.description[i]))
                    ? content.description[i]
                    : content.strings[i];
                // 添加前缀
                string prefix = (i < optionPrefixes.Length) ? optionPrefixes[i] : ((char)('A' + i)).ToString();
                label.text = $"<size=80>{prefix}</size>. {displayText}";

                var highlight = toggle.GetComponentInChildren<UIHighlight>(true);
                if (highlight != null)
                {
                    if (isExamMode)
                        highlight.gameObject.SetActive(false);
                    else
                        highlight.gameObject.SetActive(i == content.Correct);
                }

                toggle.onValueChanged.RemoveAllListeners();
                toggle.isOn = false;
                int optionIndex = i;
                toggle.onValueChanged.AddListener((isOn) =>
                {
                    if (isOn)
                        OnOptionSelected(optionIndex, content);
                });
            }
            else
            {
                label.text = "";
                toggle.onValueChanged.RemoveAllListeners();
                toggle.isOn = false;
                var highlight = toggle.GetComponentInChildren<UIHighlight>(true);
                if (highlight != null)
                    highlight.gameObject.SetActive(false);
            }
        }
       
    }

    private void OnOptionSelected(int optionIndex, InputContent content)
    {
        // 填充输入框：始终使用 strings 中的实际命令
        if (targetInputField != null && optionIndex < content.strings.Length)
            targetInputField.text = content.strings[optionIndex];
        //获取选项前缀
        string selectedPrefix = (optionIndex < optionPrefixes.Length) ? optionPrefixes[optionIndex] : ((char)('A' + optionIndex)).ToString();
        string correctPrefix = (content.Correct < optionPrefixes.Length) ? optionPrefixes[content.Correct] : ((char)('A' + content.Correct)).ToString();
        if (text != null)
        {
            if (optionIndex == content.Correct)
            {
                text.text = "正确";
            }
            else
            {
                text.text = $"选择错误，正确答案是 {correctPrefix}";
            }
        }

    }
   public void Submit()
    {
        RefreshUI();
    }
    /// <summary>
    /// 外部手动刷新（例如重置考核后重新生成题目）
    /// </summary>
    public void RegenerateQuestions()
    {
        BuildQuestionsFromExamSteps();
        previousExamIndex = exam != null ? exam.Index : -1;
        RefreshUI();
    }
}

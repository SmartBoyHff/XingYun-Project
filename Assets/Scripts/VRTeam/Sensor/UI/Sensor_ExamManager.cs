using Foundation.Console;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Sensor_ExamManager : MonoBehaviour
{
    public static Sensor_ExamManager Instance { get; private set; }

    [Header("考核配置")]
    [SerializeField] private List<Sensor_ExamStep> examSteps = new List<Sensor_ExamStep>();

    [Header("UI 提示（可选）")]
    [SerializeField] private UnityEngine.UI.Text progressText;   // 显示 "步骤 1/5"
    [SerializeField] private UnityEngine.UI.Text scoreText;      // 显示 "得分: 0"

    // 运行时状态
    private int currentStepIndex = 0;
    private int totalScore = 0;
    private bool examCompleted = false;

    // 当前步骤的预期命令（小写）
    private string CurrentExpectedCommand => examSteps[currentStepIndex].expectedCommand.ToLower();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        if (examSteps.Count == 0)
        {
            Terminal.LogError("考核步骤列表为空！请在 Inspector 中配置 ExamManager 的 examSteps。");
            return;
        }

        // 注册命令解释器（处理用户输入）
        Terminal.Add(new TerminalInterpreter
        {
            Label = "ExamCommandHandler",
            Method = OnUserCommand
        });

        // 注册 "Reset" 命令作为特殊命令
        Terminal.Add(new TerminalCommand
        {
            Label = "Reset",
            Method = ResetExam
        });

        // 注册 "skip" 命令作为特殊命令
        Terminal.Add(new TerminalCommand
        {
            Label = "Skip Current Step",
            Method = SkipCurrentStep
        });

        // 单独注册 skip 文本命令（方便键盘输入）
        Terminal.Add(new TerminalInterpreter
        {
            Label = "SkipInterpreter",
            Method = (input) =>
            {
                if (input.Trim().ToLower() == "skip")
                    SkipCurrentStep();
            }
        });

        Terminal.LogSuccess("=== 考核开始 ===");
        DisplayCurrentStepPrompt();
        UpdateUI();
    }

    // 处理用户输入的普通命令
    private void OnUserCommand(string input)
    {
        if (examCompleted)
        {
            Terminal.LogWarning("考核已经完成，无法再输入命令。");
            return;
        }

        if (currentStepIndex >= examSteps.Count)
        {
            CompleteExam();
            return;
        }

        string trimmed = input.Trim().ToLower();
        string expected = CurrentExpectedCommand;

        if (trimmed == expected)
        {
            // 正确
            HandleCorrectAnswer();
        }
        else if (trimmed == "skip")
        {
            // skip 命令在另一个 Interpreter 中处理，这里忽略避免重复
            return;
        }
        else
        {
            // 错误
            HandleWrongAnswer(expected);
        }
    }

    private void HandleCorrectAnswer()
    {
        Sensor_ExamStep step = examSteps[currentStepIndex];
        totalScore += step.scoreValue;
        Terminal.LogSuccess($" 正确！ {step.successOutput}");
        Terminal.Log($"获得 {step.scoreValue} 分，当前总分: {totalScore}");

        MoveToNextStep();
    }

    public void SkipCurrentStep()
    {
        if (examCompleted || currentStepIndex >= examSteps.Count)
            return;

        Terminal.LogWarning($" 已跳过第 {currentStepIndex + 1} 步（命令: {examSteps[currentStepIndex].expectedCommand}），不得分。");
        // 跳过不加分，直接进入下一步
        MoveToNextStep();
    }

    private void MoveToNextStep()
    {
        currentStepIndex++;
        UpdateUI();

        if (currentStepIndex >= examSteps.Count)
        {
            CompleteExam();
        }
        else
        {
            DisplayCurrentStepPrompt();
        }
    }

    private void HandleWrongAnswer(string expected)
    {
        Terminal.LogError($"命令错误。正确的命令是: {expected}，请重新输入。");
        // 不进入下一步，停留在当前步骤
    }

    private void DisplayCurrentStepPrompt()
    {
        if (currentStepIndex < examSteps.Count)
        {
            Terminal.Log($"--- 步骤 {currentStepIndex + 1}/{examSteps.Count} ---");
            Terminal.Log($"请输入命令: {examSteps[currentStepIndex].expectedCommand}（不区分大小写）");
        }
    }

    private void CompleteExam()
    {
        examCompleted = true;
        int maxScore = examSteps.Sum(s => s.scoreValue);
        float percentage = (float)totalScore / maxScore * 100f;
        Terminal.LogSuccess($"=== 考核结束 ===");
        Terminal.Log($"总分: {totalScore} / {maxScore}  ({percentage:F1}%)");
        if (percentage >= 60f)
            Terminal.LogSuccess("结果: 通过 ");
        else
            Terminal.LogError("结果: 未通过 ");
    }

    private void UpdateUI()
    {
        if (progressText != null)
            progressText.text = $"步骤: {currentStepIndex + 1}/{examSteps.Count}";
        if (scoreText != null)
            scoreText.text = $"得分: {totalScore}";
    }

    // 可选：外部重置考核的方法
    public void ResetExam()
    {
        currentStepIndex = 0;
        totalScore = 0;
        examCompleted = false;
        Terminal.Clear();
        Terminal.LogSuccess("考核已重置，重新开始。");
        DisplayCurrentStepPrompt();
        UpdateUI();
    }
}

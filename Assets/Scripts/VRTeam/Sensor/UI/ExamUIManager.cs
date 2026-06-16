using Foundation.Console;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ExamUIManager : MonoBehaviour
{
    //public static ExamUIManager Instance { get; private set; }

    [Header("考核配置")]
    [SerializeField] private List<ExamStep> examSteps = new List<ExamStep>();

    [Header("UI 提示（可选）")]
    [SerializeField] private UnityEngine.UI.Text progressText;
    [SerializeField] private UnityEngine.UI.Text scoreText;
    // 对外暴露考核步骤列表（只读）
    public List<ExamStep> Steps => examSteps;
    // 命令与动作的映射（开始命令 -> 执行方法，结束命令 -> 执行方法）
    private Dictionary<string, System.Action> startActions = new Dictionary<string, System.Action>();
    private Dictionary<string, System.Action> endActions = new Dictionary<string, System.Action>();


    public int Index=0;//外部引用索引
    public bool isExam;
    private int currentStepIndex = 0;
    private int totalScore = 0; //分数
    private bool examCompleted = false;

    // 当前步骤的状态
    private enum StepPhase { WaitingForStart, WaitingForEnd }
    private StepPhase currentPhase;
    public bool isEnd;

    //private void Awake()
    //{
    //    if (Instance == null)
    //        Instance = this;
    //    else
    //        Destroy(gameObject);
    //}

    private void Start()
    {
        if (examSteps.Count == 0)
        {
            Terminal.LogError("考核步骤列表为空！请在 Inspector 中配置 ExamManager 的 examSteps。");
            return;
        }
        // 注册终端命令解释器
        Terminal.Add(new TerminalInterpreter
        {
            Label = "ExamUIManager",
            Method = OnUserCommand
        });

        //Terminal.LogSuccess("=== 考核开始 ===");
        StartCurrentStep();
        UpdateUI();
    }

    private void OnUserCommand(string input)
    {
        if (examCompleted) return;

        string trimmed = input.Trim();
        ExamStep currentStep = examSteps[currentStepIndex];

        if (currentPhase == StepPhase.WaitingForStart)
        {
           
            // 期望开始命令
            if (trimmed == currentStep.startCommand)
            {
                // 正确输入开始命令
                HandleStartCommand(currentStep);
            }
            else
            {                
               
                Terminal.LogInput($"错误：当前步骤命令为 '{currentStep.startCommand}'系统自动纠正。");
                SkipCurrentStep();
            }
        }
        else if (currentPhase == StepPhase.WaitingForEnd && !string.IsNullOrEmpty(currentStep.endCommand))
        {
            //isEnd = true;
            // 期望结束命令
            if (trimmed == currentStep.endCommand)
            {
                HandleEndCommand(currentStep);
            }
            else
            {
                
                Terminal.LogInput($"错误：当前步骤命令为 '{currentStep.endCommand}',系统自动纠正");
                SkipCurrentStep();
            }
        }
        else
        {
            // 不应该发生
            Terminal.LogError("考核状态异常，请检查步骤配置。");
        }
    }

    private void OutputLines(string[] lines,TerminalType type)//文本输出
    {
        if (lines == null) return;
        foreach (string line in lines)
        {
            if (!string.IsNullOrEmpty(line))
                Terminal.Add(line,type);
        }
    }

    private void HandleStartCommand(ExamStep step)
    {
        // 输出开始文本数组
        OutputLines(step.startOutput,step.startColor);

        // 执行开始动作（如 ping）
        ExecuteStartCommand(step.startCommand);

        if (string.IsNullOrEmpty(step.endCommand))
        {
            totalScore += step.scoreValue;
            //Terminal.LogSuccess($"步骤 {currentStepIndex + 1} 完成！获得 {step.scoreValue} 分，当前总分: {totalScore}");
            MoveToNextStep();
        }
        else
        {
            currentPhase = StepPhase.WaitingForEnd;
            Index++;
            isEnd = true;
            //Terminal.Log($"请继续输入结束命令: {step.endCommand}");
        }
    }

    private void HandleEndCommand(ExamStep step)
    {
        OutputLines(step.endOutput,step.endColor);

        ExecuteEndCommand(step.endCommand);

        totalScore += step.scoreValue;
       // Terminal.LogSuccess($"步骤完成！获得 {step.scoreValue} 分，当前总分: {totalScore}");
        MoveToNextStep();
    }
    //添加开始事件
    public virtual void ExecuteStartCommand(string command)
    {
        switch (command)
        {
            case "ping 192.168.13.1":
                CustomCommands.Instance?.PingCommand();
                break;
            // 以后可以加其他命令
            case "vim viz_address.conf":
                CustomCommands.Instance?.VimCommand();
                break;
            case "1":
                CustomCommands.Instance?.CameraCommand();
                break;

            case "192.168.13.121 7000":
                CustomCommands.Instance?.IDCommand();
                break;
            case "4":
                CustomCommands.Instance?.OpenCommand();
                break;
            case "roscore":
                CustomCommands.Instance?.RoscoreCommand();
                break;
            default:
                //Terminal.LogWarning($"未绑定的开始命令: {command}");
                break;
        }
    }
    //添加结束事件
    public virtual void ExecuteEndCommand(string command)
    {
        switch (command)
        {
           
            case "^C":                    
                CustomCommands.Instance?.StopPing();
                break;
            case "dd":
                CustomCommands.Instance?.ShowSystemInfo();
                break;
            case "wq!":
                CustomCommands.Instance?.RestoreBackup();
                break;
            default:
               // Terminal.LogWarning($"未绑定的结束命令: {command}");
                break;
        }
    }

    /// <summary>
    /// 跳过当前步骤（不得分）
    /// </summary>
    public void SkipCurrentStep()
    {
        if (examCompleted || currentStepIndex >= examSteps.Count) return;

        ExamStep step = examSteps[currentStepIndex];
        //Terminal.LogWarning($"已跳过步骤 {currentStepIndex + 1}（命令: {step.startCommand}），不得分。");

        // 执行开始动作（模拟）
        if (!isEnd)
        {
            Terminal.LogInput($"指令{step.startCommand}");
            OutputLines(step.startOutput,step.startColor); // 输出开始文本数组
            OutputLines(step.skipOutput,step.skipColor); // 输出跳过文本数组
            ExecuteStartCommand(step.startCommand);
            
            if (string.IsNullOrEmpty(step.endCommand))   
                MoveToNextStep();
            else
            {
                currentPhase = StepPhase.WaitingForEnd;
                isEnd = true;
                Index++;
            }      
        }
        // 如果有结束命令，也执行结束动作
        else
        {
            Terminal.LogError($"指令{step.endCommand},已跳过");
            OutputLines(step.endOutput,step.endColor);  // 跳过时输出endOutput
            ExecuteEndCommand(step.endCommand); 
            MoveToNextStep();
        }
   
      
    }

    private void MoveToNextStep()
    {
        isEnd = false;
        Index++;
        currentStepIndex++;
        if (currentStepIndex >= examSteps.Count)
        {
            CompleteExam();
        }
        else
        {
            StartCurrentStep();
            UpdateUI();
        }
    }

    private void StartCurrentStep()
    {
        currentPhase = StepPhase.WaitingForStart;
        //Terminal.Log($"--- 步骤 {currentStepIndex + 1}/{examSteps.Count} ---");
        //Terminal.LogInput($"(小提示)请输入命令: {examSteps[currentStepIndex].startCommand}");
        if (examSteps[currentStepIndex].isDefaultText)
            Terminal.Log($"inwinic@Anna :~ $");
        else
            OutputLines(examSteps[currentStepIndex].startText,TerminalType.Log);
    }

    private void CompleteExam()
    {
        examCompleted = true;
        int maxScore = examSteps.Sum(s => s.scoreValue);
        float percentage = (float)totalScore / maxScore * 100f;
        Terminal.LogSuccess($"=== 考核结束 ===");
        Terminal.Log($"总分: {totalScore} / {maxScore} ({percentage:F1}%)");
        Terminal.LogSuccess(percentage >= 60f ? "结果: 通过 " : "结果: 未通过 ");
    }

    private void UpdateUI()
    {
        if (progressText != null)
            progressText.text = $"步骤: {currentStepIndex + 1}/{examSteps.Count}";
        if (scoreText != null)
            scoreText.text = $"得分: {totalScore}";
    }

    /// <summary>
    /// 外部重置考核
    /// </summary>
    public void ResetExam()
    {
        // 停止所有可能正在运行的命令（如 ping）
        if (CustomCommands.Instance != null)
            CustomCommands.Instance.StopPing();

        currentStepIndex = 0;
        totalScore = 0;
        examCompleted = false;
        Terminal.Clear();
        Terminal.LogSuccess("考核已重置，重新开始。");
        StartCurrentStep();
        UpdateUI();
    }
    public void StopCustomCommand()
    {
        CustomCommands.Instance?.Stop();
    }
}

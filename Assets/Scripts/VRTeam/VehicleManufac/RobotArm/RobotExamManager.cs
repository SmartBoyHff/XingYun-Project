using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class RobotExamManager : MonoBehaviour
{
    public static RobotExamManager Instance;

    public NodeRealtimeMonitor monitor;
    public enum ExamState { Idle, PlayingDemo, WaitingForSubmission, Finished }
    public ExamState CurrentState { get; private set; } = ExamState.Idle;

    public int CurrentStageIndex { get; private set; }
    public ExamSequence CurrentSequence { get; private set; }

    private ActionRecorder[] recorders;          // 从外部传入
    private JointAngles poseBeforeDemo;
    private List<StageResult> stageResults = new List<StageResult>();
    private int currentStageArmIndex;

    public System.Action<int, string> OnStageChanged;
    public System.Action<WholeResult> OnExamFinished;

    [SerializeField] private RobotUIManager uiManager;

    void Awake() { if (Instance == null) Instance = this; else Destroy(gameObject); }

    public void StartSequenceExam(int sequenceIndex, ActionRecorder[] recordersArray)
    {
        if (CurrentState != ExamState.Idle) return;
        if (sequenceIndex < 0 || sequenceIndex >= ExamSequenceLibrary.Instance.sequences.Count) return;
        CurrentSequence = ExamSequenceLibrary.Instance.sequences[sequenceIndex];
        if (CurrentSequence.stages.Count == 0) return;
        recorders = recordersArray;
        CurrentStageIndex = 0;
        stageResults.Clear();
        CurrentState = ExamState.PlayingDemo;
        uiManager.EnterExamMode();
        StartCoroutine(PlayCurrentDemo());
    }

    public void ReplayCurrentDemo()
    {
        if (CurrentState != ExamState.WaitingForSubmission) return;
        CurrentState = ExamState.PlayingDemo;
        StartCoroutine(PlayCurrentDemo());
    }

    IEnumerator PlayCurrentDemo()
    {
        var stage = CurrentSequence.stages[CurrentStageIndex];
        currentStageArmIndex = stage.armIndex;
        ActionRecorder targetRecorder = recorders[currentStageArmIndex];

        // 锁定 UI 机械臂
        uiManager.SetCurrentArmIndex(currentStageArmIndex);

        // 保存演示前姿态
        poseBeforeDemo = targetRecorder.GetCurrentAngles();

        uiManager.ShowMessage($"阶段 {CurrentStageIndex + 1}/{CurrentSequence.stages.Count}：{stage.stageName} 演示中…");
        targetRecorder.PlayClip(stage.task.standardClip);
        // 等待播放器停止（确保完全结束）
        yield return new WaitForSeconds(stage.task.standardClip.TotalDuration + 2f);
        targetRecorder.StopPlayback();
        // 恢复姿态
        targetRecorder.SetAngles(poseBeforeDemo);
        Debug.Log(1);
        monitor.StartMonitoring(stage.task, ActionRecorder.GetRecorder(stage.armIndex));
        CurrentState = ExamState.WaitingForSubmission;
        uiManager.ShowMessage($"请用机械臂{currentStageArmIndex + 1}操作并录制，然后提交");
        OnStageChanged?.Invoke(CurrentStageIndex, stage.stageName);
    }

    public void SubmitAction(ActionClip studentClip)
    {
        monitor.StopMonitoring();
        if (CurrentState != ExamState.WaitingForSubmission || studentClip == null) return;
        var stage = CurrentSequence.stages[CurrentStageIndex];
        AssessmentResult result = Assess(stage.task, studentClip);
        StageResult sr = new StageResult
        {
            stageName = stage.stageName,
            passed = result.errorNodes == 0,
            errors = result.errors,
            timeUsed = studentClip.TotalDuration,
            timeRatio = result.timeRatio
        };
        stageResults.Add(sr);
        uiManager.ShowStageResult(sr);

        CurrentStageIndex++;
        if (CurrentStageIndex < CurrentSequence.stages.Count)
        {
            CurrentState = ExamState.PlayingDemo;
            StartCoroutine(PlayCurrentDemo());
        }
        else
        {
            CurrentState = ExamState.Finished;
            WholeResult overall = EvaluateWholeSequence();
            uiManager.ShowOverallResult(overall);
            uiManager.ExitExamMode();
            OnExamFinished?.Invoke(overall);
            CurrentState = ExamState.Idle;
        }
    }

    // 评估方法（与之前相同，区域到达判定）
    AssessmentResult Assess(StandardTask task, ActionClip studentClip)
    {
        Debug.Log("=== 评估开始 ===");
        Debug.Log($"当前任务：{task.taskName}，节点数：{task.keyNodes.Count}");
        for (int i = 0; i < task.keyNodes.Count; i++)
        {
            var node = task.keyNodes[i];
            Debug.Log($"节点 {i}: 描述={node.description}, 容差={node.angleTolerance}");
            Debug.Log($"  目标角度: ({node.targetAngles.shoulderYaw:F2}, {node.targetAngles.shoulderPitch:F2}, " +
                      $"{node.targetAngles.elbow:F2}, {node.targetAngles.wristPitch:F2}, {node.targetAngles.wristRoll:F2})");
        }
        var result = new AssessmentResult
        {
            totalNodes = task.keyNodes.Count,
            errors = new List<ErrorDetail>()
        };

        if (studentClip == null || studentClip.frames.Count == 0)
        {
            Debug.LogWarning("学生动作无数据");
            // 没有数据，全部节点失败
            result.errorNodes = task.keyNodes.Count;
            // 可以填充错误详情（使用默认角度）
            for (int i = 0; i < task.keyNodes.Count; i++)
            {
                result.errors.Add(new ErrorDetail
                {
                    nodeIndex = i,
                    jointName = "全部关节",
                    expectedAngle = 0,
                    actualAngle = 0
                });
            }
            result.studentTime = 0;
            result.standardTime = task.standardClip.TotalDuration;
            result.timeRatio = 0;
            result.grade = "差";
            return result;
        }
        Debug.Log("===== 开始评估 ===== 学生总帧数: " + studentClip.frames.Count);
        foreach (var node in task.keyNodes)
        {
            bool passed = false;
            foreach (var frame in studentClip.frames)
            {
                if (IsAngleWithinTolerance(frame.angles, node.targetAngles, node.angleTolerance))
                {
                    passed = true;
                    Debug.Log($"节点 {task.keyNodes.IndexOf(node)} 通过：匹配帧时间 {frame.timestamp:F2}s 角度 " +
                         $"({frame.angles.shoulderYaw:F2},{frame.angles.shoulderPitch:F2},{frame.angles.elbow:F2}," +
                         $"{frame.angles.wristPitch:F2},{frame.angles.wristRoll:F2})");
                    break;
                }
            }
            if (!passed)
            {
                result.errorNodes++;
                RecordFrame closest = GetClosestFrame(studentClip, node.targetAngles);
                Debug.LogError($"节点 {task.keyNodes.IndexOf(node)} 失败：目标角度 " +
                          $"({node.targetAngles.shoulderYaw:F2},{node.targetAngles.shoulderPitch:F2},{node.targetAngles.elbow:F2}," +
                          $"{node.targetAngles.wristPitch:F2},{node.targetAngles.wristRoll:F2}) 容差 {node.angleTolerance:F1}°\n" +
                          $"最接近帧角度 ({closest.angles.shoulderYaw:F2},{closest.angles.shoulderPitch:F2},{closest.angles.elbow:F2}," +
                          $"{closest.angles.wristPitch:F2},{closest.angles.wristRoll:F2})");
                AddFailedNodeErrors(result, node, closest, task.keyNodes.IndexOf(node));
            }
        }
        result.studentTime = studentClip.TotalDuration;
        result.standardTime = task.standardClip.TotalDuration;
        result.timeRatio = result.studentTime / result.standardTime;
        if (result.errorNodes == 0)
        {
            Debug.Log("所有节点均通过！请注意初始姿态可能正好满足节点要求。");
            result.grade = result.timeRatio <= 1.2f ? "优秀" : "良好"; 
        }
        else if (result.errorNodes <= result.totalNodes * 0.2f)
            result.grade = "良好";
        else
            result.grade = "差";
        return result;
    }

    // --- 辅助判定函数 ---
    bool IsAngleWithinTolerance(JointAngles a, JointAngles b, float tol) 
    {
        return Mathf.Abs(a.shoulderYaw - b.shoulderYaw) <= tol &&
           Mathf.Abs(a.shoulderPitch - b.shoulderPitch) <= tol &&
           Mathf.Abs(a.elbow - b.elbow) <= tol &&
           Mathf.Abs(a.wristPitch - b.wristPitch) <= tol &&
           Mathf.Abs(a.wristRoll - b.wristRoll) <= tol; 
    }
    RecordFrame GetClosestFrame(ActionClip clip, JointAngles target) 
    {
        if (clip.frames == null || clip.frames.Count == 0)
            return new RecordFrame(); // 返回默认值

        RecordFrame best = clip.frames[0];
        float minDist = AngleDistance(best.angles, target);
        for (int i = 1; i < clip.frames.Count; i++)
        {
            float d = AngleDistance(clip.frames[i].angles, target);
            if (d < minDist)
            {
                minDist = d;
                best = clip.frames[i];
            }
        }
        return best;
    }
    float AngleDistance(JointAngles a, JointAngles b)
    {
        return Mathf.Abs(a.shoulderYaw - b.shoulderYaw) +
               Mathf.Abs(a.shoulderPitch - b.shoulderPitch) +
               Mathf.Abs(a.elbow - b.elbow) +
               Mathf.Abs(a.wristPitch - b.wristPitch) +
               Mathf.Abs(a.wristRoll - b.wristRoll);
    }
    void AddFailedNodeErrors(AssessmentResult res, KeyNode node, RecordFrame closest, int idx)
    {
        AddIfError(res, "shoulderYaw", node.targetAngles.shoulderYaw, closest.angles.shoulderYaw, node.angleTolerance, idx);
        AddIfError(res, "shoulderPitch", node.targetAngles.shoulderPitch, closest.angles.shoulderPitch, node.angleTolerance, idx);
        AddIfError(res, "elbow", node.targetAngles.elbow, closest.angles.elbow, node.angleTolerance, idx);
        AddIfError(res, "wristPitch", node.targetAngles.wristPitch, closest.angles.wristPitch, node.angleTolerance, idx);
        AddIfError(res, "wristRoll", node.targetAngles.wristRoll, closest.angles.wristRoll, node.angleTolerance, idx);
    }
    void AddIfError(AssessmentResult result, string joint, float expected, float actual, float tol, int idx)
    {
        if (Mathf.Abs(expected - actual) > tol)
        {
            result.errors.Add(new ErrorDetail
            {
                nodeIndex = idx,
                jointName = joint,
                expectedAngle = expected,
                actualAngle = actual
            });
        }
    }

    WholeResult EvaluateWholeSequence()
    {
        int totalNodesAll = 0, errorsAll = 0;
        foreach (var sr in stageResults)
        {
            totalNodesAll += sr.errors.Count; // 实际应该记录节点总数，这里可以从原始任务获取
            errorsAll += sr.errors.Count;
        }
        WholeResult r = new WholeResult { stageResults = new List<StageResult>(stageResults) };
        if (errorsAll == 0) r.finalGrade = "优秀";
        else if (errorsAll <= totalNodesAll * 0.1f) r.finalGrade = "良好";
        else r.finalGrade = "差";
        return r;
    }
}


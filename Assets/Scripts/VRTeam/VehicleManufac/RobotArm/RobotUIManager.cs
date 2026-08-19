using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Content.Interaction;

public class RobotUIManager : MonoBehaviour
{
    public static RobotUIManager Instance;

    [Header("多机械臂")]
    public ActionRecorder[] recorders;                 // 所有机械臂的记录器
    public TMP_Dropdown roboArmDropdown;                  // 机械臂选择下拉框
    private int currentArmIndex = 0;
    private ActionRecorder CurrentRecorder => recorders[currentArmIndex];
    public List<XRKnob> knobs;                     // 按关节顺序拖入所有旋钮
    private float[][] savedKnobValues;             // [armIndex][knobIndex]
    private ActionRecorder activePlaybackRecorder;         //
    [Header("普通动作面板")]
    public GameObject actionPanel;
    public TextMeshProUGUI actionStatusText;
    public Button startRecordButton;
    public Button stopRecordButton;
    public TMP_Dropdown actionDropdown;
    public Button playActionButton;
    public Button viewChartActionButton;
    public Button deleteActionButton;

    // ========== 考核序列与任务面板（原标准任务面板）==========
    [Header("考核序列面板")]
    public GameObject sequencePanel;
    public TextMeshProUGUI sequenceStatusText;
    public TMP_Dropdown sequenceDropdown;               // 考核序列下拉框
    public TMP_Dropdown taskDropdown;                   // 序列内任务下拉框
    public Button newSequenceButton;                // 新建考核序列
    public Button deleteSequenceButton;             // 删除当前序列
    public Button deleteTaskButton;                 // 删除序列内选中的任务

    // 标准任务制作按钮（不变）
    public Button startStandardRecordButton;
    public Button stopStandardRecordButton;
    public Button markKeyNodeButton;

    // 考核相关
    public Button startSequenceExamButton; // 开始多阶段考核
    public Button submitExamButton;       // 提交考核动作
    public Button replayDemoButton;      // 重播演示
    public TextMeshProUGUI examStatusText;          // 显示当前阶段状态

    // ========== 考核结果 ==========
    [Header("考核结果面板")]
    public GameObject resultPanel;
    public TextMeshProUGUI gradeText;
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI errorDetailsText;
    public Button closeResultButton;

    [Header("图表")]
    public CurveDrawer chartDrawer;

    [Header("引用")]
    //public ActionRecorder recorder;
    public RobotExamManager examManager;

    // 内部状态
    private bool isMakingStandardTask = false;
    private bool isMultiStageExam = false;
    private bool isExamMode = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        //j机械臂记录
        savedKnobValues = new float[recorders.Length][];
        for (int i = 0; i < recorders.Length; i++)
        {
            savedKnobValues[i] = new float[knobs.Count];
            // 默认设为零点，或者从机械臂初始关节角度反推旋钮值
            for (int j = 0; j < knobs.Count; j++)
                savedKnobValues[i][j] = 0.5f;    // 假设旋钮范围0~1，中间位置
        }
        for (int i = 0; i < knobs.Count; i++)
        {
            int index = i; // 捕获变量
            knobs[i].onValueChange.AddListener((float value) => OnKnobChanged(index, value));
        }
        // 填充机械臂下拉框
        roboArmDropdown.ClearOptions();
        List<string> armNames = new List<string>();
        for (int i = 0; i < recorders.Length; i++)
            armNames.Add("机械臂" + (i + 1));
        roboArmDropdown.AddOptions(armNames);
        roboArmDropdown.onValueChanged.AddListener(OnArmChanged);
        OnArmChanged(0); // 默认选中第一个

        // --- 普通动作按钮绑定 ---
        startRecordButton.onClick.AddListener(StartActionRecord);
        stopRecordButton.onClick.AddListener(StopActionRecord);
        playActionButton.onClick.AddListener(PlaySelectedAction);
        viewChartActionButton.onClick.AddListener(ViewChartForAction);
        deleteActionButton.onClick.AddListener(DeleteSelectedAction);

        // --- 考核序列管理按钮 ---
        newSequenceButton.onClick.AddListener(CreateNewSequence);
        deleteSequenceButton.onClick.AddListener(DeleteSelectedSequence);
        deleteTaskButton.onClick.AddListener(DeleteSelectedTask);
        sequenceDropdown.onValueChanged.AddListener(OnSequenceChanged);

        // --- 标准任务制作 ---
        startStandardRecordButton.onClick.AddListener(StartStandardRecord);
        stopStandardRecordButton.onClick.AddListener(StopStandardRecord);
        markKeyNodeButton.onClick.AddListener(MarkKeyNode);

        // --- 考核 ---
        startSequenceExamButton.onClick.AddListener(StartSequenceExam);
        submitExamButton.onClick.AddListener(SubmitExamAction);
        replayDemoButton.onClick.AddListener(ReplayDemo);

        // --- 结果 ---
        closeResultButton.onClick.AddListener(() => resultPanel.SetActive(false));

        // --- 考核事件 ---
        if (examManager != null)
        {
            examManager.OnStageChanged += OnExamStageChanged;
            examManager.OnExamFinished += OnExamFinishedHandler;
        }

        RefreshActionDropdown();
        RefreshSequenceDropdown();
        UpdateActionButtons(false);
        UpdateStandardButtons(false);
        resultPanel.SetActive(false);
        submitExamButton.gameObject.SetActive(false);
        replayDemoButton.gameObject.SetActive(false);
        examStatusText.gameObject.SetActive(false);
        Prohibited();
        Open(0);
    }
    void OnArmChanged(int index)
    {
        for (int i = 0; i < knobs.Count; i++)
            savedKnobValues[currentArmIndex][i] = knobs[i].value;
        currentArmIndex = index;
        RefreshActionDropdown();
        // 如果正在录制或回放，可以自动停止（避免混乱），这里简单处理
        Prohibited();
        for (int i = 0; i < knobs.Count; i++)
        {
            knobs[i].value = savedKnobValues[index][i];
        }
        Open(currentArmIndex);
        if (CurrentRecorder.IsRecording) CurrentRecorder.StopRecording();
        if (CurrentRecorder.IsPlaying) CurrentRecorder.StopPlayback();
    }
    // ==================== 普通动作 ====================
    void StartActionRecord()
    {
        if (isMakingStandardTask) return;
        CurrentRecorder.StartRecording();
        actionStatusText.text = $"机械臂{currentArmIndex + 1} 录制中…";
        UpdateActionButtons(true);
    }

    void StopActionRecord()
    {
        //if (isExamMode)
        //{
        //    // 考核期间由 ExamManager 通过事件处理，这里仅停止录制
        //    CurrentRecorder.StopRecording();
        //    return;
        //}
        CurrentRecorder.StopRecording();
        if (CurrentRecorder.CurrentClip != null && CurrentRecorder.CurrentClip.frames.Count > 0)
        {
            ActionLibrary.Instance.AddRecord(CurrentRecorder.CurrentClip, currentArmIndex);
        }
        actionStatusText.text = "就绪";
        UpdateActionButtons(false);
        RefreshActionDropdown();
    }

    void PlaySelectedAction()
    {
        var clip = GetSelectedActionClip();
        if (clip != null)
        {
            // 如果之前有未清理的订阅，先取消
            if (activePlaybackRecorder != null)
                activePlaybackRecorder.OnPlaybackFinished -= OnPlaybackFinishedForAction;

            activePlaybackRecorder = CurrentRecorder;
            activePlaybackRecorder.OnPlaybackFinished += OnPlaybackFinishedForAction;

            CurrentRecorder.PlayClip(clip);
            UpdateActionButtons(true);
        }
    }
    private void OnPlaybackFinishedForAction()
    {
        if (activePlaybackRecorder != null)
            activePlaybackRecorder.OnPlaybackFinished -= OnPlaybackFinishedForAction;
        activePlaybackRecorder = null;
        UpdateActionButtons(false);
        ShowMessage("播放完成");
    }

    void ViewChartForAction()
    {
        var records = ActionLibrary.Instance.GetRecordsByArm(currentArmIndex);
        int idx = actionDropdown.value;
        if (idx < 0 || idx >= records.Count) return;
        chartDrawer.DrawCurveForRecord(records[idx]);
    }

    void DeleteSelectedAction()
    {
        var records = ActionLibrary.Instance.GetRecordsByArm(currentArmIndex);
        int idx = actionDropdown.value;
        if (idx < 0 || idx >= records.Count) return;
        ActionLibrary.Instance.DeleteRecord(records[idx]);
        RefreshActionDropdown();
        ShowMessage("动作已删除");
    }

    // 获取当前机械臂选中的动作 Clip
    ActionClip GetSelectedActionClip()
    {
        var records = ActionLibrary.Instance.GetRecordsByArm(currentArmIndex);
        int idx = actionDropdown.value;
        return (idx >= 0 && idx < records.Count) ? records[idx].clip : null;
    }
    void RefreshActionDropdown()
    {
        actionDropdown.ClearOptions();
        var records = ActionLibrary.Instance.GetRecordsByArm(currentArmIndex);
        List<string> names = new List<string>();
        foreach (var r in records) names.Add(r.actionName);
        actionDropdown.AddOptions(names);
    }

    void UpdateActionButtons(bool isBusy)
    {
        //startRecordButton.interactable = !isBusy && !isExamMode;
        stopRecordButton.interactable = isBusy || isExamMode;
        //playActionButton.interactable = !isBusy && !isExamMode;
        viewChartActionButton.interactable = !isBusy;
        //deleteActionButton.interactable = !isBusy && !isExamMode;
    }

    // ==================== 考核序列管理 ====================
    void CreateNewSequence()
    {
        ExamSequence seq = new ExamSequence();
        seq.sequenceName = ExamSequenceLibrary.Instance.GenerateDefaultSequenceName();
        seq.stages = new List<ExamStage>();
        ExamSequenceLibrary.Instance.AddSequence(seq);
        RefreshSequenceDropdown();
        sequenceDropdown.value = ExamSequenceLibrary.Instance.sequences.Count - 1;
    }
        void DeleteSelectedSequence()
    {
            int idx = sequenceDropdown.value;
            if (idx < 0 || idx >= ExamSequenceLibrary.Instance.sequences.Count) return;
            string name = ExamSequenceLibrary.Instance.sequences[idx].sequenceName;
            ExamSequenceLibrary.Instance.DeleteSequence(idx);
            RefreshSequenceDropdown();
            ShowMessage("已删除序列：" + name);
        }

    void DeleteSelectedTask()
    {
        int seqIdx = sequenceDropdown.value;
        int taskIdx = taskDropdown.value;
        if (seqIdx < 0 || seqIdx >= ExamSequenceLibrary.Instance.sequences.Count) return;
        var seq = ExamSequenceLibrary.Instance.sequences[seqIdx];
        if (taskIdx < 0 || taskIdx >= seq.stages.Count) return;
        seq.stages.RemoveAt(taskIdx);
        ExamSequenceLibrary.Instance.SaveSequencesToFile();
        RefreshTaskDropdown();
        string taskName = seq.stages[taskIdx].stageName;
        ShowMessage("已删除任务：" + taskName);
    }

    void OnSequenceChanged(int index)
    {
        RefreshTaskDropdown();
    }

    void RefreshSequenceDropdown()
    {
        sequenceDropdown.onValueChanged.RemoveListener(OnSequenceChanged);
        sequenceDropdown.ClearOptions();
        var names = new List<string>();
        foreach (var seq in ExamSequenceLibrary.Instance.sequences) names.Add(seq.sequenceName);
        sequenceDropdown.AddOptions(names);
        if (ExamSequenceLibrary.Instance.sequences.Count > 0) sequenceDropdown.value = 0;
        sequenceDropdown.onValueChanged.AddListener(OnSequenceChanged);
        RefreshTaskDropdown();
    }

    void RefreshTaskDropdown()
    {
        taskDropdown.ClearOptions();
        int seqIdx = sequenceDropdown.value;
        if (seqIdx < 0 || seqIdx >= ExamSequenceLibrary.Instance.sequences.Count) return;
        var seq = ExamSequenceLibrary.Instance.sequences[seqIdx];
        List<string> taskNames = new List<string>();
        foreach (var stage in seq.stages)
            taskNames.Add($"[臂{stage.armIndex + 1}] {stage.stageName}");
        taskDropdown.AddOptions(taskNames);
    }

    // ==================== 标准任务制作（保存到当前序列）====================
    void StartStandardRecord()
    {
        if (CurrentRecorder.IsRecording || isExamMode) return;
        CurrentRecorder.ResetKeyNodes();
        CurrentRecorder.StartRecording();
        isMakingStandardTask = true;
        sequenceStatusText.text = "制作中… 按“标记节点”添加关键帧";
        UpdateStandardButtons(true);
    }

    void StopStandardRecord()
    {
        if (!isMakingStandardTask) return;
        CurrentRecorder.StopRecording();
        if (CurrentRecorder.CurrentClip == null || CurrentRecorder.CurrentClip.frames.Count == 0)
        {
            isMakingStandardTask = false;
            UpdateStandardButtons(false);
            return;
        }

        StandardTask task = new StandardTask();
        task.taskName = "任务_" + System.DateTime.Now.ToString("HHmmss");
        task.standardClip = CurrentRecorder.CurrentClip;
        task.keyNodes = new List<KeyNode>(CurrentRecorder.CurrentKeyNodes);

        // 获取当前序列，没有则创建
        ExamSequence currentSeq = GetCurrentSequence();
        if (currentSeq == null)
        {
            ExamSequence newSeq = new ExamSequence();
            newSeq.sequenceName = ExamSequenceLibrary.Instance.GenerateDefaultSequenceName();
            newSeq.stages = new List<ExamStage>();
            ExamSequenceLibrary.Instance.AddSequence(newSeq);
            RefreshSequenceDropdown();
            sequenceDropdown.value = ExamSequenceLibrary.Instance.sequences.Count - 1;
            currentSeq = newSeq;
        }

        ExamStage stage = new ExamStage
        {
            stageName = task.taskName,
            task = task,
            armIndex = currentArmIndex    //关联当前机械臂
        };
        currentSeq.stages.Add(stage);
        ExamSequenceLibrary.Instance.SaveSequencesToFile();

        isMakingStandardTask = false;
        sequenceStatusText.text = "已保存到序列：" + currentSeq.sequenceName;
        UpdateStandardButtons(false);
        RefreshTaskDropdown();
        CurrentRecorder.ResetCurrentRecording();
    }

    ExamSequence GetCurrentSequence()
    {
        int idx = sequenceDropdown.value;
        if (idx >= 0 && idx < ExamSequenceLibrary.Instance.sequences.Count)
            return ExamSequenceLibrary.Instance.sequences[idx];
        return null;
    }

    void MarkKeyNode()
    {
        if (!isMakingStandardTask) return;
        if (CurrentRecorder == null)
        {
            Debug.LogError("当前机械臂记录器为空，无法标记节点");
            return;
        }
        CurrentRecorder.AddKeyNode();
        ShowMessage("关键节点已添加");
    }

    void UpdateStandardButtons(bool isRecording)
    {
        startStandardRecordButton.interactable = !isRecording;
        stopStandardRecordButton.interactable = isRecording;
        markKeyNodeButton.interactable = isRecording;
        startSequenceExamButton.interactable = !isRecording && !isMultiStageExam;
        deleteSequenceButton.interactable = !isRecording;
        deleteTaskButton.interactable = !isRecording;
    }

    // ==================== 多阶段考核 ====================
    public void EnterExamMode()
    {
        isExamMode = true;
        examStatusText.gameObject.SetActive(true);
        submitExamButton.gameObject.SetActive(true);
        replayDemoButton.gameObject.SetActive(true);
        // 禁用序列编辑相关按钮，保留普通动作面板
        startStandardRecordButton.interactable = false;
        newSequenceButton.interactable = false;
        deleteSequenceButton.interactable = false;
        deleteTaskButton.interactable = false;
        // 提交按钮初始不可用，等演示播放完且学生有录制动作后再启用
    }

    public void ExitExamMode()
    {
        isExamMode = false;
        examStatusText.gameObject.SetActive(false);
        submitExamButton.gameObject.SetActive(false);
        replayDemoButton.gameObject.SetActive(false);
        // 恢复序列编辑
        startStandardRecordButton.interactable = true;
        newSequenceButton.interactable = true;
        deleteSequenceButton.interactable = true;
        deleteTaskButton.interactable = true;
        UpdateActionButtons(false);
    }
    // 当状态更新时，由 ExamManager 事件调用
    public void OnExamStageChanged(int index, string name)
    {
        examStatusText.text = $"考核阶段：{index + 1} - {name}";
        if (examManager.CurrentState == RobotExamManager.ExamState.WaitingForSubmission)
        {
            submitExamButton.interactable = true;   // 等待提交时启用
            replayDemoButton.interactable = true;
        }
        else
        {
            submitExamButton.interactable = false;
            replayDemoButton.interactable = false;
        }
    }
        void StartSequenceExam()
    {
        int seqIdx = sequenceDropdown.value;
        if (seqIdx < 0 || seqIdx >= ExamSequenceLibrary.Instance.sequences.Count) return;
        if (ExamSequenceLibrary.Instance.sequences[seqIdx].stages.Count == 0)
        {
            ShowMessage("该序列无任务");
            return;
        }
        examManager.StartSequenceExam(seqIdx, recorders); // 传入 recorders 引用
    }

    // 设置当前机械臂索引（由 ExamManager 调用）
    public void SetCurrentArmIndex(int index)
    {
        if (index >= 0 && index < recorders.Length)
        {
            roboArmDropdown.value = index;
        }
    }
    private void OnExamFinishedHandler(WholeResult result)
    {
        
    }


    void SubmitExamAction()
    {
        Debug.Log(2);
        if (!isExamMode) return;
        var records = ActionLibrary.Instance.GetRecordsByArm(currentArmIndex);
        int idx = actionDropdown.value;
        if (idx < 0 || idx >= records.Count)
        {
            ShowMessage("请先录制或选择一个动作");
            return;
        }
        examManager.SubmitAction(records[idx].clip);
    }

    void ReplayDemo()
    {
        if (!isExamMode) return;
        examManager.ReplayCurrentDemo();
    }
    public void EnterMultiStageExamMode()
    {
        isMultiStageExam = true;
        // 禁用制作按钮等
        UpdateStandardButtons(false);
        UpdateActionButtons(false);
    }

    public void ExitMultiStageExamMode()
    {
        isMultiStageExam = false;
        UpdateStandardButtons(false);
        UpdateActionButtons(false);
    }

    // ==================== 结果展示 ====================
    public void ShowOverallResult(WholeResult result)
    {
        resultPanel.SetActive(true);
        gradeText.text = "综合评级：" + result.finalGrade;
        string details = "";
        foreach (var sr in result.stageResults)
        {
            string pass = sr.passed ? "通过" : "未通过";
            details += $"{sr.stageName}：{pass}  用时{sr.timeUsed:F1}s (比例{sr.timeRatio:F2})\n";
            if (!sr.passed)
            {
                foreach (var e in sr.errors)
                    details += $"  节点{e.nodeIndex} {e.jointName} 期望{e.expectedAngle:F1}° 实际{e.actualAngle:F1}°\n";
            }
        }
        errorDetailsText.text = details;
        timeText.text = ""; // 可放总评
    }
    public void ShowStageResult(StageResult sr)
    {
        resultPanel.SetActive(true);
        gradeText.text = sr.passed ? "本阶段：通过" : "本阶段：未通过";
        timeText.text = $"用时 {sr.timeUsed:F1}s (比例{sr.timeRatio:F2})";
        string details = "";
        foreach (var e in sr.errors)
            details += $"节点{e.nodeIndex} {e.jointName} 期望{e.expectedAngle:F1}° 实际{e.actualAngle:F1}°\n";
        errorDetailsText.text = details;
        // 几秒后可自动关闭或手动关闭
        //StartCoroutine(AutoCloseResult(3f));
    }

    IEnumerator AutoCloseResult(float delay)
    {
        yield return new WaitForSeconds(delay);
        resultPanel.SetActive(false);
    }
    // ================== 通用辅助 ==================
    public void ShowMessage(string msg)
    {
        actionStatusText.text = msg;
        // 也可显示在通用 Text 上
    }
    public void Prohibited()
    {
        foreach (var item in recorders)
        {
            JointKnobLink[] j = item.GetComponentsInChildren<JointKnobLink>();
            foreach (JointKnobLink jkl in j)
            {
                jkl.enabled=false;
            }
        }
    }
    public void Open(int i)
    {
        JointKnobLink[] j = recorders[i].GetComponentsInChildren<JointKnobLink>();
        foreach (JointKnobLink jkl in j)
        {
            jkl.enabled = true;
        }
    }
    void OnKnobChanged(int knobIndex, float value)
    {
        if (currentArmIndex < 0 || currentArmIndex >= recorders.Length) return;
        // 保存当前机械臂的旋钮值
        savedKnobValues[currentArmIndex][knobIndex] = value;

        // 驱动当前机械臂的对应关节（通过 JointKnobLink 或直接设置）
        // 我们将在 JointKnobLink 中处理，这里只需保存值
        // 如果旋钮变化时 JointKnobLink 是启用的，它会自动更新关节
    }
}

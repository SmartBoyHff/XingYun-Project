using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
// 文件名：VR4_UIManager
// 模块：模块4 - 维护保养
// 功能：维护保养 UI 管理器，负责开始面板、步骤面板、答题面板和流程提示显示。
// 创建日期：2026-05-5
// 最后更新：2026-05-5
// ============================================================

namespace VRHelmet.VRTeam.Maintenance
{
    /// <summary>
    /// 维护维修系统 UI 管理器。负责初始面板、步骤面板、答题面板、步骤文本、倒计时和结算文本的显示。
    /// </summary>
    /// <summary>
    /// VR4_UIManager 类型说明
    /// 
    /// 【功能说明】
    /// 1. 属于模块4 - 维护保养中的 VR4_UIManager 类型。
    /// 2. 负责管理开始面板、步骤面板、答题面板和流程提示。
    /// 3. 与维护保养流程中的步骤数据、交互逻辑或 UI 显示协同工作。
    /// 
    /// 【依赖组件】
    /// - Unity 组件体系。
    /// - VRHelmet.VRTeam.Maintenance 维护保养流程脚本。
    /// </summary>
    public class VR4_UIManager : VR4_SingletonMonoBehaviour<VR4_UIManager>
    {
        #region ==========Field==========
        /// <summary>
        /// 当前实验流程使用的步骤数据来源。
        /// </summary>
        public VR4_StepData stepData;

        /// <summary>
        /// 实验流程管理器，负责发出步骤、答题、计时和结算事件。
        /// </summary>
        public VR4_ExperimentManager experimentManager;

        /// <summary>
        /// 答题系统管理器，用于进入指定题表的答题流程。
        /// </summary>
        public VR4_TopicManager daotiManger;

        [Header("Panel")]
        /// <summary>
        /// 初始面板。场景开始时显示，工作单按钮点击后关闭，答题完成后重新打开。
        /// </summary>
        public GameObject startPanel;

        /// <summary>
        /// 答题面板。初始关闭，点击“开始填写工单”按钮后打开。
        /// </summary>
        public GameObject topicPanel;

        /// <summary>
        /// 步骤面板。工作单按钮点击后打开，进入答题或回到初始面板时关闭。
        /// </summary>
        public GameObject stepPanel;

        [Header("Button")]
        /// <summary>
        /// 开始填写工单按钮。初始不可交互，当前步骤完成并进入待答题状态后才可点击。
        /// </summary>
        public Button startFillWorkOrderButton;

        [Header("Text")]
        /// <summary>
        /// 步骤说明或结算信息显示文本。
        /// </summary>
        public TextMeshProUGUI displayText;

        /// <summary>
        /// 考试倒计时显示文本。
        /// </summary>
        public TextMeshProUGUI timeText;

        /// <summary>
        /// 操作提示显示文本。
        /// </summary>
        public TextMeshProUGUI tipText;

        /// <summary>
        /// 静态提示文本引用，供工具脚本快速显示提示。
        /// </summary>
        public static TextMeshProUGUI TipText;

        [Header("Other")]
        /// <summary>
        /// 步骤语音播放源。
        /// </summary>
        public AudioSource eleAudioSource;

        /// <summary>
        /// 当前正在运行的步骤语音或文本等待协程。
        /// </summary>
        public Coroutine currentAudioCoroutine;

        private int currentTopicTableIndex = 0;
        private int pendingTopicCount = 0;
        private bool hasPendingTopic = false;
        #endregion

        #region ==========Unity Method==========
        private void Start()
        {
            if (experimentManager == null)
            {
                experimentManager = VR4_ExperimentManager.Instance;
            }

            SubscribeExperimentEvents();
            BindButtonEvents();
            TipText = tipText;
            ShowStartPanel();
        }

        private void OnDestroy()
        {
            UnbindButtonEvents();
            UnsubscribeExperimentEvents();
        }
        #endregion

        #region ==========Logic==========
        private void SubscribeExperimentEvents()
        {
            if (experimentManager == null) return;

            experimentManager.StepStarted += OnStepStarted;
            experimentManager.TopicStarted += OnTopicStarted;
            experimentManager.TimerChanged += OnTimerChanged;
            experimentManager.ExamEnded += EndExam;
            experimentManager.StepRangeCompleted += OnStepRangeCompleted;
        }

        private void UnsubscribeExperimentEvents()
        {
            if (experimentManager == null) return;

            experimentManager.StepStarted -= OnStepStarted;
            experimentManager.TopicStarted -= OnTopicStarted;
            experimentManager.TimerChanged -= OnTimerChanged;
            experimentManager.ExamEnded -= EndExam;
            experimentManager.StepRangeCompleted -= OnStepRangeCompleted;
        }

        private void BindButtonEvents()
        {
            if (startFillWorkOrderButton != null)
            {
                startFillWorkOrderButton.onClick.AddListener(OnStartFillWorkOrderClicked);
            }
        }

        private void UnbindButtonEvents()
        {
            if (startFillWorkOrderButton != null)
            {
                startFillWorkOrderButton.onClick.RemoveListener(OnStartFillWorkOrderClicked);
            }
        }

        private void InitiateProcess(int startIndex, int endIndex)
        {
            if (experimentManager == null)
            {
                return;
            }

            experimentManager.enabled = true;
            hasPendingTopic = false;
            pendingTopicCount = 0;
            SetStartFillButtonInteractable(false);
            ShowStepPanel();
            experimentManager.BeginStepRange(startIndex, endIndex);
        }

        private void OnStepStarted(OperateStep step, int index)
        {
            ResetTextAndAudio(step);
            PlayStep(step);
        }

        private void OnTopicStarted(OperateStep step, int index)
        {
            pendingTopicCount = step.topicMount;
            hasPendingTopic = true;
            ShowStepPanel();
            SetStartFillButtonInteractable(true);
        }

        private void OnStartFillWorkOrderClicked()
        {
            if (!hasPendingTopic)
            {
                return;
            }

            ShowTopicPanel();
            SetStartFillButtonInteractable(false);
            ResolveTopicManager();

            if (daotiManger != null)
            {
                daotiManger.BeginTopic(pendingTopicCount, currentTopicTableIndex);
            }
            else
            {
                Debug.LogWarning("VR4_UIManager 未绑定 VR4_TopicManager，无法打开指定 TopicTable");
            }
        }

        private void ResolveTopicManager()
        {
            if (daotiManger != null)
            {
                return;
            }

            daotiManger = FindObjectOfType<VR4_TopicManager>(true);
        }

        private void OnStepRangeCompleted(int startIndex, int endIndex)
        {
            ShowStartPanel();
        }

        private void OnTimerChanged(float remainSeconds)
        {
            if (timeText != null)
            {
                timeText.text = Mathf.Max(0f, remainSeconds).ToString("F0") + "s";
            }
        }

        private void SetStartFillButtonInteractable(bool interactable)
        {
            if (startFillWorkOrderButton != null)
            {
                startFillWorkOrderButton.interactable = interactable;
            }
        }

        private string CalculateScore(float score)
        {
            if (score >= 8.6f) return "A级";
            if (score >= 7.0f) return "B级";
            if (score >= 5.1f) return "C级";
            return "D级";
        }

        private System.Collections.IEnumerator PlayStepAudios(Step step)
        {
            if (eleAudioSource == null || step.stepClip == null)
            {
                yield break;
            }

            eleAudioSource.Stop();
            eleAudioSource.clip = step.stepClip;
            eleAudioSource.Play();
            yield return new WaitForSeconds(step.stepClip.length);
        }

        private System.Collections.IEnumerator PlayStepTexts(Step step)
        {
            yield return new WaitForSeconds(2f);
        }
        #endregion

        #region ==========API==========
        /// <summary>
        /// 从工作单按钮指定的单个步骤开始执行实验流程，题表默认使用 0 号表。
        /// </summary>
        /// <param name="stepIndex">工作单对应的步骤索引。</param>
        public void StartWorkOrder(int stepIndex)
        {
            StartWorkOrder(stepIndex, stepIndex, 0);
        }

        /// <summary>
        /// 从工作单按钮指定的单个步骤开始执行实验流程，并指定要使用的答题表。
        /// </summary>
        /// <param name="stepIndex">工作单对应的步骤索引。</param>
        /// <param name="topicTableIndex">答题管理器 topicTables 中的题表索引。</param>
        public void StartWorkOrder(int stepIndex, int topicTableIndex)
        {
            StartWorkOrder(stepIndex, stepIndex, topicTableIndex);
        }

        /// <summary>
        /// 从工作单按钮指定的步骤区间开始执行实验流程，并指定区间内步骤答题时使用的题表。
        /// </summary>
        /// <param name="startIndex">起始步骤索引，包含。</param>
        /// <param name="endIndex">结束步骤索引，包含。</param>
        /// <param name="topicTableIndex">答题管理器 topicTables 中的题表索引。</param>
        public void StartWorkOrder(int startIndex, int endIndex, int topicTableIndex)
        {
            if (experimentManager == null)
            {
                experimentManager = VR4_ExperimentManager.Instance;
            }

            currentTopicTableIndex = topicTableIndex;
            InitiateProcess(startIndex, endIndex);
        }

        /// <summary>
        /// 打开初始面板，关闭步骤面板和答题面板，并禁用“开始填写工单”按钮。
        /// </summary>
        public void ShowStartPanel()
        {
            hasPendingTopic = false;
            pendingTopicCount = 0;
            SetStartFillButtonInteractable(false);

            if (startPanel != null)
            {
                startPanel.SetActive(true);
            }

            if (stepPanel != null)
            {
                stepPanel.SetActive(false);
            }

            if (topicPanel != null)
            {
                topicPanel.SetActive(false);
            }
        }

        /// <summary>
        /// 打开步骤面板，关闭初始面板和答题面板。
        /// </summary>
        public void ShowStepPanel()
        {
            if (startPanel != null)
            {
                startPanel.SetActive(false);
            }

            if (stepPanel != null)
            {
                stepPanel.SetActive(true);
            }

            if (topicPanel != null)
            {
                topicPanel.SetActive(false);
            }
        }

        /// <summary>
        /// 打开答题面板，关闭初始面板和步骤面板。
        /// </summary>
        public void ShowTopicPanel()
        {
            if (startPanel != null)
            {
                startPanel.SetActive(false);
            }

            if (stepPanel != null)
            {
                stepPanel.SetActive(false);
            }

            if (topicPanel != null)
            {
                topicPanel.SetActive(true);
            }
        }

        /// <summary>
        /// 使用当前实验总分结束实验并显示结算文本。
        /// </summary>
        public void EndExam()
        {
            EndExam(experimentManager != null ? experimentManager.totalScore : 0f);
        }

        /// <summary>
        /// 按指定分数结束实验并显示评分等级。
        /// </summary>
        /// <param name="score">需要展示和评级的总分。</param>
        public void EndExam(float score)
        {
            string grade = CalculateScore(score);

            if (displayText != null)
            {
                displayText.text = "总得分: " + score + "     等级: " + grade;
            }

            ShowTip("");
        }

        /// <summary>
        /// 进入下一步骤时清空旧文本、停止旧音频，并显示当前步骤文本。
        /// </summary>
        /// <param name="currentStep">当前开始执行的步骤数据。</param>
        public void ResetTextAndAudio(Step currentStep)
        {
            if (VR4_ExperimentManager.IsTest)
            {
                return;
            }

            if (currentStep.stepDescribe != null && displayText != null)
            {
                displayText.text = "";
                displayText.text = currentStep.stepText;
            }

            if (currentAudioCoroutine != null)
            {
                StopCoroutine(currentAudioCoroutine);
                currentAudioCoroutine = null;
            }

            if (eleAudioSource != null)
            {
                eleAudioSource.Stop();
            }

            ShowTip("");
        }

        /// <summary>
        /// 在非考试模式下显示全局提示文本。
        /// </summary>
        /// <param name="tip">要显示的提示内容。</param>
        public static void ShowTip(string tip)
        {
            if (!VR4_ExperimentManager.IsTest && TipText != null)
            {
                TipText.text = tip;
            }
        }

        /// <summary>
        /// 播放当前步骤的语音提示；没有语音时保留原来的文本等待流程。
        /// </summary>
        /// <param name="currentStep">当前步骤数据。</param>
        public void PlayStep(Step currentStep)
        {
            if (currentStep.stepClip != null && !VR4_ExperimentManager.IsTest)
            {
                currentAudioCoroutine = StartCoroutine(PlayStepAudios(currentStep));
            }
            else if (currentStep.stepDescribe != null)
            {
                currentAudioCoroutine = StartCoroutine(PlayStepTexts(currentStep));
            }
        }
        #endregion
    }
}

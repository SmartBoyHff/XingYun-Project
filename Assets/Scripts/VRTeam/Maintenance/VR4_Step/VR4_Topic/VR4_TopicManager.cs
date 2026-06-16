using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
// 文件名：VR4_TopicManager
// 模块：模块4 - 维护保养
// 功能：答题系统管理器，负责 TopicTable 显示、题目填充、重填、完成和判分流程。
// 创建日期：2026-05-5
// 最后更新：2026-05-5
// ============================================================

namespace VRHelmet.VRTeam.Maintenance
{
    /// <summary>
    /// 答题系统管理器。当前主流程使用 TopicTable 表格模式：每张表自由配置 TopicUI 列表和 TopicData 列表，并按顺序一一填入。
    /// </summary>
    /// <summary>
    /// VR4_TopicManager 类型说明
    /// 
    /// 【功能说明】
    /// 1. 属于模块4 - 维护保养中的 VR4_TopicManager 类型。
    /// 2. 负责管理题表显示、题目填充、重填、完成和判分流程。
    /// 3. 与维护保养流程中的步骤数据、交互逻辑或 UI 显示协同工作。
    /// 
    /// 【依赖组件】
    /// - Unity 组件体系。
    /// - VRHelmet.VRTeam.Maintenance 维护保养流程脚本。
    /// </summary>
    public class VR4_TopicManager : MonoBehaviour
    {
        #region ==========Field==========
        /// <summary>
        /// 实验流程管理器。答题完成后会通知它继续推进流程，并把正确题目的分数累加到 totalScore。
        /// </summary>
        public VR4_ExperimentManager expManager;

        /// <summary>
        /// UI 管理器。答题完成后用于关闭答题面板并打开工作单面板。
        /// </summary>
        public VR4_UIManager uiManager;

        [Header("Table Button")]
        /// <summary>
        /// 重新填写当前题表按钮。点击后会重置当前表内所有 TopicUI 的下拉框选项，并恢复可交互状态。
        /// </summary>
        public Button resetTableButton;

        /// <summary>
        /// 完成当前题表按钮。点击后统一校验所有 TopicUI 的选择，并根据 TopicData.choiceAnswer 判分。
        /// </summary>
        public Button finishTableButton;

        [Header("Table UI")]
        /// <summary>
        /// 当前题表标题文本。打开题目面板并切换 TopicTable 时，会显示当前 TopicTable.tableName。
        /// </summary>
        public TextMeshProUGUI tableTitle;

        [Header("Table Topic")]
        /// <summary>
        /// 所有可用题表。每个工作单按钮可以通过 topicTableIndex 指定打开这里的某一张表。
        /// </summary>
        public List<TopicTable> topicTables = new List<TopicTable>();

        /// <summary>
        /// 当前正在显示和判分的题表索引，可由 VR4_ExperimentBtn 传入。
        /// </summary>
        public int currentTableIndex = 0;

        [Header("Legacy Single Topic")]
        /// <summary>
        /// 旧单题模式题干文本。仅在没有配置 TopicTable 时使用。
        /// </summary>
        public TextMeshProUGUI topicText;

        /// <summary>
        /// 旧单题模式题目图片。仅在没有配置 TopicTable 时使用。
        /// </summary>
        public Image image;

        /// <summary>
        /// 旧单题模式提交按钮。仅在没有配置 TopicTable 时使用。
        /// </summary>
        public Button submitBtn;

        /// <summary>
        /// 旧单题模式当前题目索引。
        /// </summary>
        public int currentTopicIndex = 0;

        /// <summary>
        /// 旧单题模式当前步骤需要回答的题目数量。
        /// </summary>
        public int topicCount;

        [Header("Legacy Choice Topic")]
        /// <summary>
        /// 旧单题模式选择题 Toggle 列表。仅在没有配置 TopicTable 时使用。
        /// </summary>
        public List<Toggle> optionToggles = new List<Toggle>();

        /// <summary>
        /// 旧单题模式选择题选项文本列表。仅在没有配置 TopicTable 时使用。
        /// </summary>
        public List<TextMeshProUGUI> optionLabels = new List<TextMeshProUGUI>();

        [Header("Legacy Input Topic")]
        /// <summary>
        /// 旧单题模式填空输入框。仅在没有配置 TopicTable 时使用。
        /// </summary>
        public TMP_InputField inputField;

        [Header("Legacy Topic Content")]
        /// <summary>
        /// 旧单题模式题库。当前表格模式请改用 TopicTable.topicDatas。
        /// </summary>
        public List<TopicData> topicList = new List<TopicData>();

        /// <summary>
        /// 旧单题模式题目音频播放源。
        /// </summary>
        public AudioSource audioSource;

        private bool isDone = false;
        private bool currentTableSubmitted = false;
        private int currentTableTopicCount = 0;
        #endregion

        #region ==========Unity Method==========
        private void Start()
        {
            currentTopicIndex = 0;

            if (expManager == null)
            {
                expManager = VR4_ExperimentManager.Instance;
            }

            if (uiManager == null && VR4_UIManager.HasInstance)
            {
                uiManager = VR4_UIManager.Instance;
            }

            if (submitBtn != null)
            {
                submitBtn.onClick.AddListener(OnSubmitClicked);
            }

            if (resetTableButton != null)
            {
                resetTableButton.onClick.AddListener(ResetCurrentTable);
            }

            if (finishTableButton != null)
            {
                finishTableButton.onClick.AddListener(FinishCurrentTable);
            }

            HideAllTopicTables();
        }

        private void OnDestroy()
        {
            if (submitBtn != null)
            {
                submitBtn.onClick.RemoveListener(OnSubmitClicked);
            }

            if (resetTableButton != null)
            {
                resetTableButton.onClick.RemoveListener(ResetCurrentTable);
            }

            if (finishTableButton != null)
            {
                finishTableButton.onClick.RemoveListener(FinishCurrentTable);
            }
        }
        #endregion

        #region ==========Logic==========
        private bool HasTopicTables()
        {
            return topicTables != null && topicTables.Count > 0;
        }

        private TopicTable GetCurrentTable()
        {
            if (!HasTopicTables())
            {
                return null;
            }

            currentTableIndex = Mathf.Clamp(currentTableIndex, 0, topicTables.Count - 1);
            return topicTables[currentTableIndex];
        }

        private void OnSubmitClicked()
        {
            if (HasTopicTables())
            {
                return;
            }

            if (!isDone) return;

            if (image != null)
            {
                image.gameObject.SetActive(false);
            }

            if (topicList == null || topicCount <= 0 || currentTopicIndex >= topicList.Count)
            {
                CompleteTopicSession();
                return;
            }

            TopicData currentTopic = topicList[currentTopicIndex];
            bool isCorrect = false;

            try
            {
                if (currentTopic.isInputTopic)
                {
                    isCorrect = CheckInputAnswer(currentTopic);
                }
                else if (currentTopic.isChoiceTopic)
                {
                    isCorrect = CheckChoiceAnswer(currentTopic);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"答题出错: {e.Message}");
                isCorrect = false;
            }

            FinishTopic(currentTopicIndex, isCorrect);

            if (isCorrect && expManager != null)
            {
                expManager.totalScore += currentTopic.topicScore;
            }

            topicCount--;
            currentTopicIndex++;

            if (currentTopicIndex < topicList.Count && topicCount > 0)
            {
                SetTopic();
            }
            else
            {
                CompleteTopicSession();
            }
        }

        private void OnToggleValueChanged(bool isOn)
        {
            isDone = isOn;
        }

        private void OnInputValueChanged(string value)
        {
            isDone = !string.IsNullOrWhiteSpace(value);
        }

        private void OnTableDropdownAnswered(VR4_TopicUI topicUI)
        {
            RefreshTableButtons();
        }

        private void CompleteTopicSession()
        {
            HideAllTopicTables();

            bool notifiedFlow = false;

            if (expManager != null)
            {
                //notifiedFlow = expManager.NotifyTopicCompleted();
                expManager.NotifyTopicCompleted();
            }

            if (!notifiedFlow && uiManager != null)
            {
                uiManager.ShowStartPanel();
            }
        }

        private void SetDisplay(TopicData currentTopic)
        {
            if (image != null)
            {
                image.sprite = currentTopic.topicImage;
                image.gameObject.SetActive(currentTopic.topicImage != null);
            }

            if (audioSource != null && currentTopic.audioClip != null)
            {
                audioSource.Stop();
                audioSource.clip = currentTopic.audioClip;
                audioSource.Play();
            }

            if (topicText != null)
            {
                topicText.text = currentTopic.topicText;
            }

            isDone = false;
        }

        private bool CheckInputAnswer(TopicData topic)
        {
            return inputField != null &&
                   !string.IsNullOrEmpty(inputField.text) &&
                   !string.IsNullOrEmpty(topic.inputAnswer) &&
                   inputField.text.Trim().Equals(topic.inputAnswer.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private bool CheckChoiceAnswer(TopicData topic)
        {
            if (topic.choiceOptions == null)
            {
                Debug.LogError("选择题选项为空");
                return false;
            }

            if (topic.choiceAnswer >= topic.choiceOptions.Count)
            {
                Debug.LogError($"选择题答案索引无效: {topic.choiceAnswer}");
                return false;
            }

            for (int i = 0; i < optionToggles.Count; i++)
            {
                if (i < topic.choiceOptions.Count && optionToggles[i].isOn)
                {
                    return i == topic.choiceAnswer;
                }
            }

            return false;
        }

        private void ShowCurrentTopicTable(int requestedTopicCount)
        {
            TopicTable table = GetCurrentTable();
            if (table == null)
            {
                CompleteTopicSession();
                return;
            }

            HideAllTopicTables();
            EnsureTableTopicUIs(table);

            if (table.tableRoot != null)
            {
                table.tableRoot.SetActive(true);
            }

            RefreshTableTitle(table);

            currentTableSubmitted = false;
            currentTableTopicCount = CalculateTableTopicCount(table, requestedTopicCount);
            WarnIfTableTopicCountLimited(table, requestedTopicCount, currentTableTopicCount);

            for (int i = 0; i < table.topicUIs.Count; i++)
            {
                VR4_TopicUI topicUI = table.topicUIs[i];
                if (topicUI == null)
                {
                    continue;
                }

                topicUI.AnswerSelected -= OnTableDropdownAnswered;

                if (i < currentTableTopicCount)
                {
                    TopicData topicData = table.topicDatas[i];
                    WarnIfTopicDataOptionsEmpty(table, topicData, i);
                    topicUI.SetData(i, topicData.topicText, topicData.choiceOptions, topicData.choiceAnswer, topicData.topicScore);
                    topicUI.AnswerSelected += OnTableDropdownAnswered;
                }
                else
                {
                    topicUI.ClearData();
                }
            }

            RefreshTableButtons();
        }

        private int CalculateTableTopicCount(TopicTable table, int requestedTopicCount)
        {
            if (table == null || table.topicUIs == null || table.topicDatas == null)
            {
                return 0;
            }

            int maxCount = Mathf.Min(table.topicUIs.Count, table.topicDatas.Count);
            if (requestedTopicCount > 0)
            {
                maxCount = Mathf.Min(maxCount, requestedTopicCount);
            }

            return maxCount;
        }

        private void EnsureTableTopicUIs(TopicTable table)
        {
            if (table == null)
            {
                return;
            }

            if (table.topicUIs == null)
            {
                table.topicUIs = new List<VR4_TopicUI>();
            }

            for (int i = table.topicUIs.Count - 1; i >= 0; i--)
            {
                if (table.topicUIs[i] == null)
                {
                    table.topicUIs.RemoveAt(i);
                }
            }

            if (table.tableRoot == null)
            {
                return;
            }

            VR4_TopicUI[] childTopicUIs = table.tableRoot.GetComponentsInChildren<VR4_TopicUI>(true);
            foreach (VR4_TopicUI topicUI in childTopicUIs)
            {
                if (topicUI != null && !table.topicUIs.Contains(topicUI))
                {
                    table.topicUIs.Add(topicUI);
                }
            }
        }

        private void WarnIfTableTopicCountLimited(TopicTable table, int requestedTopicCount, int resolvedTopicCount)
        {
            if (table == null || requestedTopicCount <= 0 || resolvedTopicCount >= requestedTopicCount)
            {
                return;
            }

            int topicUICount = table.topicUIs != null ? table.topicUIs.Count : 0;
            int topicDataCount = table.topicDatas != null ? table.topicDatas.Count : 0;
            Debug.LogWarning($"TopicTable '{table.tableName}' requested {requestedTopicCount} topics, but only {resolvedTopicCount} can be filled. TopicUIs={topicUICount}, TopicDatas={topicDataCount}.");
        }

        private void WarnIfTopicDataOptionsEmpty(TopicTable table, TopicData topicData, int index)
        {
            if (topicData != null && topicData.choiceOptions != null && topicData.choiceOptions.Count > 0)
            {
                return;
            }

            string tableName = table != null ? table.tableName : string.Empty;
            Debug.LogWarning($"TopicTable '{tableName}' TopicDatas[{index}] has no ChoiceOptions.");
        }

        private void RefreshTableTitle(TopicTable table)
        {
            if (tableTitle == null)
            {
                return;
            }

            tableTitle.text = table != null ? table.tableName : string.Empty;
        }

        private void HideAllTopicTables()
        {
            RefreshTableTitle(null);

            if (topicTables == null) return;

            foreach (TopicTable table in topicTables)
            {
                if (table == null) continue;

                if (table.tableRoot != null)
                {
                    table.tableRoot.SetActive(false);
                }

                if (table.topicUIs == null) continue;

                foreach (VR4_TopicUI topicUI in table.topicUIs)
                {
                    if (topicUI != null)
                    {
                        topicUI.AnswerSelected -= OnTableDropdownAnswered;
                    }
                }
            }
        }

        private bool IsCurrentTableCompleted()
        {
            TopicTable table = GetCurrentTable();
            if (table == null)
            {
                return false;
            }

            for (int i = 0; i < currentTableTopicCount; i++)
            {
                VR4_TopicUI topicUI = table.topicUIs[i];
                if (topicUI == null || !topicUI.HasSelected)
                {
                    return false;
                }
            }

            return currentTableTopicCount > 0;
        }

        private void ScoreCurrentTable()
        {
            TopicTable table = GetCurrentTable();
            if (table == null)
            {
                return;
            }

            for (int i = 0; i < currentTableTopicCount; i++)
            {
                VR4_TopicUI topicUI = table.topicUIs[i];
                TopicData topicData = table.topicDatas[i];

                bool isCorrect = topicUI != null && topicUI.IsCorrect;
                topicData.isCorrect = isCorrect;

                if (isCorrect && expManager != null)
                {
                    expManager.totalScore += topicUI.CurrentScore;
                }
            }
        }

        private void RefreshTableButtons()
        {
            if (resetTableButton != null)
            {
                resetTableButton.interactable = !currentTableSubmitted && currentTableTopicCount > 0;
            }

            if (finishTableButton != null)
            {
                finishTableButton.interactable = !currentTableSubmitted && IsCurrentTableCompleted();
            }
        }
        #endregion

        #region ==========API==========
        /// <summary>
        /// 开始当前步骤的答题流程，并默认使用当前 currentTableIndex 指定的题表。
        /// </summary>
        /// <param name="count">本步骤最多需要填入的题目数量。小于等于 0 时使用当前题表内全部可匹配题目。</param>
        public void BeginTopic(int count)
        {
            BeginTopic(count, currentTableIndex);
        }

        /// <summary>
        /// 开始当前步骤的答题流程，并切换到指定题表索引。
        /// </summary>
        /// <param name="count">本步骤最多需要填入的题目数量。小于等于 0 时使用当前题表内全部可匹配题目。</param>
        /// <param name="tableIndex">要显示的 TopicTable 索引，通常由 VR4_ExperimentBtn.topicTableIndex 传入。</param>
        public void BeginTopic(int count, int tableIndex)
        {
            topicCount = count;
            currentTableIndex = tableIndex;

            if (HasTopicTables())
            {
                ShowCurrentTopicTable(count);
            }
            else
            {
                SetTopic();
            }
        }

        /// <summary>
        /// 刷新并显示旧单题模式当前索引对应的题目；配置 TopicTable 后不会进入此模式。
        /// </summary>
        public void SetTopic()
        {
            if (HasTopicTables())
            {
                ShowCurrentTopicTable(topicCount);
                return;
            }

            if (topicList == null || currentTopicIndex < 0 || currentTopicIndex >= topicList.Count)
            {
                CompleteTopicSession();
                return;
            }

            TopicData currentTopic = topicList[currentTopicIndex];

            SetDisplay(currentTopic);

            if (inputField != null)
            {
                inputField.gameObject.SetActive(currentTopic.isInputTopic);
                inputField.text = string.Empty;
                inputField.onValueChanged.RemoveAllListeners();
                inputField.onValueChanged.AddListener(OnInputValueChanged);
            }

            for (int i = 0; i < optionToggles.Count; i++)
            {
                if (currentTopic.choiceOptions != null && i < currentTopic.choiceOptions.Count)
                {
                    optionToggles[i].gameObject.SetActive(true);
                    optionToggles[i].isOn = false;

                    optionToggles[i].onValueChanged.RemoveAllListeners();
                    optionToggles[i].onValueChanged.AddListener(OnToggleValueChanged);

                    if (i < optionLabels.Count && optionLabels[i] != null)
                    {
                        optionLabels[i].text = currentTopic.choiceOptions[i];
                    }
                }
                else
                {
                    optionToggles[i].gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// 重置当前答题表格。每个 VR4_TopicUI 的下拉框会恢复为“请选择”，并重新打开 Interactable。
        /// </summary>
        public void ResetCurrentTable()
        {
            TopicTable table = GetCurrentTable();
            if (table == null)
            {
                return;
            }

            currentTableSubmitted = false;

            for (int i = 0; i < currentTableTopicCount; i++)
            {
                VR4_TopicUI topicUI = table.topicUIs[i];
                if (topicUI != null)
                {
                    topicUI.ResetSelection();
                }
            }

            RefreshTableButtons();
        }

        /// <summary>
        /// 完成当前答题表格。只有所有 VR4_TopicUI 都选择了有效选项后才会判分并结束答题流程。
        /// </summary>
        public void FinishCurrentTable()
        {
            if (currentTableSubmitted || !IsCurrentTableCompleted())
            {
                RefreshTableButtons();
                return;
            }

            ScoreCurrentTable();
            currentTableSubmitted = true;
            RefreshTableButtons();
            CompleteTopicSession();

            if (uiManager == null && VR4_UIManager.HasInstance)
            {
                uiManager = VR4_UIManager.Instance;
            }

            if (uiManager != null)
            {
                uiManager.ReloadCurrentScene();
            }
        }

        /// <summary>
        /// 重置旧单题模式题目索引，并重新显示第一题。
        /// </summary>
        public void ResetTopic()
        {
            currentTopicIndex = 0;

            if (submitBtn != null)
            {
                submitBtn.interactable = true;
            }

            SetTopic();
        }

        /// <summary>
        /// 获取旧单题模式指定索引的题目文本。
        /// </summary>
        /// <param name="currentIndex">题目索引。</param>
        /// <returns>题目文本。</returns>
        public string GetTopicText(int currentIndex)
        {
            return topicList[currentIndex].topicText;
        }

        /// <summary>
        /// 获取旧单题模式指定索引题目的分值。
        /// </summary>
        /// <param name="currentIndex">题目索引。</param>
        /// <returns>题目分值。</returns>
        public float GetTopicScore(int currentIndex)
        {
            return topicList[currentIndex].topicScore;
        }

        /// <summary>
        /// 标记旧单题模式指定题目是否回答正确。
        /// </summary>
        /// <param name="currentIndex">题目索引。</param>
        /// <param name="isAnswer">是否回答正确。</param>
        public void FinishTopic(int currentIndex, bool isAnswer)
        {
            topicList[currentIndex].isCorrect = isAnswer;
        }
        #endregion
    }

    [Serializable]
    /// <summary>
    /// TopicTable 类型说明
    /// 
    /// 【功能说明】
    /// 1. 属于模块4 - 维护保养中的 TopicTable 类型。
    /// 2. 负责管理题表显示、题目填充、重填、完成和判分流程。
    /// 3. 与维护保养流程中的步骤数据、交互逻辑或 UI 显示协同工作。
    /// 
    /// 【依赖组件】
    /// - Unity 组件体系。
    /// - VRHelmet.VRTeam.Maintenance 维护保养流程脚本。
    /// </summary>
    public class TopicTable
    {
        #region ==========Field==========
        /// <summary>
        /// 题表名称，仅用于 Inspector 中识别。
        /// </summary>
        public string tableName;

        /// <summary>
        /// 当前题表的根物体，用于整体显示或隐藏这一张表。
        /// </summary>
        public GameObject tableRoot;

        /// <summary>
        /// 当前题表中的题目 UI 列表。运行时会按顺序接收 topicDatas 中的题目数据。
        /// </summary>
        public List<VR4_TopicUI> topicUIs = new List<VR4_TopicUI>();

        /// <summary>
        /// 当前题表自己的题目数据列表。第 N 个 TopicData 会填入第 N 个 TopicUI。
        /// </summary>
        public List<TopicData> topicDatas = new List<TopicData>();
        #endregion

        #region ==========Unity Method==========
        #endregion

        #region ==========Logic==========
        #endregion

        #region ==========API==========
        #endregion
    }

    [Serializable]
    /// <summary>
    /// TopicData 类型说明
    /// 
    /// 【功能说明】
    /// 1. 属于模块4 - 维护保养中的 TopicData 类型。
    /// 2. 负责管理题表显示、题目填充、重填、完成和判分流程。
    /// 3. 与维护保养流程中的步骤数据、交互逻辑或 UI 显示协同工作。
    /// 
    /// 【依赖组件】
    /// - Unity 组件体系。
    /// - VRHelmet.VRTeam.Maintenance 维护保养流程脚本。
    /// </summary>
    public class TopicData
    {
        #region ==========Field==========
        /// <summary>
        /// 题目名称，仅用于 Inspector 中识别。
        /// </summary>
        public string topicName;

        /// <summary>
        /// 题目配图。表格下拉题可以不填。
        /// </summary>
        public Sprite topicImage;

        /// <summary>
        /// 题目正文。
        /// </summary>
        [TextArea]
        public string topicText;

        /// <summary>
        /// 题目音频。表格下拉题可以不填。
        /// </summary>
        public AudioClip audioClip;

        /// <summary>
        /// 是否为填空题。旧单题模式使用。
        /// </summary>
        public bool isInputTopic;

        /// <summary>
        /// 填空题正确答案。旧单题模式使用。
        /// </summary>
        [TextArea]
        public string inputAnswer;

        /// <summary>
        /// 是否为选择题。表格模式默认按选择题处理。
        /// </summary>
        public bool isChoiceTopic;

        /// <summary>
        /// 选择题选项文本。下拉框显示顺序与这里完全一致。
        /// </summary>
        public List<string> choiceOptions = new List<string>();

        /// <summary>
        /// 选择题正确答案索引。0 表示 choiceOptions 的第一个选项。
        /// </summary>
        [Range(0, 10)]
        public int choiceAnswer;

        /// <summary>
        /// 本题回答正确时获得的分值。
        /// </summary>
        public float topicScore;

        /// <summary>
        /// 本题最近一次判分结果。
        /// </summary>
        public bool isCorrect;
        #endregion

        #region ==========Unity Method==========
        #endregion

        #region ==========Logic==========
        #endregion

        #region ==========API==========
        #endregion
    }
}

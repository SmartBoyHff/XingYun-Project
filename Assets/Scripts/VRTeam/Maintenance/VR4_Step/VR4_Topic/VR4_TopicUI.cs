using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// ============================================================
// 文件名：VR4_TopicUI
// 模块：模块4 - 维护保养
// 功能：单个答题下拉 UI，负责显示题干、填充选项、即时判分和分数显示。
// 创建日期：2026-05-5
// 最后更新：2026-05-5
// ============================================================

namespace VRHelmet.VRTeam.Maintenance
{
    /// <summary>
    /// 单个下拉选择题 UI。负责显示题干、填充选项、选择后立即判分，并显示当前题目得分。
    /// </summary>
    [RequireComponent(typeof(TMP_Dropdown))]
    /// <summary>
    /// VR4_TopicUI 类型说明
    /// 
    /// 【功能说明】
    /// 1. 属于模块4 - 维护保养中的 VR4_TopicUI 类型。
    /// 2. 负责显示题干、填充选项、即时判分和分数显示。
    /// 3. 与维护保养流程中的步骤数据、交互逻辑或 UI 显示协同工作。
    /// 
    /// 【依赖组件】
    /// - Unity 组件体系。
    /// - VRHelmet.VRTeam.Maintenance 维护保养流程脚本。
    /// </summary>
    public class VR4_TopicUI : MonoBehaviour
    {
        #region ==========Field==========
        /// <summary>
        /// 当前题目的题干文本。表格内如果没有独立题干，可以留空。
        /// </summary>
        public TextMeshProUGUI questionText;

        /// <summary>
        /// 当前题目的得分显示文本。选择答案后会显示本题获得的分数。
        /// </summary>
        public TextMeshProUGUI scoreText;

        /// <summary>
        /// 当前题目的下拉框组件。未手动指定时会从当前物体自动获取。
        /// </summary>
        public TMP_Dropdown dropdown;

        private int topicIndex = -1;
        private int correctAnswerIndex = -1;
        private float topicScore = 0f;
        private bool hasAnswered = false;
        private bool isCorrect = false;
        private float currentScore = 0f;

        /// <summary>
        /// 当前 UI 在表格中绑定的题目索引。
        /// </summary>
        public int TopicIndex => topicIndex;

        /// <summary>
        /// 当前选择的选项索引。返回值直接对应 TopicData.choiceOptions 的索引。
        /// </summary>
        public int SelectedIndex => dropdown != null ? dropdown.value : -1;

        /// <summary>
        /// 当前题目是否已经完成一次选择并判分。
        /// </summary>
        public bool HasSelected => hasAnswered;

        /// <summary>
        /// 当前题目最近一次判分是否正确。
        /// </summary>
        public bool IsCorrect => isCorrect;

        /// <summary>
        /// 当前题目最近一次判分获得的分数。
        /// </summary>
        public float CurrentScore => currentScore;

        /// <summary>
        /// 首次选择有效选项并完成判分后触发，供答题管理器刷新按钮或状态。
        /// </summary>
        public event Action<VR4_TopicUI> AnswerSelected;
        #endregion

        #region ==========Unity Method==========
        private void Reset()
        {
            AutoBindReferences();
        }

        private void OnValidate()
        {
            AutoBindReferences();
        }

        private void Awake()
        {
            AutoBindReferences();
        }

        private void OnEnable()
        {
            AutoBindReferences();

            if (dropdown != null)
            {
                dropdown.onValueChanged.AddListener(OnDropdownValueChanged);
            }
        }

        private void OnDisable()
        {
            if (dropdown != null)
            {
                dropdown.onValueChanged.RemoveListener(OnDropdownValueChanged);
            }
        }
        #endregion

        #region ==========Logic==========
        private void AutoBindReferences()
        {
            dropdown = GetComponent<TMP_Dropdown>();
            BindDropdownTextReferences();

            TextMeshProUGUI foundQuestionText = FindFirstLevelChildText("Question");
            if (foundQuestionText != null)
            {
                questionText = foundQuestionText;
            }

            TextMeshProUGUI foundScoreText = FindFirstLevelChildText("Score");
            if (foundScoreText != null)
            {
                scoreText = foundScoreText;
            }
        }

        private void CacheDropdown()
        {
            if (dropdown == null)
            {
                dropdown = GetComponent<TMP_Dropdown>();
            }

            BindDropdownTextReferences();
        }

        private void BindDropdownTextReferences()
        {
            if (dropdown == null)
            {
                return;
            }

            if (dropdown.captionText == null)
            {
                dropdown.captionText = FindDropdownText(transform, "Caption", "Label");
            }

            if (dropdown.itemText == null && dropdown.template != null)
            {
                dropdown.itemText = FindDropdownText(dropdown.template, "Item Label", "Item", "Label");
            }
        }

        private TMP_Text FindDropdownText(Transform root, params string[] nameKeywords)
        {
            if (root == null)
            {
                return null;
            }

            TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
            foreach (string keyword in nameKeywords)
            {
                for (int i = 0; i < texts.Length; i++)
                {
                    TMP_Text text = texts[i];
                    if (text != null && text.name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return text;
                    }
                }
            }

            return texts.Length > 0 ? texts[0] : null;
        }

        private TextMeshProUGUI FindFirstLevelChildText(string nameKeyword)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child.name.IndexOf(nameKeyword, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                TextMeshProUGUI text = child.GetComponent<TextMeshProUGUI>();
                if (text != null)
                {
                    return text;
                }
            }

            return null;
        }

        private void ApplyDropdownOptions(List<string> options)
        {
            if (dropdown == null)
            {
                return;
            }

            dropdown.Hide();
            dropdown.ClearOptions();

            List<TMP_Dropdown.OptionData> optionDatas = new List<TMP_Dropdown.OptionData>();
            if (options != null)
            {
                foreach (string optionText in options)
                {
                    optionDatas.Add(new TMP_Dropdown.OptionData(optionText ?? string.Empty));
                }
            }

            dropdown.AddOptions(optionDatas);
            dropdown.SetValueWithoutNotify(0);
            dropdown.RefreshShownValue();

            if (dropdown.captionText != null && dropdown.options.Count > 0)
            {
                dropdown.captionText.text = dropdown.options[0].text;
            }

            if (dropdown.itemText == null && dropdown.options.Count > 1)
            {
                Debug.LogWarning($"{name} TMP_Dropdown.itemText is missing. Options were added, but expanded item text may not display.");
            }
        }

        private void OnDropdownValueChanged(int value)
        {
            if (dropdown == null || hasAnswered)
            {
                return;
            }

            CheckAnswer(value);
            dropdown.interactable = false;
            AnswerSelected?.Invoke(this);
        }

        private void CheckAnswer(int selectedIndex)
        {
            hasAnswered = true;
            isCorrect = selectedIndex == correctAnswerIndex;
            currentScore = isCorrect ? topicScore : 0f;
            RefreshScoreText();
        }

        private void RefreshScoreText()
        {
            if (scoreText != null)
            {
                scoreText.text = currentScore.ToString("0.##");
            }
        }
        #endregion

        #region ==========API==========
        /// <summary>
        /// 绑定并显示一道题目，同时将下拉框重置为第一个选项的初始可交互状态。
        /// </summary>
        /// <param name="index">题目在当前 TopicTable.topicDatas 中的索引。</param>
        /// <param name="text">题干文本。</param>
        /// <param name="options">可选择的答案文本列表，顺序必须和 TopicData.choiceAnswer 使用同一套索引。</param>
        /// <param name="answerIndex">正确答案索引，直接对应 options 的索引。</param>
        /// <param name="score">本题答对后获得的分数。</param>
        public void SetData(int index, string text, List<string> options, int answerIndex, float score)
        {
            topicIndex = index;
            correctAnswerIndex = answerIndex;
            topicScore = score;
            hasAnswered = false;
            isCorrect = false;
            currentScore = 0f;

            if (questionText != null)
            {
                questionText.text = text;
            }

            RefreshScoreText();
            CacheDropdown();

            if (dropdown == null)
            {
                Debug.LogWarning($"{name} 未找到 TMP_Dropdown，无法填充题目选项");
                return;
            }

            dropdown.interactable = true;
            ApplyDropdownOptions(options);

            if (options == null || options.Count == 0)
            {
                Debug.LogWarning($"{name} TopicData.choiceOptions is empty. Please check TopicTable[{index}] options.");
            }
        }

        /// <summary>
        /// 只重置当前下拉框的选择和分数状态，不改变已经填入的题干和选项。
        /// </summary>
        public void ResetSelection()
        {
            CacheDropdown();
            hasAnswered = false;
            isCorrect = false;
            currentScore = 0f;
            RefreshScoreText();

            if (dropdown == null)
            {
                return;
            }

            dropdown.interactable = true;
            dropdown.SetValueWithoutNotify(0);
            dropdown.RefreshShownValue();
        }

        /// <summary>
        /// 清空当前 UI 的题目绑定，并禁用下拉框，通常用于 TopicUI 数量多于题目数量时隐藏多余行。
        /// </summary>
        public void ClearData()
        {
            topicIndex = -1;
            correctAnswerIndex = -1;
            topicScore = 0f;
            hasAnswered = false;
            isCorrect = false;
            currentScore = 0f;

            if (questionText != null)
            {
                questionText.text = string.Empty;
            }

            RefreshScoreText();
            CacheDropdown();

            if (dropdown == null)
            {
                return;
            }

            dropdown.ClearOptions();
            dropdown.SetValueWithoutNotify(0);
            dropdown.RefreshShownValue();
            dropdown.interactable = false;
        }
        #endregion
    }
}

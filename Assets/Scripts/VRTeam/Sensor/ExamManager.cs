using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[Serializable]
public class Exam
{
    public string examName;
    public List<PlacementSlot> Slots;
    public List<ExamObject> Items;
}

public class ExamManager : MonoBehaviour
{
    public static ExamManager Instance { get; private set; }

    [Header("所有考核组")]
    public List<Exam> exams;

    [Header("当前考核索引")]
    public int currentExamIndex = 0;

    [Header("UI")]
    public TextMeshProUGUI scoreText;
    public GameObject submitButton;

    // 运行时激活的当前考核
    private Exam currentExam;
    public bool examActive = false;
    private bool hasSubmitted = false;
    private int totalScore;                                 // 全部分数（所有槽位数）
    private Dictionary<int, int> examScores = new();        // 每个考核组的得分

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        CalculateTotalScore();
       // currentExam = exams[currentExamIndex];
    }
    /// <summary> 计算总分（所有考核组槽位总数）</summary>
    private void CalculateTotalScore()
    {
        totalScore = 0;
        foreach (var exam in exams)
            totalScore += exam.Slots.Count;
    }

    /// <summary> 更新分数显示（当前累计 / 总分）</summary>
    private void UpdateScoreDisplay()
    {
        if (!scoreText) return;

        int currentSum = 0;
        foreach (var score in examScores.Values)
            currentSum += score;

        scoreText.text = $"得分：{currentSum} / {totalScore}";
       
    }

    /// <summary> 切换到指定索引的考核组 </summary>
    public void SwitchExam(int index)
    {
        if (index < 0 || index >= exams.Count)
        {
            Debug.LogWarning($"考核索引 {index} 无效");
            return;
        }

        //// 1. 关闭旧的（如果已有激活）
        //if (currentExam != null)
        //{
        //    SetExamActive(currentExam, false);
        //}

        // 2. 更新当前
        currentExamIndex = index;
        currentExam = exams[currentExamIndex];

        foreach (var item in currentExam.Items)
        {
            ItemInfo info = item.GetComponent<ItemInfo>();
            info.SetHighlight(false);
        }
        // 不自动清零旧考核得分，保持累计
        UpdateScoreDisplay();
        // 3. 启用新的考核组物品和槽位（但还未开始考核，槽位标签应显示）
        //SetExamActive(currentExam, true);

        // 确保退出考核状态
        examActive = false;
        hasSubmitted = false;
      
        if (submitButton) submitButton.SetActive(true);
    }

    /// <summary> 启用/禁用某组考核的物品与槽位标签 </summary>
    //private void SetExamActive(Exam exam, bool active)
    //{
    //    foreach (var slot in exam.Slots)
    //    {
    //        slot.gameObject.SetActive(active);
    //        // 默认显示标签（非考核模式）
    //        if (active)
    //            slot.SetExamActive(false); // false 表示非考核状态 → 显示标签
    //    }

    //    foreach (var item in exam.Items)
    //    {
    //        item.gameObject.SetActive(active);
    //    }
    //}

    /// <summary> 正式开始当前考核 </summary>
    public void StartExam()
    {
        if (currentExam == null) return;
        // 重新开始当前考核：清零本组成绩
        examScores[currentExamIndex] = 0;
        UpdateScoreDisplay();
        examActive = true;
        hasSubmitted = false;
      

        // 计算预期零件数
        Dictionary<string, int> countMap = new Dictionary<string, int>();
        foreach (var item in currentExam.Items)
        {
            if (countMap.ContainsKey(item.itemName))
                countMap[item.itemName]++;
            else
                countMap[item.itemName] = 1;
        }

        foreach (var slot in currentExam.Slots)
        {
            slot.expectedCount = 1;
            countMap.TryGetValue(slot.correctItemName, out int count);
            slot.expectedCount = count;
            //slot.SetExamActive(true);   // 进入考核 → 隐藏槽位标签
        }
        //foreach (var kvp in countMap)
        //{
        //    Debug.Log($"{kvp.Key}: {kvp.Value}");
        //}
    }

    public void Submit()
    {
        if (!examActive || hasSubmitted || currentExam == null) return;
        hasSubmitted = true;
        examActive = false;

        int correctSlots = 0;
        foreach (var slot in currentExam.Slots)
            if (slot.IsCorrect()) correctSlots++;

        // 保存本组成绩
        examScores[currentExamIndex] = correctSlots;
        UpdateScoreDisplay();

        // 处理正确物品名字显示
        foreach (var item in currentExam.Items)
        {
            ItemInfo info = item?.GetComponent<ItemInfo>();
            if (!info) continue;

            bool isCorrect = item.currentSlot != null && item.currentSlot.IsCorrect();
            info.SetHighlight(false);
            info.ForceShowName(isCorrect);
        }

        if (submitButton) submitButton.SetActive(false);
    }
    public void DisplayItem()
    {
        foreach (var item in currentExam.Items)
        {
            ItemInfo info = item.GetComponent<ItemInfo>();
           info.SetHighlight(true);
        }
        StopCoroutine(nameof(TurnOffHighlightAfterDelay));   // 防止重复调用叠加
        StartCoroutine(TurnOffHighlightAfterDelay(2f));
    }
    private IEnumerator TurnOffHighlightAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        foreach (var item in currentExam.Items)
        {
            // 防止物体在等待期间被销毁
            if (item == null) continue;

            ItemInfo info = item.GetComponent<ItemInfo>();
            if (info)
                info.SetHighlight(false);
        }
    }
}

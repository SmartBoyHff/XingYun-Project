using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class SwitchManager : MonoBehaviour
{
    [Header("内容数据")]
    [SerializeField] private List<Content> contents; // 所有可切换的内容

    [Header("场景中已有的 Toggle（数量需与 contents 一致）")]
    [SerializeField] private Toggle[] contentToggles; // 手动拖拽场景中的 Toggle

    [Header("UI 元素引用")]
    [SerializeField] private TextMeshProUGUI titleText;          // 主标题文本
    [SerializeField] private Image illustrationImage; // 配图
    [SerializeField] private Transform subtitlesContainer; // 小标题列表的父物体

    [Header("小标题预制体")]
    [SerializeField] private GameObject subtitleItemPrefab; // 小标题条目预制体（第一个子物体为标题文本，第二个为内容文本）
    private List<GameObject> subtitleObj =new List<GameObject>();

    private int currentContentIndex = -1;

    void Start()
    {
        // 检查数量是否匹配
        if (contentToggles == null || contentToggles.Length != contents.Count)
        {
            Debug.LogError($"Toggle 数量 ({contentToggles?.Length ?? 0}) 与 Content 数量 ({contents.Count}) 不一致！");
            return;
        }

        // 为每个 Toggle 添加监听
        for (int i = 0; i < contentToggles.Length; i++)
        {
            int index = i; // 闭包捕获
            contentToggles[i].onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                    ShowContent(index);
            });
        }
        ShowContent(0);
        // 默认选中第一个 Toggle（如果没有任何 Toggle 被选中）
        bool anyOn = false;
        foreach (var toggle in contentToggles)
        {
            if (toggle.isOn)
            {
                anyOn = true;
                break;
            }
        }
        if (!anyOn && contentToggles.Length > 0)
            contentToggles[0].isOn = true;
    }

    /// <summary>
    /// 显示指定索引的内容
    /// </summary>
    private void ShowContent(int index)
    {
        if (currentContentIndex == index) return;

        if (index < 0 || index >= contents.Count)
        {
            Debug.LogError("内容索引超出范围：" + index);
            return;
        }

        Content content = contents[index];

        // 更新主标题
        if (titleText != null)
            titleText.text = content.title;

        // 更新配图
        if (illustrationImage != null)
            illustrationImage.sprite = content.illustration;

        // 动态生成小标题列表
        GenerateSubtitles(content.subtitles);

        currentContentIndex = index;
    }

    /// <summary>
    /// 根据 Subtitle 数组动态生成小标题条目
    /// </summary>
    private void GenerateSubtitles(Subtitle[] subtitles)
    {
        // 清空旧条目
        foreach (var child in subtitleObj)
            Destroy(child.gameObject);
        subtitleObj.Clear();
        if (subtitles == null) return;

        foreach (Subtitle sub in subtitles)
        {
            GameObject item = Instantiate(subtitleItemPrefab, subtitlesContainer);
            subtitleObj.Add(item);
            // 预制体结构：第1个子物体为标题文本，第2个子物体为内容文本
            if (item.transform.childCount >= 2)
            {
                TextMeshProUGUI titleTextComp = item.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
                TextMeshProUGUI contentTextComp = item.transform.GetChild(1).GetComponent<TextMeshProUGUI>();

                if (titleTextComp != null) titleTextComp.text = sub.title;
                else Debug.LogWarning("小标题预制体的第一个子物体缺少 Text 组件");

                if (contentTextComp != null) contentTextComp.text = sub.text;
                else Debug.LogWarning("小标题预制体的第二个子物体缺少 Text 组件");
            }
            else
            {
                Debug.LogError("小标题预制体需要至少两个子物体（标题文本、内容文本）");
            }
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class Monitor : MonoBehaviour
{
    [Header("UI 引用")]
    [SerializeField] private RectTransform monitorPanel;   // 父容器，挂有 GridLayoutGroup
    [SerializeField] private GameObject monitorItemPrefab; // 画面预制体
    [SerializeField] private Button btnAdd;                 // 添加按钮
    [SerializeField] private Button btnRemove;              // 移除按钮

    [Header("画面切换内容")]
    [SerializeField] private List<Sprite> displaySprites;   // 可选内容（示例用图片列表）
    public bool isExam;

    private GridLayoutGroup gridLayout;
    private List<GameObject> activeItems = new List<GameObject>();
    private const int MAX_ITEMS = 9; // 最多 9 个画面

    private void Start()
    {
        gridLayout = monitorPanel.GetComponent<GridLayoutGroup>();
        if (gridLayout == null)
        {
            gridLayout = monitorPanel.gameObject.AddComponent<GridLayoutGroup>();
        }

        btnAdd.onClick.AddListener(AddMonitorItem);
        btnRemove.onClick.AddListener(RemoveMonitorItem);

        // 默认添加一个画面
        AddMonitorItem();
       
    }

    public void DropValue(int i)
    {
        TMP_Dropdown dropdown = activeItems[0].GetComponentInChildren<TMP_Dropdown>();
        dropdown.value = i;
    }
    private void AddMonitorItem()
    {
        if (activeItems.Count >= MAX_ITEMS)
        {
            Debug.Log("已达到最大画面数量");
            return;
        }

        GameObject newItem = Instantiate(monitorItemPrefab, monitorPanel);
        UIHighlight Highlight = newItem.GetComponentInChildren<UIHighlight>(true);
        activeItems.Add(newItem);
        if (!isExam)
        {
            Highlight.gameObject.SetActive(true);
        }

            // 为 Dropdown 绑定切换事件
            TMP_Dropdown dropdown = newItem.GetComponentInChildren<TMP_Dropdown>();
        if (dropdown != null)
        {
            dropdown.onValueChanged.AddListener((value) => OnDropdownValueChanged(newItem, value));
        }

        UpdateLayout();
    }

    private void RemoveMonitorItem()
    {
        if (activeItems.Count == 0) return;

        GameObject toRemove = activeItems[activeItems.Count - 1];
        activeItems.Remove(toRemove);
        Destroy(toRemove);

        UpdateLayout();
    }

    private void UpdateLayout()
    {
        int count = activeItems.Count;
        float panelWidth = monitorPanel.rect.width;
        float panelHeight = monitorPanel.rect.height;

        // 根据数量设定列数和单元格大小
        int columns;
        float cellWidth, cellHeight;

        if (count == 1)
        {
            columns = 1;
            cellWidth = panelWidth;
            cellHeight = panelHeight;
        }
        else if (count == 2)
        {
            columns = 2;
            cellWidth = panelWidth / 2f;
            cellHeight = panelHeight;
        }
        else if (count <= 4)
        {
            columns = 2;         // 2x2 网格
            cellWidth = panelWidth / 2f;
            cellHeight = panelHeight / 2f;
        }
        else if(count <= 6)
        {
            columns = 3;
            cellWidth = panelWidth / 3f;
            cellHeight = panelHeight / 2f;
        }
        else
        {
            columns = 3;
            cellWidth = panelWidth / 3f;
            cellHeight = panelHeight / 3f;
        }

        // 更新 GridLayoutGroup 参数
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = columns;
        gridLayout.cellSize = new Vector2(cellWidth, cellHeight);
        gridLayout.spacing = Vector2.zero;
        gridLayout.childAlignment = TextAnchor.MiddleCenter;

        // 强制刷新布局
        LayoutRebuilder.ForceRebuildLayoutImmediate(monitorPanel);
    }

    public void OnDropdownValueChanged(GameObject item, int value)
    {
      
        // 根据 Dropdown 选项切换画面内容（示例：更换 Image 的 Sprite）
        if (displaySprites == null || displaySprites.Count == 0) return;
        if (value < 0 || value >= displaySprites.Count) return;

        Image displayImage = item.transform.GetChild(0).GetComponent<Image>();
        UIHighlight Highlight = item.GetComponentInChildren<UIHighlight>(true);
        if (displayImage != null)
        {
            displayImage.sprite = displaySprites[value];
            if( value!=0)
            Highlight.gameObject.SetActive(false);
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using Unity.XR.PXR;
using UnityEngine;

public class PlacementSlot : MonoBehaviour
{
    [Header("该槽对应的正确物品名称")]
    public string correctItemName;

    // 预期放入的零件总数（由 ExamManager 在考核启动时设置）
   public int expectedCount = 0;

    // 当前槽内的所有物品（自动维护）
    private List<ExamObject> currentObjects = new List<ExamObject>();
    public IReadOnlyList<ExamObject> CurrentObjects => currentObjects;

    [Header("槽位自身的标签（非考核时显示）")]
    public NameDisplay slotNameDisplay;   // 拖入或自动查找

    public bool look;
    private void Reset()
    {
        correctItemName = this.gameObject.name;
    }

    private void Awake()
    {
        correctItemName = this.gameObject.name;
        if (slotNameDisplay == null)
            slotNameDisplay = GetComponentInChildren<NameDisplay>();
        ShowSlotName();
    }

    public void ShowSlotName()
    {
        if (slotNameDisplay) slotNameDisplay.Show(correctItemName);
    }

    ///// <summary>
    ///// 考核状态切换：进入考核隐藏标签，退出考核显示标签
    ///// </summary>
    //public void SetExamActive(bool examActive)
    //{
    //    if (slotNameDisplay)
    //        slotNameDisplay.gameObject.SetActive(!examActive);
    //}

    private void OnTriggerEnter(Collider other)
    {
        ExamObject obj = other.GetComponent<ExamObject>();
       
        if (obj != null && !currentObjects.Contains(obj))
        {
            look = true;
            currentObjects.Add(obj);
            obj.EnterSlot(this);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        ExamObject obj = other.GetComponent<ExamObject>();
        look = false;
        if (obj != null && currentObjects.Remove(obj))
        {
            obj.ExitSlot(this);
        }
    }

    /// <summary>
    /// 检查该槽位是否正确：所有零件都在且名称匹配，且数量等于预期总数
    /// </summary>
    public bool IsCorrect()
    {
        if (expectedCount <= 0) return false;          // 未设置预期数量
        if (currentObjects.Count != expectedCount) return false;

        foreach (var obj in currentObjects)
        {
            if (obj.itemName != correctItemName)
                return false;
        }
        return true;
    }
}

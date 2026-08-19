using EPOOutline;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ItemInfo : MonoBehaviour
{
    [Header("物品配置")]
    public string itemName ;
    public bool isVideo = false;
    public int videoIndex = 0;           // 对应 VideoManager.videoLibrary 的索引

    [Header("高光")]
    public Outlinable highlight;

    [Header("名字显示")]
    public NameDisplay nameDisplay;

    private void Reset()
    {
        itemName = this.gameObject.name;
    }
    private void Awake()
    {
        this.gameObject.AddComponent<Outlinable>();
       
        highlight = GetComponent<Outlinable>();
        highlight.enabled = false;
        nameDisplay = GetComponentInChildren<NameDisplay>();
        this.nameDisplay.gameObject.SetActive(false);
    }
    
    public void SetHighlight(bool state)
    {
        if (highlight) highlight.enabled = state;
    }

    public void ShowName()
    {
        if (nameDisplay) nameDisplay.Show(itemName);
    }

    public void HideName()
    {
        if (nameDisplay) nameDisplay.Hide();
    }

    public void PlayVideo()
    {
        // 直接调全局管理器
        if(isVideo)
        VideoManager.Instance?.PlayByIndex(videoIndex);
    }
    public void ForceShowName(bool show)
    {
        if (nameDisplay)
        {
            if (show) nameDisplay.Show(itemName);
            else nameDisplay.Hide();
        }
    }
}

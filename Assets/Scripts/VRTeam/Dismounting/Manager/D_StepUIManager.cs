using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class D_StepUIManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI stepDescriptionText;
    [SerializeField] private TextMeshProUGUI stepIndexText;
    [SerializeField] private Image requiredProtectionIcon;
    [SerializeField] private Image requiredToolIcon;
    [SerializeField] private Button skipButton;

    [Header("防护图标映射")]
    [SerializeField] private Sprite insulatedGloveSprite;
    [SerializeField] private Sprite wearResistantGloveSprite;

    [Header("工具数据引用（用于查找图标）")]
    [SerializeField] private D_ToolItemData toolItemData;   // 拖入与工具栏相同的资源

    private void Start()
    {
        skipButton.onClick.AddListener(() => D_StepManager.Instance.SkipStep());
    }

    public void RefreshUI(D_StepData step, int currentIndex, int totalSteps)
    {
        if (stepIndexText != null)
            stepIndexText.text = $"步骤 {currentIndex + 1}/{totalSteps}";

        if (stepDescriptionText != null)
            stepDescriptionText.text = step.description;

        // 防护图标
        if (requiredProtectionIcon != null)
        {
            bool hasProtection = step.requiredProtection != ProtectionType.None;
            requiredProtectionIcon.enabled = hasProtection;
            if (hasProtection)
            {
                switch (step.requiredProtection)
                {
                    case ProtectionType.InsulatedGloves:
                        requiredProtectionIcon.sprite = insulatedGloveSprite; break;
                    case ProtectionType.WearResistantGloves:
                        requiredProtectionIcon.sprite = wearResistantGloveSprite; break;
                }
            }
        }

        // 工具图标（取第一个所需工具）
        if (requiredToolIcon != null)
        {
            if (step.requiredItemIDs.Count > 0 && toolItemData != null)
            {
                string firstID = step.requiredItemIDs[0];
                var entry = System.Array.Find(toolItemData.tools, t => t.itemID == firstID);
                if (entry.itemID != null) // 结构体默认判断
                {
                    requiredToolIcon.enabled = true;
                    requiredToolIcon.sprite = entry.icon;
                }
                else
                    requiredToolIcon.enabled = false;
            }
            else
                requiredToolIcon.enabled = false;
        }

        skipButton.interactable = currentIndex < totalSteps - 1;
    }
}

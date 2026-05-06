using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class D_HandDisplay : MonoBehaviour
{
   [System.Serializable]
    public struct ProtectionHandPair
    {
        public ProtectionType protectionType;
        public GameObject handPrefab; // 对应手模型预制体
    }

    [SerializeField] private List<ProtectionHandPair> handMappings;
    [SerializeField] private Transform handAnchor; // 手部挂载点（通常跟随手柄）

    private Dictionary<ProtectionType, GameObject> handDict = new Dictionary<ProtectionType, GameObject>();
    private GameObject currentHandModel;

    private void Start()
    {
        // 预实例化所有手模型并隐藏
        foreach (var pair in handMappings)
        {
            if (pair.handPrefab != null)
            {
                GameObject instance = Instantiate(pair.handPrefab, handAnchor);
                instance.SetActive(false);
                handDict[pair.protectionType] = instance;
            }
        }

        // 监听防护变化
        if (D_ProtectionManager.Instance != null)
            D_ProtectionManager.Instance.OnProtectionChanged.AddListener(OnProtectionChanged);
    }

    private void OnProtectionChanged(ProtectionType newType)
    {
        if (currentHandModel != null)
            currentHandModel.SetActive(false);

        if (handDict.TryGetValue(newType, out GameObject hand))
        {
            hand.SetActive(true);
            currentHandModel = hand;
        }
        else
        {
            currentHandModel = null;
        }
    }
}

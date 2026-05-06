using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "VRExam/ToolItemData")]
public class D_ToolItemData : ScriptableObject
{
    [Serializable]
    public struct ToolEntry
    {
        public string itemID;
        public string displayName;
        public GameObject prefab;
        public Sprite icon;
    }
    public ToolEntry[] tools;
    
}

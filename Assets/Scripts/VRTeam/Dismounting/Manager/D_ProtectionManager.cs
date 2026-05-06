using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class D_ProtectionManager : MonoBehaviour
{
    public static D_ProtectionManager Instance { get; private set; }

    public ProtectionType CurrentProtection { get; private set; } = ProtectionType.None;

    [System.Serializable]
    public class ProtectionChangedEvent : UnityEvent<ProtectionType> { }
    public ProtectionChangedEvent OnProtectionChanged;

    private void Awake()
    {
        Instance = this;
    }

    public void SwitchTo(int typeIndex)
    {
        ProtectionType newType = (ProtectionType)typeIndex; 
        if (CurrentProtection == newType) return;
        CurrentProtection = newType;
        OnProtectionChanged?.Invoke(newType);
        Debug.Log($"防护装备切换为: {newType}");
    }
    
   
}

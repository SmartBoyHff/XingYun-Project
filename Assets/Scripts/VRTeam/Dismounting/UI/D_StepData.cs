using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class D_StepData
{
    public string stepID;
    [TextArea] public string description;
    public ProtectionType requiredProtection = ProtectionType.None;
    public List<string> requiredItemIDs = new List<string>(); 
}

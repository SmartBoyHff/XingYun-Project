using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AssemblyController : MonoBehaviour
{
    private Animator animator;
    private int isGatheredHash = Animator.StringToHash("IsGathered");

    [Header("原始放置区域")]
    public OriginalZone originalZone;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        // 强制初始状态为散开（即便 Controller 默认是 Scatter，保险起见）
        animator.SetBool(isGatheredHash, false);
    }

    public void Gather()
    {
        animator.SetBool(isGatheredHash, true);
    }

    public void Scatter()
    {
        animator.SetBool(isGatheredHash, false);
    }

    public bool IsInOriginalZone()
    {
        return originalZone != null && originalZone.IsObjectInside(this);
    }
}

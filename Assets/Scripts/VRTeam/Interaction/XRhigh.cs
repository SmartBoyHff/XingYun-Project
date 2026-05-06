using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.XR.Management;


public class XRhigh : MonoBehaviour
{
    [Header("按键绑定（拖入对应InputActionReference）")]
    public InputActionReference MyLeftButton_X;  // 左手柄X键（下降）
    public InputActionReference MyLeftButton_Y;  // 左手柄Y键（上升）
    public InputActionReference MyRightButton_A; // 右手柄A键（下降）
    public InputActionReference MyRightButton_B; // 右手柄B键（上升）

    [Header("高度控制参数")]
    public float moveSpeed = 0.5f;   // 升降速度（可调整，建议0.3~1f）
    public float minHeight = 0.5f;   // 最低高度（防止穿地，根据人物初始高度设置）
    public float maxHeight = 5f;     // 最高高度（防止升太高）

    private void OnEnable()
    {
        // 启用所有按键动作（必须启用才能检测输入）
        EnableAllActions();
    }

    private void OnDisable()
    {
        // 禁用所有按键动作（避免内存泄漏）
        DisableAllActions();
    }

    void Update()
    {
        // 检测按键输入，控制高度
        CheckHeightControlInput();
    }

    /// <summary>
    /// 启用所有绑定的按键动作
    /// </summary>
    private void EnableAllActions()
    {
        MyLeftButton_X?.action.Enable();
        MyLeftButton_Y?.action.Enable();
        MyRightButton_A?.action.Enable();
        MyRightButton_B?.action.Enable();
    }

    /// <summary>
    /// 禁用所有绑定的按键动作
    /// </summary>
    private void DisableAllActions()
    {
        MyLeftButton_X?.action.Disable();
        MyLeftButton_Y?.action.Disable();
        MyRightButton_A?.action.Disable();
        MyRightButton_B?.action.Disable();
    }

    /// <summary>
    /// 检测按键输入，调整人物高度
    /// </summary>
    private void CheckHeightControlInput()
    {
        float deltaY = 0f;

        // 下降：左手柄X键 或 右手柄A键（按住持续下降）
        if (IsActionPressed(MyLeftButton_X) || IsActionPressed(MyRightButton_A))
        {
            deltaY = -moveSpeed * Time.deltaTime;
        }
        // 上升：左手柄Y键 或 右手柄B键（按住持续上升）
        else if (IsActionPressed(MyLeftButton_Y) || IsActionPressed(MyRightButton_B))
        {
            deltaY = moveSpeed * Time.deltaTime;
        }

        // 应用高度变化（限制在min~max之间）
        if (deltaY != 0f)
        {
            AdjustHeight(deltaY);
        }
    }

    /// <summary>
    /// 检查某个InputActionReference是否被按下
    /// </summary>
    private bool IsActionPressed(InputActionReference actionRef)
    {
        return actionRef != null && actionRef.action.IsPressed();
    }

    /// <summary>
    /// 调整人物Y轴高度（只改高度，不影响X/Z位置）
    /// </summary>
    private void AdjustHeight(float deltaY)
    {
        Vector3 currentPos = transform.position;
        // 计算新高度，用Mathf.Clamp限制范围
        float newY = Mathf.Clamp(currentPos.y + deltaY, minHeight, maxHeight);
        // 应用新位置（保持X和Z不变，只更新Y）
        transform.position = new Vector3(currentPos.x, newY, currentPos.z);
    }
}

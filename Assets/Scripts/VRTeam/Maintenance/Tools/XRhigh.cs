using UnityEngine;
using UnityEngine.InputSystem;

// ============================================================
// 文件名：XRhigh
// 模块：模块4 - 维护保养
// 功能：XR 高度调节控制器，负责通过手柄按键控制玩家或对象高度升降。
// 创建日期：2026-05-5
// 最后更新：2026-05-5
// ============================================================

/// <summary>
/// XRhigh 类型说明
/// 
/// 【功能说明】
/// 1. 属于模块4 - 维护保养中的 XRhigh 类型。
/// 2. 负责通过手柄按键控制高度升降。
/// 3. 与维护保养流程中的步骤数据、交互逻辑或 UI 显示协同工作。
/// 
/// 【依赖组件】
/// - Unity 组件体系。
/// - VRHelmet.VRTeam.Maintenance 维护保养流程脚本。
/// </summary>
public class XRhigh : MonoBehaviour
{
    #region ==========Field==========
    [Header("按键绑定（拖入对应InputActionReference）")]
    /// <summary>
    /// 左手柄 X 键输入，按住时控制下降。
    /// </summary>
    public InputActionReference MyLeftButton_X;

    /// <summary>
    /// 左手柄 Y 键输入，按住时控制上升。
    /// </summary>
    public InputActionReference MyLeftButton_Y;

    /// <summary>
    /// 右手柄 A 键输入，按住时控制下降。
    /// </summary>
    public InputActionReference MyRightButton_A;

    /// <summary>
    /// 右手柄 B 键输入，按住时控制上升。
    /// </summary>
    public InputActionReference MyRightButton_B;

    [Header("高度控制参数")]
    /// <summary>
    /// 人物高度升降速度。
    /// </summary>
    public float moveSpeed = 0.5f;

    /// <summary>
    /// 允许下降到的最低高度。
    /// </summary>
    public float minHeight = 0.5f;

    /// <summary>
    /// 允许上升到的最高高度。
    /// </summary>
    public float maxHeight = 5f;
    #endregion

    #region ==========Unity Method==========
    private void OnEnable()
    {
        EnableAllActions();
    }

    private void OnDisable()
    {
        DisableAllActions();
    }

    void Update()
    {
        CheckHeightControlInput();
    }
    #endregion

    #region ==========Logic==========
    private void EnableAllActions()
    {
        MyLeftButton_X?.action.Enable();
        MyLeftButton_Y?.action.Enable();
        MyRightButton_A?.action.Enable();
        MyRightButton_B?.action.Enable();
    }

    private void DisableAllActions()
    {
        MyLeftButton_X?.action.Disable();
        MyLeftButton_Y?.action.Disable();
        MyRightButton_A?.action.Disable();
        MyRightButton_B?.action.Disable();
    }

    private void CheckHeightControlInput()
    {
        float deltaY = 0f;

        if (IsActionPressed(MyLeftButton_X) || IsActionPressed(MyRightButton_A))
        {
            deltaY = -moveSpeed * Time.deltaTime;
        }
        else if (IsActionPressed(MyLeftButton_Y) || IsActionPressed(MyRightButton_B))
        {
            deltaY = moveSpeed * Time.deltaTime;
        }

        if (deltaY != 0f)
        {
            AdjustHeight(deltaY);
        }
    }

    private bool IsActionPressed(InputActionReference actionRef)
    {
        return actionRef != null && actionRef.action.IsPressed();
    }

    private void AdjustHeight(float deltaY)
    {
        Vector3 currentPos = transform.position;
        float newY = Mathf.Clamp(currentPos.y + deltaY, minHeight, maxHeight);
        transform.position = new Vector3(currentPos.x, newY, currentPos.z);
    }
    #endregion

    #region ==========API==========
    #endregion
}

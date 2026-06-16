using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.XR.Interaction.Toolkit;

// ============================================================
// 文件名：VR4_StepData
// 模块：模块4 - 维护保养
// 功能：维护保养步骤数据与任务数据定义，维护步骤运行时完成状态。
// 创建日期：2026-05-5
// 最后更新：2026-05-5
// ============================================================

namespace VRHelmet.VRTeam.Maintenance
{
    /// <summary>
    /// 负责存储维护维修步骤数据，并维护步骤运行时状态。当前版本只使用场景组件上的 oStepList。
    /// </summary>
    /// <summary>
    /// VR4_StepData 类型说明
    /// 
    /// 【功能说明】
    /// 1. 属于模块4 - 维护保养中的 VR4_StepData 类型。
    /// 2. 负责定义步骤数据、任务数据和运行时完成状态。
    /// 3. 与维护保养流程中的步骤数据、交互逻辑或 UI 显示协同工作。
    /// 
    /// 【依赖组件】
    /// - Unity 组件体系。
    /// - VRHelmet.VRTeam.Maintenance 维护保养流程脚本。
    /// </summary>
    public class VR4_StepData : MonoBehaviour
    {
        #region ==========Field==========
        [Header("旧场景步骤数据")]
        /// <summary>
        /// 场景中直接配置的步骤列表。工作单按钮的步骤区间会直接映射到该列表索引。
        /// </summary>
        public List<OperateStep> oStepList = new List<OperateStep>();

        public static readonly VR4InteractionLayer DefaultInteractionMask = VR4InteractionLayer.Nothing;

        private readonly List<StepRuntimeState> runtimeStates = new List<StepRuntimeState>();

        /// <summary>
        /// 当前实际使用的步骤只读列表。
        /// </summary>
        public IReadOnlyList<OperateStep> Steps => oStepList;

        /// <summary>
        /// 当前实际使用的步骤数量。
        /// </summary>
        public int StepCount => oStepList.Count;
        #endregion

        #region ==========Unity Method==========
        private void Awake()
        {
            EnsureRuntimeStates();
        }
        #endregion

        #region ==========Logic==========
        private void EnsureRuntimeStates()
        {
            while (runtimeStates.Count < StepCount)
            {
                runtimeStates.Add(new StepRuntimeState());
            }

            if (runtimeStates.Count > StepCount)
            {
                runtimeStates.RemoveRange(StepCount, runtimeStates.Count - StepCount);
            }
        }
        #endregion

        #region ==========API==========
        /// <summary>
        /// 获取指定索引的步骤数据，越界时返回 null。
        /// </summary>
        /// <param name="index">步骤索引。</param>
        /// <returns>对应步骤数据。</returns>
        public OperateStep GetStep(int index)
        {
            if (index < 0 || index >= StepCount)
            {
                return null;
            }

            return oStepList[index];
        }

        /// <summary>
        /// 查询指定步骤是否已经完成。
        /// </summary>
        /// <param name="index">步骤索引。</param>
        /// <returns>步骤是否完成。</returns>
        public bool IsStepCompleted(int index)
        {
            EnsureRuntimeStates();
            return index >= 0 && index < runtimeStates.Count && runtimeStates[index].isCompleted;
        }

        /// <summary>
        /// 标记指定步骤为完成，并同步任务完成状态。
        /// </summary>
        /// <param name="index">步骤索引。</param>
        public void CompleteStep(int index)
        {
            EnsureRuntimeStates();

            if (index < 0 || index >= runtimeStates.Count)
            {
                return;
            }

            runtimeStates[index].isCompleted = true;
            runtimeStates[index].isTaskFinished = true;

            OperateStep step = GetStep(index);
            if (step != null)
            {
                step.isCompleted = true;
                step.ActiveTask?.SetFinished(true);
            }
        }

        /// <summary>
        /// 重置所有步骤和任务的运行时状态。
        /// </summary>
        public void ResetRuntime()
        {
            EnsureRuntimeStates();

            for (int i = 0; i < runtimeStates.Count; i++)
            {
                ResetRuntimeAt(i);
            }
        }

        /// <summary>
        /// 重置指定步骤区间内的运行时状态，工作单重复执行时只刷新本工作单负责的步骤。
        /// </summary>
        /// <param name="startIndex">起始步骤索引，包含。</param>
        /// <param name="endIndex">结束步骤索引，包含。</param>
        public void ResetRuntimeRange(int startIndex, int endIndex)
        {
            EnsureRuntimeStates();

            if (StepCount <= 0)
            {
                return;
            }

            int start = Mathf.Clamp(Mathf.Min(startIndex, endIndex), 0, StepCount - 1);
            int end = Mathf.Clamp(Mathf.Max(startIndex, endIndex), 0, StepCount - 1);

            for (int i = start; i <= end; i++)
            {
                ResetRuntimeAt(i);
            }
        }

        /// <summary>
        /// 重置单个步骤和它的当前任务运行状态。
        /// </summary>
        /// <param name="index">需要重置的步骤索引。</param>
        public void ResetRuntimeAt(int index)
        {
            if (index < 0 || index >= runtimeStates.Count)
            {
                return;
            }

            runtimeStates[index].Reset();

            OperateStep step = GetStep(index);
            if (step == null) return;

            step.isCompleted = false;
            step.ActiveTask?.ResetRuntimeState();
        }
        #endregion
    }

    [System.Serializable]
    /// <summary>
    /// StepRuntimeState 类型说明
    /// 
    /// 【功能说明】
    /// 1. 属于模块4 - 维护保养中的 StepRuntimeState 类型。
    /// 2. 负责定义步骤数据、任务数据和运行时完成状态。
    /// 3. 与维护保养流程中的步骤数据、交互逻辑或 UI 显示协同工作。
    /// 
    /// 【依赖组件】
    /// - Unity 组件体系。
    /// - VRHelmet.VRTeam.Maintenance 维护保养流程脚本。
    /// </summary>
    public class StepRuntimeState
    {
        #region ==========Field==========
        /// <summary>
        /// 当前步骤是否已经完成。
        /// </summary>
        public bool isCompleted;

        /// <summary>
        /// 当前步骤中的任务是否已经完成。
        /// </summary>
        public bool isTaskFinished;
        #endregion

        #region ==========Unity Method==========
        #endregion

        #region ==========Logic==========
        #endregion

        #region ==========API==========
        /// <summary>
        /// 清空该步骤运行时状态。
        /// </summary>
        public void Reset()
        {
            isCompleted = false;
            isTaskFinished = false;
        }
        #endregion
    }

    [System.Serializable]
    /// <summary>
    /// Step 类型说明
    /// 
    /// 【功能说明】
    /// 1. 属于模块4 - 维护保养中的 Step 类型。
    /// 2. 负责定义步骤数据、任务数据和运行时完成状态。
    /// 3. 与维护保养流程中的步骤数据、交互逻辑或 UI 显示协同工作。
    /// 
    /// 【依赖组件】
    /// - Unity 组件体系。
    /// - VRHelmet.VRTeam.Maintenance 维护保养流程脚本。
    /// </summary>
    public class Step
    {
        #region ==========Field==========
        [Header("基本信息")]
        /// <summary>
        /// 步骤描述标题。
        /// </summary>
        public string stepDescribe;

        /// <summary>
        /// 该步骤是否已完成。运行时状态优先由 VR4_StepData 管理。
        /// </summary>
        public bool isCompleted = false;

        /// <summary>
        /// 当前步骤对应的任务类型。
        /// </summary>
        public TaskType taskType;

        /// <summary>
        /// 步骤文本说明。
        /// </summary>
        [TextArea]
        public string stepText;

        /// <summary>
        /// 步骤语音提示。
        /// </summary>
        public AudioClip stepClip;

        /// <summary>
        /// 步骤关联动画控制器列表。
        /// </summary>
        public List<Animator> stepAnimators;
        #endregion

        #region ==========Unity Method==========
        #endregion

        #region ==========Logic==========
        #endregion

        #region ==========API==========
        #endregion
    }

    [System.Serializable]
    /// <summary>
    /// DisplayStep 类型说明
    /// 
    /// 【功能说明】
    /// 1. 属于模块4 - 维护保养中的 DisplayStep 类型。
    /// 2. 负责定义步骤数据、任务数据和运行时完成状态。
    /// 3. 与维护保养流程中的步骤数据、交互逻辑或 UI 显示协同工作。
    /// 
    /// 【依赖组件】
    /// - Unity 组件体系。
    /// - VRHelmet.VRTeam.Maintenance 维护保养流程脚本。
    /// </summary>
    public class DisplayStep
    {
        #region ==========Field==========
        [Header("演示内容")]
        /// <summary>
        /// 步骤开始时需要切换显示状态的物体列表。
        /// </summary>
        public List<GameObject> startObject;

        /// <summary>
        /// 步骤完成时需要切换显示状态的物体列表。
        /// </summary>
        public List<GameObject> finishObject;

        /// <summary>
        /// 步骤演示图。
        /// </summary>
        public Sprite displayImage;
        #endregion

        #region ==========Unity Method==========
        #endregion

        #region ==========Logic==========
        #endregion

        #region ==========API==========
        #endregion
    }

    [System.Serializable]
    /// <summary>
    /// OperateStep 类型说明
    /// 
    /// 【功能说明】
    /// 1. 属于模块4 - 维护保养中的 OperateStep 类型。
    /// 2. 负责定义步骤数据、任务数据和运行时完成状态。
    /// 3. 与维护保养流程中的步骤数据、交互逻辑或 UI 显示协同工作。
    /// 
    /// 【依赖组件】
    /// - Unity 组件体系。
    /// - VRHelmet.VRTeam.Maintenance 维护保养流程脚本。
    /// </summary>
    public class OperateStep : Step
    {
        #region ==========Field==========
        /// <summary>
        /// 该步骤是否包含演示显示切换。
        /// </summary>
        public bool haveDisplay;

        /// <summary>
        /// 该步骤的演示显示数据。
        /// </summary>
        public DisplayStep displayStep;

        /// <summary>
        /// 完成该步骤可获得的分数。
        /// </summary>
        [Range(0f, 0.5f)] public float stepScore;

        /// <summary>
        /// 该步骤是否需要双手分别使用不同交互层。
        /// </summary>
        public bool isTwoHands = false;

        /// <summary>
        /// 右手和射线使用的交互层。
        /// </summary>
        public VR4InteractionLayer RlayerMask = VR4InteractionLayer.Nothing;

        /// <summary>
        /// 左手在双手模式下使用的交互层。
        /// </summary>
        public VR4InteractionLayer LlayerMask = VR4InteractionLayer.Nothing;

        [Header("操作内容")]
        /// <summary>
        /// 拾取放置任务配置。
        /// </summary>
        public PickTask pickTask;

        /// <summary>
        /// 旋转任务配置。
        /// </summary>
        public RotateTask rotateTask;

        /// <summary>
        /// 开关任务配置。
        /// </summary>
        public SwitchTask switchTask;

        /// <summary>
        /// 摇晃任务配置。
        /// </summary>
        public ShakeTask shakeTask;

        /// <summary>
        /// 碰撞停留任务配置。
        /// </summary>
        public CollisionTask collisionTask;

        /// <summary>
        /// BaseObject 通用任务配置。
        /// 胎压表或其它自定义交互脚本只要继承 BaseObject 并调用 CompleteStep，就可以完成该步骤。
        /// </summary>
        public BaseObjectTask baseObjectTask;

        /// <summary>
        /// 该步骤完成后是否进入答题。
        /// </summary>
        public bool haveTopic;

        /// <summary>
        /// 本步骤完成后需要回答的题目数量。
        /// </summary>
        [Range(0, 10)] public int topicMount;

        /// <summary>
        /// 当前任务类型对应的任务数据。
        /// </summary>
        public Task ActiveTask
        {
            get
            {
                switch (taskType)
                {
                    case TaskType.Pick:
                        return pickTask;
                    case TaskType.Rotate:
                        return rotateTask;
                    case TaskType.Switch:
                        return switchTask;
                    case TaskType.Shake:
                        return shakeTask;
                    case TaskType.Collision:
                        return collisionTask;
                    case TaskType.BaseObject:
                        return baseObjectTask;
                    default:
                        return null;
                }
            }
        }
        #endregion

        #region ==========Unity Method==========
        #endregion

        #region ==========Logic==========
        #endregion

        #region ==========API==========
        #endregion
    }

    /// <summary>
    /// TaskType 类型说明
    /// 
    /// 【功能说明】
    /// 1. 属于模块4 - 维护保养中的 TaskType 类型。
    /// 2. 负责定义步骤数据、任务数据和运行时完成状态。
    /// 3. 与维护保养流程中的步骤数据、交互逻辑或 UI 显示协同工作。
    /// 
    /// 【依赖组件】
    /// - Unity 组件体系。
    /// - VRHelmet.VRTeam.Maintenance 维护保养流程脚本。
    /// </summary>
    public enum TaskType
    {
        Pick,
        Rotate,
        Switch,
        Shake,
        Collision,
        BaseObject,
    }

    [System.Serializable]
    public struct VR4InteractionLayer
    {
        public const int MaxLayerCount = 30;
        public const int EverythingMask = (1 << MaxLayerCount) - 1;

        [SerializeField] private int mask;

        public int Mask
        {
            get => mask & EverythingMask;
            set => mask = value & EverythingMask;
        }

        public static VR4InteractionLayer Nothing => new VR4InteractionLayer(0);
        public static VR4InteractionLayer None => Nothing;
        public static VR4InteractionLayer Everything => new VR4InteractionLayer(EverythingMask);
        public static VR4InteractionLayer All => Everything;
        public static VR4InteractionLayer Default => new VR4InteractionLayer(1 << 0);
        public static VR4InteractionLayer Interactable => new VR4InteractionLayer(1 << 1);
        public static VR4InteractionLayer NonInteractable => new VR4InteractionLayer(1 << 2);

        public VR4InteractionLayer(int maskValue)
        {
            mask = maskValue & EverythingMask;
        }

        public bool Contains(VR4InteractionLayer other)
        {
            return (Mask & other.Mask) != 0;
        }

        public override string ToString()
        {
            return Mask == EverythingMask ? "Everything" : Mask.ToString();
        }

        public static int operator &(VR4InteractionLayer left, VR4InteractionLayer right)
        {
            return left.Mask & right.Mask;
        }

        public static VR4InteractionLayer operator |(VR4InteractionLayer left, VR4InteractionLayer right)
        {
            return new VR4InteractionLayer(left.Mask | right.Mask);
        }

        public static bool operator ==(VR4InteractionLayer left, VR4InteractionLayer right)
        {
            return left.Mask == right.Mask;
        }

        public static bool operator !=(VR4InteractionLayer left, VR4InteractionLayer right)
        {
            return left.Mask != right.Mask;
        }

        public static bool operator ==(VR4InteractionLayer left, int right)
        {
            return left.Mask == right;
        }

        public static bool operator !=(VR4InteractionLayer left, int right)
        {
            return left.Mask != right;
        }

        public override bool Equals(object obj)
        {
            return obj is VR4InteractionLayer other && this == other;
        }

        public override int GetHashCode()
        {
            return Mask;
        }
    }

    [System.Serializable]
    /// <summary>
    /// Task 类型说明
    /// 
    /// 【功能说明】
    /// 1. 属于模块4 - 维护保养中的 Task 类型。
    /// 2. 负责定义步骤数据、任务数据和运行时完成状态。
    /// 3. 与维护保养流程中的步骤数据、交互逻辑或 UI 显示协同工作。
    /// 
    /// 【依赖组件】
    /// - Unity 组件体系。
    /// - VRHelmet.VRTeam.Maintenance 维护保养流程脚本。
    /// </summary>
    public class Task
    {
        #region ==========Field==========
        /// <summary>
        /// 任务是否完成。
        /// </summary>
        public bool isFinish = false;

        /// <summary>
        /// 玩家需要交互的物体。
        /// </summary>
        public GameObject interactiveObject;

        /// <summary>
        /// 任务正确目标物体。
        /// </summary>
        public GameObject targetObject;

        /// <summary>
        /// 非考试模式下显示的目标箭头。
        /// </summary>
        public GameObject targetArrow;

        /// <summary>
        /// 任务是否具备基础交互物体和目标物体。
        /// </summary>
        public bool HasRequiredObjects => interactiveObject != null && targetObject != null;
        #endregion

        #region ==========Unity Method==========
        #endregion

        #region ==========Logic==========
        #endregion

        #region ==========API==========
        /// <summary>
        /// 校验任务基础数据是否完整。
        /// </summary>
        /// <param name="stepDescribe">用于错误日志的步骤描述。</param>
        /// <returns>数据是否完整。</returns>
        public virtual bool Validate(string stepDescribe)
        {
            if (HasRequiredObjects)
            {
                return true;
            }

            Debug.LogError($"{stepDescribe} 步骤数据不全");
            return false;
        }

        /// <summary>
        /// 设置目标物体显示状态。
        /// </summary>
        /// <param name="visible">是否显示目标物体。</param>
        public void SetTargetVisible(bool visible)
        {
            if (targetObject != null)
            {
                targetObject.SetActive(visible);
            }
        }

        /// <summary>
        /// 设置目标箭头显示状态。
        /// </summary>
        /// <param name="visible">是否显示目标箭头。</param>
        public void SetArrowVisible(bool visible)
        {
            if (targetArrow != null)
            {
                targetArrow.SetActive(visible);
            }
        }

        /// <summary>
        /// 重置任务运行时状态。
        /// </summary>
        public virtual void ResetRuntimeState()
        {
            isFinish = false;
            SetArrowVisible(false);
        }

        /// <summary>
        /// 设置任务完成状态，并在完成时隐藏目标箭头。
        /// </summary>
        /// <param name="value">任务是否完成。</param>
        public virtual void SetFinished(bool value)
        {
            isFinish = value;
            if (value)
            {
                SetArrowVisible(false);
            }
        }
        #endregion
    }

    [System.Serializable]
    /// <summary>
    /// PickTask 类型说明
    /// 
    /// 【功能说明】
    /// 1. 属于模块4 - 维护保养中的 PickTask 类型。
    /// 2. 负责定义步骤数据、任务数据和运行时完成状态。
    /// 3. 与维护保养流程中的步骤数据、交互逻辑或 UI 显示协同工作。
    /// 
    /// 【依赖组件】
    /// - Unity 组件体系。
    /// - VRHelmet.VRTeam.Maintenance 维护保养流程脚本。
    /// </summary>
    public class PickTask : Task
    {
        #region ==========Field==========
        /// <summary>
        /// 拾取物体使用的 XR 抓取组件。
        /// </summary>
        public XRGrabInteractable grabScript;

        /// <summary>
        /// 物体要放置的目标 Socket。
        /// </summary>
        public XRSocketInteractor targetSocket;

        #endregion

        #region ==========Unity Method==========
        #endregion

        #region ==========Logic==========
        #endregion

        #region ==========API==========
        #endregion
    }

    [System.Serializable]
    /// <summary>
    /// RotateTask 类型说明
    /// 
    /// 【功能说明】
    /// 1. 属于模块4 - 维护保养中的 RotateTask 类型。
    /// 2. 负责定义步骤数据、任务数据和运行时完成状态。
    /// 3. 与维护保养流程中的步骤数据、交互逻辑或 UI 显示协同工作。
    /// 
    /// 【依赖组件】
    /// - Unity 组件体系。
    /// - VRHelmet.VRTeam.Maintenance 维护保养流程脚本。
    /// </summary>
    public class RotateTask : Task
    {
        #region ==========Field==========
        /// <summary>
        /// 负责读取旋转值的旋钮脚本。
        /// </summary>
        public VR4_RotatableObject rotatableScript;

        /// <summary>
        /// 任务要求达到的目标旋转值。
        /// </summary>
        [Range(0f, 1f)]
        public float targetValue = 1f;

        /// <summary>
        /// 判定目标值时允许的误差。
        /// </summary>
        [Range(0f, 0.2f)]
        public float tolerance = 0.05f;
        #endregion

        #region ==========Unity Method==========
        #endregion

        #region ==========Logic==========
        #endregion

        #region ==========API==========
        #endregion
    }

    [System.Serializable]
    /// <summary>
    /// SwitchTask 类型说明
    /// 
    /// 【功能说明】
    /// 1. 属于模块4 - 维护保养中的 SwitchTask 类型。
    /// 2. 负责定义步骤数据、任务数据和运行时完成状态。
    /// 3. 与维护保养流程中的步骤数据、交互逻辑或 UI 显示协同工作。
    /// 
    /// 【依赖组件】
    /// - Unity 组件体系。
    /// - VRHelmet.VRTeam.Maintenance 维护保养流程脚本。
    /// </summary>
    public class SwitchTask : Task
    {
        #region ==========Field==========
        /// <summary>
        /// 负责开关完成事件的脚本。
        /// </summary>
        public VR4_SwitchObject switchScript;

        /// <summary>
        /// true 表示等待开关打开完成，false 表示等待开关闭合完成。
        /// </summary>
        [Tooltip("本步骤要求开关达到的目标状态。勾选表示等待开关打开完成，不勾选表示等待开关关闭完成。")]
        public bool targetSwitchOpen;

        [Tooltip("仅在目标状态为打开时生效。勾选后，开关打开时会先自动恢复为关闭状态，然后再完成当前步骤。")]
        public bool closeAfterOpenBeforeComplete;
        #endregion

        #region ==========Unity Method==========
        #endregion

        #region ==========Logic==========
        #endregion

        #region ==========API==========
        #endregion
    }

    [System.Serializable]
    /// <summary>
    /// ShakeTask 类型说明
    /// 
    /// 【功能说明】
    /// 1. 属于模块4 - 维护保养中的 ShakeTask 类型。
    /// 2. 负责定义步骤数据、任务数据和运行时完成状态。
    /// 3. 与维护保养流程中的步骤数据、交互逻辑或 UI 显示协同工作。
    /// 
    /// 【依赖组件】
    /// - Unity 组件体系。
    /// - VRHelmet.VRTeam.Maintenance 维护保养流程脚本。
    /// </summary>
    public class ShakeTask : Task
    {
        #region ==========Field==========
        /// <summary>
        /// 负责摇晃计数的脚本。
        /// </summary>
        public VR4_ShakeObject shakeScript;

        /// <summary>
        /// 完成任务所需摇晃次数。
        /// </summary>
        public int targetCount;

        /// <summary>
        /// 预留的动画状态名。
        /// </summary>
        public string animationState;
        #endregion

        #region ==========Unity Method==========
        #endregion

        #region ==========Logic==========
        #endregion

        #region ==========API==========
        #endregion
    }

    [System.Serializable]
    /// <summary>
    /// CollisionTask 类型说明
    /// 
    /// 【功能说明】
    /// 1. 属于模块4 - 维护保养中的 CollisionTask 类型。
    /// 2. 负责定义步骤数据、任务数据和运行时完成状态。
    /// 3. 与维护保养流程中的步骤数据、交互逻辑或 UI 显示协同工作。
    /// 
    /// 【依赖组件】
    /// - Unity 组件体系。
    /// - VRHelmet.VRTeam.Maintenance 维护保养流程脚本。
    /// </summary>
    public class CollisionTask : Task
    {
        #region ==========Field==========
        /// <summary>
        /// 碰撞任务中使用的抓取组件。
        /// </summary>
        public XRGrabInteractable grabScript;

        /// <summary>
        /// 负责碰撞停留判定的脚本。
        /// </summary>
        public VR4_CollisionObject collisionScript;

        /// <summary>
        /// 需要持续碰撞的时长。
        /// </summary>
        public float needTime;

        /// <summary>
        /// 碰撞计时时需要控制的动画布尔参数。
        /// </summary>
        public string animBool;
        #endregion

        #region ==========Unity Method==========
        #endregion

        #region ==========Logic==========
        #endregion

        #region ==========API==========
        #endregion
    }

    [System.Serializable]
    /// <summary>
    /// BaseObjectTask 类型说明
    /// 
    /// 【功能说明】
    /// 1. 属于模块4 - 维护保养中的 BaseObjectTask 类型。
    /// 2. 负责定义步骤数据、任务数据和运行时完成状态。
    /// 3. 与维护保养流程中的步骤数据、交互逻辑或 UI 显示协同工作。
    /// 
    /// 【依赖组件】
    /// - Unity 组件体系。
    /// - VRHelmet.VRTeam.Maintenance 维护保养流程脚本。
    /// </summary>
    public class BaseObjectTask : Task
    {
        #region ==========Field==========
        /// <summary>
        /// 当前步骤监听的通用交互物体。
        /// 如果为空，会从 interactiveObject 上自动获取 BaseObject 组件。
        /// </summary>
        public VR4_BaseObject baseObject;
        #endregion

        #region ==========Unity Method==========
        #endregion

        #region ==========Logic==========
        private void CacheBaseObject()
        {
            if (baseObject == null && interactiveObject != null)
            {
                baseObject = interactiveObject.GetComponent<VR4_BaseObject>();
            }
        }
        #endregion

        #region ==========API==========
        /// <summary>
        /// BaseObject 任务只强制要求 interactiveObject 和 BaseObject。
        /// targetObject/targetArrow 仍可选配，用于提示显示。
        /// </summary>
        /// <param name="stepDescribe">用于错误日志的步骤描述。</param>
        /// <returns>任务数据是否有效。</returns>
        public override bool Validate(string stepDescribe)
        {
            if (interactiveObject == null)
            {
                Debug.LogError($"{stepDescribe} 未配置 BaseObjectTask.interactiveObject");
                return false;
            }

            CacheBaseObject();
            if (baseObject != null)
            {
                return true;
            }

            Debug.LogError($"{stepDescribe} 未配置 BaseObjectTask.baseObject，且 interactiveObject 上未找到 BaseObject");
            return false;
        }

        /// <summary>
        /// 重置任务状态，同时重置 BaseObject 的完成锁。
        /// </summary>
        public override void ResetRuntimeState()
        {
            base.ResetRuntimeState();
            CacheBaseObject();
            baseObject?.ResetStepCompletion();
        }
        #endregion
    }
}

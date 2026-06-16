using System;
using System.Collections.Generic;
using Unity.XR.PXR;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using static Unity.XR.PXR.PXR_Input;

// ============================================================
// 文件名：VR4_ExperimentManager
// 模块：模块4 - 维护保养
// 功能：维护保养实验流程管理器，负责步骤推进、任务处理器调度、答题入口和考试状态。
// 创建日期：2026-05-5
// 最后更新：2026-05-5
// ============================================================

namespace VRHelmet.VRTeam.Maintenance
{
    /// <summary>
    /// 负责实验流程推进、任务处理器调度、交互层切换和手柄震动。
    /// </summary>
    /// <summary>
    /// VR4_ExperimentManager 类型说明
    /// 
    /// 【功能说明】
    /// 1. 属于模块4 - 维护保养中的 VR4_ExperimentManager 类型。
    /// 2. 负责推进步骤、调度任务处理器、处理答题入口和考试状态。
    /// 3. 与维护保养流程中的步骤数据、交互逻辑和 UI 显示协同工作。
    /// 
    /// 【依赖组件】
    /// - Unity 组件体系。
    /// - VRHelmet.VRTeam.Maintenance 维护保养流程脚本。
    /// </summary>
    public class VR4_ExperimentManager : VR4_SingletonMonoBehaviour<VR4_ExperimentManager>
    {
        #region ==========Field==========
        /// <summary>
        /// 当前实验流程使用的步骤数据。
        /// </summary>
        public VR4_StepData stepData;

        [Header("手部交互")]
        /// <summary>
        /// 左手直接交互器，用于切换当前步骤交互层。
        /// </summary>
        public XRDirectInteractor lController;

        /// <summary>
        /// 右手直接交互器，用于切换当前步骤交互层。
        /// </summary>
        public XRDirectInteractor rController;

        /// <summary>
        /// 右手射线交互器，用于切换当前步骤交互层。
        /// </summary>
        public XRRayInteractor rightRayInteractor;

        /// <summary>
        /// 左手射线交互器，用于切换当前步骤交互层。
        /// </summary>
        public XRRayInteractor leftRayInteractor;

        [Header("抓取输入")]
        public InputActionReference rightGrabInputAction;

        [SerializeField][Range(0.1f, 0.3f)] private float vibrationCooldown = 0.2f;
        [SerializeField] private bool enableRuntimeLogs = false;
        private float lastVibrationTime = 0f;
        private bool rightGrabInputActionEnabledByThis = false;

        /// <summary>
        /// 当前步骤索引。
        /// </summary>
        public int currentIndex = 0;

        /// <summary>
        /// 当前工作单步骤区间的起始索引，包含。
        /// </summary>
        public int currentRangeStartIndex = 0;

        /// <summary>
        /// 当前工作单步骤区间的结束索引，包含，-1 表示不限制结束索引。
        /// </summary>
        public int currentRangeEndIndex = -1;

        /// <summary>
        /// 当前实验累计总分。
        /// </summary>
        public float totalScore = 0;

        /// <summary>
        /// 考试模式剩余时间。
        /// </summary>
        public float timer = 360f;

        private bool isTopic = false;
        private bool isExperimentEnded = false;

        private static bool isTest = false;

        /// <summary>
        /// 当前是否处于考试模式。
        /// </summary>
        public static bool IsTest
        {
            get => isTest;
            set => isTest = value;
        }

        /// <summary>
        /// 步骤开始事件，参数为步骤数据和步骤索引。
        /// </summary>
        public event Action<OperateStep, int> StepStarted;

        /// <summary>
        /// 步骤完成事件，参数为步骤数据和步骤索引。
        /// </summary>
        public event Action<OperateStep, int> StepCompleted;

        /// <summary>
        /// 当前工作单步骤区间完成事件，参数为起始步骤索引和结束步骤索引。
        /// </summary>
        public event Action<int, int> StepRangeCompleted;

        /// <summary>
        /// 进入答题事件，参数为步骤数据和步骤索引。
        /// </summary>
        public event Action<OperateStep, int> TopicStarted;

        /// <summary>
        /// 考试倒计时变化事件，参数为剩余秒数。
        /// </summary>
        public event Action<float> TimerChanged;

        /// <summary>
        /// 实验结束事件，参数为最终分数。
        /// </summary>
        public event Action<float> ExamEnded;

        private readonly Dictionary<TaskType, IVR4TaskHandler> taskHandlers = new Dictionary<TaskType, IVR4TaskHandler>();
        #endregion

        #region ==========Unity Method==========
        protected override void OnSingletonAwake()
        {
            RegisterTaskHandlers();
            EnableGrabInputActions();
        }

        private void OnEnable()
        {
            EnableGrabInputActions();
        }

        private void OnDisable()
        {
            StopCurrentTaskHandler();
            DisableGrabInputActionsEnabledByThis();
        }

        protected override void OnSingletonDestroyed()
        {
            StopCurrentTaskHandler();
            DisableGrabInputActionsEnabledByThis();
            ClearAllStepInteractionPermissions();
            ClearRuntimeEvents();
            taskHandlers.Clear();
        }

        void Update()
        {
            if (!isTest || isExperimentEnded)
            {
                return;
            }

            timer -= Time.deltaTime;
            TimerChanged?.Invoke(timer);

            if (timer <= 0)
            {
                EndExperiment();
            }
        }
        #endregion

        #region ==========Logic==========
        void StartTask(OperateStep step)
        {
            LogRuntime($"[VR4Flow] Frame={Time.frameCount} StartTask Index={currentIndex}, Type={step.taskType}, Desc={step.stepDescribe}");
            ChangeInteractionLayer(step.RlayerMask, step.LlayerMask, step.isTwoHands);
            StepStarted?.Invoke(step, currentIndex);

            if (step.haveDisplay)
                HandleDisplayStep(step.displayStep, false);
        }

        bool TryPrepareTask(OperateStep step, Task task)
        {
            if (task == null || !task.Validate(step.stepDescribe))
            {
                return false;
            }

            task.SetTargetVisible(true);
            EnableFunctionalComponent(step, task.interactiveObject);

            if (!isTest)
            {
                task.SetArrowVisible(true);
            }

            return true;
        }

        void HandleAllTask(OperateStep o)
        {
            Task activeTask = o.ActiveTask;
            if (!TryPrepareTask(o, activeTask))
            {
                return;
            }

            if (!taskHandlers.TryGetValue(o.taskType, out IVR4TaskHandler handler))
            {
                Debug.LogError($"未注册任务处理器: {o.taskType}");
                return;
            }

            int taskStartIndex = currentIndex;
            handler.StartTask(o, task =>
            {
                if (taskStartIndex != currentIndex || stepData.GetStep(taskStartIndex) != o)
                {
                    LogRuntimeWarning($"[VR4Flow] Ignore stale task callback. StartedIndex={taskStartIndex}, CurrentIndex={currentIndex}, Type={o.taskType}, Desc={o.stepDescribe}");
                    return;
                }

                FinishTask(o, task);
            });
        }

        void FinishTask<T>(OperateStep o, T task) where T : Task
        {
            if (stepData.IsStepCompleted(currentIndex))
            {
                return;
            }

            task.SetFinished(true);

            if (o.haveDisplay)
                HandleDisplayStep(o.displayStep, true);

            if (taskHandlers.TryGetValue(o.taskType, out IVR4TaskHandler handler))
            {
                handler.StopTask(o);
            }

            DisableFunctionalComponent(task.interactiveObject);
            CompleteStep(o);
        }

        void CompleteStep(OperateStep o)
        {
            if (stepData.IsStepCompleted(currentIndex))
            {
                return;
            }

            stepData.CompleteStep(currentIndex);
            totalScore += o.stepScore;
            LogRuntime($"[VR4Flow] Frame={Time.frameCount} StepCompleted Index={currentIndex}, Type={o.taskType}, NextAction={(o.haveTopic ? "Topic" : "MoveNext")}");
            StepCompleted?.Invoke(o, currentIndex);

            if (o.haveTopic)
            {
                isTopic = true;
                SetRayEnabled(true);
                TopicStarted?.Invoke(o, currentIndex);
                return;
            }

            MoveToNextStep();
        }

        void MoveToNextStep()
        {
            currentIndex++;
            Debug.Log($"[VR4Flow] MoveToNextStep. CurrentIndex={currentIndex}");

            if (currentRangeEndIndex >= 0 && currentIndex > currentRangeEndIndex)
            {
                CompleteCurrentRange();
                return;
            }

            if (currentIndex < stepData.StepCount)
            {
                NextStep();
                return;
            }

            LogRuntime("Experiment completed.");
            if (isTest)
            {
                EndExperiment();
            }
        }

        void CompleteCurrentRange()
        {
            SetRayEnabled(true);
            StepRangeCompleted?.Invoke(currentRangeStartIndex, currentRangeEndIndex);

            if (isTest && currentIndex >= stepData.StepCount)
            {
                EndExperiment();
            }
        }

        void HandleDisplayStep(DisplayStep d, bool isFinish)
        {
            if (d == null)
            {
                return;
            }

            List<GameObject> objects = isFinish ? d.finishObject : d.startObject;
            if (objects == null)
            {
                return;
            }

            foreach (GameObject obj in objects)
            {
                if (obj != null)
                {
                    obj.SetActive(!obj.activeSelf);
                }
            }
        }

        void RegisterTaskHandlers()
        {
            taskHandlers.Clear();
            RegisterTaskHandler(new PickTaskHandler());
            RegisterTaskHandler(new RotateTaskHandler());
            RegisterTaskHandler(new SwitchTaskHandler());
            RegisterTaskHandler(new ShakeTaskHandler());
            RegisterTaskHandler(new CollisionTaskHandler());
            RegisterTaskHandler(new BaseObjectTaskHandler());
        }

        void RegisterTaskHandler(IVR4TaskHandler handler)
        {
            taskHandlers[handler.TaskType] = handler;
        }

        void StopCurrentTaskHandler()
        {
            if (stepData == null || currentIndex < 0 || currentIndex >= stepData.StepCount)
            {
                return;
            }

            OperateStep step = stepData.GetStep(currentIndex);
            if (step == null || !taskHandlers.TryGetValue(step.taskType, out IVR4TaskHandler handler))
            {
                return;
            }

            handler.StopTask(step);
        }

        void ClearRuntimeEvents()
        {
            StepStarted = null;
            StepCompleted = null;
            StepRangeCompleted = null;
            TopicStarted = null;
            TimerChanged = null;
            ExamEnded = null;
        }

        void LogRuntime(string message)
        {
            if (enableRuntimeLogs)
            {
                Debug.Log(message);
            }
        }

        void LogRuntimeWarning(string message)
        {
            if (enableRuntimeLogs)
            {
                Debug.LogWarning(message);
            }
        }

        void SetInteractorLayers(XRBaseInteractor interactor, InteractionLayerMask layerMask)
        {
            if (interactor != null)
            {
                interactor.interactionLayers = layerMask;
            }
        }

        void EnableGrabInputActions()
        {
            EnableInputActionIfNeeded(rightGrabInputAction, ref rightGrabInputActionEnabledByThis);
        }

        void DisableGrabInputActionsEnabledByThis()
        {
            DisableInputActionIfNeeded(rightGrabInputAction, ref rightGrabInputActionEnabledByThis);
        }

        void EnableInputActionIfNeeded(InputActionReference actionReference, ref bool enabledByThis)
        {
            if (actionReference == null || actionReference.action == null || actionReference.action.enabled)
            {
                return;
            }

            actionReference.action.Enable();
            enabledByThis = true;
        }

        void DisableInputActionIfNeeded(InputActionReference actionReference, ref bool enabledByThis)
        {
            if (!enabledByThis || actionReference == null || actionReference.action == null)
            {
                enabledByThis = false;
                return;
            }

            actionReference.action.Disable();
            enabledByThis = false;
        }

        void ApplyStepInteractionPermission(VR4InteractionLayer rightLayerMask, VR4InteractionLayer leftLayerMask, bool isTwoHands)
        {
            VR4InteractionLayer normalizedRightMask = NormalizeInteractionLayerMask(rightLayerMask);
            VR4InteractionLayer normalizedLeftMask = NormalizeInteractionLayerMask(leftLayerMask);
            VR4_FunctionalComponents[] functionalComponents = FindObjectsOfType<VR4_FunctionalComponents>(true);
            VR4_BaseObject[] baseObjects = FindObjectsOfType<VR4_BaseObject>(true);

            foreach (VR4_FunctionalComponents component in functionalComponents)
            {
                if (component == null)
                {
                    continue;
                }

                component.ConfigureStepInteractionPermission(
                    normalizedRightMask,
                    normalizedLeftMask,
                    isTwoHands,
                    lController,
                    rController,
                    leftRayInteractor,
                    rightRayInteractor);
            }

            foreach (VR4_BaseObject baseObject in baseObjects)
            {
                if (baseObject == null)
                {
                    continue;
                }

                baseObject.ConfigureStepInteractionPermission(
                    normalizedRightMask,
                    normalizedLeftMask,
                    isTwoHands,
                    lController,
                    rController,
                    leftRayInteractor,
                    rightRayInteractor);
            }

            LogRuntime($"[VR4Permission] Apply StepMask Right={normalizedRightMask}, Left={normalizedLeftMask}, TwoHands={isTwoHands}, FunctionalCount={functionalComponents.Length}, BaseObjectCount={baseObjects.Length}");
        }

        void ClearAllStepInteractionPermissions()
        {
            VR4_FunctionalComponents[] functionalComponents = FindObjectsOfType<VR4_FunctionalComponents>(true);
            foreach (VR4_FunctionalComponents component in functionalComponents)
            {
                if (component != null)
                {
                    component.ClearStepInteractionPermission();
                }
            }

            VR4_BaseObject[] baseObjects = FindObjectsOfType<VR4_BaseObject>(true);
            foreach (VR4_BaseObject baseObject in baseObjects)
            {
                if (baseObject != null)
                {
                    baseObject.ClearStepInteractionPermission();
                }
            }
        }

        VR4InteractionLayer NormalizeInteractionLayerMask(VR4InteractionLayer layerMask)
        {
            return layerMask == VR4InteractionLayer.None ? VR4_StepData.DefaultInteractionMask : layerMask;
        }

        void EnableFunctionalComponent(OperateStep step, GameObject obj)
        {
            VR4_FunctionalComponents functionalComponent = obj.GetComponent<VR4_FunctionalComponents>();
            if (functionalComponent != null)
            {
                if (!functionalComponent.IsAllowedByCurrentStep())
                {
                    LogRuntimeWarning($"[VR4Permission] Current task object is not allowed by step mask. Step={step.stepDescribe}, Object={obj.name}");
                }

                if (ShouldUseFunctionalRigidbodyControl(step))
                {
                    functionalComponent.ActivateStepRigidbody();
                }
                else
                {
                    functionalComponent.DeactivateStepRigidbody();
                }
            }

            VR4_BaseObject baseObject = obj.GetComponent<VR4_BaseObject>();
            if (baseObject != null && !baseObject.IsAllowedByCurrentStep())
            {
                LogRuntimeWarning($"[VR4Permission] Current BaseObject is not allowed by step mask. Step={step.stepDescribe}, Object={obj.name}");
            }

            IHighlightable highlight = obj.GetComponent<IHighlightable>();
            if (highlight == null)
            {
                LogRuntimeWarning($"{obj.name}的基础组件为空");
                return;
            }

            if (!isTest)
            {
                highlight.ShowHighlight();
            }
        }

        bool ShouldUseFunctionalRigidbodyControl(OperateStep step)
        {
            if (step == null)
            {
                return false;
            }

            return step.taskType == TaskType.Pick ||
                   step.taskType == TaskType.Shake ||
                   step.taskType == TaskType.Collision;
        }

        void DisableFunctionalComponent(GameObject obj)
        {
            VR4_FunctionalComponents functionalComponent = obj.GetComponent<VR4_FunctionalComponents>();
            if (functionalComponent != null)
            {
                functionalComponent.DeactivateStepRigidbody();
            }

            IHighlightable highlight = obj.GetComponent<IHighlightable>();
            if (highlight != null && !isTest)
            {
                highlight.HideHighlight();
            }
        }

        void SetRayEnabled(bool enabled)
        {
            if (leftRayInteractor != null)
            {
                leftRayInteractor.enabled = enabled;
            }
        }

        bool IsCurrentIndexOutOfRange()
        {
            return currentRangeEndIndex >= 0 && currentIndex > currentRangeEndIndex;
        }
        #endregion

        #region ==========API==========
        /// <summary>
        /// 从指定步骤区间开始执行工作单。示例：1 到 4 会依次执行 oStepList[1]、[2]、[3]、[4]。
        /// </summary>
        /// <param name="startIndex">起始步骤索引，包含。</param>
        /// <param name="endIndex">结束步骤索引，包含。</param>
        public void BeginStepRange(int startIndex, int endIndex)
        {
            if (stepData == null || stepData.StepCount <= 0)
            {
                Debug.LogError("VR4_ExperimentManager 未配置有效的 VR4_StepData 或 oStepList 为空");
                EndExperiment();
                return;
            }

            currentRangeStartIndex = Mathf.Clamp(Mathf.Min(startIndex, endIndex), 0, stepData.StepCount - 1);
            currentRangeEndIndex = Mathf.Clamp(Mathf.Max(startIndex, endIndex), 0, stepData.StepCount - 1);
            currentIndex = currentRangeStartIndex;
            isExperimentEnded = false;
            isTopic = false;

            stepData.ResetRuntimeRange(currentRangeStartIndex, currentRangeEndIndex);
            LogStepRangePlan(currentRangeStartIndex, currentRangeEndIndex);
            NextStep();
        }

        private void LogStepRangePlan(int startIndex, int endIndex)
        {
            if (stepData == null)
            {
                return;
            }

            LogRuntime($"[VR4Flow] BeginStepRange Plan Start={startIndex}, End={endIndex}, StepCount={stepData.StepCount}");

            for (int i = startIndex; i <= endIndex && i < stepData.StepCount; i++)
            {
                OperateStep step = stepData.GetStep(i);
                if (step == null)
                {
                    LogRuntime($"[VR4Flow] Plan Index={i}, Step=NULL");
                    continue;
                }

                LogRuntime($"[VR4Flow] Plan Index={i}, Type={step.taskType}, Desc={step.stepDescribe}, Completed={stepData.IsStepCompleted(i)}");
            }
        }

        /// <summary>
        /// 推进到当前索引对应的步骤，并启动该步骤任务。
        /// </summary>
        public void NextStep()
        {
            if (currentIndex == 0)
            {
                isExperimentEnded = false;
            }

            if (stepData == null || currentIndex < 0 || currentIndex >= stepData.StepCount)
            {
                EndExperiment();
                return;
            }

            if (IsCurrentIndexOutOfRange())
            {
                CompleteCurrentRange();
                return;
            }

            OperateStep curStep = stepData.GetStep(currentIndex);
            if (curStep == null)
            {
                EndExperiment();
                return;
            }

            if (stepData.IsStepCompleted(currentIndex))
            {
                return;
            }

            StartTask(curStep);
            HandleAllTask(curStep);
        }

        /// <summary>
        /// 通知流程答题已经完成，并继续推进到下一步骤。
        /// </summary>
        public void NotifyTopicCompleted()
        {
            if (!isTopic) return;

            isTopic = false;
            SetRayEnabled(true);
            MoveToNextStep();
        }

        /// <summary>
        /// 结束实验并广播结算事件。
        /// </summary>
        public void EndExperiment()
        {
            if (isExperimentEnded)
            {
                return;
            }

            isExperimentEnded = true;
            StopCurrentTaskHandler();
            ClearAllStepInteractionPermissions();
            SetRayEnabled(true);
            ExamEnded?.Invoke(totalScore);
        }

        /// <summary>
        /// 重置实验索引、分数、计时器和所有步骤运行时状态。
        /// </summary>
        public void ResetGame()
        {
            StopCurrentTaskHandler();
            ClearAllStepInteractionPermissions();
            currentIndex = 0;
            currentRangeStartIndex = 0;
            currentRangeEndIndex = -1;
            totalScore = 0;
            timer = 180;
            isTest = false;
            isTopic = false;
            isExperimentEnded = false;
            stepData?.ResetRuntime();
        }

        /// <summary>
        /// 根据当前步骤配置切换左右手和射线的交互层。
        /// </summary>
        /// <param name="rlayerMask">右手和射线使用的交互层。</param>
        /// <param name="llayerMask">双手模式下左手使用的交互层。</param>
        /// <param name="isTwoHands">是否使用双手不同交互层。</param>
        public void ChangeInteractionLayer(VR4InteractionLayer rlayerMask, VR4InteractionLayer llayerMask, bool isTwoHands)
        {
            ApplyStepInteractionPermission(rlayerMask, llayerMask, isTwoHands);
        }

        public bool IsAnyControllerGrabbing()
        {
            bool isActivated = IsInputActionActivated(rightGrabInputAction);
            if (isActivated)
            {
                Debug.Log("[VR4GrabInput] RightGrabInputAction activated once.");
            }

            return isActivated;
        }

        private static bool IsInputActionActivated(InputActionReference actionReference)
        {
            return actionReference != null &&
                   actionReference.action != null &&
                   actionReference.action.WasPressedThisFrame();
        }

        /// <summary>
        /// 请求触发一次带冷却的双手柄震动。
        /// </summary>
        public static void RequestVibration()
        {
            if (HasInstance)
            {
                Instance.HandleVibrationRequest();
            }
        }

        /// <summary>
        /// 立即处理一次手柄震动请求。
        /// </summary>
        public void HandleVibrationRequest()
        {
            if (Time.time - lastVibrationTime < vibrationCooldown)
            {
                return;
            }

            lastVibrationTime = Time.time;
            PXR_Input.SendHapticImpulse(VibrateType.BothController, 0.3f, 100, 60);
        }

        /// <summary>
        /// 停止双手柄震动。
        /// </summary>
        public static void StopVibration()
        {
            PXR_Input.SendHapticImpulse(VibrateType.BothController, 0f, 0, 60);
        }

        /// <summary>
        /// 切换左手射线启用状态。
        /// </summary>
        public void PokeToChangeRay()
        {
            if (leftRayInteractor != null)
            {
                leftRayInteractor.enabled = !leftRayInteractor.enabled;
            }
        }
        #endregion
    }

    /// <summary>
    /// IVR4TaskHandler 类型说明
    /// 
    /// 【功能说明】
    /// 1. 属于模块4 - 维护保养中的 IVR4TaskHandler 类型。
    /// 2. 负责推进步骤、调度任务处理器、处理答题入口和考试状态。
    /// 3. 与维护保养流程中的步骤数据、交互逻辑和 UI 显示协同工作。
    /// 
    /// 【依赖组件】
    /// - Unity 组件体系。
    /// - VRHelmet.VRTeam.Maintenance 维护保养流程脚本。
    /// </summary>
    public interface IVR4TaskHandler
    {
        #region ==========Field==========
        /// <summary>
        /// 该处理器负责的任务类型。
        /// </summary>
        TaskType TaskType { get; }
        #endregion

        #region ==========API==========
        /// <summary>
        /// 启动指定步骤的任务，并在任务完成时触发回调。
        /// </summary>
        /// <param name="step">当前步骤。</param>
        /// <param name="onCompleted">任务完成回调。</param>
        /// <returns>是否成功启动任务。</returns>
        bool StartTask(OperateStep step, Action<Task> onCompleted);

        /// <summary>
        /// 停止指定步骤的任务监听。
        /// </summary>
        /// <param name="step">当前步骤。</param>
        void StopTask(OperateStep step);
        #endregion
    }

    /// <summary>
    /// VR4_TaskHandler 类型说明
    /// 
    /// 【功能说明】
    /// 1. 属于模块4 - 维护保养中的 VR4_TaskHandler 类型。
    /// 2. 负责推进步骤、调度任务处理器、处理答题入口和考试状态。
    /// 3. 与维护保养流程中的步骤数据、交互逻辑和 UI 显示协同工作。
    /// 
    /// 【依赖组件】
    /// - Unity 组件体系。
    /// - VRHelmet.VRTeam.Maintenance 维护保养流程脚本。
    /// </summary>
    public abstract class VR4_TaskHandler<TTask> : IVR4TaskHandler where TTask : Task
    {
        #region ==========Field==========
        /// <summary>
        /// 该处理器负责的任务类型。
        /// </summary>
        public abstract TaskType TaskType { get; }
        #endregion

        #region ==========Unity Method==========
        #endregion

        #region ==========Logic==========
        protected abstract TTask GetTask(OperateStep step);
        protected abstract bool StartTask(OperateStep step, TTask task, Action<Task> onCompleted);
        protected virtual void StopTask(OperateStep step, TTask task) { }

        protected TComponent GetTaskObjectComponent<TComponent>(Task task) where TComponent : Component
        {
            if (task == null || task.interactiveObject == null)
            {
                return null;
            }

            return task.interactiveObject.GetComponent<TComponent>();
        }

        protected Component GetTaskObjectComponentByType(OperateStep step, Task task)
        {
            if (step == null || task == null || task.interactiveObject == null)
            {
                return null;
            }

            switch (step.taskType)
            {
                case TaskType.Pick:
                    return GetTaskObjectComponent<VR4_GrabObject>(task);
                case TaskType.Rotate:
                    return GetTaskObjectComponent<VR4_RotatableObject>(task);
                case TaskType.Switch:
                    return GetTaskObjectComponent<VR4_SwitchObject>(task);
                case TaskType.Collision:
                    return GetTaskObjectComponent<VR4_CollisionObject>(task);
                case TaskType.BaseObject:
                    return null;
                default:
                    return null;
            }
        }
        #endregion

        #region ==========API==========
        /// <summary>
        /// 启动指定步骤中与处理器匹配的任务。
        /// </summary>
        /// <param name="step">当前步骤。</param>
        /// <param name="onCompleted">任务完成回调。</param>
        /// <returns>是否成功启动任务。</returns>
        public bool StartTask(OperateStep step, Action<Task> onCompleted)
        {
            TTask task = GetTask(step);
            if (task == null)
            {
                Debug.LogError($"{step.stepDescribe} 未配置{TaskType} 任务数据");
                return false;
            }

            return StartTask(step, task, onCompleted);
        }

        /// <summary>
        /// 停止指定步骤中与处理器匹配的任务。
        /// </summary>
        /// <param name="step">当前步骤。</param>
        public void StopTask(OperateStep step)
        {
            TTask task = GetTask(step);
            if (task != null)
            {
                StopTask(step, task);
            }
        }
        #endregion
    }

    /// <summary>
    /// PickTaskHandler 类型说明
    /// 
    /// 【功能说明】
    /// 1. 属于模块4 - 维护保养中的 PickTaskHandler 类型。
    /// 2. 负责推进步骤、调度任务处理器、处理答题入口和考试状态。
    /// 3. 与维护保养流程中的步骤数据、交互逻辑和 UI 显示协同工作。
    /// 
    /// 【依赖组件】
    /// - Unity 组件体系。
    /// - VRHelmet.VRTeam.Maintenance 维护保养流程脚本。
    /// </summary>
    public class PickTaskHandler : VR4_TaskHandler<PickTask>
    {
        #region ==========Field==========
        /// <summary>
        /// 处理拾取放置任务。
        /// </summary>
        public override TaskType TaskType => TaskType.Pick;
        #endregion

        #region ==========Unity Method==========
        #endregion

        #region ==========Logic==========
        protected override PickTask GetTask(OperateStep step) => step.pickTask;

        protected override bool StartTask(OperateStep step, PickTask task, Action<Task> onCompleted)
        {
            StopTask(step, task);

            VR4_GrabObject grabObject = GetTaskObjectComponentByType(step, task) as VR4_GrabObject;
            if (grabObject == null)
            {
                Debug.LogError($"{step.stepDescribe} missing VR4_GrabObject. Use the editor task component binder first.");
                return false;
            }

            ConfigureGrabObjectStepPermission(step, grabObject);
            grabObject.Configure(task.targetSocket, task.targetObject, () =>
            {
                onCompleted?.Invoke(task);
            });
            grabObject.enabled = true;

            return true;
        }

        protected override void StopTask(OperateStep step, PickTask task)
        {
            if (task == null || task.interactiveObject == null)
            {
                return;
            }

            VR4_GrabObject grabObject = task.interactiveObject.GetComponent<VR4_GrabObject>();
            if (grabObject != null)
            {
                grabObject.Clear();
            }
        }

        private void ConfigureGrabObjectStepPermission(OperateStep step, VR4_GrabObject grabObject)
        {
            VR4_ExperimentManager manager = VR4_ExperimentManager.Instance;
            if (manager == null || grabObject == null)
            {
                return;
            }

            VR4InteractionLayer rightLayerMask = step.RlayerMask == VR4InteractionLayer.None ? VR4_StepData.DefaultInteractionMask : step.RlayerMask;
            VR4InteractionLayer leftLayerMask = step.LlayerMask == VR4InteractionLayer.None ? VR4_StepData.DefaultInteractionMask : step.LlayerMask;
            grabObject.ConfigureStepInteractionPermission(
                rightLayerMask,
                leftLayerMask,
                step.isTwoHands,
                manager.lController,
                manager.rController,
                manager.leftRayInteractor,
                manager.rightRayInteractor);
        }
        #endregion

        #region ==========API==========
        #endregion
    }

    /// <summary>
    /// RotateTaskHandler 类型说明
    /// 
    /// 【功能说明】
    /// 1. 属于模块4 - 维护保养中的 RotateTaskHandler 类型。
    /// 2. 负责推进步骤、调度任务处理器、处理答题入口和考试状态。
    /// 3. 与维护保养流程中的步骤数据、交互逻辑和 UI 显示协同工作。
    /// 
    /// 【依赖组件】
    /// - Unity 组件体系。
    /// - VRHelmet.VRTeam.Maintenance 维护保养流程脚本。
    /// </summary>
    public class RotateTaskHandler : VR4_TaskHandler<RotateTask>
    {
        #region ==========Field==========
        private readonly Dictionary<RotateTask, UnityAction<float>> listeners = new Dictionary<RotateTask, UnityAction<float>>();

        /// <summary>
        /// 处理旋转任务。
        /// </summary>
        public override TaskType TaskType => TaskType.Rotate;
        #endregion

        #region ==========Unity Method==========
        #endregion

        #region ==========Logic==========
        protected override RotateTask GetTask(OperateStep step) => step.rotateTask;

        protected override bool StartTask(OperateStep step, RotateTask task, Action<Task> onCompleted)
        {
            if (task.rotatableScript == null)
            {
                task.rotatableScript = GetTaskObjectComponentByType(step, task) as VR4_RotatableObject;
            }

            if (task.rotatableScript == null)
            {
                Debug.LogError($"{step.stepDescribe} 未找到RotatableObject");
                return false;
            }

            StopTask(step, task);

            UnityAction<float> listener = value =>
            {
                if (Mathf.Abs(value - task.targetValue) <= task.tolerance)
                {
                    onCompleted?.Invoke(task);
                }
            };

            listeners[task] = listener;
            task.rotatableScript.onValueChange.AddListener(listener);
            task.rotatableScript.enabled = true;
            return true;
        }

        protected override void StopTask(OperateStep step, RotateTask task)
        {
            if (task.rotatableScript != null && listeners.TryGetValue(task, out UnityAction<float> listener))
            {
                task.rotatableScript.onValueChange.RemoveListener(listener);
                listeners.Remove(task);
            }
        }
        #endregion

        #region ==========API==========
        #endregion
    }

    /// <summary>
    /// SwitchTaskHandler 类型说明
    /// 
    /// 【功能说明】
    /// 1. 属于模块4 - 维护保养中的 SwitchTaskHandler 类型。
    /// 2. 负责推进步骤、调度任务处理器、处理答题入口和考试状态。
    /// 3. 与维护保养流程中的步骤数据、交互逻辑和 UI 显示协同工作。
    /// 
    /// 【依赖组件】
    /// - Unity 组件体系。
    /// - VRHelmet.VRTeam.Maintenance 维护保养流程脚本。
    /// </summary>
    public class SwitchTaskHandler : VR4_TaskHandler<SwitchTask>
    {
        #region ==========Field==========
        /// <summary>
        /// 处理开关任务。
        /// </summary>
        public override TaskType TaskType => TaskType.Switch;
        #endregion

        #region ==========Unity Method==========
        #endregion

        #region ==========Logic==========
        protected override SwitchTask GetTask(OperateStep step) => step.switchTask;

        protected override bool StartTask(OperateStep step, SwitchTask task, Action<Task> onCompleted)
        {
            if (task.switchScript == null)
            {
                task.switchScript = GetTaskObjectComponentByType(step, task) as VR4_SwitchObject;
            }

            if (task.switchScript == null)
            {
                Debug.LogError($"{step.stepDescribe} 未配置SwitchObject");
                return false;
            }

            task.switchScript.OnSwitchOpenCompleted = null;
            task.switchScript.OnSwitchCloseCompleted = null;
            task.switchScript.ConfigureSwitchTarget(task.targetSwitchOpen, task.closeAfterOpenBeforeComplete);
            task.switchScript.OnSwitchTargetCompleted += () => onCompleted?.Invoke(task);

            task.switchScript.enabled = true;
            return true;
        }

        protected override void StopTask(OperateStep step, SwitchTask task)
        {
            if (task.switchScript == null)
            {
                return;
            }

            task.switchScript.OnSwitchOpenCompleted = null;
            task.switchScript.OnSwitchCloseCompleted = null;
            task.switchScript.ClearSwitchTarget();
        }
        #endregion

        #region ==========API==========
        #endregion
    }

    /// <summary>
    /// ShakeTaskHandler 类型说明
    /// 
    /// 【功能说明】
    /// 1. 属于模块4 - 维护保养中的 ShakeTaskHandler 类型。
    /// 2. 负责推进步骤、调度任务处理器、处理答题入口和考试状态。
    /// 3. 与维护保养流程中的步骤数据、交互逻辑和 UI 显示协同工作。
    /// 
    /// 【依赖组件】
    /// - Unity 组件体系。
    /// - VRHelmet.VRTeam.Maintenance 维护保养流程脚本。
    /// </summary>
    public class ShakeTaskHandler : VR4_TaskHandler<ShakeTask>
    {
        #region ==========Field==========
        /// <summary>
        /// 处理摇晃任务。
        /// </summary>
        public override TaskType TaskType => TaskType.Shake;
        #endregion

        #region ==========Unity Method==========
        #endregion

        #region ==========Logic==========
        protected override ShakeTask GetTask(OperateStep step) => step.shakeTask;

        protected override bool StartTask(OperateStep step, ShakeTask task, Action<Task> onCompleted)
        {
            if (task.shakeScript == null)
            {
                Debug.LogError($"{step.stepDescribe} 未配置ShakeObject");
                return false;
            }

            task.shakeScript.targetCount = task.targetCount;
            task.shakeScript.OnShakeCompleted = null;
            task.shakeScript.OnShakeCompleted += () => onCompleted?.Invoke(task);
            task.shakeScript.enabled = true;
            return true;
        }

        protected override void StopTask(OperateStep step, ShakeTask task)
        {
            if (task.shakeScript == null)
            {
                return;
            }

            task.shakeScript.OnShakeCompleted = null;
        }
        #endregion

        #region ==========API==========
        #endregion
    }

    /// <summary>
    /// CollisionTaskHandler 类型说明
    /// 
    /// 【功能说明】
    /// 1. 属于模块4 - 维护保养中的 CollisionTaskHandler 类型。
    /// 2. 负责推进步骤、调度任务处理器、处理答题入口和考试状态。
    /// 3. 与维护保养流程中的步骤数据、交互逻辑和 UI 显示协同工作。
    /// 
    /// 【依赖组件】
    /// - Unity 组件体系。
    /// - VRHelmet.VRTeam.Maintenance 维护保养流程脚本。
    /// </summary>
    public class CollisionTaskHandler : VR4_TaskHandler<CollisionTask>
    {
        #region ==========Field==========
        /// <summary>
        /// 处理碰撞停留任务。
        /// </summary>
        public override TaskType TaskType => TaskType.Collision;
        #endregion

        #region ==========Unity Method==========
        #endregion

        #region ==========Logic==========
        protected override CollisionTask GetTask(OperateStep step) => step.collisionTask;

        protected override bool StartTask(OperateStep step, CollisionTask task, Action<Task> onCompleted)
        {
            if (task.collisionScript == null)
            {
                task.collisionScript = GetTaskObjectComponentByType(step, task) as VR4_CollisionObject;
            }

            if (task.collisionScript == null)
            {
                Debug.LogError($"{step.stepDescribe} 未配置CollisionObject");
                return false;
            }

            task.collisionScript.AddData(task);
            task.collisionScript.OnCollisionCompleted = null;
            task.collisionScript.OnCollisionCompleted += () => onCompleted?.Invoke(task);
            task.collisionScript.enabled = true;
            return true;
        }

        protected override void StopTask(OperateStep step, CollisionTask task)
        {
            if (task.collisionScript == null)
            {
                return;
            }

            task.collisionScript.OnCollisionCompleted = null;
        }
        #endregion

        #region ==========API==========
        #endregion
    }

    /// <summary>
    /// BaseObjectTaskHandler 类型说明
    /// 
    /// 【功能说明】
    /// 1. 属于模块4 - 维护保养中的 BaseObjectTaskHandler 类型。
    /// 2. 负责推进步骤、调度任务处理器、处理答题入口和考试状态。
    /// 3. 与维护保养流程中的步骤数据、交互逻辑和 UI 显示协同工作。
    /// 
    /// 【依赖组件】
    /// - Unity 组件体系。
    /// - VRHelmet.VRTeam.Maintenance 维护保养流程脚本。
    /// </summary>
    public class BaseObjectTaskHandler : VR4_TaskHandler<BaseObjectTask>
    {
        #region ==========Field==========
        private readonly Dictionary<BaseObjectTask, UnityAction> listeners = new Dictionary<BaseObjectTask, UnityAction>();

        /// <summary>
        /// 处理继承 BaseObject 的通用自定义任务。
        /// </summary>
        public override TaskType TaskType => TaskType.BaseObject;
        #endregion

        #region ==========Unity Method==========
        #endregion

        #region ==========Logic==========
        protected override BaseObjectTask GetTask(OperateStep step) => step.baseObjectTask;

        protected override bool StartTask(OperateStep step, BaseObjectTask task, Action<Task> onCompleted)
        {
            if (task.baseObject == null && task.interactiveObject != null)
            {
                task.baseObject = task.interactiveObject.GetComponent<VR4_BaseObject>();
            }

            if (task.baseObject == null)
            {
                Debug.LogError($"{step.stepDescribe} 未配置BaseObjectTask.baseObject");
                return false;
            }

            StopTask(step, task);

            task.baseObject.ResetStepCompletion();
            if (task.baseObject is VR4_TirePressureGauge tirePressureGauge)
            {
                tirePressureGauge.BeginPressureCheck();
            }
            else if (task.baseObject is VR4_DepthGauge depthGauge)
            {
                depthGauge.BeginDepthCheck();
            }

            UnityAction listener = () =>
            {
                onCompleted?.Invoke(task);
            };
            listeners[task] = listener;
            task.baseObject.OnStepCompleted.AddListener(listener);
            task.baseObject.enabled = true;
            return true;
        }

        protected override void StopTask(OperateStep step, BaseObjectTask task)
        {
            if (task.baseObject != null && listeners.TryGetValue(task, out UnityAction listener))
            {
                task.baseObject.OnStepCompleted.RemoveListener(listener);
                listeners.Remove(task);
            }
        }
        #endregion

        #region ==========API==========
        #endregion
    }
}



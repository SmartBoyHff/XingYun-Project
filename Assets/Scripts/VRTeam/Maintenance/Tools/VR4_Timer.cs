using UnityEngine;

// ============================================================
// 文件名：VR_Timer
// 模块：模块4 - 维护保养
// 功能：通用计时器组件，支持开始、暂停、恢复、取消、完成和进度查询。
// 创建日期：2026-05-5
// 最后更新：2026-05-5
// ============================================================

namespace VRHelmet.VRTeam.Maintenance
{
    /// <summary>
    /// VR_Timer 类型说明
    /// 
    /// 【功能说明】
    /// 1. 属于模块4 - 维护保养中的 VR_Timer 类型。
    /// 2. 负责提供开始、暂停、恢复、取消、完成和进度查询。
    /// 3. 与维护保养流程中的步骤数据、交互逻辑或 UI 显示协同工作。
    /// 
    /// 【依赖组件】
    /// - Unity 组件体系。
    /// - VRHelmet.VRTeam.Maintenance 维护保养流程脚本。
    /// </summary>
    public class VR4_Timer : MonoBehaviour
    {
        #region ==========Field==========
        /// <summary>
        /// 计时器运行状态。
        /// </summary>
        /// <summary>
        /// TimerState 类型说明
        /// 
        /// 【功能说明】
        /// 1. 属于模块4 - 维护保养中的 TimerState 类型。
        /// 2. 负责提供开始、暂停、恢复、取消、完成和进度查询。
        /// 3. 与维护保养流程中的步骤数据、交互逻辑或 UI 显示协同工作。
        /// 
        /// 【依赖组件】
        /// - Unity 组件体系。
        /// - VRHelmet.VRTeam.Maintenance 维护保养流程脚本。
        /// </summary>
        public enum TimerState
        {
            Sleep,
            Running,
            Paused
        }

        /// <summary>
        /// 当前计时器状态。
        /// </summary>
        public TimerState State { get; private set; } = TimerState.Sleep;

        /// <summary>
        /// 本轮计时的目标时长。
        /// </summary>
        public float TargetTime { get; private set; }

        /// <summary>
        /// 已运行的净时间。
        /// </summary>
        public float Runtime { get; private set; }

        /// <summary>
        /// 计时进度，范围通常为 0 到 1。
        /// </summary>
        public float Progress => TargetTime > 0 ? Runtime / TargetTime : 0f;

        private float _startTime;
        private float _durationTime;

        /// <summary>
        /// 计时开始事件。
        /// </summary>
        public System.Action OnTimerStarted;

        /// <summary>
        /// 计时暂停事件。
        /// </summary>
        public System.Action OnTimerPaused;

        /// <summary>
        /// 计时恢复事件。
        /// </summary>
        public System.Action OnTimerResumed;

        /// <summary>
        /// 计时完成事件。
        /// </summary>
        public System.Action OnTimerCompleted;

        /// <summary>
        /// 计时取消或重置事件。
        /// </summary>
        public System.Action OnTimerCancelled;

        /// <summary>
        /// 当前是否正在运行。
        /// </summary>
        public bool IsRunning => State == TimerState.Running;

        /// <summary>
        /// 当前是否处于暂停状态。
        /// </summary>
        public bool IsPaused => State == TimerState.Paused;

        /// <summary>
        /// 当前计时是否已经完成。
        /// </summary>
        public bool IsComplete => State == TimerState.Sleep && TargetTime > 0 && Mathf.Approximately(Runtime, TargetTime);

        [Header("动画控制器")]
        [SerializeField] private Animator targetAnimator;
        [SerializeField] private string animParameterName = null;
        private float cachedAnimatorSpeed = 1.0f;
        #endregion

        #region ==========Unity Method==========
        void Start()
        {
            if (targetAnimator != null)
            {
                cachedAnimatorSpeed = targetAnimator.speed;
            }
        }

        private void Update()
        {
            if (State != TimerState.Running) return;

            float currentRunTime = Time.realtimeSinceStartup - _startTime - _durationTime;
            Runtime = currentRunTime;

            if (Runtime >= TargetTime)
            {
                CompleteTimer();
            }
        }
        #endregion

        #region ==========Logic==========
        private void CompleteTimer()
        {
            State = TimerState.Sleep;
            Runtime = TargetTime;
            PauseLinkedAnimation();

            OnTimerCompleted?.Invoke();
            Debug.Log($"[计时器] 完成!");
        }

        private void PauseLinkedAnimation()
        {
            if (targetAnimator != null && targetAnimator.isActiveAndEnabled)
            {
                targetAnimator.speed = 0f;
            }
        }

        private void ResumeLinkedAnimation()
        {
            if (targetAnimator != null && targetAnimator.isActiveAndEnabled)
            {
                targetAnimator.speed = cachedAnimatorSpeed;
            }
        }

        private void SetAnimator(Animator animator)
        {
            if (targetAnimator == animator) return;

            targetAnimator = animator;

            if (animator != null)
            {
                cachedAnimatorSpeed = animator.speed;
            }
        }

        private void TriggerAnimation(bool value)
        {
            if (targetAnimator != null && !string.IsNullOrEmpty(animParameterName))
            {
                targetAnimator.SetBool(animParameterName, value);
            }
        }
        #endregion

        #region ==========API==========
        /// <summary>
        /// 开始一次新的计时，并可同步驱动一个 Animator 布尔参数。
        /// </summary>
        /// <param name="needTime">计时目标时长。</param>
        /// <param name="animator">可选的联动动画控制器。</param>
        /// <param name="animBool">可选的联动动画布尔参数名。</param>
        public void StartTimer(float needTime, Animator animator = null, string animBool = null)
        {
            ResetTimerStateOnly();

            if (animator != null)
            {
                SetAnimator(animator);
            }

            if (!string.IsNullOrEmpty(animBool))
            {
                animParameterName = animBool;
            }

            TargetTime = needTime;
            _startTime = Time.realtimeSinceStartup;
            State = TimerState.Running;
            OnTimerStarted?.Invoke();

            if (targetAnimator != null && !targetAnimator.enabled)
            {
                targetAnimator.enabled = true;
                targetAnimator.speed = cachedAnimatorSpeed;
            }

            TriggerAnimation(true);

            Debug.Log($"[计时器] 开始: 目标 {needTime}秒");
        }

        /// <summary>
        /// 暂停当前计时，并暂停联动动画。
        /// </summary>
        public void PauseTimer()
        {
            State = TimerState.Paused;
            _durationTime = Time.realtimeSinceStartup - _startTime - Runtime;
            OnTimerPaused?.Invoke();

            PauseLinkedAnimation();
            Debug.Log($"[计时器] 已暂停. 已运行 {Runtime:F2}秒");
        }

        /// <summary>
        /// 从暂停中恢复当前计时，并恢复联动动画。
        /// </summary>
        public void ResumeTimer()
        {
            if (State != TimerState.Paused) return;

            State = TimerState.Running;
            _startTime = Time.realtimeSinceStartup - Runtime - _durationTime;
            OnTimerResumed?.Invoke();

            ResumeLinkedAnimation();
            Debug.Log($"[计时器] 已恢复. 继续从 {Runtime:F2}秒 开始");
        }

        /// <summary>
        /// 仅重置计时器运行状态，不清空 Animator 引用。
        /// </summary>
        public void ResetTimerStateOnly()
        {
            State = TimerState.Sleep;
            Runtime = 0f;
            TargetTime = 0f;
            _startTime = 0f;
            _durationTime = 0f;

            OnTimerCancelled?.Invoke();
        }

        /// <summary>
        /// 完全重置计时器，并停止、清空联动动画状态。
        /// </summary>
        public void ResetTimer()
        {
            ResetTimerStateOnly();

            TriggerAnimation(false);

            if (targetAnimator != null)
            {
                targetAnimator.speed = 0;
                targetAnimator = null;
            }
        }
        #endregion
    }
}

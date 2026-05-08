using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System;

// ============================================================
// 文件名：CollisionObject
// 模块：模块4 - 维护保养
// 功能：碰撞停留交互物体，负责检测抓取状态、目标碰撞、计时和完成事件。
// 创建日期：2026-05-5
// 最后更新：2026-05-5
// ============================================================

namespace VRHelmet.VRTeam.Maintenance
{
    /// <summary>
    /// CollisionObject 类型说明
    /// 
    /// 【功能说明】
    /// 1. 属于模块4 - 维护保养中的 CollisionObject 类型。
    /// 2. 负责检测抓取、目标碰撞、停留计时和完成事件。
    /// 3. 与维护保养流程中的步骤数据、交互逻辑或 UI 显示协同工作。
    /// 
    /// 【依赖组件】
    /// - Unity 组件体系。
    /// - VRHelmet.VRTeam.Maintenance 维护保养流程脚本。
    /// </summary>
    public class VR4_CollisionObject : VR4_BaseObject
    {
        #region ==========Field==========
        [Header("碰撞设置")]
        /// <summary>
        /// 是否直接使用基类碰撞完成逻辑。
        /// </summary>
        public bool useParent = false;

        protected GameObject interactiveObject;
        protected GameObject targetObject; // 目标物体
        protected XRGrabInteractable grabScript;
        protected GameObject collisionObj;

        /// <summary>
        /// 碰撞停留时间完成时触发的事件。
        /// </summary>
        public Action OnCollisionCompleted; // 碰撞停留时间完成时触发的事件

        protected bool isColliding = false; // 是否正在碰撞
        protected bool hasCompleted = false;
        protected bool isPaused = false; // 新增：暂停状态


        [Header("计时设置")]
        protected float needTime;

        [Header("动画设置")]
        protected string animBool;

        /// <summary>
        /// 碰撞计时期间联动的步骤动画控制器。
        /// </summary>
        public Animator stepAnimator;

        [Header("计时器设置")]
        [SerializeField] protected VR4_Timer _collisionTimer;
        #endregion

        #region ==========Unity Method==========
        protected virtual void Start()
        {
            if (_collisionTimer != null)
            {
                _collisionTimer.OnTimerCompleted += HandleTimerCompleted;
            }
        }

        private void OnDestroy()
        {
            if (_collisionTimer != null)
            {
                _collisionTimer.OnTimerCompleted -= HandleTimerCompleted;
            }
        }
        #endregion

        #region ==========Logic==========
        protected virtual void HandleTimerCompleted()
        {
            if (hasCompleted) return;

            if (useParent)
            {
                // 父类模式：直接完成碰撞
                CompleteCollision();
            }
            else
            {
                // 子类模式：调用子类的完成方法
                OnCustomCollisionComplete();
            }
        }

        #region ----------Start Events----------
        protected bool IsGrab() { return grabScript != null && grabScript.isSelected; }

        protected bool IsSatisfy(Collider other)
        {
            if (other.gameObject == targetObject)
                return IsGrab();

            return false;
        }

        protected void StartCollision(Collider other)
        {
            // 重置所有状态
            ResetAllStates();
            // 重置状态锁
            isColliding = true;
            collisionObj = other.gameObject;

            VR4_ExperimentManager.RequestVibration();
            if (useParent)
                VR4_UIManager.ShowTip("此实验步骤已开始");
        }
        #endregion

        private void OnTriggerEnter(Collider other)
        {
            if (hasCompleted) return;

            if (IsSatisfy(other))
            {
                StartCollision(other);
                OnCustomCollisionEnter();
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (hasCompleted || !isColliding) return;

            bool satisfy = IsSatisfy(other);

            if (satisfy)
            {
                VR4_ExperimentManager.RequestVibration();

                // 如果之前是暂停状态，恢复
                if (isPaused && _collisionTimer.IsPaused)
                {
                    _collisionTimer.ResumeTimer();
                }

                if (useParent)
                {
                    if (_collisionTimer == null) return;

                    // 父类模式总是满足计时条件
                    if (!_collisionTimer.IsRunning && !_collisionTimer.IsComplete)
                    {
                        _collisionTimer.StartTimer(needTime, stepAnimator);
                    }
                }
                else
                {
                    OnCustomCollisionStay();
                }
            }
            else
            {
                if (_collisionTimer != null && _collisionTimer.IsRunning)
                {
                    _collisionTimer.PauseTimer();
                }

                // 暂停动画状态
                if (stepAnimator != null && !string.IsNullOrEmpty(animBool))
                {
                    stepAnimator.SetBool(animBool, false);
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!isColliding) return;

            if (IsSatisfy(other))
            {
                OnCustomCollisionExit();
                ResetCollision();
            }
        }
        #region ----------Finished Events----------
        protected void CompleteCollision()
        {
            if (hasCompleted) return;
            hasCompleted = true;

            CompleteStep(OnCollisionCompleted);

            // 恢复动画状态
            if (stepAnimator != null && !string.IsNullOrEmpty(animBool))
            {
                stepAnimator.SetBool(animBool, false);
            }
            ResetAllStates();
            this.enabled = false;
        }

        protected void ResetCollision()
        {
            isColliding = false;
            isPaused = false;
            if (_collisionTimer != null)
            {
                _collisionTimer.ResetTimerStateOnly();
            }
            VR4_ExperimentManager.StopVibration();
            if (useParent)
                VR4_UIManager.ShowTip("此实验步骤已暂停");
        }

        protected void ResetAllStates()
        {
            isColliding = false;
            hasCompleted = false;
            isPaused = false;
            collisionObj = null;
            if (_collisionTimer != null)
            {
                _collisionTimer.ResetTimer(); // 完全重置计时器

                // 确保事件绑定正确
                _collisionTimer.OnTimerCompleted -= HandleTimerCompleted;
                _collisionTimer.OnTimerCompleted += HandleTimerCompleted;
            }
            VR4_ExperimentManager.StopVibration();
        }
        #endregion

        #region ----------Template Methods----------
        protected virtual void OnCustomCollisionEnter() { }
        protected virtual void OnCustomCollisionStay() { }
        protected virtual void OnCustomCollisionExit() { }
        protected virtual void OnCustomCollisionComplete() { }
        #endregion
        #endregion

        #region ==========API==========
        /// <summary>
        /// 注入当前碰撞任务运行所需的数据。
        /// </summary>
        /// <param name="task">碰撞任务配置。</param>
        public void AddData(CollisionTask task)
        {
            ResetStepCompletion();
            interactiveObject = task.interactiveObject;
            targetObject = task.targetObject;
            grabScript = task.grabScript;
            needTime = task.needTime;
            animBool = task.animBool;
        }
        #endregion
    }
}

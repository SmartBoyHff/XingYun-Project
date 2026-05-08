using UnityEngine;

// ============================================================
// 文件名：SingletonMonoBehaviour
// 模块：模块4 - 维护保养
// 功能：MonoBehaviour 单例基类，提供场景内唯一实例访问和初始化钩子。
// 创建日期：2026-05-5
// 最后更新：2026-05-5
// ============================================================

namespace VRHelmet.VRTeam.Maintenance
{
    /// <summary>
    /// 通用 MonoBehaviour 单例基类。
    /// </summary>
    /// <typeparam name="T">单例类型，需为 MonoBehaviour 派生类。</typeparam>
    /// <summary>
    /// SingletonMonoBehaviour 类型说明
    /// 
    /// 【功能说明】
    /// 1. 属于模块4 - 维护保养中的 SingletonMonoBehaviour 类型。
    /// 2. 负责提供 MonoBehaviour 单例访问和初始化钩子。
    /// 3. 与维护保养流程中的步骤数据、交互逻辑或 UI 显示协同工作。
    /// 
    /// 【依赖组件】
    /// - Unity 组件体系。
    /// - VRHelmet.VRTeam.Maintenance 维护保养流程脚本。
    /// </summary>
    public abstract class VR4_SingletonMonoBehaviour<T> : MonoBehaviour where T : MonoBehaviour
    {
        #region ==========Field==========
        private static T instance;
        private static bool isQuitting;

        /// <summary>
        /// 全局唯一实例；场景中不存在时会自动创建。
        /// </summary>
        public static T Instance
        {
            get
            {
                if (isQuitting) return null;
                if (instance != null) return instance;

                instance = FindObjectOfType<T>();
                if (instance != null) return instance;

                var go = new GameObject(typeof(T).Name);
                instance = go.AddComponent<T>();
                return instance;
            }
        }

        /// <summary>
        /// 当前是否已经存在单例实例。
        /// </summary>
        public static bool HasInstance => instance != null;

        /// <summary>
        /// 是否跨场景保持单例物体。
        /// </summary>
        protected virtual bool PersistBetweenScenes => true;
        #endregion

        #region ==========Unity Method==========
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this as T;

            OnSingletonAwake();
        }

        private void OnApplicationQuit()
        {
            isQuitting = true;
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;

            OnSingletonDestroyed();
        }
        #endregion

        #region ==========Logic==========
        #endregion

        #region ==========API==========
        /// <summary>
        /// 单例初始化扩展点，子类可在这里执行 Awake 阶段初始化。
        /// </summary>
        protected virtual void OnSingletonAwake() { }

        /// <summary>
        /// 单例销毁扩展点，子类可在这里执行清理。
        /// </summary>
        protected virtual void OnSingletonDestroyed() { }
        #endregion
    }
}
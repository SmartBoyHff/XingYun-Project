using System.Collections.Generic;
using UnityEngine;

// ============================================================
// 文件名：RestoreObject
// 模块：模块4 - 维护保养
// 功能：物体复位触发区，负责在交互物体离开区域时调用复位逻辑。
// 创建日期：2026-05-5
// 最后更新：2026-05-5
// ============================================================

namespace VRHelmet.VRTeam.Maintenance
{
    /// <summary>
    /// 针对运动学刚体的物体。
    /// </summary>
    /// <summary>
    /// RestoreObject 类型说明
    /// 
    /// 【功能说明】
    /// 1. 属于模块4 - 维护保养中的 RestoreObject 类型。
    /// 2. 负责在交互物体离开区域时调用复位逻辑。
    /// 3. 与维护保养流程中的步骤数据、交互逻辑或 UI 显示协同工作。
    /// 
    /// 【依赖组件】
    /// - Unity 组件体系。
    /// - VRHelmet.VRTeam.Maintenance 维护保养流程脚本。
    /// </summary>
    public class VR4_RestoreObject : MonoBehaviour
    {
        #region ==========Field==========
        /// <summary>
        /// 离开触发区域时需要检查并复位的功能组件列表。
        /// </summary>
        public List<VR4_FunctionalComponents> functionalComponents;
        #endregion

        #region ==========Unity Method==========
        private void OnTriggerExit(Collider other)
        {
            foreach (var a in functionalComponents)
            {
                if (other.name == a.name)
                    a.ResetObject();
            }
        }
        #endregion

        #region ==========Logic==========
        #endregion

        #region ==========API==========
        #endregion
    }
}
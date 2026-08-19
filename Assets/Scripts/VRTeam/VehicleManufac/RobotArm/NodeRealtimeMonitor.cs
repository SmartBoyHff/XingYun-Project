using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public struct NamedUnityEvent
{
    public string nodeDescription;      // 匹配节点的 description
    public UnityEvent onReachedEvent;
}
public class NodeRealtimeMonitor : MonoBehaviour
{
    [Header("监测配置")]
    [SerializeField] private ActionRecorder targetRecorder; // 可由外部传入

    [Header("节点事件映射 (按描述匹配)")]
    public List<NamedUnityEvent> nodeEventMappings;

    [Header("节点事件映射 (按索引，优先级低于按描述)")]
    public UnityEvent[] nodeEventsByIndex; // 可在 Inspector 中设置多个

    [Header("全局事件")]
    public UnityEvent<KeyNode> onNodeReached;   // 任意节点达成
    public UnityEvent onAllNodesComplete;

    private StandardTask currentTask;
    private List<KeyNode> remainingNodes = new List<KeyNode>();
    private bool isMonitoring;

    // 启动监测并绑定事件
    public void StartMonitoring(StandardTask task, ActionRecorder recorder)
    {
        if (task == null || recorder == null) return;
        currentTask = task;
        targetRecorder = recorder;

        // 重置所有节点状态并绑定事件
        BindTaskEvents(task);

        remainingNodes.Clear();
        remainingNodes.AddRange(task.keyNodes);
        isMonitoring = true;
    }

    public void StopMonitoring()
    {
        isMonitoring = false;
    }

    void Update()
    {
        if (!isMonitoring || targetRecorder == null || currentTask == null) return;

        JointAngles currentAngles = targetRecorder.GetCurrentAngles();

        for (int i = remainingNodes.Count - 1; i >= 0; i--)
        {
            var node = remainingNodes[i];
            if (node.hasBeenReached) continue;

            if (IsAngleWithinTolerance(currentAngles, node.targetAngles, node.angleTolerance))
            {
                node.hasBeenReached = true;
                node.onReached?.Invoke();          // 触发动态绑定的自定义事件
                onNodeReached?.Invoke(node);       // 全局事件
                remainingNodes.RemoveAt(i);
            }
        }

        if (remainingNodes.Count == 0 && currentTask.keyNodes.Count > 0)
        {
            onAllNodesComplete?.Invoke();
            StopMonitoring();
        }
    }

    private void BindTaskEvents(StandardTask task)
    {
        foreach (var node in task.keyNodes)
        {
            // 重置状态
            node.hasBeenReached = false;
            node.onReached.RemoveAllListeners();

            // 按描述匹配
            bool matched = false;
            foreach (var mapping in nodeEventMappings)
            {
                if (string.Equals(mapping.nodeDescription, node.description, StringComparison.OrdinalIgnoreCase))
                {
                    node.onReached.AddListener(() => mapping.onReachedEvent.Invoke());
                    matched = true;
                    break;
                }
            }

            // 若未匹配，尝试按索引
            int index = task.keyNodes.IndexOf(node);
            if (!matched && nodeEventsByIndex != null && index < nodeEventsByIndex.Length)
            {
                var evt = nodeEventsByIndex[index];
                if (evt != null)
                {
                    node.onReached.AddListener(() => evt.Invoke());
                }
            }
        }
    }

    private bool IsAngleWithinTolerance(JointAngles a, JointAngles b, float tol)
    {
        return Mathf.Abs(a.shoulderYaw - b.shoulderYaw) <= tol &&
               Mathf.Abs(a.shoulderPitch - b.shoulderPitch) <= tol &&
               Mathf.Abs(a.elbow - b.elbow) <= tol &&
               Mathf.Abs(a.wristPitch - b.wristPitch) <= tol &&
               Mathf.Abs(a.wristRoll - b.wristRoll) <= tol;
    }
}

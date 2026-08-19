using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StandardTaskCreator : MonoBehaviour
{
    [SerializeField] private RobotUIManager uiManager;
    [SerializeField] private ActionRecorder recorder;
    [SerializeField] private StandardTaskLibrary taskLibrary;

    private StandardTask currentTask;

    public void StartCreatingTask()
    {
        currentTask = new StandardTask();
        currentTask.taskName = "任务" + (taskLibrary.tasks.Count + 1);
        recorder.ResetKeyNodes();
        recorder.StartRecording();
        uiManager.ShowMessage("记录标准动作中，按Y键标记关键节点，按B结束");
    }

    public void FinishCreatingTask()
    {
        recorder.StopRecording(false);  // 不自动保存为普通动作
        if (recorder.CurrentClip == null) return;

        currentTask.standardClip = recorder.CurrentClip;
        currentTask.keyNodes = new List<KeyNode>(recorder.CurrentKeyNodes);
        taskLibrary.AddTask(currentTask);
        uiManager.ShowMessage("标准任务“" + currentTask.taskName + "”已保存");
        recorder.ResetCurrentRecording();
    }
}

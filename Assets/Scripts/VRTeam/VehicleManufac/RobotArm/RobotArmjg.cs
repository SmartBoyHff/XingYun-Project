using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
[System.Serializable]
public struct JointAngles
{
    public float shoulderYaw;
    public float shoulderPitch;
    public float elbow;
    public float wristPitch;
    public float wristRoll;
}

[System.Serializable]
public struct RecordFrame
{
    public float timestamp;
    public JointAngles angles;
    public int qualityMark; // 0=无标记, 1=优, 2=良, 3=差 (可选)
}

[System.Serializable]
public class ActionClip
{
    public List<RecordFrame> frames = new List<RecordFrame>();
   
    public float TotalDuration
    {
        get
        {
            if (frames == null || frames.Count == 0)
                return 0f;
            return frames[frames.Count - 1].timestamp;
        }
    }
}
[System.Serializable]
public class ActionRecord
{
    public string actionName;           // 动作名称，如“抓取零件A”
    public ActionClip clip;             // 关节角度时间序列
    public DateTime recordTime;         // 记录时间
    public string author;               // 记录者（教师/学生）
    public int armIndex;                // 所属机械臂索引
    public string notes;                // 备注
}
[System.Serializable]
public class StandardTask
{
    public string taskName;
    public ActionClip standardClip;           // 完整标准动作
    public List<KeyNode> keyNodes;            // 关键节点列表
}

[System.Serializable]
public class KeyNode
{
    public float timestamp;                   // 在标准动作中的时刻
    public JointAngles targetAngles;          // 目标关节角度
    public float angleTolerance;              // 允许误差（度）
    public string description;                // 匹配动作
    [System.NonSerialized]
    public UnityEvent onReached = new UnityEvent();
    [System.NonSerialized]
    public bool hasBeenReached;
}
public class AssessmentResult
{
    public bool passed;              // 是否通过（全对）
    public int totalNodes;
    public int errorNodes;
    public float timeRatio;          // 学生用时/标准用时
    public string grade;             // 优秀/良好/差
    public List<ErrorDetail> errors;
    public float studentTime;    // 学生操作总时长（秒）
    public float standardTime;   // 标准动作总时长（秒）
}

public struct ErrorDetail
{
    public int nodeIndex;
    public string jointName;
    public float expectedAngle;
    public float actualAngle;
}
[System.Serializable]
public class TeachingStep
{
    public string instruction;          // 文字提示，如“将大臂旋转至45°”
    public string targetJoint;          // 关节名
    public float targetAngle;           // 目标角度
    public GameObject knobHighlight;    // 对应旋钮的高亮对象（可预制）
    public GameObject jointHighlight;   // 关节处的半透明高亮模型（指示旋转方向）
}
[System.Serializable]
public class ExamSequence
{
    public string sequenceName;             // 如“综合考核一”
    public List<ExamStage> stages = new List<ExamStage>();
}
[System.Serializable]
public class ExamStage
{
    public string stageName;                // 如“基础抓取”
    public StandardTask task;               // 此阶段要完成的标准任务
    public int armIndex;             // 多机械臂
}
[System.Serializable]
public class WholeResult
{
    public string finalGrade;
    public List<StageResult> stageResults = new List<StageResult>();
}

[System.Serializable]
public class StageResult
{
    public string stageName;
    public bool passed;
    public float timeUsed;       // 学生用时（秒）
    public float timeRatio;      // 学生用时 / 标准用时
    public List<ErrorDetail> errors = new List<ErrorDetail>();
}
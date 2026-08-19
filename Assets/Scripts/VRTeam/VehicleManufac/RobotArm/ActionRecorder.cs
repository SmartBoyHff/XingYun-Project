using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ActionRecorder : MonoBehaviour
{
    private static Dictionary<int, ActionRecorder> instances = new Dictionary<int, ActionRecorder>();

    [SerializeField] private int armIndex = 0;      // 机械臂编号（0,1,2...）
    public int ArmIndex => armIndex;
    public static ActionRecorder Instance
    {
        get
        {
            instances.TryGetValue(0, out var rec);
            return rec;
        }
    }
    // 根据索引获取实例
    public static ActionRecorder GetRecorder(int index)
    {
        instances.TryGetValue(index, out var rec);
        return rec;
    }

    [Header("关节引用")]
    public Transform shoulderYaw;
    public Transform shoulderPitch;
    public Transform elbow;
    public Transform wristPitch;
    public Transform wristRoll;

    [Header("记录参数")]
    public float sampleInterval = 0.05f;
    public float errorAngle = 15f;

    private bool isRecording;
    private bool isPlaying;
    private float recordTimer;
    private float recordStartTime;
    public ActionClip currentClip;           // 正在记录的 clip
    public ActionClip playbackClip;          // 正在回放的 clip
    private float playbackTime;
    private int playbackFrameIndex;

    public bool IsRecording => isRecording;
    public bool IsPlaying => isPlaying;
    public ActionClip CurrentClip => currentClip;

    // 事件通知（可选）
    public System.Action<ActionClip> OnRecordFinished;
    public System.Action OnPlaybackFinished;
    private List<KeyNode> currentKeyNodes = new List<KeyNode>();
    public List<KeyNode> CurrentKeyNodes => currentKeyNodes;

    void Awake()
    {
        // 注册到字典，如果索引冲突则销毁后来的（确保每个索引唯一）
        if (instances.ContainsKey(armIndex))
        {
            Destroy(gameObject);
            return;
        }
        instances[armIndex] = this;
    }

    void Update()
    {
        if (isRecording)
        {
            recordTimer += Time.deltaTime;
            if (recordTimer >= sampleInterval)
            {
                recordTimer -= sampleInterval;
                RecordFrame();
            }
        }

        if (isPlaying)
        {
            UpdatePlayback();
        }
    }

    // ==================== 记录控制 ====================


    public void StartRecording()
    {
        if (isRecording || isPlaying)
        {
            if (isPlaying) StopPlayback();   // 如果正在播放，先停掉
            else return;
        }
        if (isRecording || isPlaying) return;
        currentClip = new ActionClip();
        recordStartTime = Time.time;
        recordTimer = 0f;
        isRecording = true;
    }

    public void StopRecording(bool autoSave = true)
    {
        if (!isRecording) return;
        isRecording = false;
        OnRecordFinished?.Invoke(currentClip);
    }

    //// 供外部直接调用保存（如 UIManager 获得名称后）
    //public void SaveRecord(string actionName, string author)
    //{
    //    if (currentClip == null || currentClip.frames.Count == 0) return;
    //    ActionLibrary.Instance?.AddRecord(currentClip, actionName, author );
    //}

    public void ResetCurrentRecording()
    {
        currentClip = null;
        isRecording = false;
        recordTimer = 0f;
    }

    // ==================== 回放控制 ====================
    public void PlayClip(ActionClip clip)
    {
        if (clip == null || clip.frames.Count < 2) return;
        StopCurrentPlayback();
        playbackClip = clip;
        playbackTime = 0f;
        playbackFrameIndex = 0;
        isPlaying = true;
        ApplyFrameAngles(clip.frames[0].angles);
    }

    public void StopPlayback()
    {
        isPlaying = false;
        playbackClip = null;
        playbackTime = 0f;
        playbackFrameIndex = 0;
        OnPlaybackFinished?.Invoke();
    }

    private void StopCurrentPlayback()
    {
        if (isPlaying)
        {
            isPlaying = false;
            OnPlaybackFinished?.Invoke();
        }
    }

    private void UpdatePlayback()
    {
        playbackTime += Time.deltaTime;
        if (playbackClip == null) return;

        // 查找当前帧区间
        while (playbackFrameIndex < playbackClip.frames.Count - 2 &&
               playbackClip.frames[playbackFrameIndex + 1].timestamp < playbackTime)
        {
            playbackFrameIndex++;
        }

        if (playbackFrameIndex >= playbackClip.frames.Count - 1)
        {
            ApplyFrameAngles(playbackClip.frames[playbackClip.frames.Count - 1].angles);
            StopPlayback();  // 统一调用停止逻辑
            return;
        }

        RecordFrame frameA = playbackClip.frames[playbackFrameIndex];
        RecordFrame frameB = playbackClip.frames[playbackFrameIndex + 1];
        float t = Mathf.InverseLerp(frameA.timestamp, frameB.timestamp, playbackTime);
        JointAngles lerped = LerpAngles(frameA.angles, frameB.angles, t);
        ApplyFrameAngles(lerped);
    }

    // ==================== 内部记录与角度工具 ====================
    private void RecordFrame()
    {
        RecordFrame frame = new RecordFrame
        {
            timestamp = Time.time - recordStartTime,
            angles = CaptureAngles(),
            qualityMark = 0
        };
        currentClip.frames.Add(frame);
    }

    private JointAngles CaptureAngles()
    {
        return new JointAngles
        {
            shoulderYaw = shoulderYaw.localEulerAngles.z,
            shoulderPitch = shoulderPitch.localEulerAngles.y,
            elbow = elbow.localEulerAngles.y,
            wristPitch = wristPitch.localEulerAngles.x,
            wristRoll = wristRoll.localEulerAngles.z
        };
    }

    public JointAngles GetInterpolatedAngles(ActionClip clip, float time)
    {
        if (clip.frames.Count == 0) return new JointAngles();
        if (time <= clip.frames[0].timestamp) return clip.frames[0].angles;
        for (int i = 0; i < clip.frames.Count - 1; i++)
        {
            if (time >= clip.frames[i].timestamp && time <= clip.frames[i + 1].timestamp)
            {
                float t = Mathf.InverseLerp(clip.frames[i].timestamp, clip.frames[i + 1].timestamp, time);
                return LerpAngles(clip.frames[i].angles, clip.frames[i + 1].angles, t);
            }
        }
        return clip.frames[clip.frames.Count - 1].angles;
    }

    private JointAngles LerpAngles(JointAngles a, JointAngles b, float t)
    {
        return new JointAngles
        {
            shoulderYaw = Mathf.LerpAngle(a.shoulderYaw, b.shoulderYaw, t),
            shoulderPitch = Mathf.LerpAngle(a.shoulderPitch, b.shoulderPitch, t),
            elbow = Mathf.LerpAngle(a.elbow, b.elbow, t),
            wristPitch = Mathf.LerpAngle(a.wristPitch, b.wristPitch, t),
            wristRoll = Mathf.LerpAngle(a.wristRoll, b.wristRoll, t)
        };
    }

    private void ApplyFrameAngles(JointAngles angles)
    {
        shoulderYaw.localRotation = Quaternion.Euler(0, 0, angles.shoulderYaw);
        shoulderPitch.localRotation = Quaternion.Euler(0, angles.shoulderPitch, 0);
        elbow.localRotation = Quaternion.Euler(0, angles.elbow, 0);
        wristPitch.localRotation = Quaternion.Euler(angles.wristPitch, 0, 0);
        wristRoll.localRotation = Quaternion.Euler(0, 0, angles.wristRoll);
    }

    public void AddKeyNode(float timestamp = -1f)
    {
        // 如果未指定时间戳，使用当前记录时间（若正在记录）或回放时间（若正在播放）
        float time = timestamp;
        if (time < 0)
        {
            if (isRecording) time = Time.time - recordStartTime;
            else if (isPlaying) time = playbackTime;
            else return;
        }

        KeyNode node = new KeyNode
        {
            timestamp = time,
            targetAngles = CaptureAngles(),   // 或用当前帧插值角度
            angleTolerance = errorAngle,
            description = null
        };
        currentKeyNodes.Add(node);
      
    }

    public void ResetKeyNodes()
    {
        currentKeyNodes.Clear();
    }

    // ==================== 质量标记（记录过程中调用）====================
    public void MarkCurrentQuality(int level)
    {
        if (!isRecording || currentClip == null || currentClip.frames.Count == 0) return;
        var lastIndex = currentClip.frames.Count - 1;
        var frame = currentClip.frames[lastIndex];
        frame.qualityMark = level;
        currentClip.frames[lastIndex] = frame;
    }

    // ==================== 重置机械臂到初始姿态 ====================
    public void ResetArmPose()
    {
        ApplyFrameAngles(new JointAngles()); // 全零角度
        // 同时把所有旋钮数值也归零（需引用对应的 JointKnobLink）
    }
    public JointAngles GetCurrentAngles()
    {
        return CaptureAngles();
    }

    public void SetAngles(JointAngles angles)
    {
        ApplyFrameAngles(angles);
    }

}

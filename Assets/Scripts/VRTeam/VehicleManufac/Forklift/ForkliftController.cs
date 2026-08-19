using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Content.Interaction;
using UnityEngine.XR.Interaction.Toolkit;

public class ForkliftController : MonoBehaviour
{
    [Header("交互控件引用")]
    public XRKnob steeringKnob;              // 方向盘
    public XRKnob gearKnob;                  // 换挡杆 (Value = 0 为空档)
    public XRKnob directionKnob;             // 换向杆 (Value = 0.5 为空档)
    public XRKnob handbrakeLever;           // 手刹
    public XRSimpleInteractable clutchPedal; // 离合踏板
    public XRSimpleInteractable brakePedal;  // 刹车踏板
    public XRSimpleInteractable throttlePedal;// 油门踏板
    public XRSimpleInteractable ignitionKey; // 钥匙按钮


    [Header("货叉操纵杆")]
    public XRKnob liftKnob;             // 上下操纵杆 (0=最低, 1=最高)
    public XRKnob tiltKnob;             // 前后操纵杆 (控制X轴旋转)
    public XRKnob sideShiftKnob;        // 左右操纵杆 (0=最左, 1=最右)

    [Header("货叉活动部件")]
    public Transform liftTarget;        // MastAssembly (控制升降)
    public Transform sideShiftTarget;   // SideShifter (控制左右平移)
    public Transform tiltTarget;        // Tilter (控制前后倾角)

    [Header("货叉运动范围与速度")]
    public float liftMinY = 0.2f;
    public float liftMaxY = 2.5f;
    public float liftSpeed = 2.0f;

    public float sideShiftMinX = -0.3f;
    public float sideShiftMaxX = 0.3f;
    public float sideShiftSpeed = 1.0f;

    public float tiltMinAngle = -10f;   // 后仰角度
    public float tiltMaxAngle = 15f;    // 前倾角度
    public float tiltSpeed = 30f;       // 度/秒
    [Header("车轮视觉")]
    public Transform[] steerWheels;          // 转向轮模型 (通常为后轮)

    [Header("车辆参数")]
    public float maxSteerAngle = 35f;        // 最大转向角度
    public float[] gearMinSpeeds = { 0, 2f, 5f, 10f }; // 各档位最低速度 (km/h)，索引1~3对应1~3档
    public float[] gearMaxSpeeds = { 0, 5f, 12f, 20f }; // 各档位最高速度 (km/h)
    public float acceleration = 2.5f;        // 加速能力 (m/s²)
    public float deceleration = 1.5f;        // 自然减速 / 发动机制动 (m/s²)
    public float brakeDeceleration = 6f;     // 刹车减速度
    public float handbrakeDeceleration = 10f;// 手刹减速度
    public float turnRate = 45f;             // 转向灵敏度

    [Header("死区与阈值")]
    public float stopThreshold = 0.1f;       // 视为静止的速度 (m/s)
    public float handbrakeThreshold = 0.1f;  // 手刹拉紧判断值 (Value < 此值视为拉紧)
    public float directionDeadZone = 0.1f;   // 换向杆空档死区
    public float gearDeadZone = 0.05f;       // 换挡杆空档死区
    public float forkDeadZone = 0.05f;       //操纵杆死区

    // 内部状态
    private bool engineRunning = false;
    public Gear currentGear = Gear.Neutral;
    public Direction currentDirection = Direction.Neutral;
    private float currentSpeed = 0f;         // 实际速度 (m/s)
    private float steer;


    private Rigidbody rb;
    private XRBaseInteractable gearInteractable;
    private XRBaseInteractable directionInteractable;
    private XRBaseInteractable liftInteractable;
    private XRBaseInteractable tiltInteractable;
    private XRBaseInteractable sideShiftInteractable;

    public enum Gear { Neutral, Gear1, Gear2, Gear3 }
    public enum Direction { Neutral, Forward, Reverse }
    private Quaternion[] initialWheelRotations;


    void Start()
    {
        initialWheelRotations = new Quaternion[steerWheels.Length];
        for (int i = 0; i < steerWheels.Length; i++)
        {
            if (steerWheels[i] != null)
                initialWheelRotations[i] = steerWheels[i].localRotation;
        }
        rb = GetComponent<Rigidbody>();
        gearInteractable = gearKnob.GetComponent<XRBaseInteractable>();
        directionInteractable = directionKnob.GetComponent<XRBaseInteractable>();

        // 监听钥匙按下事件
        ignitionKey.selectEntered.AddListener(OnIgnitionKeyPressed);
        if (liftKnob != null)
            liftInteractable = liftKnob.GetComponent<XRBaseInteractable>();
        if (tiltKnob != null)
            tiltInteractable = tiltKnob.GetComponent<XRBaseInteractable>();
        if (sideShiftKnob != null)
            sideShiftInteractable = sideShiftKnob.GetComponent<XRBaseInteractable>();
    }

    // ---------- 钥匙操作 ----------
    void OnIgnitionKeyPressed(SelectEnterEventArgs args)
    {
        if (!engineRunning)
        {
            engineRunning = true;
            Debug.Log("引擎已启动");
        }
        else
        {
            if (CanStopEngine())
            {
                engineRunning = false;
                currentSpeed = 0f;
                Debug.Log("引擎已关闭");
            }
            else
            {
                Debug.LogWarning("无法关闭：需速度为零、踩离合、两杆空档、拉紧手刹");
            }
        }
    }

    bool CanStopEngine()
    {
        return currentSpeed < stopThreshold &&
               clutchPedal.isSelected &&
               currentGear == Gear.Neutral &&
               currentDirection == Direction.Neutral &&
               IsHandbrakeEngaged();
    }

    // ---------- 控件状态读取 ----------
    bool IsHandbrakeEngaged() => handbrakeLever.value< handbrakeThreshold;
    bool IsClutchPressed() => clutchPedal.isSelected;
    bool IsBrakePressed() => brakePedal.isSelected;
    float ThrottleInput() => throttlePedal.isSelected ? 1f : 0f;

    // ---------- 档位/方向映射 ----------
    Gear GetGearFromValue(float val)
    {
        if (val < gearDeadZone)
        {
            Debug.Log("空");
            return Gear.Neutral;
        }
        if (val < 0.33f)return Gear.Gear1;   
        if (val < 0.66f) return Gear.Gear2;
        return Gear.Gear3;
    }

    Direction GetDirectionFromValue(float val)
    {
        if (val < 0.5f - directionDeadZone) return Direction.Reverse;
        if (val > 0.5f + directionDeadZone) return Direction.Forward;
        return Direction.Neutral;
    }

    // ---------- 移动条件 ----------
    bool CanMove()
    {
        return engineRunning &&
               !IsHandbrakeEngaged() &&
               !IsClutchPressed() &&
               currentGear != Gear.Neutral &&
               currentDirection != Direction.Neutral;
    }

    void Update()
    {
        // ★ 换挡/换向杆仅在速度为零且踩下离合时可操作
        bool allowGearChange = (currentSpeed < stopThreshold) && IsClutchPressed();
        gearInteractable.enabled = allowGearChange;
        directionInteractable.enabled = allowGearChange;

        if (allowGearChange)
        {
            currentGear = GetGearFromValue(gearKnob.value);
            currentDirection = GetDirectionFromValue(directionKnob.value);
        }
        // 若不允许操作，保持现有档位和方向不变

        // 轮胎视觉转向
        float steerInput = (steeringKnob.value - 0.5f) * 2f;   // [-1,1]
        float steerAngle = steerInput * maxSteerAngle;

        for (int i = 0; i < steerWheels.Length; i++)
        {
            if (steerWheels[i] != null)
            {
                steerWheels[i].localRotation = initialWheelRotations[i] * Quaternion.Euler(0, steerAngle, 0);
            }
        }

        bool forkControlActive = engineRunning;   // 钥匙启动后可用
        if (liftInteractable != null) liftInteractable.enabled = forkControlActive;
        if (tiltInteractable != null) tiltInteractable.enabled = forkControlActive;
        if (sideShiftInteractable != null) sideShiftInteractable.enabled = forkControlActive;

        if (forkControlActive)
        {
            // 上下移动 (Value: 0=最低, 1=最高)
            if (liftTarget != null && liftKnob != null)
            {
                float input = liftKnob.value - 0.5f;
                if (Mathf.Abs(input) > forkDeadZone)
                {
                    // 符号：正（>0.5）向下，负（<0.5）向上
                    float direction = Mathf.Sign(input); // 正值向下，负值向上
                    float speed = liftSpeed; // 可以乘以绝对值实现比例控制: speed * Mathf.Abs(input)*2
                    float move = direction * -speed * Time.deltaTime;
                    Vector3 pos = liftTarget.localPosition;
                    pos.y = Mathf.Clamp(pos.y + move, liftMinY, liftMaxY);
                    liftTarget.localPosition = pos;
                }
            }

            // 左右平移：Value > 0.5 向左（X减小），Value < 0.5 向右（X增大）
            if (sideShiftTarget != null && sideShiftKnob != null)
            {
                float input = sideShiftKnob.value - 0.5f;
                if (Mathf.Abs(input) > forkDeadZone)
                {
                    float direction = Mathf.Sign(input); // 正向左，负向右
                    float move = direction * -sideShiftSpeed * Time.deltaTime;
                    Vector3 pos = sideShiftTarget.localPosition;
                    pos.x = Mathf.Clamp(pos.x + move, sideShiftMinX, sideShiftMaxX);
                    sideShiftTarget.localPosition = pos;
                }
            }

            // 前后倾斜：Value > 0.5 后仰（角度减小），Value < 0.5 前倾（角度增加）
            if (tiltTarget != null && tiltKnob != null)
            {
                float input = tiltKnob.value - 0.5f;
                if (Mathf.Abs(input) > forkDeadZone)
                {
                    float direction = Mathf.Sign(input); // 正向后仰，负向前倾
                    float angleChange = direction * tiltSpeed * Time.deltaTime;
                    Vector3 euler = tiltTarget.localEulerAngles;
                    float currentAngle = (euler.x > 180) ? euler.x - 360 : euler.x;
                    float newAngle = Mathf.Clamp(currentAngle + angleChange, tiltMinAngle, tiltMaxAngle);
                    tiltTarget.localEulerAngles = new Vector3(newAngle, 0, 0);
                }
            }
        }
    }


    void FixedUpdate()
    {
        float targetSpeed = 0f;
        //Debug.Log($"[{Time.time}] Move: {CanMove()}, Speed: {currentSpeed:F2}, Engine: {engineRunning}, " +
        //  $"Handbrake: {IsHandbrakeEngaged()}, Clutch: {IsClutchPressed()}, Gear: {currentGear}, Dir: {currentDirection}");
        if (CanMove())
        {
            int gearIdx = (int)currentGear;
            float minSpd = gearMinSpeeds[gearIdx] / 3.6f;   // km/h → m/s
            float maxSpd = gearMaxSpeeds[gearIdx] / 3.6f;
            targetSpeed = Mathf.Lerp(minSpd, maxSpd, ThrottleInput());
        }

        // 手刹最强减速
        if (IsHandbrakeEngaged())
        {
            targetSpeed = 0f;
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, handbrakeDeceleration * Time.fixedDeltaTime);
        }
        // 脚刹
        else if (IsBrakePressed())
        {
            targetSpeed = 0f;
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, brakeDeceleration * Time.fixedDeltaTime);
        }
        else
        {
            // 正常加减速
            if (targetSpeed > currentSpeed)
                currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.fixedDeltaTime);
            else if (targetSpeed < currentSpeed)
                currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, deceleration * Time.fixedDeltaTime);
        }

        // 踩离合额外切断动力并自然减速 (与 CanMove 逻辑互补)
        if (IsClutchPressed() && !CanMove())
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, deceleration * Time.fixedDeltaTime);

        currentSpeed = Mathf.Max(currentSpeed, 0f);

        // 移动速度向量
        int dirSign = 0;
        if (currentDirection == Direction.Forward) dirSign = 1;
        else if (currentDirection == Direction.Reverse) dirSign = -1;

        Vector3 moveVel = transform.forward * dirSign * currentSpeed;
        rb.velocity = new Vector3(moveVel.x, rb.velocity.y, moveVel.z);

        // 转向 (车轮转角与速度方向关联，保证倒车时方向盘手感正确)
        if(currentDirection==Direction.Forward)
        {
            steer = (steeringKnob.value - 0.5f) * 2f;
        }
        else if(currentDirection == Direction.Reverse)
        {
            steer = -(steeringKnob.value - 0.5f) * 2f;
        }
        
        if (Mathf.Abs(currentSpeed) > 0.01f)
        {
            float angularVel = steer * turnRate * currentSpeed * dirSign;
            rb.angularVelocity = new Vector3(0, angularVel, 0);
        }
        else
        {
            rb.angularVelocity = Vector3.zero;
        }
        
    }
    public void Close()
    {
        liftKnob.enabled = false;    
        tiltKnob.enabled = false;         
   sideShiftKnob.enabled = false;
}
}

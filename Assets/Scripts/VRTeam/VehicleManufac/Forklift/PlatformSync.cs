using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class PlatformSync : MonoBehaviour
{
    [Header("同步目标")]
    public Transform xrOrigin;

    [Header("平台刚体")]
    public Rigidbody platformRigidbody;

    [Header("触发器")]
    public Collider platformTrigger; // 确保是 IsTrigger 的碰撞体
    public CharacterController playerCon;
    private bool isPlayerOnPlatform = false;
    private Vector3 lastPlatformPosition;
    private Quaternion lastPlatformRotation;
    [Header("射线")]
    public XRInteractorLineVisual rRayInteractor;
    public XRInteractorLineVisual lRayInteractor;
    private Gradient _invalidGrad;
    Gradient validGrad = new Gradient();
    GradientColorKey[] colorKeys = new GradientColorKey[]
    {
        new GradientColorKey(new Color(1,1,1,0), 0f),
        new GradientColorKey(new Color(1,1,1,0), 1f)
    };

    GradientAlphaKey[] alphaKeys = new GradientAlphaKey[]
    {
        new GradientAlphaKey(0f, 0f),
        new GradientAlphaKey(0f, 1f)
    };
    void Start()
    {
        if (platformRigidbody == null)
            platformRigidbody = GetComponent<Rigidbody>();
        if (platformTrigger == null)
            platformTrigger = GetComponent<Collider>();
        _invalidGrad = lRayInteractor.invalidColorGradient;
        validGrad.SetKeys(colorKeys, alphaKeys);
        // 初始记录
        lastPlatformPosition = platformRigidbody.position;
        lastPlatformRotation = platformRigidbody.rotation;
    }

    void LateUpdate()
    {
        // 始终更新记录，保证同步准确（即使玩家不在平台上，也更新坐标记录）
        Vector3 currentPos = platformRigidbody.position;
        Quaternion currentRot = platformRigidbody.rotation;

        if (isPlayerOnPlatform && xrOrigin != null)
        {
            // 计算帧间真实变化量
            Vector3 deltaPos = currentPos - lastPlatformPosition;
            Quaternion deltaRot = currentRot * Quaternion.Inverse(lastPlatformRotation);

            // 正确的位置：新平台位置 + 旋转后的相对偏移（不需要额外加 deltaPos！）
            Vector3 playerOffset = xrOrigin.position - lastPlatformPosition; // 上一帧玩家相对旧平台位置
            xrOrigin.position = currentPos + deltaRot * playerOffset;   // 新位置

            // 旋转同步：玩家朝向跟随平台旋转
            xrOrigin.rotation = deltaRot * xrOrigin.rotation;
        }

        // 更新记录为下一帧使用
        lastPlatformPosition = currentPos;
        lastPlatformRotation = currentRot;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerCon.enabled = false;
            isPlayerOnPlatform = true;
            // 进入瞬间，重新对齐记录，防止突变
            lastPlatformPosition = platformRigidbody.position;
            lastPlatformRotation = platformRigidbody.rotation;
            rRayInteractor.invalidColorGradient= validGrad;
            lRayInteractor.invalidColorGradient = validGrad;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            //Debug.Log(1);
            isPlayerOnPlatform = false;
            //playerCon.enabled = true;
            rRayInteractor.invalidColorGradient = _invalidGrad;
            lRayInteractor.invalidColorGradient = _invalidGrad;
        }
    }

    // 可选：在玩家进入平台时，禁用其物理模拟以防止冲突
    void SetPlayerPhysicsEnabled(bool enabled)
    {
        if (xrOrigin == null) return;
        Rigidbody playerRb = xrOrigin.GetComponent<Rigidbody>();
        if (playerRb != null)
            playerRb.isKinematic = !enabled;
        // 如果使用 CharacterController，可调整其 stepOffset 等
    }
}

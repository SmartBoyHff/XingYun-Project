using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// ============================================================
// 文件名：VR6_ExplosionEffect
// 模块：模块6 - 三体式飞行汽车展示
// 功能：三体式飞行汽车爆炸效果图
// 创建日期：2026-04-28
// 最后更新：2026-04-29
// ============================================================

namespace VRHelmet.VRTeam.Manufacturing.VehicleTest.ThreeBodyCar
{
    /// <summary>
    /// VR三体式飞行汽车爆炸效果控制器
    /// 
    /// 【功能说明】
    /// 1. 维护三个模块（DiPan、FeiXingQi、JiCang）的初始位置和旋转
    /// 2. 支持按下A按钮触发爆炸/吸附动画
    /// 3. 通过动画曲线控制三个模块的分解和还原过程
    /// 4. 支持运行时动态缓存初始姿态，确保可靠的还原效果
    /// 
    /// 【依赖组件】
    /// - InputActionReference (aBtn)：PICO手柄A按钮输入
    /// - Transform (DiPan, FeiXingQi, JiCang)：三个模块的变换
    /// - Transform (DiPanTarget, FeiXingQiTarget, JiCangTarget)：爆炸后的目标位置
    /// </summary>
    public class VR6_ExplosionEffect : MonoBehaviour
    {
        [SerializeField] private InputActionReference aBtn;

        [Header("三个模块")]
        [SerializeField] private Transform DiPan;
        [SerializeField] private Transform FeiXingQi;
        [SerializeField] private Transform JiCang;

        [Header("三个模块的爆炸目标位置")]
        [SerializeField] private Transform DiPanTarget;
        [SerializeField] private Transform FeiXingQiTarget;
        [SerializeField] private Transform JiCangTarget;

        [Header("移动参数")]
        [SerializeField] private float duration = 1f;
        [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        private Coroutine moveRoutine;

        private bool isExploded;
        private bool hasInitialPose;

        private Vector3 diPanInitialPos;
        private Vector3 feiXingQiInitialPos;
        private Vector3 jiCangInitialPos;

        private Quaternion diPanInitialRot;
        private Quaternion feiXingQiInitialRot;
        private Quaternion jiCangInitialRot;

        private void Awake()
        {
            CacheInitialPose();

            if (aBtn != null && aBtn.action != null)
            {
                aBtn.action.performed += OnABtnPressed;
            }
        }

        private void OnEnable()
        {
            aBtn?.action?.Enable();
        }

        private void OnDisable()
        {
            aBtn?.action?.Disable();
        }

        private void OnDestroy()
        {
            if (aBtn != null && aBtn.action != null)
            {
                aBtn.action.performed -= OnABtnPressed;
            }
        }

        private void OnABtnPressed(InputAction.CallbackContext context)
        {
            ToggleExplosion();
        }

        public void OnABtnPressed()
        {
            ToggleExplosion();
        }

        [ContextMenu("切换分解/吸附")]
        public void ToggleExplosion()
        {
            if (!CheckReferences())
            {
                return;
            }

            if (!hasInitialPose)
            {
                CacheInitialPose();
            }

            if (moveRoutine != null)
            {
                StopCoroutine(moveRoutine);
            }

            isExploded = !isExploded;

            moveRoutine = StartCoroutine(MoveRoutine(isExploded));
        }

        private void CacheInitialPose()
        {
            if (DiPan == null || FeiXingQi == null || JiCang == null)
            {
                return;
            }

            diPanInitialPos = DiPan.position;
            feiXingQiInitialPos = FeiXingQi.position;
            jiCangInitialPos = JiCang.position;

            diPanInitialRot = DiPan.rotation;
            feiXingQiInitialRot = FeiXingQi.rotation;
            jiCangInitialRot = JiCang.rotation;

            hasInitialPose = true;
        }

        private bool CheckReferences()
        {
            if (DiPan == null || FeiXingQi == null || JiCang == null)
            {
                Debug.LogError("请分配 DiPan、FeiXingQi、JiCang 三个模块");
                return false;
            }

            if (DiPanTarget == null || FeiXingQiTarget == null || JiCangTarget == null)
            {
                Debug.LogError("请分配 DiPanTarget、FeiXingQiTarget、JiCangTarget 三个目标位置");
                return false;
            }

            return true;
        }

        private IEnumerator MoveRoutine(bool moveToExplosion)
        {
            Transform[] modules =
            {
            DiPan,
            FeiXingQi,
            JiCang
        };

            Vector3[] startPos =
            {
            DiPan.position,
            FeiXingQi.position,
            JiCang.position
        };

            Quaternion[] startRot =
            {
            DiPan.rotation,
            FeiXingQi.rotation,
            JiCang.rotation
        };

            Vector3[] endPos;
            Quaternion[] endRot;

            if (moveToExplosion)
            {
                endPos = new[]
                {
                DiPanTarget.position,
                FeiXingQiTarget.position,
                JiCangTarget.position
            };

                endRot = new[]
                {
                DiPanTarget.rotation,
                FeiXingQiTarget.rotation,
                JiCangTarget.rotation
            };
            }
            else
            {
                endPos = new[]
                {
                diPanInitialPos,
                feiXingQiInitialPos,
                jiCangInitialPos
            };

                endRot = new[]
                {
                diPanInitialRot,
                feiXingQiInitialRot,
                jiCangInitialRot
            };
            }

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float curveT = moveCurve.Evaluate(t);

                for (int i = 0; i < modules.Length; i++)
                {
                    modules[i].position = Vector3.Lerp(startPos[i], endPos[i], curveT);
                    modules[i].rotation = Quaternion.Slerp(startRot[i], endRot[i], curveT);
                }

                yield return null;
            }

            for (int i = 0; i < modules.Length; i++)
            {
                modules[i].position = endPos[i];
                modules[i].rotation = endRot[i];
            }

            moveRoutine = null;
        }
    }
}
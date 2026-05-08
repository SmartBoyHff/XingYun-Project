using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

// ============================================================
// 文件名：VR4_TirePressureGauge
// 模块：模块4 - 维护保养
// 功能：胎压表交互脚本，负责随机胎压、释压/补气修正、答案记录和步骤完成。
// 创建日期：2026-05-5
// 最后更新：2026-05-5
// ============================================================

namespace VRHelmet.VRTeam.Maintenance
{
    /// <summary>
    /// ̥ѹ��ű�����ʱûע��
    /// </summary>
    /// <summary>
    /// VR4_TirePressureGauge 类型说明
    /// 
    /// 【功能说明】
    /// 1. 属于模块4 - 维护保养中的 VR4_TirePressureGauge 类型。
    /// 2. 负责处理随机胎压、释压补气、答案记录和步骤完成。
    /// 3. 与维护保养流程中的步骤数据、交互逻辑或 UI 显示协同工作。
    /// 
    /// 【依赖组件】
    /// - Unity 组件体系。
    /// - VRHelmet.VRTeam.Maintenance 维护保养流程脚本。
    /// </summary>
    public class VR4_TirePressureGauge : VR4_BaseObject
    {
        #region ==========Field==========
        [Header("Display")]
        public TextMeshProUGUI pressureText;

        [Header("Pressure Range")]
        public float minPressure = 2.7f;
        public float maxPressure = 3.1f;
        public float fixedNormalPressure = 2.9f;

        [Header("XR Interaction")]
        public XRGrabInteractable grabInteractable;
        public XRBaseInteractable randomPressurePoke;
        public XRBaseInteractable releasePressurePoke;
        public InputActionReference grabInputAction;

        [Header("Answer")]
        public int normalAnswerValue = 1;
        public int abnormalAnswerValue = 2;
        public int[] answerValues = new int[4];

        [Header("Topic Data Write Back")]
        public VR4_TopicManager topicManager;
        public int targetTopicTableIndex = 0;
        public int targetTopicDataStartIndex = 0;

        public bool logicEnabled = false;

        private const int AnswerCapacity = 4;
        private const float RandomMinPressure = 2.5f;
        private const float RandomMaxPressure = 3.5f;

        private float currentPressure;
        private bool hasPressure = false;
        private bool currentPressureResolved = false;
        private int answerIndex = 0;
        #endregion

        #region ==========Unity Method==========
        private void Reset()
        {
            grabInteractable = GetComponent<XRGrabInteractable>();
            pressureText = GetComponentInChildren<TextMeshProUGUI>(true);
        }

        private void OnValidate()
        {
            if (maxPressure < minPressure)
            {
                maxPressure = minPressure;
            }

            EnsureAnswerArray();
        }

        private void Awake()
        {
            EnsureAnswerArray();

            if (grabInteractable == null)
            {
                grabInteractable = GetComponent<XRGrabInteractable>();
            }
        }

        private void OnEnable()
        {
            if (grabInteractable != null)
            {
                grabInteractable.selectEntered.AddListener(OnGaugeSelected);
            }

            if (randomPressurePoke != null)
            {
                randomPressurePoke.selectEntered.AddListener(OnRandomPressurePoked);
            }

            if (releasePressurePoke != null)
            {
                releasePressurePoke.selectEntered.AddListener(OnReleasePressurePoked);
            }

            if (grabInputAction != null && grabInputAction.action != null)
            {
                grabInputAction.action.performed += OnGrabInputPerformed;
                grabInputAction.action.Enable();
            }
        }

        private void OnDisable()
        {
            if (grabInteractable != null)
            {
                grabInteractable.selectEntered.RemoveListener(OnGaugeSelected);
            }

            if (randomPressurePoke != null)
            {
                randomPressurePoke.selectEntered.RemoveListener(OnRandomPressurePoked);
            }

            if (releasePressurePoke != null)
            {
                releasePressurePoke.selectEntered.RemoveListener(OnReleasePressurePoked);
            }

            if (grabInputAction != null && grabInputAction.action != null)
            {
                grabInputAction.action.performed -= OnGrabInputPerformed;
            }
        }
        #endregion

        #region ==========Logic==========
        private void OnGaugeSelected(SelectEnterEventArgs args)
        {
            logicEnabled = true;
        }

        private void OnRandomPressurePoked(SelectEnterEventArgs args)
        {
            if (!logicEnabled)
            {
                return;
            }

            currentPressure = UnityEngine.Random.Range(RandomMinPressure, RandomMaxPressure);
            hasPressure = true;
            currentPressureResolved = false;

            RefreshPressureText();

            EvaluateAnswerAfterRandomPressure();
            if (IsPressureReasonable())
            {
                CompleteCurrentPressureStep();
            }
        }

        private void OnReleasePressurePoked(SelectEnterEventArgs args)
        {
            if (!logicEnabled || !hasPressure)
            {
                return;
            }

            if (currentPressure <= maxPressure)
            {
                return;
            }

            SetPressureToNormal();
            CompleteCurrentPressureStep();
        }

        private void OnGrabInputPerformed(InputAction.CallbackContext context)
        {
            if (!logicEnabled || !hasPressure)
            {
                return;
            }

            if (currentPressure >= minPressure)
            {
                return;
            }

            SetPressureToNormal();
            CompleteCurrentPressureStep();
        }

        private bool IsPressureReasonable()
        {
            return currentPressure >= minPressure && currentPressure <= maxPressure;
        }

        private void EvaluateAnswerAfterRandomPressure()
        {
            StoreAnswerValue(IsPressureReasonable() ? normalAnswerValue : abnormalAnswerValue);
        }

        private void SetPressureToNormal()
        {
            currentPressure = fixedNormalPressure;
            RefreshPressureText();
        }

        private void RefreshPressureText()
        {
            if (pressureText != null)
            {
                pressureText.text = $"{currentPressure:0.00}bar";
            }
        }

        private void CompleteCurrentPressureStep()
        {
            if (currentPressureResolved)
            {
                return;
            }

            currentPressureResolved = true;
            CompleteStep();
        }

        private void StoreAnswerValue(int value)
        {
            EnsureAnswerArray();

            if (answerIndex >= answerValues.Length)
            {
                return;
            }

            answerValues[answerIndex] = value;
            answerIndex++;

            if (answerIndex >= answerValues.Length)
            {
                WriteAnswersToTopicData();
            }
        }

        private void WriteAnswersToTopicData()
        {
            if (topicManager == null || topicManager.topicTables == null)
            {
                return;
            }

            if (targetTopicTableIndex < 0 || targetTopicTableIndex >= topicManager.topicTables.Count)
            {
                return;
            }

            TopicTable table = topicManager.topicTables[targetTopicTableIndex];
            if (table == null || table.topicDatas == null)
            {
                return;
            }

            for (int i = 0; i < answerValues.Length; i++)
            {
                int topicDataIndex = targetTopicDataStartIndex + i;
                if (topicDataIndex < 0 || topicDataIndex >= table.topicDatas.Count)
                {
                    return;
                }

                table.topicDatas[topicDataIndex].choiceAnswer = answerValues[i];
            }
        }

        private void EnsureAnswerArray()
        {
            if (answerValues == null)
            {
                answerValues = new int[AnswerCapacity];
            }

            if (answerValues.Length != AnswerCapacity)
            {
                Array.Resize(ref answerValues, AnswerCapacity);
            }
        }
        #endregion

        #region ==========API==========
        public void ResetGauge()
        {
            logicEnabled = false;
            ResetStepCompletion();
            hasPressure = false;
            currentPressureResolved = false;
            currentPressure = 0f;
            answerIndex = 0;

            EnsureAnswerArray();
            Array.Clear(answerValues, 0, answerValues.Length);

            if (pressureText != null)
            {
                pressureText.text = string.Empty;
            }
        }
        #endregion
    }
}
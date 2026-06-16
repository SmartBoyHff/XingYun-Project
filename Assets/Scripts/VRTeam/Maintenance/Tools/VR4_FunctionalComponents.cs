using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;

// ============================================================
// 文件名：VR_FunctionalComponents
// 模块：模块4 - 维护保养
// 功能：交互物体通用功能组件，负责高光、刚体状态和工具类物体复位。
// 创建日期：2026-05-5
// 最后更新：2026-05-5
// ============================================================

namespace VRHelmet.VRTeam.Maintenance
{
    /// <summary>
    /// IHighlightable 类型说明
    /// 
    /// 【功能说明】
    /// 1. 属于模块4 - 维护保养中的 IHighlightable 类型。
    /// 2. 负责提供高光、刚体状态处理和工具复位。
    /// 3. 与维护保养流程中的步骤数据、交互逻辑或 UI 显示协同工作。
    /// 
    /// 【依赖组件】
    /// - Unity 组件体系。
    /// - VRHelmet.VRTeam.Maintenance 维护保养流程脚本。
    /// </summary>
    public interface IHighlightable
    {
        #region ==========API==========
        /// <summary>
        /// 显示物体高光。
        /// </summary>
        void ShowHighlight();

        /// <summary>
        /// 隐藏物体高光。
        /// </summary>
        void HideHighlight();
        #endregion
    }

    /// <summary>
    /// IResettableInteractable 类型说明
    /// 
    /// 【功能说明】
    /// 1. 属于模块4 - 维护保养中的 IResettableInteractable 类型。
    /// 2. 负责提供高光、刚体状态处理和工具复位。
    /// 3. 与维护保养流程中的步骤数据、交互逻辑或 UI 显示协同工作。
    /// 
    /// 【依赖组件】
    /// - Unity 组件体系。
    /// - VRHelmet.VRTeam.Maintenance 维护保养流程脚本。
    /// </summary>
    public interface IResettableInteractable
    {
        #region ==========API==========
        /// <summary>
        /// 将交互物体恢复到初始状态。
        /// </summary>
        void ResetObject();
        #endregion
    }

    internal static class VR4_HighlightMaterialProvider
    {
        private const string HighlightShaderName = "Custom/Highlight_Shader";
        private static Material sharedMaterial;
        private static bool missingShaderLogged;

        public static Material SharedMaterial
        {
            get
            {
                EnsureMaterial();
                return sharedMaterial;
            }
        }

        public static string DiagnosticMessage =>
            $"未找到高光 Shader：{HighlightShaderName}。请确认 Highlight_Shader.shader 存在，且构建时没有被剔除。";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeState()
        {
            sharedMaterial = null;
            missingShaderLogged = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Prewarm()
        {
            EnsureMaterial();
        }

        private static void EnsureMaterial()
        {
            if (sharedMaterial != null)
            {
                return;
            }

            Shader highlightShader = Shader.Find(HighlightShaderName);
            if (highlightShader == null)
            {
                if (!missingShaderLogged)
                {
                    missingShaderLogged = true;
                    Debug.LogWarning($"[VR4Highlight] {DiagnosticMessage}");
                }

                return;
            }

            sharedMaterial = new Material(highlightShader)
            {
                name = "VR4_Runtime_Highlight_Mat",
                hideFlags = HideFlags.HideAndDontSave,
                enableInstancing = true
            };
        }
    }

    /// <summary>
    /// 交互物体通用功能组件。
    /// 负责高光显示、释放后的刚体状态处理，以及工具类物体的初始位置恢复。
    /// </summary>
    /// <summary>
    /// VR_FunctionalComponents 类型说明
    /// 
    /// 【功能说明】
    /// 1. 属于模块4 - 维护保养中的 VR_FunctionalComponents 类型。
    /// 2. 负责提供高光、刚体状态处理和工具复位。
    /// 3. 与维护保养流程中的步骤数据、交互逻辑或 UI 显示协同工作。
    /// 
    /// 【依赖组件】
    /// - Unity 组件体系。
    /// - VRHelmet.VRTeam.Maintenance 维护保养流程脚本。
    /// </summary>
    [RequireComponent(typeof(XRBaseInteractable))]
    public class VR4_FunctionalComponents : MonoBehaviour, IHighlightable, IResettableInteractable, IXRHoverFilter, IXRSelectFilter
    {
        #region ==========Field==========
        /// <summary>
        /// 悬停时显示的标签物体。
        /// </summary>
        public GameObject label;

        /// <summary>
        /// 当前物体的刚体组件，用于控制运动学状态。
        /// </summary>
        public Rigidbody rig;

        /// <summary>
        /// 是否为工具类物体。为 true 时释放后自动恢复到初始位置和旋转。
        /// </summary>
        [SerializeField] private bool isTools = false;

        [Header("Interaction Permission")]
        [SerializeField] private VR4InteractionLayer interactionLayer = VR4InteractionLayer.Default;
        [SerializeField] private bool enablePermissionLogs = true;

        /// <summary>
        /// 需要显示轮廓高光的渲染器列表。
        /// </summary>
        [SerializeField] private Renderer[] renderers;

        /// <summary>
        /// 运行时共享高光材质，由 VR4_HighlightMaterialProvider 自动创建。
        /// </summary>
        private Material gaoguangMaterial;

        private readonly Dictionary<Renderer, GameObject> outlineObjects = new Dictionary<Renderer, GameObject>();
        private bool outlineObjectsCreated = false;
        private XRGrabInteractable grabInteractable;
        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private bool isStepRigidbodyActive;
        private Coroutine releaseCoroutine;
        private XRBaseInteractable baseInteractable;
        private XRBaseInteractor activeLeftInteractor;
        private XRBaseInteractor activeRightInteractor;
        private XRBaseInteractor activeLeftRayInteractor;
        private XRBaseInteractor activeRightRayInteractor;
        private VR4InteractionLayer activeRightLayerMask = VR4InteractionLayer.Nothing;
        private VR4InteractionLayer activeLeftLayerMask = VR4InteractionLayer.Nothing;
        private bool activeTwoHandsMode;
        private bool hasStepInteractionPermission;
        private float lastPermissionLogTime = -10f;

        public bool canProcess => isActiveAndEnabled;
        #endregion

        #region ==========Unity Method==========
        private void Awake()
        {
            baseInteractable = GetComponent<XRBaseInteractable>();
            CacheInitialTransform();
            LoadHighlightMaterial();
            CreateOutlineObjects();
        }

        private void Start()
        {
            grabInteractable = GetComponent<XRGrabInteractable>();
            if (baseInteractable == null)
            {
                baseInteractable = GetComponent<XRBaseInteractable>();
            }
            rig = GetComponent<Rigidbody>();

            if (rig != null)
            {
                SetRigidbodyKinematic(true);
            }

            BindGrabEvents();
        }

        private void OnDestroy()
        {
            UnbindGrabEvents();
            StopReleaseCoroutine();
            DestroyOutlineObjects();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Ground"))
            {
                ResetObject();
            }
        }
        #endregion

        #region ==========Logic==========
        #region ----------Interactable Event----------
        private void BindGrabEvents()
        {
            if (baseInteractable == null)
            {
                return;
            }

            UnbindGrabEvents();
            baseInteractable.hoverFilters.Add(this);
            baseInteractable.selectFilters.Add(this);
            baseInteractable.hoverEntered.AddListener(OnHoverStart);
            baseInteractable.hoverExited.AddListener(OnHoverEnd);
            baseInteractable.selectEntered.AddListener(OnSelectStart);
            baseInteractable.selectExited.AddListener(OnSelectEnd);
        }

        private void UnbindGrabEvents()
        {
            if (baseInteractable == null)
            {
                return;
            }

            baseInteractable.hoverFilters.Remove(this);
            baseInteractable.selectFilters.Remove(this);
            baseInteractable.hoverEntered.RemoveListener(OnHoverStart);
            baseInteractable.hoverExited.RemoveListener(OnHoverEnd);
            baseInteractable.selectEntered.RemoveListener(OnSelectStart);
            baseInteractable.selectExited.RemoveListener(OnSelectEnd);
        }

        private void OnHoverStart(HoverEnterEventArgs args)
        {
            if (!CanInteract(args.interactorObject, "Hover"))
            {
                return;
            }

            if (label != null)
            {
                label.SetActive(true);
            }

            if (CanShowHighlightForInteractor(args.interactorObject as XRBaseInteractor))
            {
                AddGaoguang();
            }
        }

        private void OnHoverEnd(HoverExitEventArgs args)
        {
            if (label != null)
            {
                label.SetActive(false);
            }

            CloseGaoguang();
        }

        private void OnSelectStart(SelectEnterEventArgs args)
        {
            if (!CanInteract(args.interactorObject, "Select"))
            {
                return;
            }

            if (label != null)
            {
                label.SetActive(false);
            }

            if (isStepRigidbodyActive && ShouldEnablePhysicsForSelect(args.interactorObject))
            {
                SetRigidbodyKinematic(false);
            }
        }

        private void OnSelectEnd(SelectExitEventArgs args)
        {
            if (isStepRigidbodyActive && ShouldEnablePhysicsForSelect(args.interactorObject))
            {
                SetRigidbodyKinematic(false);
            }

            if (isTools)
            {
                ResetObject();
            }

            CloseGaoguang();
        }

        private bool CanInteract(IXRHoverInteractor interactor, string actionName)
        {
            return CanInteractInternal(interactor as XRBaseInteractor, actionName);
        }

        private bool CanInteract(IXRSelectInteractor interactor, string actionName)
        {
            return CanInteractInternal(interactor as XRBaseInteractor, actionName);
        }

        private bool CanInteractInternal(XRBaseInteractor interactor, string actionName)
        {
            if (!hasStepInteractionPermission)
            {
                return true;
            }

            VR4InteractionLayer allowedMask = GetAllowedMask(interactor);
            bool allowed = (interactionLayer & allowedMask) != 0;
            if (!allowed)
            {
                LogPermissionDenied(interactor, actionName, allowedMask);
            }

            return allowed;
        }

        private VR4InteractionLayer GetAllowedMask(XRBaseInteractor interactor)
        {
            if (!activeTwoHandsMode)
            {
                return activeRightLayerMask;
            }

            if (interactor != null && (interactor == activeLeftInteractor || interactor == activeLeftRayInteractor))
            {
                return activeLeftLayerMask;
            }

            return activeRightLayerMask;
        }

        private bool CanShowHighlightForInteractor(XRBaseInteractor interactor)
        {
            if (!hasStepInteractionPermission || interactor == null)
            {
                return false;
            }

            if (interactor == activeLeftRayInteractor)
            {
                return IsExactHighlightLayer(activeLeftLayerMask);
            }

            if (interactor == activeRightRayInteractor)
            {
                return IsExactHighlightLayer(activeRightLayerMask);
            }

            return false;
        }

        private bool CanShowHighlightForCurrentRaySettings()
        {
            if (!hasStepInteractionPermission)
            {
                return false;
            }

            return IsExactHighlightLayer(activeRightLayerMask) ||
                   (activeTwoHandsMode && IsExactHighlightLayer(activeLeftLayerMask));
        }

        private bool IsExactHighlightLayer(VR4InteractionLayer rayLayerMask)
        {
            return rayLayerMask == interactionLayer;
        }

        private void LogPermissionDenied(XRBaseInteractor interactor, string actionName, VR4InteractionLayer allowedMask)
        {
            if (!enablePermissionLogs || Time.time - lastPermissionLogTime < 0.5f)
            {
                return;
            }

            lastPermissionLogTime = Time.time;
            string interactorName = interactor != null ? interactor.name : "UnknownInteractor";
            Debug.LogWarning($"[VR4Permission] Denied {actionName}. Object={name}, ObjectLayer={interactionLayer}, Interactor={interactorName}, AllowedMask={allowedMask}");
        }

        private void SetRigidbodyKinematic(bool isKinematic)
        {
            if (rig == null)
            {
                return;
            }

            if (!rig.isKinematic)
            {
                ClearRigidbodyVelocity();
            }

            rig.isKinematic = isKinematic;
            rig.useGravity = !isKinematic;

            if (!rig.isKinematic)
            {
                ClearRigidbodyVelocity();
            }
        }

        private void ClearRigidbodyVelocity()
        {
            if (rig == null)
            {
                return;
            }

            rig.velocity = Vector3.zero;
            rig.angularVelocity = Vector3.zero;
        }

        private bool ShouldEnablePhysicsForSelect(IXRSelectInteractor interactor)
        {
            return interactor is XRDirectInteractor;
        }
        #endregion

        #region ----------Highlight----------
        private void LoadHighlightMaterial()
        {
            gaoguangMaterial = VR4_HighlightMaterialProvider.SharedMaterial;
        }

        private void CreateOutlineObjects()
        {
            if (outlineObjectsCreated)
            {
                return;
            }

            if (renderers == null || renderers.Length == 0)
            {
                Debug.LogWarning($"{name} 未配置高光 Renderers 列表");
                return;
            }

            foreach (Renderer sourceRenderer in renderers)
            {
                if (sourceRenderer == null)
                {
                    continue;
                }

                GameObject outlineObject = CreateOutlineObject(sourceRenderer);
                if (outlineObject == null)
                {
                    continue;
                }

                outlineObjects.Add(sourceRenderer, outlineObject);
                outlineObject.SetActive(false);
            }

            outlineObjectsCreated = true;
        }

        private GameObject CreateOutlineObject(Renderer sourceRenderer)
        {
            if (gaoguangMaterial == null)
            {
                Debug.LogWarning($"{name} {VR4_HighlightMaterialProvider.DiagnosticMessage}");
                return null;
            }

            GameObject outlineObject = new GameObject($"{sourceRenderer.name}_Outline");
            outlineObject.transform.SetParent(sourceRenderer.transform, false);
            outlineObject.transform.localPosition = Vector3.zero;
            outlineObject.transform.localRotation = Quaternion.identity;
            outlineObject.transform.localScale = Vector3.one;

            if (sourceRenderer is SkinnedMeshRenderer sourceSkinnedRenderer)
            {
                SkinnedMeshRenderer outlineRenderer = outlineObject.AddComponent<SkinnedMeshRenderer>();
                outlineRenderer.sharedMesh = sourceSkinnedRenderer.sharedMesh;
                outlineRenderer.rootBone = sourceSkinnedRenderer.rootBone;
                outlineRenderer.bones = sourceSkinnedRenderer.bones;
                outlineRenderer.localBounds = sourceSkinnedRenderer.localBounds;
                outlineRenderer.updateWhenOffscreen = sourceSkinnedRenderer.updateWhenOffscreen;
                ConfigureOutlineRenderer(outlineRenderer, GetSubMeshCount(sourceSkinnedRenderer.sharedMesh));
                return outlineObject;
            }

            if (sourceRenderer is MeshRenderer)
            {
                MeshFilter sourceMeshFilter = sourceRenderer.GetComponent<MeshFilter>();
                if (sourceMeshFilter == null || sourceMeshFilter.sharedMesh == null)
                {
                    Destroy(outlineObject);
                    return null;
                }

                MeshFilter outlineMeshFilter = outlineObject.AddComponent<MeshFilter>();
                outlineMeshFilter.sharedMesh = sourceMeshFilter.sharedMesh;

                MeshRenderer outlineRenderer = outlineObject.AddComponent<MeshRenderer>();
                ConfigureOutlineRenderer(outlineRenderer, GetSubMeshCount(sourceMeshFilter.sharedMesh));
                return outlineObject;
            }

            Destroy(outlineObject);
            return null;
        }

        private int GetSubMeshCount(Mesh mesh)
        {
            return mesh != null ? Mathf.Max(1, mesh.subMeshCount) : 1;
        }

        private void ConfigureOutlineRenderer(Renderer outlineRenderer, int materialCount)
        {
            outlineRenderer.sharedMaterials = BuildOutlineMaterials(materialCount);
            outlineRenderer.shadowCastingMode = ShadowCastingMode.Off;
            outlineRenderer.receiveShadows = false;
            outlineRenderer.lightProbeUsage = LightProbeUsage.Off;
            outlineRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            outlineRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            outlineRenderer.allowOcclusionWhenDynamic = false;
        }

        private Material[] BuildOutlineMaterials(int materialCount)
        {
            Material[] materials = new Material[materialCount];
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = gaoguangMaterial;
            }

            return materials;
        }

        private void SetOutlineObjectsActive(bool active)
        {
            CreateOutlineObjects();

            foreach (GameObject outlineObject in outlineObjects.Values)
            {
                if (outlineObject != null)
                {
                    outlineObject.SetActive(active);
                }
            }
        }

        private void DestroyOutlineObjects()
        {
            foreach (GameObject outlineObject in outlineObjects.Values)
            {
                if (outlineObject != null)
                {
                    Destroy(outlineObject);
                }
            }

            outlineObjects.Clear();
            outlineObjectsCreated = false;
        }
        #endregion

        #region ----------Restore Transform----------
        private void CacheInitialTransform()
        {
            initialPosition = transform.position;
            initialRotation = transform.rotation;
        }

        private IEnumerator ReleaseFromInteractors()
        {
            XRBaseInteractable interactable = GetComponent<XRBaseInteractable>();
            if (interactable == null)
            {
                yield break;
            }

            List<IXRSelectInteractor> interactorsList = new List<IXRSelectInteractor>(interactable.interactorsSelecting);
            foreach (IXRSelectInteractor interactor in interactorsList)
            {
                if (!(interactor is XRBaseInteractor baseInteractor))
                {
                    continue;
                }

                XRInteractionManager interactionManager = baseInteractor.interactionManager;
                if (interactionManager == null)
                {
                    continue;
                }

                if (baseInteractor is XRSocketInteractor socketInteractor)
                {
                    socketInteractor.socketActive = false;
                    interactionManager.SelectExit(interactor, interactable);
                    yield return new WaitForSeconds(0.5f);
                    socketInteractor.socketActive = true;
                }
                else
                {
                    interactionManager.SelectExit(interactor, interactable);
                }
            }

            releaseCoroutine = null;
        }

        private void StopReleaseCoroutine()
        {
            if (releaseCoroutine == null)
            {
                return;
            }

            StopCoroutine(releaseCoroutine);
            releaseCoroutine = null;
        }
        #endregion
        #endregion

        #region ==========API==========
        /// <summary>
        /// 显示 Renderers 列表对应的轮廓高光副 Renderer。
        /// </summary>
        public bool Process(IXRHoverInteractor interactor, IXRHoverInteractable interactable)
        {
            return CanInteract(interactor, "HoverFilter");
        }

        public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
        {
            return CanInteract(interactor, "SelectFilter");
        }

        public bool IsInteractorAllowed(IXRInteractor interactor, string actionName)
        {
            return CanInteractInternal(interactor as XRBaseInteractor, actionName);
        }

        public void ConfigureStepInteractionPermission(
            VR4InteractionLayer rightLayerMask,
            VR4InteractionLayer leftLayerMask,
            bool twoHandsMode,
            XRBaseInteractor leftInteractor,
            XRBaseInteractor rightInteractor,
            XRBaseInteractor leftRayInteractor,
            XRBaseInteractor rayInteractor)
        {
            activeRightLayerMask = NormalizeLayerMask(rightLayerMask);
            activeLeftLayerMask = NormalizeLayerMask(leftLayerMask);
            activeTwoHandsMode = twoHandsMode;
            activeLeftInteractor = leftInteractor;
            activeRightInteractor = rightInteractor;
            activeLeftRayInteractor = leftRayInteractor;
            activeRightRayInteractor = rayInteractor;
            hasStepInteractionPermission = true;
        }

        public void ClearStepInteractionPermission()
        {
            hasStepInteractionPermission = false;
            activeRightLayerMask = VR4InteractionLayer.Nothing;
            activeLeftLayerMask = VR4InteractionLayer.Nothing;
            activeTwoHandsMode = false;
            activeLeftInteractor = null;
            activeRightInteractor = null;
            activeLeftRayInteractor = null;
            activeRightRayInteractor = null;
        }

        public bool IsAllowedByCurrentStep()
        {
            if (!hasStepInteractionPermission)
            {
                return true;
            }

            return (interactionLayer & activeRightLayerMask) != 0 || (activeTwoHandsMode && (interactionLayer & activeLeftLayerMask) != 0);
        }

        private VR4InteractionLayer NormalizeLayerMask(VR4InteractionLayer layerMask)
        {
            return layerMask;
        }

        public void AddGaoguang()
        {
            SetOutlineObjectsActive(true);
        }

        /// <summary>
        /// 隐藏 Renderers 列表对应的轮廓高光副 Renderer。
        /// </summary>
        public void CloseGaoguang()
        {
            SetOutlineObjectsActive(false);
        }

        /// <summary>
        /// 显示物体高光，供 IHighlightable 调用。
        /// </summary>
        public void ShowHighlight()
        {
            if (CanShowHighlightForCurrentRaySettings())
            {
                AddGaoguang();
            }
            else
            {
                CloseGaoguang();
            }
        }

        /// <summary>
        /// 隐藏物体高光，供 IHighlightable 调用。
        /// </summary>
        public void HideHighlight()
        {
            CloseGaoguang();
        }

        /// <summary>
        /// 步骤开始时进入刚体管理状态，但保持无重力，等待玩家真正抓取后再启用物理。
        /// </summary>
        public void ActivateStepRigidbody()
        {
            isStepRigidbodyActive = true;
            SetRigidbodyKinematic(true);
        }

        /// <summary>
        /// 步骤结束时关闭当前物体的物理运动并禁用重力。
        /// </summary>
        public void DeactivateStepRigidbody()
        {
            isStepRigidbodyActive = false;
            SetRigidbodyKinematic(true);
        }

        /// <summary>
        /// 释放当前交互器，并将物体恢复到初始位置和初始旋转。
        /// </summary>
        public void ResetObject()
        {
            StopReleaseCoroutine();
            releaseCoroutine = StartCoroutine(ReleaseFromInteractors());
            transform.SetPositionAndRotation(initialPosition, initialRotation);
            SetRigidbodyKinematic(true);
        }
        #endregion
    }
}

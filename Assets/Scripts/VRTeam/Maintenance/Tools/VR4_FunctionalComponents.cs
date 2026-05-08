using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit;

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
    public class VR4_FunctionalComponents : MonoBehaviour, IHighlightable, IResettableInteractable
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

        /// <summary>
        /// 需要显示轮廓高光的渲染器列表。
        /// </summary>
        [SerializeField] private Renderer[] renderers;

        /// <summary>
        /// 用于轮廓副 Renderer 的高光材质。为空时会从 Resources/Material/Highlight_Mat 加载。
        /// </summary>
        [SerializeField] private Material gaoguangMaterial;

        private readonly Dictionary<Renderer, GameObject> outlineObjects = new Dictionary<Renderer, GameObject>();
        private bool outlineObjectsCreated = false;
        private XRGrabInteractable grabInteractable;
        private Vector3 initialPosition;
        private Quaternion initialRotation;
        #endregion

        #region ==========Unity Method==========
        private void Awake()
        {
            CacheInitialTransform();
            LoadHighlightMaterial();
            CreateOutlineObjects();
        }

        private void Start()
        {
            grabInteractable = GetComponent<XRGrabInteractable>();
            rig = GetComponent<Rigidbody>();

            if (rig != null)
            {
                rig.useGravity = false;
                rig.isKinematic = true;
            }

            BindGrabEvents();
        }

        private void OnDestroy()
        {
            UnbindGrabEvents();
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
            if (grabInteractable == null)
            {
                return;
            }

            grabInteractable.hoverEntered.AddListener(OnHoverStart);
            grabInteractable.hoverExited.AddListener(OnHoverEnd);
            grabInteractable.selectEntered.AddListener(OnSelectStart);
            grabInteractable.selectExited.AddListener(OnSelectEnd);
        }

        private void UnbindGrabEvents()
        {
            if (grabInteractable == null)
            {
                return;
            }

            grabInteractable.hoverEntered.RemoveListener(OnHoverStart);
            grabInteractable.hoverExited.RemoveListener(OnHoverEnd);
            grabInteractable.selectEntered.RemoveListener(OnSelectStart);
            grabInteractable.selectExited.RemoveListener(OnSelectEnd);
        }

        private void OnHoverStart(HoverEnterEventArgs args)
        {
            if (label != null)
            {
                label.SetActive(true);
            }

            AddGaoguang();
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
            if (label != null)
            {
                label.SetActive(false);
            }
        }

        private void OnSelectEnd(SelectExitEventArgs args)
        {
            if (rig != null && rig.isKinematic)
            {
                SetRigidbodyKinematic(false);
            }

            if (isTools)
            {
                ResetObject();
            }

            CloseGaoguang();
        }

        private void SetRigidbodyKinematic(bool isKinematic)
        {
            if (rig == null)
            {
                return;
            }

            rig.isKinematic = isKinematic;
            rig.useGravity = !isKinematic;
        }
        #endregion

        #region ----------Highlight----------
        private void LoadHighlightMaterial()
        {
            if (gaoguangMaterial == null)
            {
                gaoguangMaterial = Resources.Load<Material>("Material/Highlight_Mat");
            }
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
                Debug.LogWarning($"{name} 未找到高光材质 Resources/Material/Highlight_Mat");
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
        }
        #endregion
        #endregion

        #region ==========API==========
        /// <summary>
        /// 显示 Renderers 列表对应的轮廓高光副 Renderer。
        /// </summary>
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
            AddGaoguang();
        }

        /// <summary>
        /// 隐藏物体高光，供 IHighlightable 调用。
        /// </summary>
        public void HideHighlight()
        {
            CloseGaoguang();
        }

        /// <summary>
        /// 释放当前交互器，并将物体恢复到初始位置和初始旋转。
        /// </summary>
        public void ResetObject()
        {
            StartCoroutine(ReleaseFromInteractors());
            transform.SetPositionAndRotation(initialPosition, initialRotation);
            SetRigidbodyKinematic(true);
        }
        #endregion
    }
}

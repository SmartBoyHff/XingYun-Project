using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace VR.Tool
{
    /// <summary>
    /// 功能组件
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public class FunctionalComponents : MonoBehaviour
    {
        public GameObject label;

        Camera mainCamera;

        public Rigidbody rig;

        [SerializeField] bool isTools = false;

        [SerializeField] Renderer[] renderers;

        private void Awake()
        {
            mainCamera = Camera.main;

            GetStartTransform();
          renderers = GetComponentsInChildren<Renderer>(true);

            gaoguangMaterial = Resources.Load<Material>("Material/Highlight_Mat");
        }

        private void Start()
        {
            XRGrabInteractable grabInteractable = GetComponent<XRGrabInteractable>();
            rig = GetComponent<Rigidbody>();

            if (grabInteractable != null)
            {
                if (rig != null)
                {
                    rig.useGravity = false;
                    rig.isKinematic = true;
                }

                grabInteractable.hoverEntered.AddListener(args => OnHoverStart());
                grabInteractable.hoverExited.AddListener(args => OnHoverEnd());
                grabInteractable.selectEntered.AddListener(args => OnSelectSatrt());
                grabInteractable.selectExited.AddListener(args => OnSelectEnd());
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Ground"))
            {
                ResetObject();
            }
        }

        #region InteractableEvent
        void OnHoverStart()
        {
            if (label != null)
                label.SetActive(true);

            AddGaoguang();
        }

        void OnHoverEnd()
        {
            if (label != null)
                label.SetActive(false);

            CloseGaoguang();
        }

        void OnSelectSatrt()
        {
            if (label != null)
                label.SetActive(false);
        }

        private void OnSelectEnd()
        {
            if (rig.isKinematic)
            {
                ControlRig(false);
            }

            if (isTools)
            {
                ResetObject();
            }

            CloseGaoguang();
        }

        


        void ControlRig(bool isKinemetic)
        {
            rig.isKinematic = isKinemetic;
            rig.useGravity = !isKinemetic;
        }
        #endregion

        #region gaoguang
        [SerializeField] private Material gaoguangMaterial;

        // 存储原始材质的字典（Renderer作为键，原始材质数组作为值）
        private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
        // 是否已保存原始材质
        private bool materialsSaved = false;

        void SaveOriginalMaterials()
        {
            if (materialsSaved)
                return;

            if (renderers == null)
                renderers = GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in renderers)
            {
                // 深度复制材质数组
                Material[] materialsCopy = new Material[renderer.sharedMaterials.Length];
                renderer.sharedMaterials.CopyTo(materialsCopy, 0);

                originalMaterials.Add(renderer, materialsCopy);
            }

            materialsSaved = true;
        }

        void SwitchMaterial(Material newMaterial)
        {
            // 如果没有保存过原始材质，先保存
            if (!materialsSaved)
            {
                SaveOriginalMaterials();
            }

            if (newMaterial == null)
            {
                return;
            }

            foreach (Renderer renderer in renderers)
            {
                // 获取原始材质数组
                Material[] originalMaterials = renderer.sharedMaterials;

                // 创建长度+1的新数组
                Material[] newMaterials = new Material[originalMaterials.Length + 1];

                // 复制原有材质
                for (int i = 0; i < originalMaterials.Length; i++)
                {
                    newMaterials[i] = originalMaterials[i];
                }

                // 添加新材质到末尾
                newMaterials[originalMaterials.Length] = newMaterial;

                // 应用新材质数组
                renderer.materials = newMaterials;
            }
        }

        void RestoreOriginalMaterials()
        {
            if (!materialsSaved)
                return;

            foreach (var kvp in originalMaterials)
            {
                if (kvp.Key != null) // 检查渲染器是否被销毁
                {
                    kvp.Key.sharedMaterials = kvp.Value;
                }
            }
        }

        public void AddGaoguang()
        {
            SwitchMaterial(gaoguangMaterial);
        }

        public void CloseGaoguang()
        {
            RestoreOriginalMaterials();
        }
        #endregion

        #region RestoreTransformComponents
        private Vector3 initialPosition;
        private Quaternion initialRotation;

        void GetStartTransform()
        {
            // 记录物体的初始位置
            initialPosition = transform.position;
            // 记录物体的初始旋转
            initialRotation = transform.rotation;
        }

        public void ResetObject()
        {
            StartCoroutine(ReleaseFromInteractors());
            // 重置物体的位置到初始位置
            // 重置物体的旋转到初始旋转
            transform.SetPositionAndRotation(initialPosition, initialRotation);
            ControlRig(true);
        }

        private IEnumerator ReleaseFromInteractors()
        {
            // 获取物体上的 XRBaseInteractable 组件（XRGrabInteractable 继承自 XRBaseInteractable）
            XRBaseInteractable interactable = GetComponent<XRBaseInteractable>();
            if (interactable != null)
            {
                // 复制 interactorsSelecting 集合到一个新的列表
                var interactorsList = new System.Collections.Generic.List<IXRSelectInteractor>(interactable.interactorsSelecting);

                // 遍历新列表
                foreach (var interactor in interactorsList)
                {
                    if (interactor is XRBaseInteractor baseInteractor)
                    {
                        // 获取交互管理器
                        XRInteractionManager interactionManager = baseInteractor.interactionManager;
                        if (interactionManager != null)
                        {
                            if (baseInteractor is XRSocketInteractor socketInteractor)
                            {
                                // 对于 XRSocketInteractor，先禁用再启用 socket
                                socketInteractor.socketActive = false;
                                interactionManager.SelectExit(interactor, interactable);

                                // 暂时禁用 XRSocketInteractor 一段时间
                                yield return new WaitForSeconds(0.5f);
                                socketInteractor.socketActive = true;
                            }
                            else
                            {
                                // 对于其他交互器，直接触发取消选择操作
                                interactionManager.SelectExit(interactor, interactable);
                            }
                        }
                    }
                }
            }
        }
        #endregion
    }
}
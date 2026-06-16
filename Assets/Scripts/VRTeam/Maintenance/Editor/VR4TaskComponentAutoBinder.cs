using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace VRHelmet.VRTeam.Maintenance.Editor
{
    public static class VR4TaskComponentAutoBinder
    {
        [MenuItem("VR4/Maintenance/补齐选中 StepData 任务组件")]
        private static void BindSelectedStepData()
        {
            int changedCount = 0;
            foreach (Object selectedObject in Selection.objects)
            {
                if (selectedObject is GameObject gameObject &&
                    gameObject.TryGetComponent(out VR4_StepData stepData))
                {
                    changedCount += EnsureTaskComponents(stepData);
                }
            }

            Debug.Log($"[VR4TaskComponentAutoBinder] 已补齐任务组件数量: {changedCount}");
        }

        [MenuItem("VR4/Maintenance/补齐选中 StepData 任务组件", true)]
        private static bool CanBindSelectedStepData()
        {
            foreach (Object selectedObject in Selection.objects)
            {
                if (selectedObject is GameObject gameObject &&
                    gameObject.GetComponent<VR4_StepData>() != null)
                {
                    return true;
                }
            }

            return false;
        }

        public static int EnsureTaskComponents(VR4_StepData stepData)
        {
            if (stepData == null || stepData.oStepList == null)
            {
                return 0;
            }

            int changedCount = 0;
            Undo.RecordObject(stepData, "Auto Bind VR4 Task Components");

            for (int i = 0; i < stepData.oStepList.Count; i++)
            {
                OperateStep step = stepData.oStepList[i];
                if (step == null)
                {
                    continue;
                }

                changedCount += EnsureTaskComponent(step);
            }

            if (changedCount > 0)
            {
                EditorUtility.SetDirty(stepData);
                EditorSceneManager.MarkSceneDirty(stepData.gameObject.scene);
            }

            return changedCount;
        }

        private static int EnsureTaskComponent(OperateStep step)
        {
            switch (step.taskType)
            {
                case TaskType.Pick:
                    return EnsurePickComponent(step.pickTask);
                case TaskType.Rotate:
                    return EnsureRotateComponent(step.rotateTask);
                case TaskType.Switch:
                    return EnsureSwitchComponent(step.switchTask);
                case TaskType.Collision:
                    return EnsureCollisionComponent(step.collisionTask);
                case TaskType.BaseObject:
                    return 0;
                default:
                    return 0;
            }
        }

        private static int EnsureRotateComponent(RotateTask task)
        {
            VR4_RotatableObject component = EnsureComponent<VR4_RotatableObject>(task, out int changedCount);
            if (component != null && task.rotatableScript != component)
            {
                task.rotatableScript = component;
                changedCount++;
            }

            return changedCount;
        }

        private static int EnsurePickComponent(PickTask task)
        {
            int changedCount = EnsureComponent<VR4_GrabObject>(task);
            if (task != null && task.interactiveObject != null && task.grabScript == null)
            {
                XRGrabInteractable grabInteractable = task.interactiveObject.GetComponent<XRGrabInteractable>();
                if (grabInteractable != null)
                {
                    task.grabScript = grabInteractable;
                    changedCount++;
                }
            }

            return changedCount;
        }

        private static int EnsureSwitchComponent(SwitchTask task)
        {
            VR4_SwitchObject component = EnsureComponent<VR4_SwitchObject>(task, out int changedCount);
            if (component != null && task.switchScript != component)
            {
                task.switchScript = component;
                changedCount++;
            }

            return changedCount;
        }

        private static int EnsureCollisionComponent(CollisionTask task)
        {
            VR4_CollisionObject component = EnsureComponent<VR4_CollisionObject>(task, out int changedCount);
            if (component != null && task.collisionScript != component)
            {
                task.collisionScript = component;
                changedCount++;
            }

            if (task != null && task.interactiveObject != null && task.grabScript == null)
            {
                XRGrabInteractable grabInteractable = task.interactiveObject.GetComponent<XRGrabInteractable>();
                if (grabInteractable != null)
                {
                    task.grabScript = grabInteractable;
                    changedCount++;
                }
            }

            return changedCount;
        }

        private static int EnsureComponent<TComponent>(Task task) where TComponent : Component
        {
            EnsureComponent<TComponent>(task, out int changedCount);
            return changedCount;
        }

        private static TComponent EnsureComponent<TComponent>(Task task, out int changedCount) where TComponent : Component
        {
            changedCount = 0;
            if (task == null || task.interactiveObject == null)
            {
                return null;
            }

            TComponent component = task.interactiveObject.GetComponent<TComponent>();
            if (component != null)
            {
                return component;
            }

            component = Undo.AddComponent<TComponent>(task.interactiveObject);
            changedCount++;
            return component;
        }
    }

    [CustomEditor(typeof(VR4_StepData))]
    [CanEditMultipleObjects]
    public class VR4StepDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            if (GUILayout.Button("补齐任务组件"))
            {
                int changedCount = 0;
                foreach (Object targetObject in targets)
                {
                    changedCount += VR4TaskComponentAutoBinder.EnsureTaskComponents(targetObject as VR4_StepData);
                }

                Debug.Log($"[VR4TaskComponentAutoBinder] 已补齐任务组件数量: {changedCount}");
            }
        }
    }
}

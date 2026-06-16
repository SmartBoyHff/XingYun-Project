using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VRHelmet.VRTeam.Maintenance.Editor
{
    [CustomPropertyDrawer(typeof(VR4InteractionLayer))]
    public class VR4InteractionLayerDrawer : PropertyDrawer
    {
        private static readonly string[] fallbackLayerNames =
        {
            "Default",
            "Interactable",
            "NonInteractable"
        };

        private static VR4InteractionLayerSettings cachedSettings;
        private static string[] cachedLayerNames;

        static VR4InteractionLayerDrawer()
        {
            EditorApplication.projectChanged += ClearCache;
            Undo.undoRedoPerformed += ClearCache;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty maskProperty = property.FindPropertyRelative("mask");
            if (maskProperty == null)
            {
                EditorGUI.LabelField(position, label.text, "Invalid VR4InteractionLayer");
                return;
            }

            string[] layerNames = GetLayerNames();
            int maxMask = layerNames.Length >= VR4InteractionLayer.MaxLayerCount
                ? VR4InteractionLayer.EverythingMask
                : (1 << layerNames.Length) - 1;

            int currentMask = maskProperty.intValue & VR4InteractionLayer.EverythingMask;
            bool isEverything = currentMask == VR4InteractionLayer.EverythingMask;
            bool isNothing = currentMask == 0;

            string displayName = label.text;
            if (isEverything)
            {
                displayName += " (Everything)";
            }
            else if (isNothing)
            {
                displayName += " (Nothing)";
            }

            EditorGUI.BeginProperty(position, label, property);
            int editedMask = EditorGUI.MaskField(position, displayName, isEverything ? maxMask : currentMask, layerNames);
            maskProperty.intValue = editedMask == maxMask ? VR4InteractionLayer.EverythingMask : editedMask;
            EditorGUI.EndProperty();
        }

        private static string[] GetLayerNames()
        {
            if (cachedLayerNames != null)
            {
                return cachedLayerNames;
            }

            VR4InteractionLayerSettings settings = GetSettings();
            if (settings == null)
            {
                cachedLayerNames = fallbackLayerNames;
                return cachedLayerNames;
            }

            string[] names = settings.GetLayerNameArray();
            if (names == null || names.Length == 0)
            {
                cachedLayerNames = fallbackLayerNames;
                return cachedLayerNames;
            }

            if (names.Length <= VR4InteractionLayer.MaxLayerCount)
            {
                cachedLayerNames = names;
                return cachedLayerNames;
            }

            List<string> trimmedNames = new List<string>(VR4InteractionLayer.MaxLayerCount);
            for (int i = 0; i < VR4InteractionLayer.MaxLayerCount; i++)
            {
                trimmedNames.Add(names[i]);
            }

            cachedLayerNames = trimmedNames.ToArray();
            return cachedLayerNames;
        }

        private static VR4InteractionLayerSettings GetSettings()
        {
            if (cachedSettings == null)
            {
                cachedSettings = FindSettings();
            }

            return cachedSettings;
        }

        private static void ClearCache()
        {
            cachedSettings = null;
            cachedLayerNames = null;
        }

        private static VR4InteractionLayerSettings FindSettings()
        {
            string[] guids = AssetDatabase.FindAssets("t:VR4InteractionLayerSettings");
            if (guids == null || guids.Length == 0)
            {
                return null;
            }

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<VR4InteractionLayerSettings>(path);
        }
    }
}

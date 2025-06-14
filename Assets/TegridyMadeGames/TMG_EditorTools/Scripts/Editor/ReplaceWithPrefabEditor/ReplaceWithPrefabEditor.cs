using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace TMG_EditorTools
{

    public class ReplaceWithPrefabEditor : EditorWindow
    {
        private GameObject prefabToReplaceWith;
        private List<GameObject> sceneObjects = new List<GameObject>();
        private Vector2 scrollPos;

        private bool useWorldScale = false;
        private bool useBothScales = false;

        [MenuItem("Tools/TMG_EditorTools/Replace Scene Objs With Prefab")]
        public static void ShowWindow()
        {
            GetWindow<ReplaceWithPrefabEditor>("Replace With Prefab");
        }

        private void OnGUI()
        {
            GUILayout.Label("Replace Scene Objects with Prefab", EditorStyles.boldLabel);

            prefabToReplaceWith = (GameObject)EditorGUILayout.ObjectField("Replacement Prefab", prefabToReplaceWith, typeof(GameObject), false);

            EditorGUILayout.Space();
            GUILayout.Label("Scene Objects to Replace", EditorStyles.label);

            if (GUILayout.Button("Add Selected Objects"))
            {
                foreach (var obj in Selection.gameObjects)
                {
                    if (!sceneObjects.Contains(obj))
                    {
                        sceneObjects.Add(obj);
                    }
                }
            }

            if (GUILayout.Button("Clear List"))
            {
                sceneObjects.Clear();
            }

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(150));
            for (int i = 0; i < sceneObjects.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                sceneObjects[i] = (GameObject)EditorGUILayout.ObjectField(sceneObjects[i], typeof(GameObject), true);
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    sceneObjects.RemoveAt(i);
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
            GUILayout.Label("Scale Options", EditorStyles.boldLabel);

            useWorldScale = EditorGUILayout.Toggle("Use World Scale", useWorldScale);
            useBothScales = EditorGUILayout.Toggle("Apply Both Scales", useBothScales);

            // Ensure only one toggle is active
            if (useWorldScale && useBothScales)
            {
                useBothScales = false;
            }

            EditorGUILayout.Space();

            GUI.enabled = prefabToReplaceWith != null && sceneObjects.Count > 0;
            if (GUILayout.Button("Replace All"))
            {
                ReplaceAll();
            }
            GUI.enabled = true;
        }

        private void ReplaceAll()
        {
            if (prefabToReplaceWith == null)
            {
                Debug.LogError("No prefab selected.");
                return;
            }

            Undo.RegisterCompleteObjectUndo(this, "Replace GameObjects With Prefab");

            foreach (var obj in sceneObjects)
            {
                if (obj == null) continue;

                Transform originalTransform = obj.transform;

                GameObject newObj = (GameObject)PrefabUtility.InstantiatePrefab(prefabToReplaceWith, obj.scene);
                Undo.RegisterCreatedObjectUndo(newObj, "Instantiate Prefab");

                Transform newTransform = newObj.transform;

                newTransform.position = originalTransform.position;
                newTransform.rotation = originalTransform.rotation;
                newTransform.parent = originalTransform.parent;

                if (useBothScales)
                {
                    newTransform.localScale = originalTransform.localScale;
                    newTransform.localScale = Vector3.Scale(newTransform.localScale, GetWorldScaleRatio(originalTransform, newTransform));
                }
                else if (useWorldScale)
                {
                    SetWorldScale(newTransform, originalTransform.lossyScale);
                }
                else
                {
                    newTransform.localScale = originalTransform.localScale;
                }

                Undo.DestroyObjectImmediate(obj);
            }

            sceneObjects.Clear();
        }

        private void SetWorldScale(Transform transform, Vector3 worldScale)
        {
            Transform parent = transform.parent;
            if (parent == null)
            {
                transform.localScale = worldScale;
            }
            else
            {
                Vector3 parentScale = parent.lossyScale;
                transform.localScale = new Vector3(
                    worldScale.x / parentScale.x,
                    worldScale.y / parentScale.y,
                    worldScale.z / parentScale.z
                );
            }
        }

        private Vector3 GetWorldScaleRatio(Transform original, Transform replacement)
        {
            Vector3 originalWorld = original.lossyScale;
            Vector3 newWorld = replacement.lossyScale;
            return new Vector3(
                originalWorld.x / (newWorld.x == 0 ? 1 : newWorld.x),
                originalWorld.y / (newWorld.y == 0 ? 1 : newWorld.y),
                originalWorld.z / (newWorld.z == 0 ? 1 : newWorld.z)
            );
        }
    }

}
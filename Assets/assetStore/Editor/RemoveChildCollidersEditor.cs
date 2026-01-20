using UnityEngine;
using UnityEditor;

public class RemoveChildCollidersEditor : EditorWindow
{
    [MenuItem("Tools/Remove Child BoxColliders")]
    static void Init()
    {
        RemoveChildCollidersEditor window = (RemoveChildCollidersEditor)EditorWindow.GetWindow(typeof(RemoveChildCollidersEditor));
        window.Show();
    }

    void OnGUI()
    {
        if (GUILayout.Button("Remove BoxColliders"))
        {
            if (Selection.activeGameObject != null)
            {
                RemoveBoxCollidersFromChildren(Selection.activeGameObject.transform);
                Debug.Log("BoxColliders removed from all children of " + Selection.activeGameObject.name);
            }
            else
            {
                Debug.LogError("Please select a GameObject first.");
            }
        }
    }

    void RemoveBoxCollidersFromChildren(Transform parent)
    {
        foreach (Transform child in parent)
        {
            BoxCollider collider = child.GetComponent<BoxCollider>();
            if (collider != null)
            {
                DestroyImmediate(collider);
            }
            // Recursively check all child objects
            RemoveBoxCollidersFromChildren(child);
        }
    }
}
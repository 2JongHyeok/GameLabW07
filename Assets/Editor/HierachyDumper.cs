using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Text;

public static class HierarchyDumper
{
    [MenuItem("Tools/Hierarchy/Copy Scene Hierarchy")]
    public static void CopyHierarchy()
    {
        var sb = new StringBuilder();
        var scene = SceneManager.GetActiveScene();
        foreach (var go in scene.GetRootGameObjects())
            Dump(go.transform, 0, sb);
        EditorGUIUtility.systemCopyBuffer = sb.ToString();
        Debug.Log("[Hierarchy Dump]\n" + sb);
    }

    [MenuItem("Tools/Hierarchy/Copy Selected Subtree")]
    public static void CopySelected()
    {
        var t = Selection.activeTransform;
        if (!t) { Debug.Log("Select a root in Hierarchy and retry."); return; }
        var sb = new StringBuilder();
        Dump(t, 0, sb);
        EditorGUIUtility.systemCopyBuffer = sb.ToString();
        Debug.Log("[Hierarchy Dump - Selected]\n" + sb);
    }

    private static void Dump(Transform t, int depth, StringBuilder sb)
    {
        sb.Append(' ', depth * 2).Append("└─ ").Append(t.name).AppendLine();
        for (int i = 0; i < t.childCount; i++)
            Dump(t.GetChild(i), depth + 1, sb);
    }
}

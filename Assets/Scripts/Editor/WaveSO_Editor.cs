using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WaveSO))]
public class WaveSO_Editor : Editor
{
    private SerializedProperty rangerCountProp;
    private SerializedProperty rangerTankCountProp;
    private SerializedProperty kamikazeCountProp;
    private SerializedProperty kamikazeTankCountProp;
    private SerializedProperty parasiteCountProp;
    private SerializedProperty spawnIntervalProp;
    private SerializedProperty minSpawnPerIntervalProp;
    private SerializedProperty maxSpawnPerIntervalProp;
    private SerializedProperty bossCountProp;


    private void OnEnable()
    {
        rangerCountProp = serializedObject.FindProperty("rangerCount");
        rangerTankCountProp = serializedObject.FindProperty("rangerTankCount");
        kamikazeCountProp = serializedObject.FindProperty("kamikazeCount");
        kamikazeTankCountProp = serializedObject.FindProperty("kamikazeTankCount");
        parasiteCountProp = serializedObject.FindProperty("parasiteCount");
        spawnIntervalProp = serializedObject.FindProperty("spawnInterval");
        minSpawnPerIntervalProp = serializedObject.FindProperty("minSpawnPerInterval");
        maxSpawnPerIntervalProp = serializedObject.FindProperty("maxSpawnPerInterval");
        bossCountProp = serializedObject.FindProperty("bossCount");
    }

    public override void OnInspectorGUI()
    {
        WaveSO waveSO = (WaveSO)target;
        serializedObject.Update();

        // 스크립트 참조 표시
        GUI.enabled = false;
        EditorGUILayout.ObjectField("Script", MonoScript.FromScriptableObject((ScriptableObject)target), typeof(ScriptableObject), false);
        GUI.enabled = true;
        
        EditorGUILayout.Space(5);
        
        EditorGUILayout.Space(10);

        // Enemy Composition - 세로 배치
        EditorGUILayout.LabelField("Enemy Composition", EditorStyles.boldLabel);
        
        // 박스 스타일 생성
        GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.padding = new RectOffset(10, 10, 10, 10);

        EditorGUILayout.BeginVertical(boxStyle);
        {
            EditorGUILayout.PropertyField(rangerCountProp, new GUIContent("Ranger"));
            EditorGUILayout.PropertyField(rangerTankCountProp, new GUIContent("Ranger Tank"));
            EditorGUILayout.PropertyField(kamikazeCountProp, new GUIContent("Kamikaze"));
            EditorGUILayout.PropertyField(kamikazeTankCountProp, new GUIContent("Kamikaze Tank"));
            EditorGUILayout.PropertyField(parasiteCountProp, new GUIContent("Parasite"));
            EditorGUILayout.PropertyField(bossCountProp, new GUIContent("Boss"));
            
        }
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space(10);

        // Spawn Timing
        EditorGUILayout.LabelField("Spawn Timing", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(spawnIntervalProp, new GUIContent("Spawn Interval"));
        EditorGUILayout.PropertyField(minSpawnPerIntervalProp, new GUIContent("Min Spawn Per Interval"));
        EditorGUILayout.PropertyField(maxSpawnPerIntervalProp, new GUIContent("Max Spawn Per Interval"));

        // 총 적 수 표시 (읽기 전용)
        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox($"Total Enemies: {waveSO.GetTotalEnemyCount()}", MessageType.Info);

        serializedObject.ApplyModifiedProperties();
    }
}

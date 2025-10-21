using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(BaseForgeSO), true)]
public class BaseForgeSO_Editor : Editor
{
    private SerializedProperty forgeIdProp;
    private SerializedProperty upgradeNameProp;
    private SerializedProperty upgradeDescriptionProp;
    private SerializedProperty coalCostProp;
    private SerializedProperty ironCostProp;
    private SerializedProperty goldCostProp;
    private SerializedProperty diamondCostProp;
    private SerializedProperty postSubBranchesProp;

    private void OnEnable()
    {
        forgeIdProp = serializedObject.FindProperty("forgeId");
        upgradeNameProp = serializedObject.FindProperty("upgradeName");
        upgradeDescriptionProp = serializedObject.FindProperty("upgradeDescription");
        coalCostProp = serializedObject.FindProperty("coalCost");
        ironCostProp = serializedObject.FindProperty("ironCost");
        goldCostProp = serializedObject.FindProperty("goldCost");
        diamondCostProp = serializedObject.FindProperty("diamondCost");
        postSubBranchesProp = serializedObject.FindProperty("postSubBranches");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 스크립트 참조 표시
        GUI.enabled = false;
        EditorGUILayout.ObjectField("Script", MonoScript.FromScriptableObject((ScriptableObject)target), typeof(ScriptableObject), false);
        GUI.enabled = true;
        
        EditorGUILayout.Space(5);

        // 식별 정보 (읽기 전용)
        EditorGUILayout.LabelField("Identification", EditorStyles.boldLabel);
        GUI.enabled = false;
        EditorGUILayout.PropertyField(forgeIdProp, new GUIContent("Forge ID (Auto)"));
        GUI.enabled = true;
        
        EditorGUILayout.Space(10);

        // 업그레이드 정보
        EditorGUILayout.LabelField("Upgrade Info", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(upgradeNameProp);
        EditorGUILayout.PropertyField(upgradeDescriptionProp, GUILayout.Height(60));
        EditorGUILayout.Space(10);

        // 커스텀 비용 UI
        EditorGUILayout.LabelField("Ore Costs", EditorStyles.boldLabel);
        
        // 박스 스타일 생성
        GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.padding = new RectOffset(10, 10, 10, 10);

        EditorGUILayout.BeginVertical(boxStyle);
        {
            // 헤더 라벨들
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Label("Coal", EditorStyles.boldLabel, GUILayout.Width(80));
                GUILayout.Label("Iron", EditorStyles.boldLabel, GUILayout.Width(80));
                GUILayout.Label("Gold", EditorStyles.boldLabel, GUILayout.Width(80));
                GUILayout.Label("Diamond", EditorStyles.boldLabel, GUILayout.Width(80));
            }
            EditorGUILayout.EndHorizontal();

            // 값 입력 필드들
            EditorGUILayout.BeginHorizontal();
            {
                coalCostProp.intValue = EditorGUILayout.IntField(coalCostProp.intValue, GUILayout.Width(80));
                ironCostProp.intValue = EditorGUILayout.IntField(ironCostProp.intValue, GUILayout.Width(80));
                goldCostProp.intValue = EditorGUILayout.IntField(goldCostProp.intValue, GUILayout.Width(80));
                diamondCostProp.intValue = EditorGUILayout.IntField(diamondCostProp.intValue, GUILayout.Width(80));
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        // 후속 브랜치
        EditorGUILayout.PropertyField(postSubBranchesProp, true);

        EditorGUILayout.Space(10);

        // 자식 클래스의 추가 필드들 표시 (BaseForgeSO에 정의된 필드 제외)
        DrawPropertiesExcluding(serializedObject, 
            "m_Script",
            "forgeId",
            "upgradeName", 
            "upgradeDescription", 
            "coalCost", 
            "ironCost", 
            "goldCost", 
            "diamondCost", 
            "postSubBranches");

        serializedObject.ApplyModifiedProperties();
    }
}

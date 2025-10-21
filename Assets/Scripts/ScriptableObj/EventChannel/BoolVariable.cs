// BoolVariable.cs
using UnityEngine;

// 이 메뉴를 통해 Project 창에서 쉽게 생성할 수 있게 됩니다.
[CreateAssetMenu(fileName = "New Bool Variable", menuName = "Variables/Bool Variable")]
public class BoolVariable : ScriptableObject
{
    // 이 스크립터블 오브젝트가 저장할 실제 bool 값입니다.
    public bool Value;
}
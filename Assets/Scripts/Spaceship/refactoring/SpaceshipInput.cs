using UnityEngine;

// 더 이상 PlayerInput은 필요 없어. 네가 버렸으니까.
public class SpaceshipInput : MonoBehaviour
{
    [Tooltip("역추진 시 적용될 힘의 배율 (0.5 = 50%)")]
    [SerializeField] private float reverseThrustMultiplier = 0.5f;

    // 이 값들은 다른 스크립트들이 여전히 사용하겠지.
    public float ThrustInput { get; private set; }
    public float RotateInput { get; private set; }
    public bool IsBoosting { get; private set; }
    
    // 이딴 건 이제 필요 없어.
    // public bool ToggleControlPressed { get; private set; }


    // Awake()도 필요 없어. Input System을 안 쓰니까.
    // private void Awake() { }

    // 모든 걸 이 원시적인 Update() 안에서 해결해주지.
    private void Update()
    {
        // 전진/후진 (W/S)
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            ThrustInput = 1.0f;
        }
        else if (Input.GetKey(KeyCode.S)|| Input.GetKey(KeyCode.DownArrow))
        {
            ThrustInput = -reverseThrustMultiplier;
        }
        else
        {
            ThrustInput = 0.0f;
        }
        
        // 부스트 (Shift)
        // IsBoosting = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        // 회전 (A/D)
        // A키는 양수, D키는 음수 값을 줘야 Motor에서 제대로 회전해.
        if (Input.GetKey(KeyCode.A)|| Input.GetKey(KeyCode.LeftArrow))
        {
            RotateInput = -1f;
        }
        else if (Input.GetKey(KeyCode.D)|| Input.GetKey(KeyCode.RightArrow))
        {
            RotateInput = 1f;
        }
        else
        {
            RotateInput = 0f;
        }
    }
}
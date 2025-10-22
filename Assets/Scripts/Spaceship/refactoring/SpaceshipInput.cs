using UnityEngine;
using UnityEngine.InputSystem;

// 더 이상 PlayerInput은 필요 없어. 네가 버렸으니까.
public class SpaceshipInput : MonoBehaviour
{
    [Tooltip("역추진 시 적용될 힘의 배율 (0.5 = 50%)")] [SerializeField]
    private float reverseThrustMultiplier = 0.5f;

    [Header("Gamepad Settings")] private float gamepadDeadZone = 0.1f; // 게임패드 데드존 설정

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
        if (Gamepad.current != null)
        {
            float gamepadThrust = Gamepad.current.leftStick.ReadValue().y;
            if (Mathf.Abs(gamepadThrust) > gamepadDeadZone)
            {
                ThrustInput = gamepadThrust;
                // ThrustInput = gamepadThrust; 부분에서 reverseThrustMultiplier를 적용하려면 gamepadThrust가 음수일 때
                // gamepadThrust * reverseThrustMultiplier와 같이 추가 로직이 필요할 수 있습니다. 현재는 스틱의 Y축 값을 그대로 사용합니다
            }
            else
            {
                ThrustInput = 0.0f;
            }

        }

        if (ThrustInput == 0.0f)
        {
            // 전진/후진 (W/S)
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            {
                ThrustInput = 1.0f;
            }
            else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            {
                ThrustInput = -reverseThrustMultiplier;
            }
        }
        else
        {
            ThrustInput = 0.0f;
        }

        // 부스트 (Shift)
        // IsBoosting = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        // 회전 (A/D)
        // A키는 양수, D키는 음수 값을 줘야 Motor에서 제대로 회전해.
        // 게임패드 왼쪽 스틱 X축 입력 처리 (회전)
        if (Gamepad.current != null)
        {
            float gamepadRotate = Gamepad.current.leftStick.ReadValue().x;
            if (Mathf.Abs(gamepadRotate) > gamepadDeadZone) // 데드존 적용
            {
                RotateInput = gamepadRotate; // 스틱 X축 값을 직접 사용
            }
            else
            {
                RotateInput = 0f; // 데드존 이내면 입력 없음
            }
        }

        // 게임패드 입력이 없거나 데드존 이내일 경우 키보드 입력 사용 
        // 게임패드 입력이 없었을 때만 키보드 확인
        if (RotateInput == 0f)
        {
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            {
                RotateInput = -1f;
            }
            else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            {
                RotateInput = 1f;
            }
            else
            {
                RotateInput = 0f;
            }
        }
    }
}
using UnityEngine;
// using UnityEngine.InputSystem;

// 더 이상 PlayerInput은 필요 없어. 네가 버렸으니까.
public class SpaceshipInput : MonoBehaviour
{
    [Tooltip("역추진 시 적용될 힘의 배율 (0.5 = 50%)")] [SerializeField]
    private float reverseThrustMultiplier = 0.5f;
    
    // ## 수정 ## : 회전 관련 변수 추가
    [Header("Rotation Settings")]
    [Tooltip("마우스가 이 각도(도) 이상 벗어나야 회전 입력을 생성합니다.")]
    [SerializeField] private float mouseRotationThreshold = 5.0f; // 약간의 오차 허용
    // ## 수정 끝 ##

    [Header("Gamepad Settings")] private float gamepadDeadZone = 0.1f; // 게임패드 데드존 설정

    // 이 값들은 다른 스크립트들이 여전히 사용하겠지.
    public float ThrustInput { get; private set; }
    public float RotateInput { get; private set; }
    public bool IsBoosting { get; private set; }
    
    private Camera mainCamera; // 마우스 위치 변환용
    
    void Awake()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
        }
    }

    // 이딴 건 이제 필요 없어.
    // public bool ToggleControlPressed { get; private set; }


    // Awake()도 필요 없어. Input System을 안 쓰니까.
    // private void Awake() { }

    // 모든 걸 이 원시적인 Update() 안에서 해결해주지.
    private void Update()
    {
        // 추후 게임패드 추가
        /*if (Gamepad.current != null)
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

        }*/

        if (ThrustInput == 0.0f)
        {
            // 전진/후진 (W/S)
            // 추후 여기에 키보드 인풋 추가
        }
        else
        {
            ThrustInput = 0.0f;
        }
        
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            ThrustInput = 1.0f;
        }
        else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            ThrustInput = -reverseThrustMultiplier;
        }

        
        CalculateRotationInputFromMouse();
        // 부스트 (Shift)
        // IsBoosting = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        // 회전 (A/D)
        // A키는 양수, D키는 음수 값을 줘야 Motor에서 제대로 회전해.
        // 게임패드 왼쪽 스틱 X축 입력 처리 (회전)
        
        // 추후 게임패드 추가
        /*if (Gamepad.current != null)
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
        }*/

        // 게임패드 입력이 없거나 데드존 이내일 경우 키보드 입력 사용 
        // 게임패드 입력이 없었을 때만 키보드 확인
        /*if (RotateInput == 0f)
        {
            // 추후 여기에 키보드 인풋 추가
        }
        
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
        }*/
    }
    
    private void CalculateRotationInputFromMouse()
    {
        // 기본값은 회전 없음
        RotateInput = 0f;

        if (mainCamera == null) return;

        // 1. 마우스 스크린 좌표 -> 월드 좌표 변환
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = Mathf.Abs(mainCamera.transform.position.z - transform.position.z); // 카메라와 우주선 사이의 거리
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);

        // 2. 우주선 위치에서 마우스 월드 위치를 향하는 방향 벡터 계산
        Vector2 directionToMouse = (Vector2)mouseWorldPos - (Vector2)transform.position;

        // 마우스가 우주선 바로 위에 있는 경우 (거리가 매우 가까운 경우) 회전 입력을 0으로 처리 (오류 방지)
        if (directionToMouse.sqrMagnitude < 0.01f)
        {
            return;
        }

        // 3. 우주선의 현재 '앞쪽' 방향 벡터 가져오기 (스프라이트가 위쪽을 본다고 가정)
        Vector2 shipForward = transform.up;

        // 4. 우주선 앞쪽 방향과 마우스를 향하는 방향 사이의 각도 계산 (SignedAngle 사용)
        //   SignedAngle은 두 벡터 사이의 각도를 -180 ~ +180 범위로 반환 (시계방향 -, 반시계방향 +)
        float angleDifference = Vector2.SignedAngle(shipForward, directionToMouse);

        // 5. 각도 차이가 임계값보다 클 때만 회전 입력 생성
        if (Mathf.Abs(angleDifference) > mouseRotationThreshold)
        {
            // 각도 차이가 양수이면 마우스가 오른쪽 -> 오른쪽 회전 (1)
            // 각도 차이가 음수이면 마우스가 왼쪽 -> 왼쪽 회전 (-1)
            RotateInput = -Mathf.Sign(angleDifference);
        }
        // 임계값 이내면 RotateInput은 0 유지 (회전 안 함)
    }
}
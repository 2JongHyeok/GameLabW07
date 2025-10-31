using UnityEngine;
// using UnityEngine.InputSystem;

// 더 이상 PlayerInput은 필요 없어. 네가 버렸으니까.
public class SpaceshipInput : MonoBehaviour
{
    [Tooltip("역추진 시 적용될 힘의 배율 (0.5 = 50%)")] [SerializeField]
    private float reverseThrustMultiplier = 0.5f;

    // 회전 관련 변수 추가
    [Header("Rotation Settings")] [Tooltip("마우스가 이 각도(도) 이상 벗어나야 회전 입력을 생성합니다.")] [SerializeField]
    private float mouseRotationThreshold = 5.0f; // 약간의 오차 허용
    [SerializeField] private float mouseRotationDeadzone = 0.5f; // 5.0f -> 0.5f (또는 1.0f)로 추천
    
    // ## 더블 클릭 로직을 위한 변수 수정 ##
    [Header("Double Click Settings")]
    [Tooltip("더블 클릭으로 인정할 최대 시간 간격 (초)")]
    [SerializeField] private float doubleClickTimeThreshold = 0.3f;
    private float timeOfLastRmbClick = -10f; // 마지막 우클릭 시간 (초기값은 멀리 둠)

    // 'isReverseThrustToggled' 대신, 현재 홀드 상태를 저장할 변수
    private bool isHoldingForReverse = false; 

    [Header("Gamepad Settings")] private float gamepadDeadZone = 0.1f; // 게임패드 데드존 설정

    // 이 값들은 다른 스크립트들이 여전히 사용하겠지.
    public float ThrustInput { get; private set; }
    public float RotateInput { get; private set; }
    public bool IsBoosting { get; private set; }

    private Camera mainCamera; // 마우스 위치 변환용

    [SerializeField] private float mouseSensitivity = 45.0f; // 마우스 감도 조절용 변수
    public bool isMouseControlEnabled = true; // 마우스 제어 활성화 여부
    public float mouseX;
    public float mouseY;
    void Awake()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("메인 카메라를 찾을 수 없습니다! 카메라에 'MainCamera' 태그가 있는지 확인하세요.");
        }
    }
    
    
    private void Update()
    {
        #region 게임 패드 코드

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

        #endregion


        #region 기존 이동 로직 (삭제됨)
        /*
        if (ThrustInput == 0.0f)
        {
            // 전진/후진 (W/S)
            // 추후 여기에 키보드 인풋 추가
        }
        else
        {
            ThrustInput = 0.0f;
        }

        if (Input.GetMouseButton(1))
        {
            ThrustInput = 1.0f;
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            ThrustInput = -reverseThrustMultiplier;
        }
        */
        #endregion


        // 1. 마우스 우클릭을 '누른' 프레임에만 홀드 상태를 결정합니다.
        if (Input.GetMouseButtonDown(1))
        {
            float timeSinceLastClick = Time.time - timeOfLastRmbClick;

            if (timeSinceLastClick <= doubleClickTimeThreshold)
            {
                // 더블 클릭 성공: 이번 홀드는 '역추진' 홀드입니다.
                isHoldingForReverse = true;
                timeOfLastRmbClick = -10f; // 타이머 리셋 (3연속 클릭 방지)
            }
            else
            {
                // 일반 클릭: 이번 홀드는 '전진' 홀드입니다.
                isHoldingForReverse = false;
                timeOfLastRmbClick = Time.time; // 다음 더블 클릭을 위해 시간 기록
            }
        }

        // 2. 현재 입력값을 0으로 초기화
        ThrustInput = 0.0f;

        // 3. 마우스 우클릭이 '눌려있는 동안'(홀드) 상태에 따라 추력을 적용합니다.
        if (Input.GetMouseButton(1) || Input.GetKey(KeyCode.UpArrow)) // 꾹 누르고 있는 동안
        {
            if (isHoldingForReverse)
            {
                // '역추진' 홀드 상태입니다.
                ThrustInput = -reverseThrustMultiplier;
            }
            else
            {
                // '전진' 홀드 상태입니다.
                ThrustInput = 1.0f;
            }
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            // 4. 마우스를 누르고 있지 않을 때만 S키 입력을 받습니다.
            ThrustInput = -reverseThrustMultiplier;
        }
        

        // 5. 마우스 기반 회전 계산 (기본)
        CalculateRotationInputFromMouse();


        #region 부스트 및 게임 패드, 키보드 회전 코드

        // 부스트 (Shift)
        // IsBoosting = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        // ## 여기부터 수정 ##
        // 6. 키보드 회전 입력 (마우스 입력을 덮어씀)
        // A키는 양수, D키는 음수 값을 줘야 Motor에서 제대로 회전해.
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            // Motor.cs의 AddTorque(-rotateInput) 로직에 따라, 
            // 왼쪽(반시계) 회전은 rotateInput이 -1f가 되어야 합니다.
            RotateInput = -0.2f;
        }
        else if (Input.GetKey(KeyCode.RightArrow))
        {
            // 오른쪽(시계) 회전은 rotateInput이 1f가 되어야 합니다.
            RotateInput = 0.2f;
        }
        // 키보드 입력이 없으면, 5번에서 계산한 마우스 회전값(RotateInput)이 그대로 사용됩니다.
        // else { RotateInput = 0f; } <-- 이 코드는 절대 넣으면 안 됩니다. (마우스 회전을 0으로 덮어쓰기 때문)


        // 게임패드 왼쪽 스틱 X축 입력 처리 (회전)
        // 추후 게임패드 추가
        /*if (Gamepad.current != null)
        {
            float gamepadRotate = Gamepad.current.leftStick.ReadValue().x;
            if (Mathf.Abs(game22padRotate) > gamepadDeadZone) // 데드존 적용
            {
                // 게임패드 입력이 있다면, 키보드/마우스 입력을 다시 덮어씁니다.
                RotateInput = gamepadRotate; // 스틱 X축 값을 직접 사용
            }
            else
            {
                // 게임패드 입력이 데드존 안이면, 키보드/마우스 값을 유지합니다.
            }
        }*/

        #endregion
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
        if (directionToMouse.sqrMagnitude < 0.01f) return;

        // 3. 우주선의 현재 '앞쪽' 방향 벡터 가져오기 (스프라이트가 위쪽을 본다고 가정)
        Vector2 shipForward = transform.up;
        // 4. 우주선 앞쪽 방향과 마우스를 향하는 방향 사이의 각도 계산 (SignedAngle 사용)
        float angleDifference = Vector2.SignedAngle(shipForward, directionToMouse);
        
        // 각도 차이가 '데드존'보다 작으면, 'RotateInput'을 0으로 확정합니다.
        if (Mathf.Abs(angleDifference) < mouseRotationDeadzone)
        {
            RotateInput = 0f;
        }
        else
        {
            // 데드존보다 클 때만 회전 입력을 계산합니다.
            float proportionalRotation = Mathf.Clamp(angleDifference / mouseSensitivity, -1f, 1f);
            RotateInput = -proportionalRotation;
        }
    }
} 


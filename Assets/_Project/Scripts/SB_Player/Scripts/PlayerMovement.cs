using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("참조 컴포넌트")]
    public CharacterController characterController;
    public Transform headTransform;

    [Header("양손 트리거 버튼 액션")]
    public InputActionProperty leftHandTrigger;
    public InputActionProperty rightHandTrigger;

    [Header("속도 제어 버튼 액션 (오른손 A/B)")]
    public InputActionProperty slowButton; // XRI Right Locomotion/Jump
    public InputActionProperty fastButton; // XRI Right Locomotion/FastMove

    [Header("좌측 조이스틱 (좌우 이동용 Value)")]
    [Tooltip("XRI Left Locomotion/Move 액션을 연결하세요")]
    public InputActionProperty leftJoystickMove;

    [Header("전진 속도 설정")]
    public float normalSpeed = 2.5f;
    public float slowSpeed = 1.0f;
    public float fastSpeed = 5.0f;

    [Header("좌우(옆걸음) 배율 설정")]
    [Tooltip("전진 속도 대비 좌우 이동 속도의 비율입니다. (1.0이면 전진 속도와 100% 동일)")]
    [Range(0.1f, 2.0f)]
    public float strafeSpeedMultiplier = 0.8f;

    private float currentSpeed;

    private void Start()
    {
        currentSpeed = normalSpeed;
    }

    private void OnEnable()
    {
        leftHandTrigger.action?.Enable();
        rightHandTrigger.action?.Enable();
        slowButton.action?.Enable();
        fastButton.action?.Enable();
        leftJoystickMove.action?.Enable();
    }

    private void Update()
    {
        if (characterController == null || headTransform == null) return;

        // 1. 실시간으로 감속/가속 버튼 입력 감지하여 '통합 현재 속도' 결정
        bool isSlowPressed = slowButton.action != null && slowButton.action.IsPressed();
        bool isFastPressed = fastButton.action != null && fastButton.action.IsPressed();

        if (isSlowPressed) currentSpeed = slowSpeed;       // A 누르면 무조건 느린 모드 (1.0)
        else if (isFastPressed) currentSpeed = fastSpeed;   // B 누르면 무조건 부스터 모드 (5.0)
        else currentSpeed = normalSpeed;                    // 기본 평소 모드 (2.5)

        // 최종 이동 벡터 (속도까지 결합하여 계산)
        Vector3 finalMovement = Vector3.zero;

        // ----------------------------------------------------
        // 기능 A: 양손 트리거를 동시에 누르고 있을 때 [앞으로 전진]
        // ----------------------------------------------------
        bool isLeftTriggerPressed = leftHandTrigger.action != null && leftHandTrigger.action.IsPressed();
        bool isRightTriggerPressed = rightHandTrigger.action != null && rightHandTrigger.action.IsPressed();

        if (isLeftTriggerPressed && isRightTriggerPressed)
        {
            Vector3 forwardDirection = headTransform.forward;
            forwardDirection.y = 0;
            forwardDirection.Normalize();

            // 전진 방향 벡터 * 현재 기어 속도
            finalMovement += forwardDirection * currentSpeed;
        }

        // ----------------------------------------------------
        // 기능 B: 좌측 조이스틱을 밀 때 [부드러운 좌우 이동] (앞/뒤 차단)
        // ----------------------------------------------------
        if (leftJoystickMove.action != null)
        {
            Vector2 joystickInput = leftJoystickMove.action.ReadValue<Vector2>();

            // 오직 joystickInput.x(좌우 값)만 사용하여 옆걸음질 처리
            if (Mathf.Abs(joystickInput.x) > 0.01f)
            {
                Vector3 rightDirection = headTransform.right;
                rightDirection.y = 0;
                rightDirection.Normalize();

                // [핵심 공식 변경] 
                // 조이스틱 기울기(joystickInput.x) * 현재 통합 속도(currentSpeed) * 좌우 배율(Multiplier)
                float currentStrafeSpeed = currentSpeed * strafeSpeedMultiplier;
                finalMovement += rightDirection * joystickInput.x * currentStrafeSpeed;
            }
        }

        // 2. 최종 이동 값이 존재한다면 실제로 캐릭터를 부드럽게 이동시킴
        if (finalMovement != Vector3.zero)
        {
            characterController.Move(finalMovement * Time.deltaTime);
        }
    }
}
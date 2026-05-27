using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events; // 인스펙터에서 이벤트를 연결하기 위해 필요합니다.

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

    [Header("오른손 그립 버튼 (버스벨용)")]
    [Tooltip("XRI Right Interaction/Select 액션을 연결하세요")]
    public InputActionProperty rightGripButton;

    [Header("버스벨 이벤트 설정")]
    [Tooltip("여기에 벨 소리를 내거나 불이 켜지는 스크립트의 함수를 연결할 예정입니다.")]
    public UnityEvent onBusBellPressed;

    [Header("전진 속도 설정")]
    public float normalSpeed = 2.5f;
    public float slowSpeed = 1.0f;
    public float fastSpeed = 5.0f;

    [Header("좌우(옆걸음) 배율 설정")]
    [Range(0.1f, 2.0f)]
    public float strafeSpeedMultiplier = 0.8f;

    private float currentSpeed;
    private bool wasGripPressedLastFrame = false; // 연속으로 벨이 울리는 것을 방지

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
        rightGripButton.action?.Enable();
    }

    private void Update()
    {
        if (characterController == null || headTransform == null) return;

        // 1. 실시간으로 감속/가속 버튼 입력 감지하여 속도 세팅
        bool isSlowPressed = slowButton.action != null && slowButton.action.IsPressed();
        bool isFastPressed = fastButton.action != null && fastButton.action.IsPressed();

        if (isSlowPressed) currentSpeed = slowSpeed;
        else if (isFastPressed) currentSpeed = fastSpeed;
        else currentSpeed = normalSpeed;

        // 2. 오른손 그립 버튼 입력 체크 (버스벨 기능)
        if (rightGripButton.action != null)
        {
            bool isGripPressed = rightGripButton.action.IsPressed();

            // 이전 프레임에는 안 눌렸다가, 이번 프레임에 막 눌렀을 때 딱 한 번만 실행 (Down 판정)
            if (isGripPressed && !wasGripPressedLastFrame)
            {
                Debug.Log("오른손 그립 버튼 클릭! 버스 벨을 울립니다.");

                // 인스펙터에 연결된 버스벨 기능을 실행시킵니다.
                onBusBellPressed?.Invoke();
            }

            wasGripPressedLastFrame = isGripPressed;
        }

        // 최종 이동 벡터
        Vector3 finalMovement = Vector3.zero;

        // 기능 A: 양손 트리거 전진
        bool isLeftTriggerPressed = leftHandTrigger.action != null && leftHandTrigger.action.IsPressed();
        bool isRightTriggerPressed = rightHandTrigger.action != null && rightHandTrigger.action.IsPressed();

        if (isLeftTriggerPressed && isRightTriggerPressed)
        {
            Vector3 forwardDirection = headTransform.forward;
            forwardDirection.y = 0;
            forwardDirection.Normalize();
            finalMovement += forwardDirection * currentSpeed;
        }

        // 기능 B: 좌측 조이스틱 좌우 이동
        if (leftJoystickMove.action != null)
        {
            Vector2 joystickInput = leftJoystickMove.action.ReadValue<Vector2>();

            if (Mathf.Abs(joystickInput.x) > 0.01f)
            {
                Vector3 rightDirection = headTransform.right;
                rightDirection.y = 0;
                rightDirection.Normalize();

                float currentStrafeSpeed = currentSpeed * strafeSpeedMultiplier;
                finalMovement += rightDirection * joystickInput.x * currentStrafeSpeed;
            }
        }

        // 최종 캐릭터 이동
        if (finalMovement != Vector3.zero)
        {
            characterController.Move(finalMovement * Time.deltaTime);
        }
    }
}
using UnityEngine;
using UnityEngine.InputSystem;

public class TwoHandedMoveController : MonoBehaviour
{
    [Header("참조 컴포넌트")]
    public CharacterController characterController;
    public Transform headTransform;

    [Header("양손 트리거 버튼 액션")]
    public InputActionProperty leftHandTrigger;
    public InputActionProperty rightHandTrigger;

    [Header("속도 제어 버튼 액션 (오른손 A/B)")]
    public InputActionProperty slowButton; // XRI Right Locomotion/Jump (A버튼)
    public InputActionProperty fastButton; // XRI Right Locomotion/Turn (조이스틱)

    [Header("속도 설정")]
    public float normalSpeed = 2.5f;
    public float slowSpeed = 1.0f;
    public float fastSpeed = 5.0f;

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
    }

    private void Update()
    {
        if (characterController == null || headTransform == null) return;

        // 1. 실시간으로 버튼 입력 감지 (트리거와 상관없이 독립적으로 작동)
        bool isSlowPressed = slowButton.action != null && slowButton.action.IsPressed();
        bool isFastPressed = fastButton.action != null && fastButton.action.IsPressed();

        // 2. 누르고 있는 버튼에 따라 속도 상태 저장
        if (isSlowPressed)
        {
            currentSpeed = slowSpeed; // A 버튼 누르면 기어 낮춤
        }
        else if (isFastPressed)
        {
            currentSpeed = fastSpeed; // 가속 버튼 누르면 기어 높임
        }
        else
        {
            currentSpeed = normalSpeed; // 손 떼면 원래 속도
        }

        // 3. 양손 트리거가 동시에 눌렸을 때만 실제로 이동 처리
        bool isLeftTriggerPressed = leftHandTrigger.action != null && leftHandTrigger.action.IsPressed();
        bool isRightTriggerPressed = rightHandTrigger.action != null && rightHandTrigger.action.IsPressed();

        if (isLeftTriggerPressed && isRightTriggerPressed)
        {
            Vector3 forwardDirection = headTransform.forward;
            forwardDirection.y = 0;
            forwardDirection.Normalize();

            // 결정된 속도로 이동
            characterController.Move(forwardDirection * currentSpeed * Time.deltaTime);
        }
    }
}
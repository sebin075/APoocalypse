using UnityEngine;
using UnityEngine.InputSystem;

public class BellButton : MonoBehaviour
{
    [Header("Intro Manager")]
    [SerializeField] private IntroManager introManager;

    [Header("Input")]
    [Tooltip("키보드 테스트용")]
    [SerializeField] private Key keyboardKey = Key.Space;

    [Tooltip("충돌/트리거 방식으로 벨을 누를지")]
    [SerializeField] private bool useTriggerPress = true;

    [Header("Debug")]
    [SerializeField] private bool isPressed = false;

    private void Update()
    {
        if (isPressed)
            return;

        // 키보드 테스트용
        if (Keyboard.current != null && Keyboard.current[keyboardKey].wasPressedThisFrame)
        {
            PressBell();
        }

        // VR 버튼 입력은 나중에 XR Input Action과 연결 가능
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!useTriggerPress)
            return;

        if (isPressed)
            return;

        // 손/컨트롤러에 Controller 또는 Hand 태그를 주면 사용 가능
        if (other.CompareTag("Controller") || other.CompareTag("Hand"))
        {
            PressBell();
        }
    }

    public void PressBell()
    {
        if (isPressed)
            return;

        isPressed = true;

        if (introManager != null)
        {
            introManager.PressBell();
        }
    }
}
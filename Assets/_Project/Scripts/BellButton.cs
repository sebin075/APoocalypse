using UnityEngine;
using UnityEngine.InputSystem;

public class BellButton : MonoBehaviour
{
    [Header("Intro Manager")]
    [Tooltip("벨을 누르면 인트로 진행을 넘겨줄 IntroManager")]
    [SerializeField] private IntroManager introManager;

    [Header("Visual Feedback")]
    [Tooltip("벨 버튼의 MeshRenderer")]
    [SerializeField] private MeshRenderer bellRenderer;

    [Tooltip("벨을 눌렀을 때 바뀔 색상")]
    [SerializeField] private Color pressedColor = Color.red;

    [Header("Input")]
    [Tooltip("키보드 테스트용 입력 키")]
    [SerializeField] private Key keyboardKey = Key.Space;

    [Tooltip("컨트롤러/손이 Trigger에 닿았을 때 벨을 누를지 여부")]
    [SerializeField] private bool useTriggerPress = true;

    [Header("Debug")]
    [SerializeField] private bool isPressed = false;

    private Color originalColor;

    private void Start()
    {
        if (bellRenderer != null)
        {
            originalColor = bellRenderer.material.color;
        }
    }

    private void Update()
    {
        if (isPressed)
            return;

        if (Keyboard.current != null &&
            Keyboard.current[keyboardKey].wasPressedThisFrame)
        {
            PressBell();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!useTriggerPress)
            return;

        if (isPressed)
            return;

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

        if (bellRenderer != null)
        {
            bellRenderer.material.color = pressedColor;
        }

        Debug.Log("[하차벨] 버튼이 눌렸습니다.");

        if (introManager != null)
        {
            introManager.PressBell();
        }
    }
}
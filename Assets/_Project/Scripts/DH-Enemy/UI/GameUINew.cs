using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

public class GameUINew : MonoBehaviour
{
    [Header("손목 UI - 게이지 연결")]
    public Image bowelGaugeFill;
    public TextMeshProUGUI bowelPercentText;

    // [수정 관련 주석] 게이지 수치에 따라 이미지를 교체할 컴포넌트와 스프라이트 변수들을 추가했습니다.
    [Header("단계별 이미지 교체 설정")]
    public Image statusDisplayImage; // 이미지를 보여줄 Image 컴포넌트
    public Sprite stage0_Normal;     // 0% ~ 50% 미만
    public Sprite stage1_Danger;     // 50% ~ 90% 미만
    public Sprite stage2_Disaster;   // 90% 이상 (대참사)

    [Header("게이지 색상 (똥색 설정)")]
    public Color safeColor = new Color(0.6f, 0.4f, 0.1f);
    public Color dangerColor = new Color(0.3f, 0.15f, 0.05f);

    [Header("위치 환경 세팅")]
    public Transform leftController;
    public Transform mainCamera;

    [Header("PC 테스트 화면 설정")]
    public Vector3 pcViewOffset = new Vector3(0f, 0.15f, 0.5f);
    public float pcViewScale = 0.002f;

    [Header("VR 스마트워치 세팅")]
    public float showAngle = 45f;
    [Tooltip("UI 각도를 아래로 내리려면 X값을 조절해보세요 (예: 45 또는 -45)")]
    public Vector3 uiRotationOffset = new Vector3(45f, 0f, 0f);

    private bool isInsideTrigger = false;
    private Vector3 wristLocalPos;
    private Quaternion wristLocalRot;
    private Vector3 wristLocalScale;
    private Canvas myCanvas;

    void Start()
    {
        wristLocalPos = transform.localPosition;
        wristLocalRot = transform.localRotation;
        wristLocalScale = transform.localScale;

        myCanvas = GetComponent<Canvas>();
        if (myCanvas != null) myCanvas.enabled = false;
    }

    void Update()
    {
        // [수정 관련 주석] PC 개발을 위한 게이지 조정 기능입니다. 필요 없으시면 아래줄을 // 로 막으세요.
        // DebugGaugeInput(); 

        UpdateBowelGauge();
        CheckVisibility();
    }

    private void DebugGaugeInput()
    {
        if (PlayerStatus.Instance == null) return;
        float debugSpeed = 0.3f;

#if UNITY_EDITOR || UNITY_STANDALONE
        if (Keyboard.current != null)
        {
            if (Keyboard.current.upArrowKey.isPressed)
                PlayerStatus.Instance.bowelLevel += debugSpeed * Time.deltaTime;
            else if (Keyboard.current.downArrowKey.isPressed)
                PlayerStatus.Instance.bowelLevel -= debugSpeed * Time.deltaTime;

            PlayerStatus.Instance.bowelLevel = Mathf.Clamp(PlayerStatus.Instance.bowelLevel, 0f, 1f);
        }
#endif
    }

    private void UpdateBowelGauge()
    {
        if (PlayerStatus.Instance == null) return;
        float currentBowel = PlayerStatus.Instance.bowelLevel;

        // 1. 기존 게이지 및 텍스트 업데이트
        if (bowelGaugeFill != null)
        {
            bowelGaugeFill.fillAmount = currentBowel;
            bowelGaugeFill.color = Color.Lerp(safeColor, dangerColor, currentBowel);
        }
        if (bowelPercentText != null)
        {
            bowelPercentText.text = $"{(int)(currentBowel * 100)}%";
        }

        // [수정 관련 주석] 게이지 수치(0~1)에 따라 이미지를 단계별로 교체합니다.
        if (statusDisplayImage != null)
        {
            if (currentBowel < 0.5f)
                statusDisplayImage.sprite = stage0_Normal;    // 0~50%
            else if (currentBowel < 0.9f)
                statusDisplayImage.sprite = stage1_Danger;    // 50~90%
            else
                statusDisplayImage.sprite = stage2_Disaster;  // 90~100%
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("MainCamera")) isInsideTrigger = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("MainCamera")) isInsideTrigger = false;
    }

    private void CheckVisibility()
    {
        if (leftController == null || mainCamera == null || myCanvas == null) return;

        bool isCPressed = Keyboard.current != null && Keyboard.current.cKey.isPressed;
        if (Gamepad.current != null && Gamepad.current.buttonNorth.isPressed) isCPressed = true;

        if (isCPressed)
        {
            myCanvas.enabled = true;
            transform.position = mainCamera.position
                               + (mainCamera.right * pcViewOffset.x)
                               + (mainCamera.up * pcViewOffset.y)
                               + (mainCamera.forward * pcViewOffset.z);
            transform.rotation = Quaternion.LookRotation(transform.position - mainCamera.position);
            transform.localScale = Vector3.one * pcViewScale;
        }
        else
        {
            transform.localPosition = wristLocalPos;
            transform.localRotation = wristLocalRot * Quaternion.Euler(uiRotationOffset);
            transform.localScale = wristLocalScale;

            if (isInsideTrigger) myCanvas.enabled = true;
            else myCanvas.enabled = false;
        }
    }
}
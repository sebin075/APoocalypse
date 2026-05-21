using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections; // [수정 관련 주석] 기존 코루틴 기능을 유지하기 위해 남겨두었습니다.

public class GameUINew : MonoBehaviour
{
    [Header("손목 UI - 게이지 연결")]
    public Image bowelGaugeFill;
    public TextMeshProUGUI bowelPercentText;

    // [수정 관련 주석] 경고 문구(GaugeTxt2) 관련 변수는 디버깅을 위해 계속 주석 처리해 둡니다.
    // public TextMeshProUGUI statusMessageText;

    [Header("게이지 색상 (똥색 설정)")]
    public Color safeColor = new Color(0.6f, 0.4f, 0.1f);
    public Color dangerColor = new Color(0.3f, 0.15f, 0.05f);

    [Header("위치 환경 세팅")]
    public Transform leftController;
    public Transform mainCamera;

    [Header("PC 테스트 화면 설정")]
    public Vector3 pcViewOffset = new Vector3(0f, 0.15f, 0.5f);
    public float pcViewScale = 0.002f;

    // [수정 관련 주석] 콜리더 방식을 사용하므로 showAngle(각도) 변수는 기능하지 않지만, 혹시 몰라 값은 남겨두었습니다.
    [Header("VR 스마트워치 세팅")]
    public float showAngle = 45f;
    [Tooltip("UI 각도를 아래로 내리려면 X값을 조절해보세요 (예: 45 또는 -45)")]
    public Vector3 uiRotationOffset = new Vector3(45f, 0f, 0f);

    // [수정 관련 주석] 손목이 내 몸 안쪽(콜리더 영역)으로 들어왔는지 확인하는 상태 변수입니다.
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
        DebugGaugeInput();
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

        if (Gamepad.current != null)
        {
            if (Gamepad.current.dpad.up.isPressed)
                PlayerStatus.Instance.bowelLevel += debugSpeed * Time.deltaTime;
            else if (Gamepad.current.dpad.down.isPressed)
                PlayerStatus.Instance.bowelLevel -= debugSpeed * Time.deltaTime;

            PlayerStatus.Instance.bowelLevel = Mathf.Clamp(PlayerStatus.Instance.bowelLevel, 0f, 1f);
        }
#endif
    }

    private void UpdateBowelGauge()
    {
        if (PlayerStatus.Instance == null) return;
        float currentBowel = PlayerStatus.Instance.bowelLevel;

        if (bowelGaugeFill != null)
        {
            bowelGaugeFill.fillAmount = currentBowel;
            bowelGaugeFill.color = Color.Lerp(safeColor, dangerColor, currentBowel);
        }
        if (bowelPercentText != null)
        {
            bowelPercentText.text = $"{(int)(currentBowel * 100)}%";
        }
    }

    // [수정 관련 주석] 박스 콜리더 영역(가슴/얼굴 앞)에 손목 UI가 닿았을 때 자동으로 실행되는 함수입니다.
    // [상황 설명 주석] 손을 몸 안쪽으로 당기면 이 함수가 실행되어 isInsideTrigger가 true로 바뀝니다.
    private void OnTriggerEnter(Collider other)
    {
        // 부딪힌 대상의 태그가 "MainCamera"인지 확인합니다. (에디터 세팅 필요)
        if (other.CompareTag("MainCamera"))
        {
            isInsideTrigger = true;
        }
    }

    // [수정 관련 주석] 손목 UI가 박스 콜리더 영역 밖으로 나갔을 때 실행됩니다.
    // [상황 설명 주석] 손을 다시 내리거나 바깥으로 뻗으면 UI가 꺼지도록 false로 바꿉니다.
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("MainCamera"))
        {
            isInsideTrigger = false;
        }
    }

    private void CheckVisibility()
    {
        if (leftController == null || mainCamera == null || myCanvas == null) return;

        bool isCPressed = Keyboard.current != null && Keyboard.current.cKey.isPressed;

        if (Gamepad.current != null && Gamepad.current.buttonNorth.isPressed)
        {
            isCPressed = true;
        }

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

            // [수정 관련 주석] 기존의 복잡했던 각도 계산 로직을 싹 지우고, 콜리더 안에 들어왔는지 여부만으로 캔버스를 켭니다.
            if (isInsideTrigger)
            {
                myCanvas.enabled = true;
            }
            else
            {
                myCanvas.enabled = false;
            }
        }
    }
}
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

/// <summary>
/// 플레이어의 장 게이지(bowelLevel)를 VR 손목 UI 또는 PC 테스트 화면에 표시하는 클래스입니다.
/// VR에서는 왼쪽 컨트롤러에 부착된 Canvas를 손목 방향을 확인해 표시/숨김 처리합니다.
/// PC에서는 C키 또는 게임패드 Y버튼으로 화면 앞에 UI를 띄울 수 있습니다.
/// </summary>
public class GameUINew : MonoBehaviour
{
    [Header("손목 UI - 게이지 연결")]
    // 장 게이지의 fillAmount를 제어할 Image 컴포넌트
    public Image bowelGaugeFill;
    // 게이지 수치를 퍼센트(%)로 표시할 텍스트 컴포넌트
    public TextMeshProUGUI bowelPercentText;

    // ──────────────────────────────────────────────────────────────
    // [제거된 항목 1] 단계별 이미지 교체 설정
    //   제거 이유: 기획 변경으로 이미지 스프라이트 교체 기능을 사용하지 않음
    // ──────────────────────────────────────────────────────────────

    // ──────────────────────────────────────────────────────────────
    // [제거된 항목 2] 게이지 색상 (똥색 설정)
    //   제거 이유: 색상 Lerp 연출을 사용하지 않기로 결정
    // ──────────────────────────────────────────────────────────────

    [Header("위치 환경 세팅")]
    // 왼쪽 컨트롤러 Transform (UI가 손목에 부착될 기준 오브젝트)
    public Transform leftController;
    // 메인 카메라 Transform (시야 방향 및 PC 모드 UI 위치 계산에 사용)
    public Transform mainCamera;

    [Header("PC 테스트 화면 설정")]
    // 카메라 기준 UI 오프셋: x=좌우, y=상하, z=앞뒤 거리
    public Vector3 pcViewOffset = new Vector3(0f, 0.15f, 0.5f);
    // PC 모드에서 UI의 월드 스케일 (너무 크지 않도록 작게 설정)
    public float pcViewScale = 0.002f;

    [Header("VR 스마트워치 세팅")]
    // 손목 UI를 표시할 최대 각도 (현재 코드에서 직접 사용되지 않으나 확장용으로 보존)
    public float showAngle = 45f;
    [Tooltip("UI 각도를 아래로 내리려면 X값을 조절해보세요 (예: 45 또는 -45)")]
    // 손목에 부착된 UI의 회전 보정값 (기본값: X축 45도 기울임)
    public Vector3 uiRotationOffset = new Vector3(45f, 0f, 0f);

    // 메인 카메라 콜라이더가 이 오브젝트의 Trigger 범위 안에 있는지 여부
    private bool isInsideTrigger = false;

    // Start() 시점에 저장해두는 손목 UI의 기본 로컬 위치/회전/스케일
    private Vector3 wristLocalPos;
    private Quaternion wristLocalRot;
    private Vector3 wristLocalScale;

    // 표시 여부를 제어할 Canvas 컴포넌트
    private Canvas myCanvas;

    /// <summary>
    /// 초기화: 손목 기본 Transform 값을 저장하고, Canvas를 비활성화 상태로 시작합니다.
    /// </summary>
    void Start()
    {
        // 씬 로드 시점의 로컬 Transform을 손목 기준으로 저장
        wristLocalPos = transform.localPosition;
        wristLocalRot = transform.localRotation;
        wristLocalScale = transform.localScale;

        // Canvas를 시작 시 숨김 처리
        myCanvas = GetComponent<Canvas>();
        if (myCanvas != null) myCanvas.enabled = false;
    }

    /// <summary>
    /// 매 프레임 게이지 업데이트와 UI 표시 여부를 갱신합니다.
    /// </summary>
    void Update()
    {
        // PlayerStatus의 bowelLevel을 읽어 UI를 갱신 및 100% 시 숨김 처리
        UpdateBowelGauge();

        // 손목 각도 또는 PC 입력에 따라 Canvas 표시/숨김 처리
        CheckVisibility();
    }

    /// <summary>
    /// PlayerStatus.bowelLevel 값을 읽어 게이지 UI(fillAmount, 퍼센트 텍스트)를 업데이트합니다.
    /// 100%에 도달하면 UI 게임오브젝트를 아예 꺼버립니다.
    /// </summary>
    private void UpdateBowelGauge()
    {
        if (PlayerStatus.Instance == null) return;

        // 현재 장 수치 (0.0 ~ 1.0)
        float currentBowel = PlayerStatus.Instance.bowelLevel;

        // --- 핵심 추가 로직: 100% 이상이면 아예 끄기 ---
        if (currentBowel >= 1f)
        {
            // 게이지 바와 텍스트의 게임 오브젝트를 완전히 비활성화 (화면에서 제거)
            if (bowelGaugeFill != null) bowelGaugeFill.gameObject.SetActive(false);
            if (bowelPercentText != null) bowelPercentText.gameObject.SetActive(false);

            // 더 이상 업데이트할 필요가 없으므로 아래 로직 생략
            return;
        }
        else
        {
            // 100% 미만일 때는 다시 게임 오브젝트 활성화 (게이지가 다시 줄어들 경우를 대비)
            if (bowelGaugeFill != null && !bowelGaugeFill.gameObject.activeSelf)
                bowelGaugeFill.gameObject.SetActive(true);

            if (bowelPercentText != null && !bowelPercentText.gameObject.activeSelf)
                bowelPercentText.gameObject.SetActive(true);
        }
        // ------------------------------------------------

        // 게이지 바 fill 비율 업데이트
        if (bowelGaugeFill != null)
        {
            bowelGaugeFill.fillAmount = currentBowel;
        }

        // 퍼센트 텍스트 업데이트 (소수점 버림)
        if (bowelPercentText != null)
        {
            bowelPercentText.text = $"{(int)(currentBowel * 100)}%";
        }
    }

    /// <summary>
    /// 메인 카메라 콜라이더가 이 Trigger 영역에 진입하면 isInsideTrigger를 true로 설정합니다.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("MainCamera")) isInsideTrigger = true;
    }

    /// <summary>
    /// 메인 카메라 콜라이더가 이 Trigger 영역을 벗어나면 isInsideTrigger를 false로 설정합니다.
    /// </summary>
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("MainCamera")) isInsideTrigger = false;
    }

    /// <summary>
    /// PC 모드(C키 / 게임패드 Y버튼)와 VR 손목 모드를 구분하여 Canvas 표시 여부와 UI Transform을 결정합니다.
    /// </summary>
    private void CheckVisibility()
    {
        if (leftController == null || mainCamera == null || myCanvas == null) return;

        // 장 게이지가 100%(1.0) 이상 다 찼는지 확인
        bool isGaugeFull = PlayerStatus.Instance != null && PlayerStatus.Instance.bowelLevel >= 1f;

        bool isCPressed = Keyboard.current != null && Keyboard.current.cKey.isPressed;
        if (Gamepad.current != null && Gamepad.current.buttonNorth.isPressed) isCPressed = true;

        if (isCPressed)
        {
            // ── PC 모드 ──────────────────────────────────────────
            // 다 찼을 때는 캔버스 자체도 렌더링하지 않음
            myCanvas.enabled = !isGaugeFull;

            transform.position = mainCamera.position
                               + (mainCamera.right * pcViewOffset.x)
                               + (mainCamera.up * pcViewOffset.y)
                               + (mainCamera.forward * pcViewOffset.z);

            transform.rotation = Quaternion.LookRotation(transform.position - mainCamera.position);
            transform.localScale = Vector3.one * pcViewScale;
        }
        else
        {
            // ── VR 손목 모드 ─────────────────────────────────────
            transform.localPosition = wristLocalPos;
            transform.localRotation = wristLocalRot * Quaternion.Euler(uiRotationOffset);
            transform.localScale = wristLocalScale;

            // 시선이 손목(Trigger)을 향하고 있고, 게이지가 다 차지 않았을 때만 캔버스 활성화
            myCanvas.enabled = isInsideTrigger && !isGaugeFull;
        }
    }
}
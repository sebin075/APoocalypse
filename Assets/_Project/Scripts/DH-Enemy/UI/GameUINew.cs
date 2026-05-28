using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

/*
 * [상황 설명 주석]
 * 플레이어가 휠체어를 끌고 있어 손을 자유롭게 쓰기 힘든 기획에 맞춰, 
 * 고개를 숙여 하단의 태블릿을 보는 방식으로 UI 활성화 조건 및 컨셉을 변경했습니다.
 * 기존의 불편했던 바닥 기준(Vector3.down) 각도 계산을 폐기하고,
 * 정면(수평)을 기준으로 고개를 '아래로 몇 도 숙였는지' 직관적으로 계산하도록 수정했습니다.
 * - PC 모드: C키를 누른 상태에서 일정 각도 이상 고개를 숙여야 태블릿(게이지)이 보입니다.
 * - VR 모드: 기기를 착용한 채로 고개를 살짝(예: 25도 이상) 숙이면 자연스럽게 태블릿이 켜집니다.
 */

/// <summary>
/// 플레이어의 장 게이지(bowelLevel)를 하단의 태블릿(VR) 또는 PC 테스트 화면에 표시하는 클래스입니다.
/// VR에서는 휠체어를 끄는 상황을 고려하여, 머리(카메라)를 아래로 숙인 각도를 확인해 태블릿 UI를 표시/숨김 처리합니다.
/// PC에서는 C키를 누른 상태로 시선을 내리면 화면 앞에 태블릿 UI를 띄울 수 있습니다.
/// </summary>
public class GameUINew : MonoBehaviour
{
    [Header("태블릿 UI - 게이지 연결")]
    // 장 게이지의 fillAmount를 제어할 Image 컴포넌트
    public Image bowelGaugeFill;
    [Tooltip("게이지의 배경 이미지 또는 프레임 GameObject를 할당하세요. (예: GaugeBG)")]
    public GameObject gaugeBackgroundObject;
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
    // 왼쪽 컨트롤러 Transform (현재는 UI가 부착될 위치 기준 오브젝트로 사용)
    public Transform leftController;
    // 메인 카메라 Transform (시야 방향 측정 및 PC 모드 태블릿 UI 위치 계산에 사용)
    public Transform mainCamera;

    [Header("PC 테스트 화면 설정")]
    // 카메라 기준 UI 오프셋: x=좌우, y=상하, z=앞뒤 거리
    public Vector3 pcViewOffset = new Vector3(0f, 0.15f, 0.5f);
    // PC 모드에서 UI의 월드 스케일 (너무 크지 않도록 작게 설정)
    public float pcViewScale = 0.002f;

    [Header("태블릿(시선) 활성화 설정")]
    [Tooltip("수평(정면)을 기준으로 고개를 '아래로' 몇 도 이상 숙였을 때 태블릿 UI를 활성화할지 설정 (예: 25도). 65도 이하로 숙이면 보여지게 하는 것과 동일한 효과 (90 - 65 = 25).")]
    public float lookDownThreshold = 25.0f;
    
    [Tooltip("UI 각도를 아래로 내리려면 X값을 조절해보세요 (예: 45 또는 -45)")]
    // 태블릿 UI의 회전 보정값 (기본값: X축 45도 기울임)
    public Vector3 uiRotationOffset = new Vector3(45f, 0f, 0f);

    [Header("Hierarchy Toggle (Optional)")]
    [Tooltip("UI 전체를 하이라키에서 완전 비활성화하려면, UI를 담은 GameObject를 할당하세요. 이 스크립트가 붙은 게임오브젝트와는 다른 객체여야 합니다.")]
    // 한국어: 하이라키에서 완전히 꺼버리고 싶을 때 할당하세요.
    // 예: 에디터에서 텍스처를 확인하거나 작업할 때 UI가 눈에 띄면 거슬리므로
    // 해당 UI를 포함한 최상위 GameObject를 이 필드에 넣으면 이 스크립트가
    // Show/Hide 시 `uiRoot.SetActive(true/false)`로 완전 비활성화합니다.
    // 주의: 이 스크립트가 붙은 오브젝트와 동일한 오브젝트를 할당하면 내부 분기를 사용하지 않습니다.
    public GameObject uiRoot;

    // uiRoot를 사용할지 여부(할당되어 있고 이 스크립트가 붙은 오브젝트와 다를 때만 true)
    private bool useUiRoot = false;

    // Start() 시점에 저장해두는 태블릿(기존 손목) UI의 기본 로컬 위치/회전/스케일
    private Vector3 wristLocalPos;
    private Quaternion wristLocalRot;
    private Vector3 wristLocalScale;

    // 표시 여부를 제어할 Canvas 컴포넌트
    private Canvas myCanvas;
    
    [Header("Debug")]
    [Tooltip("Enable to log gauge values (useful to see true fillAmount vs visual).")]
    public bool debugGauge = false;
    [Tooltip("PC 모드에서 현재 고개 숙인 각도를 표시할 TextMeshProUGUI 컴포넌트.")]
    public TextMeshProUGUI angleDebugText;

    /// <summary>
    /// 초기화: 태블릿 기본 Transform 값을 저장하고, Canvas를 비활성화 상태로 시작합니다.
    /// </summary>
    void Start()
    {
        // 씬 로드 시점의 로컬 Transform을 저장 (기존 wrist 변수명 유지)
        wristLocalPos = transform.localPosition;
        wristLocalRot = transform.localRotation;
        wristLocalScale = transform.localScale;

        // 한국어: 초기화 처리
        myCanvas = GetComponent<Canvas>();
        useUiRoot = uiRoot != null && uiRoot != this.gameObject && !transform.IsChildOf(uiRoot.transform);
        if (useUiRoot)
        {
            uiRoot.SetActive(false);
        }
        else
        {
            if (myCanvas != null) myCanvas.enabled = false;
        }
    }

    /// <summary>
    /// 매 프레임 게이지 업데이트와 태블릿 UI 표시 여부를 갱신합니다.
    /// </summary>
    void Update()
    {
        UpdateBowelGauge();
        CheckVisibility();
    }

    /// <summary>
    /// PlayerStatus.bowelLevel 값을 읽어 게이지 UI(fillAmount, 퍼센트 텍스트)를 업데이트합니다.
    /// </summary>
    private void UpdateBowelGauge()
    {
        if (PlayerStatus.Instance == null) return;

        // 현재 장 수치 (0.0 ~ 1.0)
        float currentBowel = PlayerStatus.Instance.bowelLevel;

        // 게이지 바 fill 비율 업데이트 (이미지 여백 보정 없이 0.0 ~ 1.0 그대로 적용)
        if (bowelGaugeFill != null) bowelGaugeFill.fillAmount = currentBowel;

        // 퍼센트 텍스트 업데이트 (소수점 1자리 표시)
        if (bowelPercentText != null) bowelPercentText.text = $"{(currentBowel * 100f):F1}%";

        // 디버그: 특정 임계치(예: 85% 이상)에서 실제 값과 이미지 fillAmount를 로그로 출력
        if (debugGauge && currentBowel >= 0.85f && bowelGaugeFill != null)
        {
            string spriteName = bowelGaugeFill != null && bowelGaugeFill.sprite != null ? bowelGaugeFill.sprite.name : "(none)";
            float fillAmt = bowelGaugeFill != null ? bowelGaugeFill.fillAmount : -1f;
            Debug.Log($"[GaugeDebug] bowelLevel={currentBowel:F3}, fillAmount={fillAmt:F3}, sprite={spriteName}");
        }
    }

    /// <summary>
    /// PC 모드(C키)와 VR 시선 모드를 구분하여 태블릿 Canvas 표시 여부와 UI Transform을 결정합니다.
    /// </summary>
    private void CheckVisibility()
    {
        if (leftController == null || mainCamera == null || (!useUiRoot && myCanvas == null)) return;

        bool isGaugeFull = PlayerStatus.Instance != null && Mathf.Approximately(PlayerStatus.Instance.bowelLevel, 1f);

        if (isGaugeFull || (PlayerStatus.Instance != null && PlayerStatus.Instance.isGameOver))
        { // UI를 강제로 비활성화한 뒤, 아래의 PC 배치나 VR 손목 감지 연산을 일절 타지 않고 즉시 종료합니다.
            if (useUiRoot)
                uiRoot.SetActive(false);
            else
                if (myCanvas != null) myCanvas.enabled = false;

            return; 
        }
        
        // ------------------------------------------------
        // [수정 관련 주석] 직관적인 고개 숙임(Tilt Down) 각도 계산 로직으로 교체
        // 카메라의 수평 방향 벡터(y=0)를 구하여 기준점(0도)으로 삼습니다.
        Vector3 flatCamForward = new Vector3(mainCamera.forward.x, 0f, mainCamera.forward.z).normalized;
        float currentTiltDownAngle = 0f;

        // 시선이 수평 기준선보다 '아래(y < 0)'를 향할 때만 각도를 계산합니다. (위로 쳐다보는 경우는 무시)
        if (mainCamera.forward.y < 0)
        {
            // 수평선(flatCamForward)과 현재 시선(mainCamera.forward) 사이의 각도를 구합니다.
            currentTiltDownAngle = Vector3.Angle(flatCamForward, mainCamera.forward);
        }

        // 숙인 각도가 설정한 기준치(예: 25도) 이상이면 true
        bool isTilted = currentTiltDownAngle >= lookDownThreshold;
        // ------------------------------------------------

        // 2. 입력 체크
        bool isCPressed = (Keyboard.current != null && Keyboard.current.cKey.isPressed) || 
                          (Gamepad.current != null && Gamepad.current.buttonNorth.isPressed);

        bool canvasOn = false;
        bool gaugeVisible = false;
        bool debugTextVisible = false;

        if (isCPressed) // PC 모드
        {
            canvasOn = true;           
            debugTextVisible = true;   
            gaugeVisible = isTilted;   // 각도 기준(예: 바닥 기준 65도)을 만족해야 게이지 요소 활성화

            // 현재 고개 숙인 각도를 표시
            if (angleDebugText != null) angleDebugText.text = $"Tilt Down: {currentTiltDownAngle:F1}°";
        }
        else // VR 모드 (C키를 누르지 않았을 때)
        {
            canvasOn = isTilted;       
            gaugeVisible = isTilted;
            debugTextVisible = false;  
        }

        // 3. UI 활성화 적용
        if (useUiRoot) uiRoot.SetActive(canvasOn);
        else if (myCanvas != null) myCanvas.enabled = canvasOn;

        // 4. 내부 요소 가시성 조절
        if (canvasOn)
        {   
            // 게이지 배경 오브젝트가 명시적으로 할당되어 있다면 그것을 제어합니다.
            if (gaugeBackgroundObject != null)
                gaugeBackgroundObject.SetActive(gaugeVisible);
            // 그렇지 않다면, bowelGaugeFill의 부모 오브젝트를 게이지 배경으로 가정하고 제어합니다.
            // (이전 코드의 동작 방식)
            else if (bowelGaugeFill != null && bowelGaugeFill.transform.parent != null)
                bowelGaugeFill.transform.parent.gameObject.SetActive(gaugeVisible);
            
            if (bowelPercentText != null) bowelPercentText.gameObject.SetActive(gaugeVisible);
            if (angleDebugText != null) angleDebugText.gameObject.SetActive(debugTextVisible);

            // 5. 위치 업데이트
            if (isCPressed)
            {
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
            }
        }
    }
}
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

    // Start() 시점에 저장해두는 손목 UI의 기본 로컬 위치/회전/스케일
    private Vector3 wristLocalPos;
    private Quaternion wristLocalRot;
    private Vector3 wristLocalScale;

    // 표시 여부를 제어할 Canvas 컴포넌트
    private Canvas myCanvas;
    [Header("Debug")]
    [Tooltip("Enable to log gauge values (useful to see true fillAmount vs visual).")]
    public bool debugGauge = false;

    /// <summary>
    /// 초기화: 손목 기본 Transform 값을 저장하고, Canvas를 비활성화 상태로 시작합니다.
    /// </summary>
    void Start()
    {
        // 씬 로드 시점의 로컬 Transform을 손목 기준으로 저장
        wristLocalPos = transform.localPosition;
        wristLocalRot = transform.localRotation;
        wristLocalScale = transform.localScale;

        // 한국어: 초기화 처리
        // - uiRoot가 지정되어 있고 이 스크립트와 다른 오브젝트일 경우에는
        //   처음부터 하이라키에서 완전 비활성화하기 위해 uiRoot.SetActive(false) 처리.
        // - 그렇지 않은 경우(기존 방식)에는 Canvas.enabled = false 로 렌더링만 끔.
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
    /// 메인 카메라 관련 콜라이더인지 여부를 판단합니다.
    /// </summary>
    private bool IsCameraCollider(Collider other)
    {
        if (mainCamera == null) return false;
        return other.transform == mainCamera
            || other.transform.IsChildOf(mainCamera)
            || mainCamera.IsChildOf(other.transform);
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

        // --- 핵심 추가 로직: 정확히 100%에 도달했을 때만 하이라키에서 완전 비활성화 ---
        if (Mathf.Approximately(currentBowel, 1f))
        {
            // 한국어: 게이지가 가득 차면 UI를 보이지 않게 함.
            // uiRoot가 설정되어 있으면 하이라키에서 완전 비활성화하여
            // Hierarchy 창에 남아있지 않도록 처리합니다(작업 중 눈에 거슬리지 않음).
            // uiRoot가 없으면 기존처럼 개별 컴포넌트/Canvas만 비활성화합니다.
            if (useUiRoot)
            {
                uiRoot.SetActive(false);
            }
            else
            {
                if (bowelGaugeFill != null) bowelGaugeFill.gameObject.SetActive(false);
                if (bowelPercentText != null) bowelPercentText.gameObject.SetActive(false);
                if (myCanvas != null) myCanvas.enabled = false;
            }

            // 더 이상 업데이트할 필요가 없으므로 아래 로직 생략
            return;
        }
        else
        {
            // 한국어: 100% 미만일 때
            // - uiRoot를 사용중이라면 하이라키 활성화/비활성 관리는 CheckVisibility에서 처리합니다.
            // - uiRoot를 사용하지 않는 경우에는 게이지 이미지와 텍스트 게임오브젝트를 다시 활성화합니다.
            if (!useUiRoot)
            {
                if (bowelGaugeFill != null && !bowelGaugeFill.gameObject.activeSelf)
                    bowelGaugeFill.gameObject.SetActive(true);

                if (bowelPercentText != null && !bowelPercentText.gameObject.activeSelf)
                    bowelPercentText.gameObject.SetActive(true);
            }
        }
        // ------------------------------------------------

        // [수정 관련 주석] 기존에도 fillAmount 방식을 사용하고 있었으므로, 갈색 이미지가 핑크색 외각선 밖으로 삐져나가지 않도록 유니티 UI 내부 fillAmount 연산을 정상 반영합니다.
        // 게이지 바 fill 비율 업데이트
        if (bowelGaugeFill != null)
        {
            // [수정 관련 주석: 현재 이미지 리소스의 우측 여백 한계로 인해 fillAmount가 0.925f일 때 시각적으로 꽉 차 보입니다.
            //  따라서 실제 0.0~1.0의 데이터를 이미지 종횡비 및 여백에 맞춰 최대 0.92489f 범위로 리매핑하여 대입합니다.]
            bowelGaugeFill.fillAmount = currentBowel * 0.92489f;
        }

        // 퍼센트 텍스트 업데이트 (소수점 1자리 표시)
        if (bowelPercentText != null)
        {
            // [수정 관련 주석: 화면에 보이는 텍스트는 실제 데이터 비율 그대로 0% ~ 100% 범위로 온전하게 출력해야 하므로 기존 연산식을 그대로 유지합니다.]
            bowelPercentText.text = $"{(currentBowel * 100f):F1}%";
        }

        // 디버그: 특정 임계치(예: 85% 이상)에서 실제 값과 이미지 fillAmount를 로그로 출력
        if (debugGauge && currentBowel >= 0.85f)
        {
            string spriteName = bowelGaugeFill != null && bowelGaugeFill.sprite != null ? bowelGaugeFill.sprite.name : "(none)";
            float fillAmt = bowelGaugeFill != null ? bowelGaugeFill.fillAmount : -1f;
            Debug.Log($"[GaugeDebug] bowelLevel={currentBowel:F3}, fillAmount={fillAmt:F3}, sprite={spriteName}");
        }
    }

    /// <summary>
    /// 메인 카메라 콜라이더가 이 Trigger 영역에 진입하면 isInsideTrigger를 true로 설정합니다.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (IsCameraCollider(other)) isInsideTrigger = true;
    }

    /// <summary>
    /// 메인 카메라 콜라이더가 이 Trigger 영역을 벗어나면 isInsideTrigger를 false로 설정합니다.
    /// </summary>
    private void OnTriggerExit(Collider other)
    {
        if (IsCameraCollider(other)) isInsideTrigger = false;
    }

    /// <summary>
    /// PC 모드(C키 / 게임패드 Y버튼)와 VR 손목 모드를 구분하여 Canvas 표시 여부와 UI Transform을 결정합니다.
    /// </summary>
    private void CheckVisibility()
    {
        // 한국어: 필수 참조 체크
        // leftController, mainCamera는 반드시 필요하며,
        // myCanvas는 uiRoot를 사용하지 않는 경우에만 필수입니다.
        if (leftController == null || mainCamera == null || (!useUiRoot && myCanvas == null)) return;

        // 장 게이지가 정확히 100%(1.0)인지 확인 (시각적 반올림/정밀도 문제 방지)
        bool isGaugeFull = PlayerStatus.Instance != null && Mathf.Approximately(PlayerStatus.Instance.bowelLevel, 1f);

        bool isCPressed = Keyboard.current != null && Keyboard.current.cKey.isPressed;
        if (Gamepad.current != null && Gamepad.current.buttonNorth.isPressed) isCPressed = true;

        if (isCPressed)
        {
            // ── PC 모드 처리
            // 한국어: PC에서는 C키(또는 게임패드 Y)를 누를 때 카메라 앞에 UI를 띄웁니다.
            // 게이지가 가득 찼다면 uiRoot 또는 Canvas를 비활성화합니다.
            if (useUiRoot)
                uiRoot.SetActive(!isGaugeFull);
            else
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
            // ── VR 손목 모드 처리
            // 한국어: VR에서는 손목에 붙은 원래 로컬 Transform을 복원하고,
            // 플레이어의 카메라가 손목 트리거 안에 있을 때만 UI를 보여줍니다.
            transform.localPosition = wristLocalPos;
            transform.localRotation = wristLocalRot * Quaternion.Euler(uiRotationOffset);
            transform.localScale = wristLocalScale;

            // 시선(카메라)이 손목 트리거 내부에 있고 게이지가 가득 차지 않았을 때만 활성화
            if (useUiRoot)
                uiRoot.SetActive(isInsideTrigger && !isGaugeFull);
            else
                myCanvas.enabled = isInsideTrigger && !isGaugeFull;
        }
    }
}
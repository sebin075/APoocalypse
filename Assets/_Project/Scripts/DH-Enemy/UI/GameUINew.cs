using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections; // [수정 관련 주석] 시간 지연(2.5초 대기) 처리를 위한 코루틴을 쓰기 위해 추가했습니다.

public class GameUINew : MonoBehaviour
{
    [Header("손목 UI - 게이지 연결")]
    public Image bowelGaugeFill;
    public TextMeshProUGUI bowelPercentText;

    [Tooltip("새로 추가하신 상태 메시지 텍스트(GaugeTxt2)를 연결하세요!")]
    public TextMeshProUGUI statusMessageText;

    // 💩 새로 추가된 똥색 컬러 설정 (유니티 인스펙터에서도 수정 가능)
    [Header("게이지 색상 (똥색 설정)")]
    public Color safeColor = new Color(0.6f, 0.4f, 0.1f);      // 연한 황토색 (평온할 때)
    public Color dangerColor = new Color(0.3f, 0.15f, 0.05f); // 짙은 똥색 (위급할 때)

    [Header("위치 환경 세팅")]
    public Transform leftController;
    public Transform mainCamera;

    [Header("PC 테스트 화면 설정")]
    public Vector3 pcViewOffset = new Vector3(0f, 0.15f, 0.5f);
    public float pcViewScale = 0.002f;

    [Header("VR 스마트워치 세팅")]
    public float showAngle = 45f;

    private Vector3 wristLocalPos;
    private Quaternion wristLocalRot;
    private Vector3 wristLocalScale;
    private Canvas myCanvas;

    // [수정 관련 주석] 현재 피격 전용 메시지가 화면에 띄워져 있는지 확인하기 위한 변수입니다.
    private bool isShowingHitMessage = false;

    void Start()
    {
        wristLocalPos = transform.localPosition;
        wristLocalRot = transform.localRotation;
        wristLocalScale = transform.localScale;

        myCanvas = GetComponent<Canvas>();
        if (myCanvas != null) myCanvas.enabled = false;

        // [수정 관련 주석] PlayerStatus의 피격 이벤트(OnPlayerHit)를 구독하여 신호를 받을 준비를 합니다.
        // [상황 설명 주석] 플레이어가 맞을 때마다 OnPlayerTookDamage 함수가 자동으로 실행됩니다.
        if (PlayerStatus.Instance != null)
        {
            PlayerStatus.Instance.OnPlayerHit += OnPlayerTookDamage;
        }
    }

    // [수정 관련 주석] 씬이 넘어가거나 오브젝트가 파괴될 때 이벤트 구독을 해제하여 메모리 누수를 막습니다.
    void OnDestroy()
    {
        if (PlayerStatus.Instance != null)
        {
            PlayerStatus.Instance.OnPlayerHit -= OnPlayerTookDamage;
        }
    }

    void Update()
    {
        // [수정 관련 주석] PC에서 UI 게이지 증감을 즉각적으로 확인하기 위한 디버그용 함수 호출을 추가했습니다.
        DebugGaugeInput();

        UpdateBowelGauge();
        CheckVisibility();
    }

    // [수정 관련 주석] 키보드 위/아래 방향키를 눌러 PlayerStatus.Instance.bowelLevel 값을 임의로 조절하는 함수를 추가했습니다.
    // [상황 설명 주석] PC 테스트 시 'C' 키를 눌러 UI를 화면에 띄운 후, 위 방향키를 누르면 게이지가 차오르고 아래 방향키를 누르면 줄어듭니다. 이를 통해 상태 텍스트 변화를 VR 기기 없이 바로 확인할 수 있습니다.
    private void DebugGaugeInput()
    {
        // PlayerStatus.Instance가 없거나 InputSystem이 비활성화 상태면 작동하지 않도록 예외 처리합니다.
        if (Keyboard.current == null || PlayerStatus.Instance == null) return;

        // [상황 설명 주석] 초당 게이지가 증감하는 속도입니다. 너무 빠르거나 느리면 이 값을 조절하세요.
        float debugSpeed = 0.3f;

#if UNITY_EDITOR || UNITY_STANDALONE
        if (Keyboard.current.upArrowKey.isPressed)
        {
            PlayerStatus.Instance.bowelLevel += debugSpeed * Time.deltaTime;
            PlayerStatus.Instance.bowelLevel = Mathf.Clamp(PlayerStatus.Instance.bowelLevel, 0f, 1f);
        }
        else if (Keyboard.current.downArrowKey.isPressed)
        {
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
            // 💩 초록/빨강 대신 안전한 똥색 -> 위급한 똥색으로 변하도록 수정
            bowelGaugeFill.color = Color.Lerp(safeColor, dangerColor, currentBowel);
        }
        if (bowelPercentText != null)
        {
            bowelPercentText.text = $"{(int)(currentBowel * 100)}%";
        }

        // ⭐️ 여기서 상태 메시지 업데이트!
        // [수정 관련 주석] 피격 전용 메시지가 떠 있을 때는 평상시 대사(UpdateStatusMessage)로 덮어씌우지 않도록 막습니다.
        if (!isShowingHitMessage)
        {
            UpdateStatusMessage(currentBowel);
        }
    }

    // [수정 관련 주석] PlayerStatus에서 보낸 피격 신호(데미지 수치)를 받아 처리하는 함수입니다.
    private void OnPlayerTookDamage(float damage)
    {
        // [상황 설명 주석] 짧은 시간에 여러 대를 맞을 경우를 대비해 기존 코루틴을 끄고 새 메시지를 띄웁니다.
        StopAllCoroutines();
        StartCoroutine(ShowHitMessageCoroutine(damage));
    }

    // [수정 관련 주석] 기획하신 적 종류별 대사를 2.5초간 띄워주는 코루틴입니다.
    private IEnumerator ShowHitMessageCoroutine(float damage)
    {
        isShowingHitMessage = true;

        if (statusMessageText != null)
        {
            // [상황 설명 주석] 넘어온 데미지가 30인지 15인지 5인지에 따라 대사를 다르게 설정합니다.
            if (damage >= 30f)
            {
                statusMessageText.text = "덤프트럭에 치인 기분... 하마터면 강제 배출될 뻔했다...";
                statusMessageText.color = Color.red; // 강렬한 경고
            }
            else if (damage >= 15f)
            {
                statusMessageText.text = "깜짝아! 놀래서 순간 힘 풀릴 뻔했잖아!!";
                statusMessageText.color = new Color(1f, 0.5f, 0f); // 주황색
            }
            else
            {
                // 쓰레기 밟음 (데미지 5f 이하)
                statusMessageText.text = "아씨, 미끌! 힘주며 걷느라 다리가 안 떨어진다...";
                statusMessageText.color = new Color(0.8f, 0.7f, 0.4f); // 탁한 흙빛 경고색
            }
        }

        // 2.5초 동안 대기 (이 시간 동안은 UpdateBowelGauge가 텍스트를 건드리지 못함)
        yield return new WaitForSeconds(2.5f);

        // 시간이 지나면 플래그를 꺼서 다시 평상시 대사가 나오게 함
        isShowingHitMessage = false;
    }

    // ⭐️ K-직장인 심리 상태 텍스트
    private void UpdateStatusMessage(float currentBowel)
    {
        if (statusMessageText == null) return;

        int percent = (int)(currentBowel * 100);

        if (percent < 25)
        {
            statusMessageText.text = "뱃속이 평온하다. 아직은 살만해.";
            statusMessageText.color = Color.white;
        }
        else if (percent < 50)
        {
            statusMessageText.text = "어...? 갑자기 아랫배가 싸한데?";
            statusMessageText.color = Color.yellow; // 노란색 경고
        }
        else if (percent < 75)
        {
            statusMessageText.text = "식은땀이 난다. 괄약근에 힘을 주자.";
            statusMessageText.color = new Color(1f, 0.5f, 0f); // 주황색
        }
        else if (percent < 90)
        {
            statusMessageText.text = "신이시여 제발... 존엄성만은 지키게 해주세요!";
            statusMessageText.color = Color.red; // 빨간색 위급
        }
        else if (percent < 100)
        {
            statusMessageText.text = "아악!! 화장실!! 나 지금 걸음걸이 이상해!!!";
            statusMessageText.color = Color.red;
        }
        else
        {
            statusMessageText.text = "(사회적 죽음을 맞이했습니다...)";
            statusMessageText.color = Color.gray;
        }
    }

    private void CheckVisibility()
    {
        if (leftController == null || mainCamera == null || myCanvas == null) return;

        bool isCPressed = Keyboard.current != null && Keyboard.current.cKey.isPressed;

        // [수정 관련 주석] 기존에도 isLookingAtWrist 변수를 통해 VR 손목 각도를 체크하는 로직이 있었으나, 작동을 더 확실하게 보장하기 위해 위치 복구 로직 밖으로 분리했습니다.
        // [상황 설명 주석] 조이스틱(컨트롤러)의 위쪽(up) 방향과 카메라(유저의 시선) 사이의 각도를 계산하여 45도(showAngle) 이내인지 판별합니다.
        float angleToFace = Vector3.Angle(leftController.up, mainCamera.position - leftController.position);
        bool isLookingAtWrist = angleToFace < showAngle;

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
            // [수정 관련 주석] C키를 누르지 않았을 때는 무조건 원래 조이스틱(손목) 위치와 회전값으로 되돌리도록 변경했습니다.
            // [상황 설명 주석] 이렇게 해야 VR 게임 내에서 혹은 씬 뷰에서 조이스틱을 이리저리 움직여도 UI가 제자리에 잘 붙어있게 됩니다.
            transform.localPosition = wristLocalPos;
            transform.localRotation = wristLocalRot;
            transform.localScale = wristLocalScale;

            // [상황 설명 주석] 조이스틱 각도가 카메라를 향한다면 (VR에서 유저가 시계를 보는 행동을 하면) UI를 켭니다.
            if (isLookingAtWrist)
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
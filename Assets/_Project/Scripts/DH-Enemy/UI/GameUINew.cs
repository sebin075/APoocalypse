using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class GameUI : MonoBehaviour
{
    [Header("손목 UI - 게이지 연결")]
    public Image bowelGaugeFill;
    public TextMeshProUGUI bowelPercentText;

    [Tooltip("새로 추가하신 상태 메시지 텍스트(GaugeTxt2)를 연결하세요!")]
    public TextMeshProUGUI statusMessageText;

    // 💩 새로 추가된 똥색 컬러 설정 (유니티 인스펙터에서도 수정 가능)
    [Header("게이지 색상 (똥색 설정)")]
    public Color safeColor = new Color(0.6f, 0.4f, 0.1f);     // 연한 황토색 (평온할 때)
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
        UpdateBowelGauge();
        CheckVisibility();
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
        UpdateStatusMessage(currentBowel);
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
        else if (isLookingAtWrist)
        {
            myCanvas.enabled = true;
            transform.localPosition = wristLocalPos;
            transform.localRotation = wristLocalRot;
            transform.localScale = wristLocalScale;
        }
        else
        {
            myCanvas.enabled = false;
        }
    }
}
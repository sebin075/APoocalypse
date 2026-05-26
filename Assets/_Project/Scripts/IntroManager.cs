using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class IntroManager : MonoBehaviour
{
    [Header("Position Lock")]
    [Tooltip(
    "OpeningScene 동안 플레이어 위치 이동을 막음\n" +
    "회전은 허용")]
    [SerializeField] private bool lockPlayerPosition = true;

    [Header("Debug Position Lock")]
    [SerializeField] private bool isAutoMoving = false;

    [SerializeField] private Vector3 lockedPosition;

    [Header("Scene Settings")]
    [Tooltip(
        "인트로가 끝난 뒤 이동할 씬 이름\n" +
        "Build Settings에 해당 씬이 반드시 등록되어 있어야 함"
    )]
    [SerializeField] private string nextSceneName = "GameScene";

    [Header("Player / XR Origin")]
    [Tooltip(
        "자동 이동시킬 플레이어 오브젝트\n" +
        "보통 XR Origin을 넣으면 됨"
    )]
    [SerializeField] private Transform xrOrigin;

    [Tooltip(
        "플레이어 이동을 막을 이동 관련 스크립트\n" +
        "예: Continuous Move Provider, PlayerMovement 등\n" +
        "OpeningScene에서는 이동을 막는 것을 추천"
    )]

    [SerializeField]
    private XRPositionLock xrPositionLock;

    [SerializeField] private Behaviour[] movementScripts;

    [Header("Intro Position Points")]
    [Tooltip("버스 앞쪽 시작 위치")]
    [SerializeField] private Transform busStartPoint;

    [Tooltip("버스 뒷문 앞 위치")]
    [SerializeField] private Transform busDoorPoint;

    [Tooltip("버스 밖 하차 위치")]
    [SerializeField] private Transform outsidePoint;

    [Header("Bus / Outside Movement")]
    [Tooltip(
        "창밖 배경 이동 스크립트\n" +
        "없어도 인트로 진행은 가능함"
    )]
    [SerializeField] private MovingOutside movingOutside;

    [Header("Timing Settings")]
    [Tooltip("버스 앞쪽에서 뒷문까지 자동 이동하는 시간")]
    [SerializeField] private float moveToDoorDuration = 4f;

    [Tooltip(
        "뒷문 앞에 도착한 뒤 하차벨 입력을 기다리는 시간\n" +
        "이 시간이 지나면 자동으로 하차벨 처리"
    )]
    [SerializeField] private float autoBellTime = 6f;

    [Tooltip("하차벨을 누른 뒤 버스가 서서히 멈추는 시간")]
    [SerializeField] private float stopDuration = 2f;

    [Tooltip("버스 밖으로 자동 이동하는 시간")]
    [SerializeField] private float exitMoveDuration = 1f;

    [Tooltip("화면이 어두워지는 시간")]
    [SerializeField] private float fadeOutDuration = 1f;

    [Header("UI")]
    [Tooltip("상황 자막 텍스트")]
    [SerializeField] private TextMeshProUGUI subtitleText;

    [Tooltip("하차벨 안내 텍스트")]
    [SerializeField] private TextMeshProUGUI guideText;

    [Tooltip("검은색 전체화면 Image")]
    [SerializeField] private Image fadeImage;

    [Header("Subtitle Lines")]
    [Tooltip("인트로 시작 시 출력될 자막")]
    [SerializeField] private string startSubtitle = "아... 화장실...";

    [Tooltip("벨 입력 대기 중 안내 문구")]
    [SerializeField] private string bellGuideText = "하차벨을 눌러주세요";

    [Tooltip("벨을 눌렀거나 자동 벨 처리될 때 출력될 자막")]
    [SerializeField] private string bellPressedSubtitle = "못 참겠다...";

    [Header("Debug Status")]
    [Tooltip("현재 인트로가 시작되었는지")]
    [SerializeField] private bool introStarted;

    [Tooltip("현재 하차벨 입력 대기 상태인지")]
    [SerializeField] private bool waitingForBell;

    [Tooltip("하차벨이 눌렸는지")]
    [SerializeField] private bool bellPressed;

    [Tooltip("현재 벨 대기 타이머")]
    [SerializeField] private float bellWaitTimer;

    [Tooltip("현재 인트로 종료 연출 중인지")]
    [SerializeField] private bool introEnding;

    private Coroutine introRoutine;

    private void Start()
    {
        // 시작 시 화면은 보이게 설정
        SetFadeAlpha(0f);

        // OpeningScene에서는 플레이어 직접 이동을 막음
        SetMovementScriptsActive(false);

        // 시작 위치로 XR Origin 배치
        if (xrOrigin != null && busStartPoint != null)
        {
            xrOrigin.position = busStartPoint.position;
            xrOrigin.rotation = busStartPoint.rotation;
        }

        // 초기 UI 설정
        if (subtitleText != null)
            subtitleText.text = startSubtitle;

        if (guideText != null)
            guideText.text = "";

        // 인트로 시작
        introRoutine = StartCoroutine(IntroStartRoutine());
        
        // 현재 위치 저장
        if (xrOrigin != null)
        {
            lockedPosition = xrOrigin.position;
        }
    }


    private void Update()
    {
        // 벨 입력 대기 상태가 아니면 타이머를 세지 않음
        if (!waitingForBell || bellPressed || introEnding)
            return;

        bellWaitTimer += Time.deltaTime;

        // 일정 시간 동안 누르지 않으면 자동으로 하차벨 처리
        if (bellWaitTimer >= autoBellTime)
        {
            PressBell();
        }
    }

    private void LateUpdate()
    {
        // 위치 잠금 사용 안 함
        if (!lockPlayerPosition)
            return;

        // 자동 이동 중에는 잠금 해제
        if (isAutoMoving)
            return;

        if (xrOrigin == null)
            return;

        // 위치만 고정
        // 회전은 그대로 허용
        xrOrigin.position = lockedPosition;
    }


    private IEnumerator IntroStartRoutine()
    {
        introStarted = true;

        // 1. 버스 앞쪽에서 뒷문까지 자동 이동
        if (xrOrigin != null && busDoorPoint != null)
        {
            yield return MoveTransform(
                xrOrigin,
                xrOrigin.position,
                busDoorPoint.position,
                xrOrigin.rotation,
                busDoorPoint.rotation,
                moveToDoorDuration
            );
        }

        // 2. 뒷문 도착 후 하차벨 입력 대기
        waitingForBell = true;
        bellWaitTimer = 0f;

        if (guideText != null)
            guideText.text = bellGuideText;
    }

    public void PressBell()
    {
        // 중복 입력 방지
        if (bellPressed || introEnding)
            return;

        bellPressed = true;
        waitingForBell = false;

        if (guideText != null)
            guideText.text = "";

        if (subtitleText != null)
            subtitleText.text = bellPressedSubtitle;

        StartCoroutine(IntroEndRoutine());
    }

    private IEnumerator IntroEndRoutine()
    {
        introEnding = true;

        // 1. 창밖 배경을 감속시켜 버스 정차 느낌 연출
        if (movingOutside != null)
        {
            movingOutside.StopMoving(stopDuration);
        }

        yield return new WaitForSeconds(stopDuration);

        // 2. 플레이어를 버스 밖으로 자동 이동
        if (xrOrigin != null && outsidePoint != null)
        {
            yield return MoveTransform(
                xrOrigin,
                xrOrigin.position,
                outsidePoint.position,
                xrOrigin.rotation,
                outsidePoint.rotation,
                exitMoveDuration
            );
        }

        // 3. 화면 어두워짐
        yield return FadeOut();

        // 4. 씬 전환
        SceneManager.LoadScene(nextSceneName);
    }

    private IEnumerator MoveTransform(
        Transform target,
        Vector3 startPosition,
        Vector3 endPosition,
        Quaternion startRotation,
        Quaternion endRotation,
        float duration
    )
    {
        // 자동 이동 시작
        if (xrPositionLock != null)
        {
            xrPositionLock.SetAutoMoving(true);
        }

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float t = timer / duration;

            target.position = Vector3.Lerp(startPosition, endPosition, t);
            target.rotation = Quaternion.Slerp(startRotation, endRotation, t);

            yield return null;
        }

        target.position = endPosition;
        target.rotation = endRotation;

        // 자동 이동 종료
        if (xrPositionLock != null)
        {
            xrPositionLock.SetAutoMoving(false);
        }
    }

    private IEnumerator FadeOut()
    {
        float timer = 0f;

        while (timer < fadeOutDuration)
        {
            timer += Time.deltaTime;

            float alpha = timer / fadeOutDuration;

            SetFadeAlpha(alpha);

            yield return null;
        }

        SetFadeAlpha(1f);
    }

    private void SetFadeAlpha(float alpha)
    {
        if (fadeImage == null)
            return;

        Color color = fadeImage.color;
        color.a = alpha;
        fadeImage.color = color;
    }

    private void SetMovementScriptsActive(bool active)
    {
        if (movementScripts == null)
            return;

        for (int i = 0; i < movementScripts.Length; i++)
        {
            if (movementScripts[i] != null)
            {
                movementScripts[i].enabled = active;
            }
        }
    }
}
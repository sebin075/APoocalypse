using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BusOpeningManager : MonoBehaviour
{
    [Header("시간 및 씬 설정")]
    [SerializeField] private string mainSceneName = "MainScene";
    [SerializeField] private float autoPressTime = 3.0f;       // 10초부터 13초까지 대기하는 시간 (3초)
    [SerializeField] private float fadeDuration = 1.5f;        // 페이드아웃 걸리는 시간

    [Header("플레이어 및 이동 관련")]
    [SerializeField] private Transform vrOrigin;               // XR Origin 오브젝트
    [SerializeField] private Transform backSeatPosition;       // 1~4초 시작 지점 (버스 뒷자리)
    [SerializeField] private Transform backDoorPosition;       // 4초 시점에 도착할 뒷문 앞 위치
    [SerializeField] private MonoBehaviour moveProvider;       // Continuous Move Provider 등 이동 컴포넌트

    [Header("연출 및 UI 요소")]
    [SerializeField] private AudioSource stomachAudio;         // 꾸르륵 소리 AudioSource
    [SerializeField] private AudioSource bellAudio;            // 띵동~ 벨소리 AudioSource
    [SerializeField] private GameObject guideUI;               // "버튼 누르고 화장실가기" 세계관 UI (World Space Canvas)
    [SerializeField] private Animator busDoorAnimator;         // 버스 뒷문 애니메이터

    private bool isBellPressed = false;
    private Coroutine autoPressCoroutine;

    private void Start()
    {
        // 초기 세팅: 플레이어를 뒷자리에 두고, UI는 꺼둠
        if (vrOrigin != null && backSeatPosition != null)
            vrOrigin.position = backSeatPosition.position;

        if (guideUI != null) guideUI.SetActive(false);

        // 오프닝 시퀀스 시작
        StartCoroutine(OpeningSequence());
    }

    private IEnumerator OpeningSequence()
    {
        // -------------------------------------------------------------
        // [1~4초] 뒷자리에서 꾸르륵 소리나며 뒷문 앞으로 자동 이동
        // -------------------------------------------------------------
        if (moveProvider != null) moveProvider.enabled = false; // 자유 이동 금지
        if (stomachAudio != null) stomachAudio.Play();          // 꾸르륵 소리 재생

        float moveTimer = 0f;
        Vector3 startPos = backSeatPosition.position;
        Vector3 endPos = backDoorPosition.position;

        while (moveTimer < 4.0f)
        {
            moveTimer += Time.deltaTime;
            // 4초 동안 뒷자리에서 뒷문 앞으로 부드럽게 이동 (시야 회전은 VR 헤드셋으로 자유로움)
            if (vrOrigin != null)
                vrOrigin.position = Vector3.Lerp(startPos, endPos, moveTimer / 4.0f);
            yield return null;
        }

        // -------------------------------------------------------------
        // [5~10초] 뒷문 앞 고정 대기 + 가이드 UI 등장
        // -------------------------------------------------------------
        if (vrOrigin != null) vrOrigin.position = backDoorPosition.position; // 위치 고정
        if (guideUI != null) guideUI.SetActive(true);                        // UI 켜기

        // 5초 대기 (5초~10초 구간)
        yield return new WaitForSeconds(5.0f);

        // -------------------------------------------------------------
        // [10~13초] 플레이어가 안 누르면 3초 뒤 강제 하차벨 작동
        // -------------------------------------------------------------
        if (!isBellPressed)
        {
            autoPressCoroutine = StartCoroutine(AutoPressTimer());
        }
    }

    private IEnumerator AutoPressTimer()
    {
        yield return new WaitForSeconds(autoPressTime); // 3초 대기
        if (!isBellPressed)
        {
            Debug.Log("플레이어 입력 없음: 강제 하차벨 작동");
            TriggerStopButton();
        }
    }

    // [정지벨 상호작용 및 강제 발동 시 호출될 함수]
    public void TriggerStopButton()
    {
        if (isBellPressed) return; // 중복 실행 방지
        isBellPressed = true;

        if (autoPressCoroutine != null) StopCoroutine(autoPressCoroutine); // 타이머 중지
        if (guideUI != null) guideUI.SetActive(false);                     // UI 끄기

        StartCoroutine(DoorOpenAndLeaveSequence());
    }

    private IEnumerator DoorOpenAndLeaveSequence()
    {
        // 1. 벨소리 피드백
        if (bellAudio != null) bellAudio.Play();

        // 2. 버스 뒷문 열기 애니메이션 실행 (문 4개가 한 번에 열리는 클립)
        if (busDoorAnimator != null) busDoorAnimator.SetTrigger("Open");
        yield return new WaitForSeconds(1.0f); // 문이 살짝 열리는 시간 대기

        // 3. 계단 밑 탈출 위치로 플레이어 강제 이동 연출
        float exitTimer = 0f;
        Vector3 startPos = vrOrigin.position;
        // 뒷문 앞 포지션에서 살짝 아래+앞쪽(계단 밑)으로 가상의 목적지 설정
        Vector3 exitPos = startPos + (vrOrigin.forward * 1.5f) + (Vector3.down * 1.0f);

        while (exitTimer < 1.0f)
        {
            exitTimer += Time.deltaTime;
            vrOrigin.position = Vector3.Lerp(startPos, exitPos, exitTimer / 1.0f);
            yield return null;
        }

        // 4. 하얀색으로 Fade Out 진행
        VRScreenFade fade = FindObjectOfType<VRScreenFade>();
        if (fade != null) fade.FadeOut(fadeDuration);

        yield return new WaitForSeconds(fadeDuration);

        // 5. 메인 씬 전환
        SceneManager.LoadScene(mainSceneName);
    }
}
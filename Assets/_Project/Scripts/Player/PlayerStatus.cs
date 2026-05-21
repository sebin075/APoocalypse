using UnityEngine;

public class PlayerStatus : MonoBehaviour
{
    // 게임 내 어디서든 이 수치에 접근할 수 있게 해줌 (싱글톤)
    public static PlayerStatus Instance { get; private set; }

    [Header("상태 수치")]
    [Range(0f, 1f)]
    public float bowelLevel = 0.0f; // 변의 (0.0: 평온, 1.0: 바지에 쌈)

    [Header("게임 상태")]
    public bool isGameOver = false; // 게임 오버 상태 확인용

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Update()
    {
        // 이미 게임 오버 상태라면 아래 코드를 실행하지 않고 멈춤
        if (isGameOver) return;

        // (테스트용) 매 프레임마다 변의가 서서히 오릅니다.
        bowelLevel += Time.deltaTime * 0.01f;

        // 게이지가 0(0%) ~ 1(100%) 사이를 벗어나지 않게 고정
        bowelLevel = Mathf.Clamp01(bowelLevel);

        // ⭐️ 100% (1.0) 도달 시 게임 오버 발동!
        if (bowelLevel >= 1f)
        {
            TriggerGameOver();
        }
    }

    // ⭐️ 게임 오버 처리 함수
    private void TriggerGameOver()
    {
        isGameOver = true; // 게임 오버 상태로 변경
        Debug.Log("게임 오버! 사회적 죽음을 맞이했습니다...");

        // ========================================================
        // 🛑 플레이어 움직임 멈추기 (원하는 방식 1개만 주석 해제해서 쓰세요!)
        // ========================================================

        // [방식 1] 게임 전체의 시간을 아예 멈춰버림 (가장 강력하고 확실함)
        // 장점: 몬스터, 타이머 등 모든 게 다 멈춤.
        // 단점: 너무 뚝 끊기는 느낌이 들 수 있음.
        Time.timeScale = 0f;

        // [방식 2] 플레이어의 이동 스크립트만 꺼버림 (추천!)
        // 장점: 걸어갈 수는 없지만, 고개를 돌려 자신의 참상(?)이나 하늘을 바라볼 수는 있어서 더 비참하고 리얼함.
        // (사용법: 아래 코드 맨 앞의 슬래시 '//' 두 개를 지우고, Time.timeScale = 0f; 앞에는 '//'를 붙여서 끄세요)

        // GetComponent<PlayerMovement>().enabled = false;
    }
}
using UnityEngine;
using System; // [수정 관련 주석] 이벤트(Action)를 사용하기 위해 System 네임스페이스를 추가했습니다.

public class PlayerStatus : MonoBehaviour
{
    // 게임 내 어디서든 이 수치에 접근할 수 있게 해줌 (싱글톤)
    public static PlayerStatus Instance { get; private set; }

    [Header("상태 수치")]
    [Range(0f, 1f)]
    public float bowelLevel = 0.0f; // 변의 (0.0: 평온, 1.0: 바지에 쌈)

    [Header("게임 상태")]
    public bool isGameOver = false; // 게임 오버 상태 확인용

    // [수정 관련 주석] 피격 시 스마트워치 UI 등에 신호를 보내기 위한 이벤트(Action) 변수를 추가했습니다.
    // [상황 설명 주석] 이 이벤트를 통해 데미지 값(30, 15, 5)을 UI로 전달하여 어떤 적에게 맞았는지 구분하게 합니다.
    public event Action<float> OnPlayerHit;

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

    // ========================================================
    // [수정 관련 주석] 기존의 데미지 및 디버프 관련 코드를 하나로 합쳤습니다.
    // ========================================================

    // [수정 관련 주석] 플레이어가 데미지를 받을 때 실제 변의(bowelLevel)가 증가하고 UI에 신호를 보내도록 수정했습니다.
    public void TakeDamage(float damage)
    {
        Debug.Log("Damage: " + damage);

        // [상황 설명 주석] 데미지가 30이면 0.3(30%), 15면 0.15(15%)만큼 게이지를 즉시 증가시킵니다.
        bowelLevel += (damage / 100f);
        bowelLevel = Mathf.Clamp01(bowelLevel);

        // [상황 설명 주석] UI 스크립트에게 방금 맞은 데미지 수치와 함께 피격 사실을 알립니다.
        OnPlayerHit?.Invoke(damage);
    }

    // [수정 관련 주석] 쓰레기 등으로 인해 이동 속도가 느려지는 디버프 함수입니다.
    public void ApplySlowDebuff(float slowAmount, float duration)
    {
        Debug.Log("Trash Debuff: " + slowAmount + " / " + duration);
    }
}
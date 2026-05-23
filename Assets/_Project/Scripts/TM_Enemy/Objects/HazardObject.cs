using UnityEngine;

public class HazardObject : MonoBehaviour
{
    [Header("Hazard Type")]
    [Tooltip("오브젝트 종류\nNormalZombie=일반 좀비\nBigZombie=대형 좀비\nTrash=쓰레기\n※ 프리팹별로 반드시 맞게 설정")]
    [SerializeField] private HazardType hazardType;

    [Header("Damage Settings")]
    [Tooltip("플레이어 충돌 데미지\n일반 좀비 추천: 12~15\n대형 좀비 추천: 25~30\n쓰레기 추천: 3~5")]
    [Range(0f, 100f)]
    [SerializeField] private float damage = 10f;

    [Header("Trash Debuff Settings")]
    [Tooltip("쓰레기 충돌 시 이동속도 감소량\n0.2 = 20% 감소\n0.3 = 30% 감소")]
    [Range(0f, 1f)]
    [SerializeField] private float slowAmount = 0.2f;

    [Tooltip("쓰레기 디버프 지속 시간\n추천: 3~5초")]
    [Range(0f, 10f)]
    [SerializeField] private float debuffDuration = 5f;

    [Header("Destroy Settings")]
    [Tooltip("플레이어와 충돌 후 제거 여부\n현재 게임에서는 ON 추천")]
    [SerializeField] private bool destroyOnHit = true;

    [Header("Debug Settings")]
    [Tooltip("Console 로그 출력 여부\n개발 중 ON, 최종 빌드 전 OFF 추천")]
    [SerializeField] private bool showDebugLog = true;

    [Header("Debug Status")]
    [Tooltip("이미 충돌 처리되었는지 확인")]
    [SerializeField] private bool hasHit = false;

    [Tooltip("마지막으로 충돌한 오브젝트 이름")]
    [SerializeField] private string lastHitObjectName;

    private void OnTriggerEnter(Collider other)
    {
        if (hasHit)
            return;

        // XR 구조에서는 Collider가 자식에 있을 수 있으므로 부모에서 PlayerStatus 검색
        PlayerStatus playerStatus = other.GetComponentInParent<PlayerStatus>();

        if (playerStatus == null)
            return;

        hasHit = true;
        lastHitObjectName = other.name;

        if (showDebugLog)
        {
            Debug.Log($"{hazardType} Hit Player / Damage: {damage}");
        }

        // 데미지 적용
        playerStatus.TakeDamage(damage);

        // 쓰레기일 때만 디버프 적용
        if (hazardType == HazardType.Trash)
        {
            playerStatus.ApplySlowDebuff(slowAmount, debuffDuration);

            if (showDebugLog)
            {
                Debug.Log($"Trash Debuff Applied / Slow: {slowAmount} / Duration: {debuffDuration}");
            }
        }

        if (destroyOnHit)
        {
            Destroy(gameObject);
        }
    }
}
using UnityEngine;

public class HazardObject : MonoBehaviour
{
    [Header("Hazard Type")]
    [Tooltip(
        "오브젝트 종류 설정\n" +
        "NormalZombie = 일반 좀비\n" +
        "BigZombie = 대형 좀비\n" +
        "Trash = 쓰레기"
    )]
    [SerializeField]
    private HazardType hazardType;

    [Header("Damage Settings")]
    [Tooltip(
        "플레이어에게 줄 데미지\n" +
        "일반 좀비 추천 : 12~15\n" +
        "대형 좀비 추천 : 25~30\n" +
        "쓰레기 추천 : 3~5"
    )]
    [Range(0f, 100f)]
    [SerializeField]
    private float damage = 10f;

    [Header("Trash Debuff Settings")]
    [Tooltip(
        "쓰레기 충돌 시 이동속도 감소량\n" +
        "0.2 = 20% 감소"
    )]
    [Range(0f, 1f)]
    [SerializeField]
    private float slowAmount = 0.2f;

    [Tooltip(
        "쓰레기 디버프 지속 시간\n" +
        "추천 : 3~5초"
    )]
    [Range(0f, 10f)]
    [SerializeField]
    private float debuffDuration = 5f;

    [Header("Destroy Settings")]
    [Tooltip("플레이어와 충돌 후 오브젝트 제거 여부")]
    [SerializeField]
    private bool destroyOnHit = true;

    [Header("Debug Settings")]
    [Tooltip("Console에 충돌 로그를 출력할지 여부")]
    [SerializeField]
    private bool showDebugLog = true;

    [Header("Debug Status")]
    [Tooltip("이미 플레이어와 충돌했는지 확인")]
    [SerializeField]
    private bool hasHit = false;

    [Tooltip("마지막으로 충돌한 오브젝트 이름")]
    [SerializeField]
    private string lastHitObjectName;

    private void OnTriggerEnter(Collider other)
    {
        // 이미 충돌 처리된 경우 중복 처리 방지
        if (hasHit)
            return;

        // 충돌한 오브젝트 또는 부모에서 PlayerStatus 찾기
        // XR 구조에서는 실제 Collider가 자식 오브젝트에 있을 수 있음
        PlayerStatus playerStatus =
            other.GetComponentInParent<PlayerStatus>();

        // PlayerStatus가 없으면 플레이어가 아니므로 무시
        if (playerStatus == null)
            return;

        // 충돌 처리 완료 표시
        hasHit = true;

        // Debug Status용 이름 저장
        lastHitObjectName = other.name;

        // 충돌 로그 출력
        if (showDebugLog)
        {
            Debug.Log(
                $"{hazardType} Hit Player / Damage : {damage}"
            );
        }

        // 플레이어에게 데미지 적용
        playerStatus.TakeDamage(damage);

        // 쓰레기일 경우에만 디버프 적용
        if (hazardType == HazardType.Trash)
        {
            playerStatus.ApplySlowDebuff(
                slowAmount,
                debuffDuration
            );

            if (showDebugLog)
            {
                Debug.Log(
                    $"Trash Debuff Applied / Slow : {slowAmount} / Duration : {debuffDuration}"
                );
            }
        }

        // 충돌 후 제거
        if (destroyOnHit)
        {
            Destroy(gameObject);
        }
    }
}
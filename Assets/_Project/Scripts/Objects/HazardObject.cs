using UnityEngine;

public class HazardObject : MonoBehaviour
{
    [Header("Hazard Type")]
    // 오브젝트 타입 설정
    // 일반 좀비 / 대형 좀비 / 쓰레기
    [SerializeField] private HazardType hazardType;

    [Header("Damage Settings")]
    // 플레이어에게 줄 데미지
    [SerializeField] private float damage = 10f;

    [Header("Trash Debuff Settings")]
    // 쓰레기 충돌 시 이동속도 감소량
    [SerializeField] private float slowAmount = 0.2f;

    // 쓰레기 디버프 지속 시간
    [SerializeField] private float debuffDuration = 5f;

    [Header("Destroy Settings")]
    // 플레이어와 충돌 후 제거 여부
    [SerializeField] private bool destroyOnHit = true;

    [Header("Debug")]
    // 디버그 로그 출력 여부
    [SerializeField] private bool showDebugLog = true;

    // 중복 충돌 방지용 변수
    private bool hasHit = false;

    private void OnTriggerEnter(Collider other)
    {
        // 이미 충돌 처리된 경우 종료
        if (hasHit)
            return;

        // 충돌한 오브젝트 또는 부모에서
        // PlayerStatus 스크립트 찾기
        PlayerStatus playerStatus =
            other.GetComponentInParent<PlayerStatus>();

        // PlayerStatus가 없으면 플레이어가 아니므로 무시
        if (playerStatus == null)
            return;

        // 중복 충돌 방지 활성화
        hasHit = true;

        // 디버그 로그 출력
        if (showDebugLog)
        {
            Debug.Log(
                $"{hazardType} Hit Player / Damage : {damage}"
            );
        }

        // 플레이어 데미지 적용
        playerStatus.TakeDamage(damage);

        // 쓰레기일 경우 디버프 적용
        if (hazardType == HazardType.Trash)
        {
            playerStatus.ApplySlowDebuff(
                slowAmount,
                debuffDuration
            );

            // 디버프 로그 출력
            if (showDebugLog)
            {
                Debug.Log(
                    $"Trash Debuff Applied / Slow : {slowAmount} / Duration : {debuffDuration}"
                );
            }
        }

        // 충돌 후 오브젝트 제거
        if (destroyOnHit)
        {
            Destroy(gameObject);
        }
    }
}
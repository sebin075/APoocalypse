using UnityEngine;

public class HazardObject : MonoBehaviour
{
    public enum HazardType
    {
        NormalZombie, // 일반 좀비
        BigZombie,    // 거대 좀비
        Obstacle      // 장애물
    }

    [Header("Hazard Type")]
    [Tooltip(
        "오브젝트 종류 설정\n" +
        "NormalZombie = 일반 좀비\n" +
        "BigZombie = 거대 좀비\n" +
        "Obstacle = 장애물"
    )]
    [SerializeField]
    private HazardType hazardType = HazardType.NormalZombie;

    [Header("Damage Settings")]
    [Tooltip(
        "일반 좀비 충돌 데미지\n" +
        "추천: 5~10"
    )]
    [Range(0f, 100f)]
    [SerializeField]
    private float normalZombieDamage = 10f;

    [Tooltip(
        "거대 좀비 충돌 데미지\n" +
        "추천: 15~25"
    )]
    [Range(0f, 100f)]
    [SerializeField]
    private float bigZombieDamage = 20f;

    [Header("Collision Settings")]
    [Tooltip(
        "충돌 후 오브젝트 제거 여부\n" +
        "일반 좀비는 ON 추천\n" +
        "거대 좀비는 OFF 추천"
    )]
    [SerializeField]
    private bool destroyOnHit = true;

    [Tooltip(
        "충돌 가능한 플레이어 태그\n" +
        "WheelchairRoot 또는 PlayerRoot에 Player 태그 추천"
    )]
    [SerializeField]
    private string playerTag = "Player";

    [Header("Obstacle Settings")]
    [Tooltip(
        "장애물을 통과 가능하게 할지 여부\n" +
        "Obstacle 타입은 ON 추천"
    )]
    [SerializeField]
    private bool canPassThrough = true;

    [Header("Effect Settings")]
    [Tooltip(
        "충돌 효과음"
    )]
    [SerializeField]
    private AudioSource hitAudio;

    [Tooltip(
        "충돌 이펙트"
    )]
    [SerializeField]
    private GameObject hitEffect;

    [Header("Debug Status")]
    [Tooltip("마지막 충돌 여부")]
    [SerializeField]
    private bool hasCollided;

    [Tooltip("마지막 충돌 대상 이름")]
    [SerializeField]
    private string lastHitObjectName;

    private Collider objectCollider;

    private void Start()
    {
        objectCollider = GetComponent<Collider>();

        SetupObstacleCollision();
    }

    private void SetupObstacleCollision()
    {
        // 장애물은 밟고 지나갈 수 있도록 Trigger 사용
        if (hazardType == HazardType.Obstacle &&
            canPassThrough &&
            objectCollider != null)
        {
            objectCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleCollision(other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        HandleCollision(collision.gameObject);
    }

    private void HandleCollision(GameObject other)
    {
        // 플레이어가 아니면 무시
        if (!other.CompareTag(playerTag))
            return;

        hasCollided = true;
        lastHitObjectName = other.name;

        // 장애물은 효과 없음
        if (hazardType == HazardType.Obstacle)
        {
            Debug.Log("[장애물] 플레이어가 밟고 지나감");

            return;
        }

        // 플레이어 상태 스크립트 찾기
        PlayerStatus playerStatus =
            other.GetComponent<PlayerStatus>();

        if (playerStatus == null)
        {
            playerStatus =
                other.GetComponentInParent<PlayerStatus>();
        }

        // 데미지 적용
        if (playerStatus != null)
        {
            switch (hazardType)
            {
                case HazardType.NormalZombie:

                    playerStatus.TakeDamage(normalZombieDamage);

                    Debug.Log(
                        "[일반 좀비 충돌] 데미지 : " +
                        normalZombieDamage
                    );

                    break;

                case HazardType.BigZombie:

                    playerStatus.TakeDamage(bigZombieDamage);

                    Debug.Log(
                        "[거대 좀비 충돌] 데미지 : " +
                        bigZombieDamage
                    );

                    break;
            }
        }

        // 효과음 재생
        if (hitAudio != null)
        {
            hitAudio.Play();
        }

        // 이펙트 생성
        if (hitEffect != null)
        {
            Instantiate(
                hitEffect,
                transform.position,
                Quaternion.identity
            );
        }

        // 충돌 후 제거
        if (destroyOnHit)
        {
            Destroy(gameObject);
        }
    }
}
using UnityEngine;

public class ObjectMover : MonoBehaviour
{
    // ================================
    // 오브젝트 이동 방식 종류
    // ================================
    public enum MoveMode
    {
        None,           // 이동 안 함 (쓰레기)
        Straight,       // 한 방향 직진 (대형 좀비)
        ChasePlayer     // 플레이어 발견 후 발견 위치로 돌진 (일반 좀비)
    }

    // ================================
    // 이동 타입 설정
    // ================================
    [Header("Move Type")]

    [Tooltip(
        "오브젝트 이동 방식\n" +
        "None = 이동 안 함\n" +
        "Straight = 직진 이동\n" +
        "ChasePlayer = 플레이어 발견 후 돌진"
    )]
    [SerializeField]
    private MoveMode moveMode;

    // ================================
    // 플레이어 타겟 설정
    // ================================
    [Header("Target")]

    [Tooltip(
        "플레이어 Transform\n" +
        "XR에서는 Main Camera 연결 추천"
    )]
    [SerializeField]
    private Transform player;

    // ================================
    // 이동 관련 설정
    // ================================
    [Header("Move Settings")]

    [Tooltip(
        "기본 이동 속도\n" +
        "일반 좀비 추천 : 3~4\n" +
        "대형 좀비 추천 : 2~3"
    )]
    [Range(0f, 10f)]
    [SerializeField]
    private float moveSpeed = 3f;

    [Tooltip(
        "기본 이동 방향\n" +
        "현재 프로젝트 추천값 : (0,0,-1)"
    )]
    [SerializeField]
    private Vector3 moveDirection = Vector3.back;

    // ================================
    // 회전 설정
    // ================================
    [Header("Rotation Settings")]

    [Tooltip(
        "이동 방향을 바라보게 할지 여부\n" +
        "Mixamo 애니메이션 사용 시 ON 추천"
    )]
    [SerializeField]
    private bool lookMoveDirection = true;

    // ================================
    // 플레이어 추적 설정
    // ================================
    [Header("Chase Settings")]

    [Tooltip(
        "플레이어 감지 거리\n" +
        "일반 좀비 추천 : 8~10"
    )]
    [Range(0f, 30f)]
    [SerializeField]
    private float chaseDistance = 10f;

    [Tooltip(
        "플레이어와 가까워질수록 속도 증가"
    )]
    [SerializeField]
    private bool useDistanceSpeedUp = true;

    [Tooltip(
        "추가되는 최대 속도\n" +
        "일반 좀비 추천 : 1~2"
    )]
    [Range(0f, 10f)]
    [SerializeField]
    private float maxSpeedBonus = 2f;

    // ================================
    // 제거 설정
    // ================================
    [Header("Destroy Settings")]
    [Tooltip(
        "플레이어 뒤쪽으로 이 거리 이상 지나가면 제거됩니다.\n" +
        "값이 너무 작으면 가까운 곳에서 사라지고,\n" +
        "값이 너무 크면 뒤쪽 오브젝트가 오래 남습니다."
    )]
    [Range(0f, 50f)]
    [SerializeField]
    private float destroyBehindDistance = 15f;

    // ================================
    // 디버그 상태 확인용
    // Inspector에서 실시간 확인 가능
    // ================================
    [Header("Debug Status")]

    [Tooltip("현재 플레이어를 발견했는지 여부")]
    [SerializeField]
    private bool hasDetectedPlayer;

    [Tooltip("현재 실제 이동 속도")]
    [SerializeField]
    private float currentSpeed;

    [Tooltip("현재 플레이어와 거리")]
    [SerializeField]
    private float currentDistanceToPlayer;

    // 플레이어 발견 당시 돌진 방향 저장
    private Vector3 fixedChaseDirection;

    private void Start()
    {
        // Player가 비어있으면
        // Main Camera 자동 연결
        // XR Interaction Simulator 대응
        if (player == null && Camera.main != null)
        {
            player = Camera.main.transform;
        }

        // Y 방향 제거
        // 위아래 이동 방지
        moveDirection.y = 0f;

        // 방향 벡터 정규화
        moveDirection.Normalize();
    }

    private void Update()
    {
        // 플레이어 없으면 실행 안 함
        if (player == null)
            return;

        // 현재 플레이어 거리 계산
        currentDistanceToPlayer =
            Vector3.Distance(
                transform.position,
                player.position
            );

        // 현재 실제 속도 계산
        currentSpeed = GetFinalSpeed();

        // 이동 처리
        MoveObject();

        // 지나간 오브젝트 제거
        CheckDestroyDistance();
    }

    private void MoveObject()
    {
        switch (moveMode)
        {
            // 이동 안 함
            case MoveMode.None:
                break;

            // 한 방향 직진
            case MoveMode.Straight:

                MoveInDirection(
                    moveDirection,
                    currentSpeed
                );

                break;

            // 플레이어 발견 후 돌진
            case MoveMode.ChasePlayer:

                MoveChasePlayer(currentSpeed);

                break;
        }
    }

    private void MoveChasePlayer(float speed)
    {
        // 아직 플레이어 발견 전
        if (!hasDetectedPlayer)
        {
            // 감지 거리 안으로 들어왔는지 확인
            if (currentDistanceToPlayer <= chaseDistance)
            {
                // 플레이어 발견 처리
                hasDetectedPlayer = true;

                // 플레이어 현재 위치 저장
                Vector3 detectedPosition =
                    player.position;

                // Y값 제거
                // 위아래 방향 방지
                detectedPosition.y =
                    transform.position.y;

                // 발견 당시 방향 계산
                fixedChaseDirection =
                    (
                        detectedPosition -
                        transform.position
                    ).normalized;

                // Y 제거
                fixedChaseDirection.y = 0f;

                // 방향 정규화
                fixedChaseDirection.Normalize();
            }
            else
            {
                // 발견 전에는 직진
                MoveInDirection(
                    moveDirection,
                    speed
                );

                return;
            }
        }

        // 발견 후에는
        // 발견 당시 위치 방향으로만 돌진
        MoveInDirection(
            fixedChaseDirection,
            speed
        );
    }

    private float GetFinalSpeed()
    {
        // 기본 속도
        float finalSpeed = moveSpeed;

        // 거리 기반 가속 사용 시
        if (
            useDistanceSpeedUp &&
            currentDistanceToPlayer <= chaseDistance
        )
        {
            // 거리 비율 계산
            float t =
                1f -
                (
                    currentDistanceToPlayer /
                    chaseDistance
                );

            // 추가 속도 적용
            finalSpeed += maxSpeedBonus * t;
        }

        return finalSpeed;
    }

    private void MoveInDirection(
        Vector3 direction,
        float speed
    )
    {
        // 방향 없으면 이동 안 함
        if (direction == Vector3.zero)
            return;

        // 이동 처리
        transform.position +=
            direction *
            speed *
            Time.deltaTime;

        // 이동 방향 바라보기
        if (lookMoveDirection)
        {
            transform.forward = direction;
        }
    }

    private void CheckDestroyDistance()
    {
        // 플레이어보다 얼마나 뒤로 지나갔는지 계산
        float distanceBehind =
            player.position.z -
            transform.position.z;

        // 일정 거리 이상 뒤로 지나가면 제거
        if (distanceBehind >= destroyBehindDistance)
        {
            Destroy(gameObject);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // 플레이어 감지 범위 표시
        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(
            transform.position,
            chaseDistance
        );
    }
}
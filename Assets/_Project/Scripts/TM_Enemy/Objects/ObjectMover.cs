using UnityEngine;

public class ObjectMover : MonoBehaviour
{
    public enum MoveMode
    {
        None,       // 쓰레기: 이동 안 함
        Straight,   // 대형 좀비: 한 방향 직진
        ChasePlayer // 일반 좀비: 플레이어 발견 위치로 돌진
    }

    [Header("Move Type")]
    [Tooltip("이동 방식 선택\nNone=이동 안함\nStraight=직진\nChasePlayer=플레이어 발견 위치로 돌진\n※ 일반 팀원은 함부로 수정하지 않는 것을 추천")]
    [SerializeField] private MoveMode moveMode;

    [Header("Target")]
    [Tooltip("플레이어 위치 기준\nXR에서는 Main Camera를 넣는 것을 추천\n잘못 넣으면 좀비가 시작 위치만 바라볼 수 있음")]
    [SerializeField] private Transform player;

    [Header("Move Settings")]
    [Tooltip("기본 이동 속도\n일반 좀비 추천: 3~4\n대형 좀비 추천: 2~3\n너무 낮으면 위협이 없고, 너무 높으면 피하기 어려움")]
    [Range(0f, 10f)]
    [SerializeField] private float moveSpeed = 3f;

    [Tooltip("기본 이동 방향\n현재 추천값: (0,0,-1)\n앞→뒤: (0,0,-1), 뒤→앞: (0,0,1), 왼쪽: (-1,0,0), 오른쪽: (1,0,0)")]
    [SerializeField] private Vector3 moveDirection = Vector3.back;

    [Header("Rotation Settings")]
    [Tooltip("이동 방향을 바라보게 할지 여부\n3D 모델/Mixamo 애니메이션 사용 시 ON 추천")]
    [SerializeField] private bool lookMoveDirection = true;

    [Header("Chase Settings")]
    [Tooltip("플레이어 감지 거리\n일반 좀비 추천: 8~10\n너무 낮으면 반응이 늦고, 너무 높으면 멀리서부터 돌진함")]
    [Range(0f, 30f)]
    [SerializeField] private float chaseDistance = 10f;

    [Tooltip("플레이어와 가까워질수록 속도 증가\n일반/대형 좀비 모두 ON 추천")]
    [SerializeField] private bool useDistanceSpeedUp = true;

    [Tooltip("가까워졌을 때 추가되는 최대 속도\n일반 좀비 추천: 1~2\n대형 좀비 추천: 3~4")]
    [Range(0f, 10f)]
    [SerializeField] private float maxSpeedBonus = 2f;

    [Header("Destroy Settings")]
    [Tooltip("플레이어 뒤쪽으로 이 거리 이상 지나가면 제거\n일반 좀비/쓰레기 추천: 10~15\n대형 좀비 추천: 15~20\n너무 작으면 갑자기 사라지고, 너무 크면 오브젝트가 오래 남음")]
    [Range(0f, 50f)]
    [SerializeField] private float destroyBehindDistance = 15f;

    [Header("Debug Status")]
    [Tooltip("현재 플레이어를 발견했는지 확인")]
    [SerializeField] private bool hasDetectedPlayer;

    [Tooltip("현재 실제 이동 속도")]
    [SerializeField] private float currentSpeed;

    [Tooltip("현재 플레이어와 거리")]
    [SerializeField] private float currentDistanceToPlayer;

    private Vector3 fixedChaseDirection;

    private void Start()
    {
        // XR 테스트 대응: Player가 비어 있으면 Main Camera 자동 연결
        if (player == null && Camera.main != null)
        {
            player = Camera.main.transform;
        }

        // 위아래 이동 방지
        moveDirection.y = 0f;
        moveDirection.Normalize();
    }

    private void Update()
    {
        if (player == null)
            return;

        currentDistanceToPlayer = Vector3.Distance(transform.position, player.position);
        currentSpeed = GetFinalSpeed();

        MoveObject();
        CheckDestroyDistance();
    }

    private void MoveObject()
    {
        switch (moveMode)
        {
            case MoveMode.None:
                break;

            case MoveMode.Straight:
                MoveInDirection(moveDirection, currentSpeed);
                break;

            case MoveMode.ChasePlayer:
                MoveChasePlayer(currentSpeed);
                break;
        }
    }

    private void MoveChasePlayer(float speed)
    {
        // 아직 플레이어를 발견하지 않았다면 거리 확인
        if (!hasDetectedPlayer)
        {
            if (currentDistanceToPlayer <= chaseDistance)
            {
                hasDetectedPlayer = true;

                // 발견 당시 플레이어 위치 저장
                Vector3 detectedPosition = player.position;
                detectedPosition.y = transform.position.y;

                // 발견한 위치로 향하는 방향 고정
                fixedChaseDirection = (detectedPosition - transform.position).normalized;
                fixedChaseDirection.y = 0f;
                fixedChaseDirection.Normalize();
            }
            else
            {
                // 발견 전에는 기본 방향으로 직진
                MoveInDirection(moveDirection, speed);
                return;
            }
        }

        // 발견 후에는 실시간 추적하지 않고, 발견 당시 방향으로만 돌진
        MoveInDirection(fixedChaseDirection, speed);
    }

    private float GetFinalSpeed()
    {
        float finalSpeed = moveSpeed;

        if (useDistanceSpeedUp && currentDistanceToPlayer <= chaseDistance)
        {
            float t = 1f - (currentDistanceToPlayer / chaseDistance);
            finalSpeed += maxSpeedBonus * t;
        }

        return finalSpeed;
    }

    private void MoveInDirection(Vector3 direction, float speed)
    {
        if (direction == Vector3.zero)
            return;

        transform.position += direction * speed * Time.deltaTime;

        if (lookMoveDirection)
        {
            transform.forward = direction;
        }
    }

    private void CheckDestroyDistance()
    {
        float distanceBehind = player.position.z - transform.position.z;

        if (distanceBehind >= destroyBehindDistance)
        {
            Destroy(gameObject);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // 빨간 원 = 플레이어 감지 범위
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseDistance);
    }
}
using UnityEngine;

public class ObjectMover : MonoBehaviour
{
    public enum MoveMode
    {
        None,       // 이동 안 함: 거대 좀비, 장애물
        Directional // 지정 방향 직선 이동: 일반 좀비
    }

    [Header("Move Type")]
    [Tooltip(
        "오브젝트 이동 방식\n" +
        "None = 이동 안 함 (거대 좀비 / 장애물)\n" +
        "Directional = 직선 이동 (일반 좀비)"
    )]
    [SerializeField] private MoveMode moveMode = MoveMode.None;

    [Header("Move Settings")]
    [Tooltip(
        "이동 속도\n" +
        "일반 좀비 추천: 2~4\n" +
        "거대 좀비 / 장애물은 Move Mode를 None으로 설정"
    )]
    [Range(0f, 10f)]
    [SerializeField] private float moveSpeed = 3f;

    [Tooltip(
        "직접 이동 방향을 지정할 때 사용\n" +
        "앞: (0,0,1)\n" +
        "뒤: (0,0,-1)\n" +
        "왼쪽: (-1,0,0)\n" +
        "오른쪽: (1,0,0)"
    )]
    [SerializeField] private Vector3 moveDirection = Vector3.forward;

    [Header("Random Direction")]
    [Tooltip(
        "일반 좀비가 생성될 때 상/하/좌/우 중 랜덤 방향으로 이동할지 여부\n" +
        "일반 좀비는 ON 추천\n" +
        "거대 좀비 / 장애물은 Move Mode가 None이면 영향 없음"
    )]
    [SerializeField] private bool useRandomDirection = true;

    [Header("Rotation Settings")]
    [Tooltip(
        "이동 방향을 바라보게 할지 여부\n" +
        "3D 모델 / Mixamo 애니메이션 사용 시 ON 추천"
    )]
    [SerializeField] private bool lookMoveDirection = true;

    [Header("Destroy Settings")]
    [Tooltip(
        "플레이어와 너무 멀어진 오브젝트를 자동 제거할지 여부\n" +
        "오브젝트가 계속 쌓이는 것을 방지"
    )]
    [SerializeField] private bool useDistanceDestroy = true;

    [Tooltip(
        "거리 제거 기준이 되는 플레이어 Transform\n" +
        "휠체어 게임에서는 WheelchairRoot 또는 PlayerRoot 추천"
    )]
    [SerializeField] private Transform player;

    [Tooltip(
        "플레이어와 이 거리 이상 멀어지면 제거\n" +
        "추천: 25~40\n" +
        "너무 작으면 빨리 사라지고, 너무 크면 오브젝트가 오래 남음"
    )]
    [Range(0f, 100f)]
    [SerializeField] private float destroyDistance = 35f;

    [Header("Debug Status")]
    [Tooltip("현재 실제 이동 방향")]
    [SerializeField] private Vector3 currentMoveDirection;

    [Tooltip("현재 실제 이동 속도")]
    [SerializeField] private float currentSpeed;

    [Tooltip("플레이어와 현재 거리")]
    [SerializeField] private float currentDistanceToPlayer;

    private void Start()
    {
        // Player가 비어 있으면 Player 태그로 자동 찾기
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }

        // 일반 좀비라면 상/하/좌/우 중 랜덤 방향 선택
        if (moveMode == MoveMode.Directional && useRandomDirection)
        {
            moveDirection = GetRandomCardinalDirection();
        }

        // Y축 이동 제거
        moveDirection.y = 0f;

        // 방향값이 비어 있으면 기본값 지정
        if (moveDirection == Vector3.zero)
        {
            moveDirection = Vector3.forward;
        }

        // 방향 정규화
        moveDirection.Normalize();

        currentMoveDirection = moveDirection;
        currentSpeed = moveMode == MoveMode.Directional ? moveSpeed : 0f;
    }

    private void Update()
    {
        MoveObject();
        CheckDistanceDestroy();
    }

    private void MoveObject()
    {
        switch (moveMode)
        {
            case MoveMode.None:
                currentSpeed = 0f;
                break;

            case MoveMode.Directional:
                currentSpeed = moveSpeed;
                MoveInDirection(moveDirection, currentSpeed);
                break;
        }
    }

    public void SetMoveDirection(Vector3 direction)
    {
        direction.y = 0f;

        if (direction == Vector3.zero)
            return;

        moveDirection = direction.normalized;
        currentMoveDirection = moveDirection;
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

        currentMoveDirection = direction;
    }

    private Vector3 GetRandomCardinalDirection()
    {
        int randomIndex = Random.Range(0, 4);

        switch (randomIndex)
        {
            case 0:
                return Vector3.forward; // 상 / 앞

            case 1:
                return Vector3.back; // 하 / 뒤

            case 2:
                return Vector3.left; // 좌

            case 3:
                return Vector3.right; // 우
        }

        return Vector3.forward;
    }

    private void CheckDistanceDestroy()
    {
        if (!useDistanceDestroy)
            return;

        if (player == null)
            return;

        currentDistanceToPlayer =
            Vector3.Distance(transform.position, player.position);

        if (currentDistanceToPlayer >= destroyDistance)
        {
            Destroy(gameObject);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // 파란 선 = 이동 방향
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(
            transform.position,
            transform.position + moveDirection.normalized * 3f
        );

        // 회색 원 = 제거 거리
        if (useDistanceDestroy)
        {
            Gizmos.color = Color.gray;
            Gizmos.DrawWireSphere(transform.position, destroyDistance);
        }
    }
}
using UnityEngine;

public class ObjectMover : MonoBehaviour
{
    // 오브젝트 이동 방식
    public enum MoveMode
    {
        None,           // 이동 안 함 : 쓰레기
        Straight,       // 한 방향 직진 : 대형 좀비
        ChasePlayer     // 플레이어 발견 후 돌진 : 일반 좀비
    }

    [Header("Move Type")]
    // 이동 방식 선택
    [SerializeField] private MoveMode moveMode;

    [Header("Target")]
    // 플레이어 위치 기준
    // XR에서는 Main Camera 연결 추천
    [SerializeField] private Transform player;

    [Header("Move Settings")]
    // 기본 이동 속도
    [SerializeField] private float moveSpeed = 3f;

    // 기본 직진 방향
    [SerializeField] private Vector3 moveDirection = Vector3.back;

    [Header("Rotation Settings")]
    // 이동 방향 바라보기 여부
    [SerializeField] private bool lookMoveDirection = true;

    [Header("Chase Settings")]
    // 플레이어 감지 거리
    [SerializeField] private float chaseDistance = 10f;

    // 가까워질수록 속도 증가 여부
    [SerializeField] private bool useDistanceSpeedUp = true;

    // 최대 추가 속도
    [SerializeField] private float maxSpeedBonus = 3f;

    [Header("Destroy Settings")]
    // 플레이어 뒤쪽 제거 거리
    [SerializeField] private float destroyBehindDistance = 15f;

    // 플레이어 발견 여부
    private bool hasDetectedPlayer = false;

    // 플레이어 발견 당시 위치
    private Vector3 detectedPlayerPosition;

    // 발견 당시 고정 돌진 방향
    private Vector3 fixedChaseDirection;

    private void Start()
    {
        // Player가 비어있으면 Main Camera 자동 사용
        // XR Interaction Simulator 대응
        if (player == null)
        {
            if (Camera.main != null)
            {
                player = Camera.main.transform;
            }
        }

        // 이동 방향 Y 제거
        moveDirection.y = 0f;

        // 방향 벡터 정규화
        moveDirection.Normalize();
    }

    private void Update()
    {
        // 플레이어 없으면 실행 안 함
        if (player == null)
            return;

        // 이동 처리
        MoveObject();

        // 지나간 오브젝트 제거
        CheckDestroyDistance();
    }

    private void MoveObject()
    {
        // 최종 이동 속도 계산
        float finalSpeed = GetFinalSpeed();

        switch (moveMode)
        {
            // 이동 안 함
            case MoveMode.None:
                break;

            // 한 방향 직진
            case MoveMode.Straight:

                MoveInDirection(
                    moveDirection,
                    finalSpeed
                );

                break;

            // 플레이어 발견 후 돌진
            case MoveMode.ChasePlayer:

                MoveChasePlayer(finalSpeed);

                break;
        }
    }

    private void MoveChasePlayer(float finalSpeed)
    {
        // 아직 플레이어 발견 전
        if (!hasDetectedPlayer)
        {
            // 플레이어 거리 계산
            float distanceToPlayer =
                Vector3.Distance(
                    transform.position,
                    player.position
                );

            // 감지 거리 안으로 들어왔는지 확인
            if (distanceToPlayer <= chaseDistance)
            {
                // 플레이어 발견 처리
                hasDetectedPlayer = true;

                // 발견 당시 플레이어 위치 저장
                detectedPlayerPosition = player.position;

                // Y값 제거
                detectedPlayerPosition.y =
                    transform.position.y;

                // 발견 당시 방향 계산
                fixedChaseDirection =
                    (
                        detectedPlayerPosition -
                        transform.position
                    ).normalized;

                // Y 이동 제거
                fixedChaseDirection.y = 0f;

                // 방향 정규화
                fixedChaseDirection.Normalize();
            }
            else
            {
                // 플레이어 발견 전에는 직진
                MoveInDirection(
                    moveDirection,
                    finalSpeed
                );

                return;
            }
        }

        // 발견 후에는
        // 발견 당시 방향으로만 돌진
        MoveInDirection(
            fixedChaseDirection,
            finalSpeed
        );
    }

    private float GetFinalSpeed()
    {
        float finalSpeed = moveSpeed;

        // 거리 기반 속도 증가 사용 시
        if (useDistanceSpeedUp && player != null)
        {
            // 플레이어 거리 계산
            float distance =
                Vector3.Distance(
                    transform.position,
                    player.position
                );

            // 감지 거리 안이면 속도 증가
            if (distance <= chaseDistance)
            {
                // 거리 비율 계산
                float t =
                    1f - (distance / chaseDistance);

                // 추가 속도 적용
                finalSpeed += maxSpeedBonus * t;
            }
        }

        return finalSpeed;
    }

    private void MoveInDirection(
        Vector3 direction,
        float speed
    )
    {
        // 방향이 없으면 이동 안 함
        if (direction == Vector3.zero)
            return;

        // 이동 처리
        transform.position +=
            direction * speed * Time.deltaTime;

        // 이동 방향 바라보기
        if (lookMoveDirection)
        {
            transform.forward = direction;
        }
    }

    private void CheckDestroyDistance()
    {
        // 플레이어보다 뒤쪽 거리 계산
        float distanceBehind =
            player.position.z - transform.position.z;

        // 일정 거리 이상 지나가면 제거
        if (distanceBehind >= destroyBehindDistance)
        {
            Destroy(gameObject);
        }
    }
}
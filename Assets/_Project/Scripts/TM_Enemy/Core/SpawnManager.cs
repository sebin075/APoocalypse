using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    [Header("Player Target")]
    [Tooltip(
        "스폰 기준이 되는 플레이어 Transform\n" +
        "휠체어 게임에서는 WheelchairRoot 또는 PlayerRoot 추천\n" +
        "비워두면 Player 태그 오브젝트를 자동으로 찾음"
    )]
    [SerializeField]
    private Transform player;

    [Header("Prefabs")]
    [Tooltip("일반 좀비 프리팹")]
    [SerializeField]
    private GameObject normalZombiePrefab;

    [Tooltip("거대 좀비 프리팹")]
    [SerializeField]
    private GameObject bigZombiePrefab;

    [Tooltip("장애물 프리팹")]
    [SerializeField]
    private GameObject obstaclePrefab;

    [Header("Spawn Area")]
    [Tooltip(
        "플레이어 앞쪽 최소 생성 거리\n" +
        "추천: 12~15\n" +
        "너무 작으면 눈앞에 갑자기 생성됨"
    )]
    [Range(1f, 50f)]
    [SerializeField]
    private float minForwardDistance = 15f;

    [Tooltip(
        "플레이어 앞쪽 최대 생성 거리\n" +
        "추천: 25~35\n" +
        "너무 크면 너무 멀리 생성됨"
    )]
    [Range(1f, 80f)]
    [SerializeField]
    private float maxForwardDistance = 30f;

    [Tooltip(
        "좌우 랜덤 생성 범위\n" +
        "추천: 6~10\n" +
        "값이 클수록 더 넓은 범위에 생성됨"
    )]
    [Range(0f, 30f)]
    [SerializeField]
    private float sideSpawnRange = 8f;

    [Tooltip(
        "기본 생성 높이\n" +
        "오브젝트가 땅에 박히면 올리고, 공중에 뜨면 낮추기"
    )]
    [SerializeField]
    private float baseSpawnHeight = 0f;

    [Header("Fixed Spawn Direction")]
    [Tooltip(
        "플레이어 전방 기준 방향\n" +
        "플레이어가 보는 방향이 아니라 맵 진행 방향 기준\n" +
        "예: Z+ 진행이면 (0,0,1), Z- 진행이면 (0,0,-1)"
    )]
    [SerializeField]
    private Vector3 spawnForwardDirection = Vector3.forward;

    [Tooltip(
        "좌우 기준 방향\n" +
        "보통 (1,0,0) 사용"
    )]
    [SerializeField]
    private Vector3 spawnRightDirection = Vector3.right;

    [Header("Spawn Settings")]
    [Tooltip(
        "몇 초마다 오브젝트를 생성할지\n" +
        "추천: 1.5~2.5"
    )]
    [Range(0.1f, 10f)]
    [SerializeField]
    private float spawnInterval = 2f;

    [Tooltip(
        "한 번에 최대 몇 개 생성할지\n" +
        "추천: 1~2\n" +
        "너무 많으면 피할 공간이 없어짐"
    )]
    [Range(1, 5)]
    [SerializeField]
    private int maxSpawnCountPerWave = 2;

    [Header("Spawn Chance")]
    [Tooltip("일반 좀비 생성 비율")]
    [Range(0, 100)]
    [SerializeField]
    private int normalZombieChance = 60;

    [Tooltip("거대 좀비 생성 비율")]
    [Range(0, 100)]
    [SerializeField]
    private int bigZombieChance = 20;

    [Tooltip("장애물 생성 비율")]
    [Range(0, 100)]
    [SerializeField]
    private int obstacleChance = 20;

    [Header("Spawn Height Offset")]
    [Tooltip("일반 좀비 높이 보정")]
    [SerializeField]
    private float normalZombieYOffset = 0f;

    [Tooltip(
        "거대 좀비 높이 보정\n" +
        "땅에 박히면 올리고, 공중에 뜨면 낮추기"
    )]
    [SerializeField]
    private float bigZombieYOffset = 0f;

    [Tooltip("장애물 높이 보정")]
    [SerializeField]
    private float obstacleYOffset = 0f;

    [Header("Overlap Check")]
    [Tooltip(
        "생성 위치 주변 겹침 검사 범위\n" +
        "추천: 1.5~3\n" +
        "너무 작으면 겹치고, 너무 크면 생성이 자주 취소됨"
    )]
    [Range(0f, 10f)]
    [SerializeField]
    private float checkRadius = 2f;

    [Tooltip(
        "겹침 검사 대상 Layer\n" +
        "일반 좀비 / 거대 좀비 / 장애물 프리팹 Layer를 Hazard로 설정하고\n" +
        "여기에도 Hazard 선택"
    )]
    [SerializeField]
    private LayerMask hazardLayer;

    [Header("Debug Status")]
    [Tooltip("현재 생성 타이머")]
    [SerializeField]
    private float currentSpawnTimer;

    [Tooltip("마지막 생성 가능 여부")]
    [SerializeField]
    private bool lastCanSpawnResult;

    [Tooltip("마지막 생성 위치")]
    [SerializeField]
    private Vector3 lastSpawnPosition;

    [Tooltip("마지막 생성된 오브젝트 이름")]
    [SerializeField]
    private string lastSpawnedObjectName;

    private float spawnTimer;

    private void Start()
    {
        // Player가 비어 있으면 Player 태그로 자동 찾기
        if (player == null)
        {
            GameObject playerObject =
                GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }

        NormalizeDirections();
    }

    private void Update()
    {
        if (player == null)
            return;

        spawnTimer += Time.deltaTime;
        currentSpawnTimer = spawnTimer;

        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;
            SpawnWave();
        }
    }

    private void NormalizeDirections()
    {
        // 위아래 방향 제거
        spawnForwardDirection.y = 0f;
        spawnRightDirection.y = 0f;

        if (spawnForwardDirection == Vector3.zero)
            spawnForwardDirection = Vector3.forward;

        if (spawnRightDirection == Vector3.zero)
            spawnRightDirection = Vector3.right;

        spawnForwardDirection.Normalize();
        spawnRightDirection.Normalize();
    }

    private void SpawnWave()
    {
        int spawnCount =
            Random.Range(1, maxSpawnCountPerWave + 1);

        for (int i = 0; i < spawnCount; i++)
        {
            GameObject prefab = GetRandomPrefab();

            if (prefab == null)
                continue;

            Vector3 spawnPosition =
                GetRandomSpawnPosition();

            spawnPosition.y += GetYOffset(prefab);

            lastSpawnPosition = spawnPosition;
            lastCanSpawnResult = CanSpawn(spawnPosition);
            lastSpawnedObjectName = prefab.name;

            // 이미 주변에 오브젝트가 있으면 생성하지 않음
            if (!lastCanSpawnResult)
                continue;

            GameObject spawnedObject = Instantiate(
                prefab,
                spawnPosition,
                 GetSpawnRotation()
            );

            SetNormalZombieInwardDirection(spawnedObject, spawnPosition);
        }
    }

    private void SetNormalZombieInwardDirection(GameObject spawnedObject, Vector3 spawnPosition)
    {
        if (spawnedObject == null)
            return;

        if (spawnedObject != normalZombiePrefab && !spawnedObject.name.Contains(normalZombiePrefab.name))
            return;

        ObjectMover mover = spawnedObject.GetComponent<ObjectMover>();

        if (mover == null)
            return;

        Vector3 centerLinePosition =
            player.position +
            spawnForwardDirection *
            Vector3.Dot(
                spawnPosition - player.position,
                spawnForwardDirection
            );

        Vector3 inwardDirection =
            centerLinePosition - spawnPosition;

        inwardDirection.y = 0f;

        if (inwardDirection == Vector3.zero)
        {
            inwardDirection = -spawnForwardDirection;
        }

        mover.SetMoveDirection(inwardDirection);
    }

    private Vector3 GetRandomSpawnPosition()
    {
        // 플레이어 앞쪽 거리 랜덤
        float forwardDistance =
            Random.Range(minForwardDistance, maxForwardDistance);

        // 좌우 위치 랜덤
        float sideOffset =
            Random.Range(-sideSpawnRange, sideSpawnRange);

        Vector3 spawnPosition =
            player.position +
            spawnForwardDirection * forwardDistance +
            spawnRightDirection * sideOffset;

        spawnPosition.y = baseSpawnHeight;

        return spawnPosition;
    }

    private Quaternion GetSpawnRotation()
    {
        // 기본적으로 진행 방향 반대를 바라보게 함
        // 프리팹 방향이 이상하면 여기 수정 가능
        if (spawnForwardDirection == Vector3.zero)
            return Quaternion.identity;

        return Quaternion.LookRotation(-spawnForwardDirection);
    }

    private GameObject GetRandomPrefab()
    {
        int totalChance =
            normalZombieChance +
            bigZombieChance +
            obstacleChance;

        if (totalChance <= 0)
            return null;

        int randomValue =
            Random.Range(0, totalChance);

        if (randomValue < normalZombieChance)
            return normalZombiePrefab;

        if (randomValue < normalZombieChance + bigZombieChance)
            return bigZombiePrefab;

        return obstaclePrefab;
    }

    private float GetYOffset(GameObject prefab)
    {
        if (prefab == normalZombiePrefab)
            return normalZombieYOffset;

        if (prefab == bigZombiePrefab)
            return bigZombieYOffset;

        if (prefab == obstaclePrefab)
            return obstacleYOffset;

        return 0f;
    }

    private bool CanSpawn(Vector3 position)
    {
        bool hasOverlap =
            Physics.CheckSphere(
                position,
                checkRadius,
                hazardLayer
            );

        return !hasOverlap;
    }

    private void OnDrawGizmosSelected()
    {
        if (player == null)
            return;

        NormalizeDirections();

        Gizmos.color = Color.yellow;

        // 대략적인 스폰 영역 표시
        Vector3 nearCenter =
            player.position +
            spawnForwardDirection * minForwardDistance;

        Vector3 farCenter =
            player.position +
            spawnForwardDirection * maxForwardDistance;

        Vector3 nearLeft =
            nearCenter - spawnRightDirection * sideSpawnRange;

        Vector3 nearRight =
            nearCenter + spawnRightDirection * sideSpawnRange;

        Vector3 farLeft =
            farCenter - spawnRightDirection * sideSpawnRange;

        Vector3 farRight =
            farCenter + spawnRightDirection * sideSpawnRange;

        Gizmos.DrawLine(nearLeft, nearRight);
        Gizmos.DrawLine(farLeft, farRight);
        Gizmos.DrawLine(nearLeft, farLeft);
        Gizmos.DrawLine(nearRight, farRight);

        // 마지막 생성 위치 표시
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(lastSpawnPosition, checkRadius);
    }
}
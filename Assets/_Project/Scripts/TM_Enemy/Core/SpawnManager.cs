using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    [Header("Player Target")]
    [Tooltip("스폰 기준 위치\nXR에서는 Main Camera 연결 추천")]
    [SerializeField] private Transform player;

    [Header("Prefabs")]
    [SerializeField] private GameObject normalZombiePrefab;
    [SerializeField] private GameObject bigZombiePrefab;
    [SerializeField] private GameObject trashPrefab;

    [Header("Dynamic Spawn Position")]
    [Tooltip("플레이어 앞 몇 m 지점에서 생성할지\n추천: 20")]
    [Range(5f, 50f)]
    [SerializeField] private float spawnDistance = 20f;

    [Tooltip("좌우 라인 간격\n추천: 2")]
    [Range(0.5f, 10f)]
    [SerializeField] private float laneWidth = 2f;

    [Tooltip("기본 생성 높이\n추천: 1")]
    [SerializeField] private float baseSpawnHeight = 1f;

    [Header("Fixed Direction Settings")]
    [Tooltip("스폰 진행 방향\n플레이어가 바라보는 방향과 무관하게 이 방향 기준으로 생성됨\n예: 앞으로 Z+ 방향이면 (0,0,1), Z- 방향이면 (0,0,-1)")]
    [SerializeField] private Vector3 spawnDirection = Vector3.forward;

    [Tooltip("라인 좌우 방향\n보통 (1,0,0) 사용")]
    [SerializeField] private Vector3 laneRightDirection = Vector3.right;

    [Header("Spawn Settings")]
    [Tooltip("몇 초마다 생성할지\n추천: 2")]
    [Range(0.1f, 10f)]
    [SerializeField] private float spawnInterval = 2f;

    [Tooltip("한 번에 최대 몇 개 생성할지\n3라인 기준 2 추천")]
    [Range(1, 3)]
    [SerializeField] private int maxSpawnCountPerWave = 2;

    [Header("Spawn Chance")]
    [Range(0, 100)]
    [SerializeField] private int normalZombieChance = 60;

    [Range(0, 100)]
    [SerializeField] private int bigZombieChance = 20;

    [Range(0, 100)]
    [SerializeField] private int trashChance = 20;

    [Header("Spawn Height Offset")]
    [SerializeField] private float normalZombieYOffset = 0f;

    [Tooltip("대형 좀비가 땅에 박히면 이 값을 올리기\n추천: 1~1.5")]
    [SerializeField] private float bigZombieYOffset = 1f;

    [SerializeField] private float trashYOffset = 0f;

    [Header("Overlap Check")]
    [Tooltip("생성 위치 주변 겹침 검사 범위\n추천: 1.5~2")]
    [Range(0f, 5f)]
    [SerializeField] private float checkRadius = 1.5f;

    [Tooltip("겹침 검사 대상 Layer\n좀비/쓰레기 프리팹 Layer를 Hazard로 설정하고 여기에도 Hazard 선택")]
    [SerializeField] private LayerMask hazardLayer;

    [Header("Debug Status")]
    [SerializeField] private float currentSpawnTimer;
    [SerializeField] private bool lastCanSpawnResult;
    [SerializeField] private int lastSpawnLineIndex = -1;
    [SerializeField] private string lastSpawnedObjectName;

    private float spawnTimer;
    private const int LaneCount = 3;

    private void Start()
    {
        if (player == null && Camera.main != null)
        {
            player = Camera.main.transform;
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
        spawnDirection.y = 0f;
        laneRightDirection.y = 0f;

        if (spawnDirection == Vector3.zero)
            spawnDirection = Vector3.forward;

        if (laneRightDirection == Vector3.zero)
            laneRightDirection = Vector3.right;

        spawnDirection.Normalize();
        laneRightDirection.Normalize();
    }

    private void SpawnWave()
    {
        int spawnCount = Random.Range(1, maxSpawnCountPerWave + 1);

        // 3라인 중 최소 1라인은 항상 비움
        spawnCount = Mathf.Min(spawnCount, LaneCount - 1);

        bool[] usedLines = new bool[LaneCount];

        for (int i = 0; i < spawnCount; i++)
        {
            int lineIndex = GetRandomUnusedLine(usedLines);

            if (lineIndex == -1)
                return;

            usedLines[lineIndex] = true;

            GameObject prefab = GetRandomPrefab();

            if (prefab == null)
                continue;

            Vector3 spawnPosition = GetDynamicSpawnPosition(lineIndex);
            spawnPosition.y += GetYOffset(prefab);

            lastCanSpawnResult = CanSpawn(spawnPosition);
            lastSpawnLineIndex = lineIndex;
            lastSpawnedObjectName = prefab.name;

            if (!lastCanSpawnResult)
                continue;

            Instantiate(prefab, spawnPosition, GetSpawnRotation());
        }
    }

    private Vector3 GetDynamicSpawnPosition(int lineIndex)
    {
        Vector3 centerPosition =
            player.position +
            spawnDirection * spawnDistance;

        centerPosition.y = baseSpawnHeight;

        // 0 = Left, 1 = Center, 2 = Right
        if (lineIndex == 0)
            return centerPosition - laneRightDirection * laneWidth;

        if (lineIndex == 2)
            return centerPosition + laneRightDirection * laneWidth;

        return centerPosition;
    }

    private Quaternion GetSpawnRotation()
    {
        // 스폰된 오브젝트가 진행 방향 반대로 바라보게 설정
        // 필요하면 프리팹 회전값에 맞게 수정 가능
        if (spawnDirection == Vector3.zero)
            return Quaternion.identity;

        return Quaternion.LookRotation(-spawnDirection);
    }

    private int GetRandomUnusedLine(bool[] usedLines)
    {
        int safetyCount = 0;

        while (safetyCount < 20)
        {
            int index = Random.Range(0, usedLines.Length);

            if (!usedLines[index])
                return index;

            safetyCount++;
        }

        return -1;
    }

    private GameObject GetRandomPrefab()
    {
        int totalChance =
            normalZombieChance +
            bigZombieChance +
            trashChance;

        if (totalChance <= 0)
            return null;

        int randomValue = Random.Range(0, totalChance);

        if (randomValue < normalZombieChance)
            return normalZombiePrefab;

        if (randomValue < normalZombieChance + bigZombieChance)
            return bigZombiePrefab;

        return trashPrefab;
    }

    private float GetYOffset(GameObject prefab)
    {
        if (prefab == normalZombiePrefab)
            return normalZombieYOffset;

        if (prefab == bigZombiePrefab)
            return bigZombieYOffset;

        if (prefab == trashPrefab)
            return trashYOffset;

        return 0f;
    }

    private bool CanSpawn(Vector3 position)
    {
        return !Physics.CheckSphere(position, checkRadius, hazardLayer);
    }

    private void OnDrawGizmosSelected()
    {
        if (player == null)
            return;

        NormalizeDirections();

        Gizmos.color = Color.yellow;

        for (int i = 0; i < LaneCount; i++)
        {
            Vector3 position = GetDynamicSpawnPosition(i);
            Gizmos.DrawWireSphere(position, checkRadius);
        }

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(
            player.position,
            player.position + spawnDirection * spawnDistance
        );
    }
}
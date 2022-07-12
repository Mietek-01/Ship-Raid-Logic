using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

using UnityRandom = UnityEngine.Random;

[Serializable]
public class Raid
{
    public event Action RaidStarted = delegate { };
    public event Action<bool> RaidEnded = delegate { };
    public event Action EnemyDestroyed = delegate { };
    public event Action KnowledgeStolen = delegate { };

    public bool AttackInProgress
    {
        get;
        private set;
    }

    [SerializeField]
    private RaidData raidData = null;
    [SerializeField]
    private PlatformGrid platformGrid = null;
    [SerializeField]
    private CitadelPortsManager citadelPortsManager = null;
    [SerializeField]
    private Transform enemyShipsPoolParent = null;

    private SkillsManager skillsManager;
    private Storage storage;
    private List<List<EnemyShip>> enemyShipPools = new List<List<EnemyShip>>();

    private int activeEnemyShips;
    private int currentAttackID = -1;

    public void Init(Storage storage, SkillsManager skillsManager)
    {
        Assert.IsTrue(raidData.AttacksData.Count > 0);
        this.storage = storage;
        this.skillsManager = skillsManager;
        citadelPortsManager.CreateCitadelPorts();

        for (int i = 0; i < RaidAttackData.EnemyShipTypes; i++)
        {
            enemyShipPools.Add(new List<EnemyShip>());
        }
    }

#if UNITY_EDITOR
    public void StartRaidEditor()
    {
        StartRaid();
    }
#endif

    public void StartRaid()
    {
        if (AttackInProgress)
        {
            Debug.LogWarning("You cannot start a raid before the previous one is completed");
            return;
        }

        if(++currentAttackID >= raidData.AttacksData.Count)
        {
            Debug.LogWarning("All waves used");
            return;
        }

        AttackInProgress = true;
        activeEnemyShips = 0;
        skillsManager.ResetSkills();
        CreateFleet(raidData.AttacksData[currentAttackID]);
        RaidStarted();
    }

    private void EndRaid(bool successfully)
    {
        AttackInProgress = false;
        RaidEnded(successfully);
    }

    private void CreateFleet(RaidAttackData attackData)
    {
        Assert.IsTrue(attackData.EnemyForces.Count == RaidAttackData.EnemyShipTypes);
        float direction = UnityRandom.Range(0f, 359.9f);
        for (int enemyTypeID = 0; enemyTypeID < RaidAttackData.EnemyShipTypes; enemyTypeID++)
        {
            for (int i = 0; i < attackData.EnemyForces[enemyTypeID]; i++)
            {
                var enemy = GetShip(enemyTypeID);
                Vector3 enemyShipPosition = CalculateEnemyShipPosition(activeEnemyShips, direction);

                var tilePath = PlatformTilePathfinder.GenerateTilePath(enemyShipPosition, platformGrid.TileRings);
                if (tilePath == null)
                {
                    Debug.LogError($"{enemy} have no a defined tile path");
                    ReturnToPool(enemy);
                }
                else
                {
                    AddListeners(enemy);
                    activeEnemyShips++;
                    enemy.MyStart(enemyShipPosition);
                    enemy.StartAttack(tilePath);
                }
            }
        }
    }

    private EnemyShip GetShip(int typeId)
    {
        var pool = enemyShipPools[typeId];
        int count = pool.Count;
        if (count == 0)
        {
            var prefab = raidData.Enemies[typeId].Prefab;
            var ship = GameObject.Instantiate(prefab, enemyShipsPoolParent);
            ship.Init(raidData.Enemies[typeId], typeId);
            return ship;
        }
        else
        {
            var ship = pool[count - 1];
            pool.RemoveAt(count - 1);
            ship.gameObject.SetActive(true);
            return ship;
        }
    }

    private void ReturnToPool(EnemyShip enemyShip)
    {
        enemyShip.gameObject.SetActive(false);
        enemyShipPools[enemyShip.TypeId].Add(enemyShip);
    }

    private void AddListeners(EnemyShip enemyShip)
    {
        enemyShip.InnerZoneReached += OnInnerZoneReached;
        enemyShip.RaidFinished += OnEnemyShipRaidFinished;
        enemyShip.Destroyed += OnEnemyShipDestroyed;
        enemyShip.Plundering += OnPlundering;
        enemyShip.PlatformTileReached += OnPlatformTileReached;
    }

    private void RemoveListeners(EnemyShip enemyShip)
    {
        enemyShip.InnerZoneReached -= OnInnerZoneReached;
        enemyShip.RaidFinished -= OnEnemyShipRaidFinished;
        enemyShip.Destroyed -= OnEnemyShipDestroyed;
        enemyShip.Plundering -= OnPlundering;
        enemyShip.PlatformTileReached -= OnPlatformTileReached;
    }

    private Vector3 CalculateEnemyShipPosition(int counter, float directionAngle)
    {
        float angle = directionAngle + (((counter << 1 % 4 - 1) * counter) << 1);
        float distance = raidData.EnemyStartDistance + (counter % 3 == 2 ? 1 : -1) * 30f;
        return PlatformTilePathfinder.CountPointFromAngle(angle, distance);
    }

    private void OnInnerZoneReached(EnemyShip enemyShip)
    {
        var citadelPort = citadelPortsManager.FindFreeCitadelPort(enemyShip.Position);

        if (citadelPort != null)
        {
            enemyShip.SetMyCurrentPort(citadelPortsManager.FindFreeCitadelPort(enemyShip.Position));
        }
        else
        {
            enemyShip.WaitForCitadelPort();
            citadelPortsManager.RegisterWaitingEnemyShip(enemyShip);
        }
    }

    private void OnEnemyShipRaidFinished(EnemyShip enemyShip)
    {
        RemoveListeners(enemyShip);
        ReturnToPool(enemyShip);

        if (--activeEnemyShips == 0)
        {
            EndRaid(true);
        }
    }

    private void OnEnemyShipDestroyed(EnemyShip enemyShip)
    {
        int restoredKnowledge = enemyShip.RobbedKnowledge >> 1;
        if (restoredKnowledge > 0)
        {
            storage.Add(EResourceType.Knowledge, restoredKnowledge, true);
            KnowledgeStolen();
        }

        citadelPortsManager.UnregisterWaitingEnemyShip(enemyShip);
        RemoveListeners(enemyShip);
        ReturnToPool(enemyShip);
        EnemyDestroyed();

        if (--activeEnemyShips == 0)
        {
            EndRaid(false);
        }
    }

    private void OnPlundering(EnemyShip enemyShip)
    {
        // Return if plundering was successful
        bool hasKnowledge = storage.Remove(EResourceType.Knowledge, 1);
        if (hasKnowledge)
        {
            KnowledgeStolen();
        }
        else
        {
            enemyShip.StopPlundering();
        }
    }

    private void OnPlatformTileReached(EnemyShipAttacker enemyShipAttacker, PlatformTile reachedPlatformTile)
    {
        var nearestPlatforms = platformGrid.GetAdjacentTilesWithPlatforms(reachedPlatformTile.TileCell);
        enemyShipAttacker.SetNearestPlatforms(nearestPlatforms);
    }
}
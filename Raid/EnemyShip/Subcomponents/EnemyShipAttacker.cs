using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyShipAttacker : IUpdateable
{
    public bool HasTargetBuilding => targetBuilding != null;

    private List<Platform> platformsWithBuilding = new List<Platform>();
    private Building targetBuilding;
    private int targetPlatform;

    private Stopwatch stopwatch;
    private int damage;
    private float cooldown;

    public EnemyShipAttacker(float cooldown, int damage)
    {
        this.damage = damage;
        this.cooldown = cooldown;
    }

    public void MyStart()
    {
        stopwatch = new Stopwatch(cooldown, Attack);
        targetBuilding = null;
    }

    public void MyUpdate(float delta)
    {
        stopwatch.Update(delta);
    }

    public void SetNearestPlatforms(List<PlatformTile> platformTilesWithPlatform)
    {
        this.platformsWithBuilding.Clear();
        targetBuilding = null;

        foreach (var platform in platformTilesWithPlatform)
        {
            if (platform.Platform.BuildingCount > 0)
            {
                platformsWithBuilding.Add(platform.Platform);
            }
        }

        if (platformsWithBuilding.Count != 0)
        {
            targetPlatform = 0;
            FindTargetBuilding();
        }
    }

    private void Attack()
    {
        if (targetBuilding == null || targetBuilding.Condition.TakeDamage(damage))
        {
            FindTargetBuilding();
        }
    }

    private void FindTargetBuilding()
    {
        if (platformsWithBuilding[targetPlatform].BuildingCount == 0)
        {
            for (++targetPlatform; targetPlatform < platformsWithBuilding.Count; targetPlatform++)
            {
                if (platformsWithBuilding[targetPlatform].BuildingCount > 0)
                {
                    targetBuilding = RandomUtils.GetRandomElement(platformsWithBuilding[targetPlatform].Buildings);
                    return;
                }
            }

            targetBuilding = null;
        }
        else
        {
            targetBuilding = RandomUtils.GetRandomElement(platformsWithBuilding[targetPlatform].Buildings);
        }

    }
}

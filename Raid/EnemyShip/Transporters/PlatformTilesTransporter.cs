using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformTilesTransporter : EnemyShipTransporter
{
    public event Action<PlatformTile> PlatformTileReached = delegate { };

    private int targetTileIndex;

    public PlatformTilesTransporter(EnemyShipCaptain captain, EnemyShipData enemyUnitData) : base(captain, enemyUnitData)
    {
    }

    public override void Init()
    {
        base.Init();

        targetTileIndex = 0;
        targetPosition = path[targetTileIndex].transform.position;

        captain.StartTurn(targetPosition, enemyUnitData.RotationSpeed);
    }

    public override void Update(float delta)
    {
        if (PlatformTilePathfinder.Count2DDistance(captain.ShipPosition, targetPosition) < startRotationDistance)
        {
            PlatformTileReached(path[targetTileIndex]);

            if (++targetTileIndex >= path.Count)
            {
                EndTransporting();
            }
            else
            {
                targetPosition = path[targetTileIndex].transform.position;
                captain.StartTurn(targetPosition, enemyUnitData.RotationSpeed);

            }
        }
    }
}

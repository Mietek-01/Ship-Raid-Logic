using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointTransporter : EnemyShipTransporter
{
    public PointTransporter(EnemyShipCaptain captain, EnemyShipData enemyUnitData) : base(captain, enemyUnitData)
    {
    }

    public override void Init()
    {
        base.Init();
        captain.StartTurn(targetPosition, enemyUnitData.RotationSpeed);
    }

    public override void Update(float delta)
    {
        if (PlatformTilePathfinder.Count2DDistance(captain.ShipPosition, targetPosition) < 5f)
        {
            EndTransporting();
        }
    }
}

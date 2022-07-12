using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CitadelPortTransporter : EnemyShipTransporter
{
    private Vector3 fixedCitadelPortPosition;
    private bool citadelFieldReached;
    private bool flowingIntoCitadelPort;

    public CitadelPortTransporter(EnemyShipCaptain captain, EnemyShipData enemyUnitData) : base(captain, enemyUnitData)
    {
    }

    public override void Init()
    {
        base.Init();

        fixedCitadelPortPosition = PlatformTilePathfinder.CountPointFromAngle(TargetPositionAngle, enemyUnitData.DistanceToCitadelField);
        citadelFieldReached = false;
        flowingIntoCitadelPort = false;
        captain.StartTurn(fixedCitadelPortPosition , enemyUnitData.RotationSpeed);
    }

    public override void Update(float delta)
    {
        if (!citadelFieldReached)
        {
            if (PlatformTilePathfinder.Count2DDistance(captain.ShipPosition, CitadelPosition) <= enemyUnitData.DistanceToCitadelField
                || PlatformTilePathfinder.ComparePositionsIn2DDimension(captain.ShipPosition, fixedCitadelPortPosition))
            {
                citadelFieldReached = true;

                if (PlatformTilePathfinder.ComparePositionsIn2DDimension(captain.ShipPosition, fixedCitadelPortPosition))
                {
                    flowingIntoCitadelPort = true;
                    captain.StartTurn(targetPosition, enemyUnitData.RotationSpeed * 1.5f);
                }
                else
                {
                    bool clockwiseDirection = PlatformTilePathfinder.DefineClockwiseDirection(captain.ShipAngle, TargetPositionAngle);
                    captain.StartCircularMovement(TargetPositionAngle, enemyUnitData.AngleToEnterCitadelPort, enemyUnitData.DistanceToCitadelField
                        , clockwiseDirection, () => { flowingIntoCitadelPort = true; captain.StartTurn(targetPosition, enemyUnitData.RotationSpeed * 1.5f); });
                }
            }
        }
        else if (flowingIntoCitadelPort)
        {
            if (PlatformTilePathfinder.ComparePositionsIn2DDimension(captain.ShipPosition, targetPosition))
            {
                captain.StartSpeedChanging(0f, 0f);
                EndTransporting();
            }
        }
    }
}

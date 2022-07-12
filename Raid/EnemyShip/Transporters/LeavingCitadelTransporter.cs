using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LeavingCitadelTransporter : EnemyShipTransporter
{
    public event Action CitadelPortLeft = delegate { };

    private bool citadelPortLeft;
    private bool rotation;
    private float rotationCounter;
    private float rotationChange;

    public LeavingCitadelTransporter(EnemyShipCaptain captain, EnemyShipData enemyUnitData) : base(captain, enemyUnitData)
    {
    }

    public override void Init()
    {
        base.Init();

        citadelPortLeft = false;
        rotationCounter = 0f;
        rotation = true;
    }

    public override void Update(float delta)
    {
        if(rotation)
        {
            RotateShip();
            return;
        }

        if (!citadelPortLeft)
        {
            if (PlatformTilePathfinder.Count2DDistance(captain.ShipPosition, CitadelPosition) >= enemyUnitData.DistanceToCitadelField)
            {
                CitadelPortLeft();
                citadelPortLeft = true;

                bool clockwiseDirection = PlatformTilePathfinder.DefineClockwiseDirection(captain.ShipAngle, TargetPositionAngle);

                captain.StartCircularMovement(TargetPositionAngle, enemyUnitData.AngleToLeaveCitadel, enemyUnitData.DistanceToCitadelField + 20f
                    , clockwiseDirection, () => { captain.StartTurn(targetPosition, enemyUnitData.RotationSpeed); EndTransporting(); });
            }
        }
    }

    private void RotateShip()
    {
        rotationChange = enemyUnitData.RotationSpeed * Time.deltaTime;
        rotationCounter += rotationChange;

        if (rotationCounter >= 180f)
        {
            captain.StartSpeedChanging(0f, 1f);
            rotation = false;
        }
        else
        {
            captain.RotateShip(rotationChange);
        }
    }
}

using System;
using UnityEngine;
using UnityEngine.Assertions;

public class EnemyShipCaptain
{
    public readonly float MaxSpeed;
    const float FromWhatAngleTurning = 3f;

    public Vector3 ShipPosition => ship.Position;
    public float ShipAngle => PlatformTilePathfinder.CountAngleFromPosition(ShipPosition);
    public Vector3 ShipDirection => ship.transform.forward;

    public float CurrentSpeed
    {
        get;
        private set;
    }

    public PlatformTilesTransporter InnerZoneTransporter
    {
        get;
        private set;
    }
    public CitadelPortTransporter CitadelPortTransporter
    {
        get;
        private set;
    }

    public LeavingCitadelTransporter LeavingCitadelTransporter
    {
        get;
        private set;
    }

    public PlatformTilesTransporter OuterZoneTransporter
    {
        get;
        private set;
    }

    public PointTransporter PointTransporter
    {
        get;
        private set;
    }

    private EnemyShipTransporter currentTransporter;
    private EnemyShip ship;

    private bool turning;
    private Vector3 targetDirection;
    private Vector3 targetPoint;
    private bool starboardDirection;
    private float rotationSpeed;
    private Action rotationEnded = delegate { };

    private bool circularMovement;
    private float targetCircularMovementAngle;
    private float endValue;
    private float distanceForCircularMovement;
    private bool clockwiseDirection;
    private Action circularMovementEnded = delegate { };

    private bool speedChanging;
    private float howLongSpeedChanging;
    private float timer;
    private float targetSpeed;
    private float oldSpeed;

    public EnemyShipCaptain(EnemyShip enemyShip, float maxSpeed)
    {
        CurrentSpeed = MaxSpeed = maxSpeed;
        ship = enemyShip;
        InnerZoneTransporter = new PlatformTilesTransporter(this, ship.Data);
        CitadelPortTransporter = new CitadelPortTransporter(this, ship.Data);
        LeavingCitadelTransporter = new LeavingCitadelTransporter(this, ship.Data);
        OuterZoneTransporter = new PlatformTilesTransporter(this, ship.Data);
        PointTransporter = new PointTransporter(this, ship.Data);
    }

    public void MyStart()
    {
        currentTransporter = null;
        CurrentSpeed = MaxSpeed;
        turning = circularMovement = speedChanging = false;
    }

    public void Update(float delta)
    {
        if (currentTransporter != null && currentTransporter.Enabled)
        {
            currentTransporter.Update(delta);
        }

        if (turning)
        {
            Turn();
        }

        if (circularMovement)
        {
            CircularMovement();
        }

        if (speedChanging)
        {
            ChangeSpeed(delta);
        }
    }

    public EnemyShipTransporter ActivateTransporter(EEnemyShipDirection transportingType)
    {
        switch (transportingType)
        {
            case EEnemyShipDirection.ToInnerZone:
                currentTransporter = InnerZoneTransporter;
                break;
            case EEnemyShipDirection.ToCitadelPort:
                currentTransporter = CitadelPortTransporter;
                break;
            case EEnemyShipDirection.ToMiddleZone:
                currentTransporter = LeavingCitadelTransporter;
                break;
            case EEnemyShipDirection.ToOuterZone:
                currentTransporter = OuterZoneTransporter;
                break;
            case EEnemyShipDirection.ToPrimaryPosition:
                currentTransporter = PointTransporter;
                break;
        }

        return currentTransporter;
    }

    public bool SetConfusion(bool value)
    {
        if(currentTransporter is CitadelPortTransporter)
        {
            return false;
        }

        if (value)
        {
            //for now
            StartTurn(ShipPosition + (-ShipDirection * 1000f), ship.Data.RotationSpeed / 2f);
            StartSpeedChanging(.5f, .3f);
        }
        else
        {
            StartTurn(currentTransporter.TargetPoint, ship.Data.RotationSpeed / 2f);
            StartSpeedChanging(.5f, 1f);
        }
        
        currentTransporter.Enabled = !value;
        return true;
    }

    public void SetForwardDirection(Vector3 toPoint)
    {
        // I want to ignore y axis
        toPoint.y = ship.transform.position.y;
        ship.transform.forward = (toPoint - ship.transform.position).normalized;
    }

    public void RotateShip(float value)
    {
        ship.transform.Rotate(new Vector3(0, value, 0));
    }

    public void StartTurn(Vector3 targetPoint, float rotationSpeed, Action rotationEnded = null)
    {
        targetDirection = new Vector3(targetPoint.x, 0f, targetPoint.z) - new Vector3(ShipPosition.x, 0f, ShipPosition.z);
        targetDirection.Normalize();

        if (Math.Abs(Vector3.Angle(ShipDirection, targetDirection)) < FromWhatAngleTurning)
        {
            SetForwardDirection(targetPoint);
            rotationEnded?.Invoke();
            return;
        }

        turning = true;
        this.targetPoint = targetPoint;
        this.rotationSpeed = rotationSpeed;
        this.rotationEnded = rotationEnded;

        starboardDirection = Vector2.SignedAngle(new Vector2(ShipDirection.x, ShipDirection.z), new Vector2(targetDirection.x, targetDirection.z)) > 0f;
    }

    public void EndTurning()
    {
        turning = false;
    }

    public void StartCircularMovement(float targetCircularMovementAngle, float endValue, float distanceForCircularMovement, bool clockwiseDirection, Action circularMovementEnded)
    {
        circularMovement = true;
        this.targetCircularMovementAngle = targetCircularMovementAngle;
        this.endValue = endValue;
        this.distanceForCircularMovement = distanceForCircularMovement;
        this.clockwiseDirection = clockwiseDirection;
        this.circularMovementEnded = circularMovementEnded;
    }

    public void EndCircularMovement()
    {
        circularMovement = false;
    }

    public void StartSpeedChanging(float howLongSpeedChanging, float targetSpeedInPercent)
    {
        Assert.IsTrue(targetSpeedInPercent >= 0f && targetSpeedInPercent <= 1f);

        this.howLongSpeedChanging = howLongSpeedChanging;

        speedChanging = true;
        timer = 0f;
        targetSpeed = MaxSpeed * targetSpeedInPercent;
        oldSpeed = CurrentSpeed;
    }

    private void CircularMovement()
    {
        float nextAngle = ShipAngle + (clockwiseDirection ? -1f : 1f);

        Vector3 newTargetPoint = PlatformTilePathfinder.CountPointFromAngle(nextAngle, distanceForCircularMovement);
        SetForwardDirection(newTargetPoint);

        if (Math.Abs(nextAngle - targetCircularMovementAngle) <= endValue)
        {
            circularMovement = false;
            circularMovementEnded?.Invoke();
        }
    }

    private void Turn()
    {
        float turnValue = rotationSpeed * Time.deltaTime;

        if (starboardDirection)
        {
            ship.transform.Rotate(0, -turnValue, 0);
        }
        else
        {
            ship.transform.Rotate(0, turnValue, 0);
        }

        targetDirection = new Vector3(targetPoint.x, 0, targetPoint.z) - new Vector3(ShipPosition.x, 0, ShipPosition.z);
        targetDirection.Normalize();

        bool inRange = Vector2.SignedAngle(new Vector2(ShipDirection.x, ShipDirection.z), new Vector2(targetDirection.x, targetDirection.z)) > 0f;
        
        if ((starboardDirection && !inRange) || (!starboardDirection && inRange))
        {
            SetForwardDirection(targetPoint);
            turning = false;
            rotationEnded?.Invoke();
        }
    }

    private void ChangeSpeed(float delta)
    {
        timer += delta;

        if (timer > howLongSpeedChanging)
        {
            CurrentSpeed = targetSpeed;
            speedChanging = false;
        }
        else
        {
            CurrentSpeed = Mathf.Lerp(oldSpeed, targetSpeed, timer / howLongSpeedChanging);
        }
    }

}

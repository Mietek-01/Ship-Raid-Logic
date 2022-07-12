using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class EnemyShipTransporter
{
    public static readonly Vector3 CitadelPosition = Vector3.zero;

    public event Action TransportingEnded = delegate { };

    public Vector3 TargetPoint => targetPosition;

    protected float TargetPositionAngle => PlatformTilePathfinder.CountAngleFromPosition(targetPosition);

    public bool Enabled
    {
        get;
        set;
    }

    protected EnemyShipCaptain captain;
    protected EnemyShipData enemyUnitData;

    protected Vector3 targetPosition;
    protected List<PlatformTile> path;
    protected float startRotationDistance;

    public abstract void Update(float delta);

    public virtual void Init()
    {
        Enabled = true;
    }

    public EnemyShipTransporter(EnemyShipCaptain captain, EnemyShipData enemyUnitData)
    {
        this.captain = captain;
        this.enemyUnitData = enemyUnitData;
        startRotationDistance = enemyUnitData.StartRotationDistance;
    }

    public void Start(Vector3 targetPoint)
    {
        this.targetPosition = targetPoint;
        Init();
    }

    public void Start(List<PlatformTile> path)
    {
        this.path = path;
        this.targetPosition = path[0].transform.position;
        Init();
    }

    protected void EndTransporting()
    {
        Enabled = false;
        captain.EndTurning();
        captain.EndCircularMovement();
        TransportingEnded();
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyShip : MonoBehaviour, IUpdateable, IDamageable
{
    public event Action<EnemyShip> InnerZoneReached = delegate { };
    public event Action<EnemyShip> Destroyed = delegate { };
    public event Action<EnemyShip> CitadelPortReached = delegate { };
    public event Action<EnemyShip> CitadelRobbed = delegate { };
    public event Action CitadelPortLeft = delegate { };
    public event Action<EnemyShipAttacker, PlatformTile> PlatformTileReached = delegate { };
    public event Action<EnemyShip> Plundering = delegate { };
    public event Action<EnemyShip> RaidFinished = delegate { };

    public Vector3 Position => transform.position;

    public int TypeId
    {
        get;
        private set;
    }

    public int Durability
    {
        get;
        private set;
    }

    public EnemyShipData Data
    {
        get;
        private set;
    }

    public int RobbedKnowledge
    {
        get;
        private set;
    }

    public bool AfterConfusion
    {
        get;
        private set;
    }

    private Vector3 primaryPosition;
    private Vector3 entryPositionToInnerZone;
    private Stopwatch plunderingStopwatch;
    private Stopwatch afterConfusionStopwatch;
    private bool plundering;
    private bool waitingForCitadelPort;
    private bool inConfusion;

    private EnemyShipCaptain captain;
    private EnemyShipAttacker attacker;

    private List<PlatformTile> pathToCitadel;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == Layers.Platform && inConfusion)
        {
            Destroy();
        }
    }

    public void MyUpdate(float delta)
    {
        captain.Update(delta);
        transform.position += transform.forward * captain.CurrentSpeed * delta;

        if (plundering)
        {
            plunderingStopwatch.Update(delta);
        }

        if (attacker.HasTargetBuilding)
        {
            attacker.MyUpdate(delta);
        }

        if (AfterConfusion)
        {
            afterConfusionStopwatch.Update(delta);
        }
    }

    public bool TakeDamage(int damage)
    {
        Durability -= damage;
        if (Durability <= 0)
        {
            Destroy();
            return true;
        }
        return false;
    }

    public void Init(EnemyShipData enemyUnitData, int typeId)
    {
        Data = enemyUnitData;
        TypeId = typeId;

        captain = new EnemyShipCaptain(this, Data.StartSpeed);
        attacker = new EnemyShipAttacker(Data.AttackData.Cooldown, Data.AttackData.Damage);

        captain.InnerZoneTransporter.TransportingEnded += OnInnerZoneReached;
        captain.InnerZoneTransporter.PlatformTileReached += OnNewPlatfromTileReached;

        captain.CitadelPortTransporter.TransportingEnded += OnCitadelPortReached;

        captain.LeavingCitadelTransporter.CitadelPortLeft += OnCitadelPortLeft;
        captain.LeavingCitadelTransporter.TransportingEnded += OnCitadelLeft;

        captain.OuterZoneTransporter.TransportingEnded += OnOuterZoneReached;

        captain.PointTransporter.TransportingEnded += OnPrimaryPositionReached;
    }

    public void MyStart(Vector3 startPosition)
    {
        transform.position = startPosition;
        plundering = false;
        waitingForCitadelPort = false;
        inConfusion = false;
        AfterConfusion = false;
        primaryPosition = startPosition;
        RobbedKnowledge = 0;
        Durability = Data.Durability;

        captain.MyStart();
        attacker.MyStart();

        captain.SetForwardDirection(EnemyShipTransporter.CitadelPosition);
        UpdateManager.Instance.RegisterLogic(this);
    }

    public void StopPlundering()
    {
        plundering = false;
        CitadelRobbed(this);
        StartSailAway();
    }

    public bool ActivateConfusion()
    {
        if(AfterConfusion)
        {
            return false;
        }

        inConfusion = captain.SetConfusion(true);
        return inConfusion;
    }

    public void DeactivateConfusion()
    {
        if (inConfusion)
        {
            AfterConfusion = true;
            inConfusion = false;
            afterConfusionStopwatch = new Stopwatch(2f, () => AfterConfusion = false);
            captain.SetConfusion(false);
        }
    }

    public void WaitForCitadelPort()
    {
        waitingForCitadelPort = true;
        captain.SetForwardDirection(EnemyShipTransporter.CitadelPosition);
        captain.StartSpeedChanging(2f, .3f);
    }

    public void StartAttack(List<PlatformTile> pathToCitadel)
    {
        this.pathToCitadel = pathToCitadel;
        captain.SetForwardDirection(pathToCitadel[0].transform.position);
        captain.ActivateTransporter(EEnemyShipDirection.ToInnerZone).Start(pathToCitadel);
    }

    public void SetMyCurrentPort(CitadelPort citadelPort)
    {
        citadelPort.SetEnemyUnit(this);
        captain.ActivateTransporter(EEnemyShipDirection.ToCitadelPort).Start(citadelPort.Position);

        if (waitingForCitadelPort)
        {
            waitingForCitadelPort = false;
            captain.StartSpeedChanging(2f, 1f);
        }
    }

    private void StartSailAway()
    {
        captain.ActivateTransporter(EEnemyShipDirection.ToMiddleZone).Start(entryPositionToInnerZone);
    }

    private void Destroy()
    {
        Destroyed(this);
        UpdateManager.Instance.UnregisterLogic(this);
    }

    private void OnInnerZoneReached()
    {
        InnerZoneReached(this);
        entryPositionToInnerZone = transform.position;
    }

    private void OnNewPlatfromTileReached(PlatformTile platformTile)
    {
        PlatformTileReached(attacker, platformTile);
    }

    private void OnCitadelPortReached()
    {
        plunderingStopwatch = new Stopwatch(Data.PlunderingTime / (float)Data.Capacity, Plunder);
        plundering = true;
        CitadelPortReached(this);
    }

    private void OnCitadelLeft()
    {
        pathToCitadel.Reverse();
        captain.ActivateTransporter(EEnemyShipDirection.ToOuterZone).Start(pathToCitadel);
    }

    private void Plunder()
    {
        Plundering(this);

        if (plundering && ++RobbedKnowledge == Data.Capacity)
        {
            StopPlundering();
        }
    }

    private void OnOuterZoneReached()
    {
        captain.ActivateTransporter(EEnemyShipDirection.ToPrimaryPosition).Start(primaryPosition);
    }

    private void OnPrimaryPositionReached()
    {
        RaidFinished(this);
        UpdateManager.Instance.UnregisterLogic(this);
    }

    private void OnCitadelPortLeft()
    {
        CitadelPortLeft();
    }
}

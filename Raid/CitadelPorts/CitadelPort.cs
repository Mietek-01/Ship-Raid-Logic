using System;
using UnityEngine;

public class CitadelPort
{
    public event Action<CitadelPort> AvailabilityRestored = delegate { };

    public bool IsFree => enemyShip == null;
    
    public Vector3 Position 
    { 
        get; 
        private set; 
    }

    private EnemyShip enemyShip;

    public CitadelPort(Vector3 position)
    {
        Position = position;
    }

    public void SetEnemyUnit(EnemyShip enemyShip)
    {
        this.enemyShip = enemyShip;
        enemyShip.Destroyed += OnMyEnemyDestroyed;
        enemyShip.CitadelPortLeft += OnMyEnemyCitadelPortLeaved;
    }

    private void OnMyEnemyDestroyed(EnemyShip enemyShip)
    {
        RestoreAvailability();
    }

    private void OnMyEnemyCitadelPortLeaved()
    {
        RestoreAvailability();
    }

    private void RestoreAvailability()
    {
        enemyShip.Destroyed -= OnMyEnemyDestroyed;
        enemyShip.CitadelPortLeft -= OnMyEnemyCitadelPortLeaved;
        enemyShip = null;
        AvailabilityRestored(this);
    }
}

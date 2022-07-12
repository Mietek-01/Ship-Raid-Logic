using System;
using UnityEngine;

[Serializable]
public class EnemyShipAttackData
{
    [field: SerializeField]
    public int Damage
    {
        get;
        private set;
    } = 1;

    [field: SerializeField]
    public float Cooldown
    {
        get;
        private set;
    } = 1f;
}


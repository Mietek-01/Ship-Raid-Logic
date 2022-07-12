using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RaidAttackData
{
    public const int EnemyShipTypes = 3;

    [field: SerializeField]
    public List<int> EnemyForces
    {
        get;
        private set;
    } = new List<int>() { 2, 4, 6 };
}

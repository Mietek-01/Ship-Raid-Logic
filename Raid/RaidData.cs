using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RaidData", menuName = "ScriptableObjects/Raid/Data")]
public class RaidData : ScriptableObject
{
    public IReadOnlyList<EnemyShipData> Enemies => enemies;
    public IReadOnlyList<RaidAttackData> AttacksData => attacksData;

    [field: SerializeField]
    public float EnemyStartDistance
    {
        get;
        private set;
    } = 500f;

    [SerializeField]
    private List<EnemyShipData> enemies = new List<EnemyShipData>();
    [SerializeField]
    private List<RaidAttackData> attacksData = new List<RaidAttackData>();
}

using UnityEngine;

[CreateAssetMenu(fileName = "EnemyShipData", menuName = "ScriptableObjects/Raid/Enemy ship data")]
public class EnemyShipData : ScriptableObject
{
    [field: SerializeField]
    public int Capacity
    {
        get;
        private set;
    } = 10;

    [field: SerializeField]
    public float PlunderingTime
    {
        get;
        private set;
    } = 5f;

    [field: SerializeField]
    public int Durability
    {
        get;
        private set;
    } = 2;

    [field: SerializeField]
    public EnemyShipAttackData AttackData
    {
        get;
        private set;
    } = null;

    [field: SerializeField]
    public float DistanceToCitadelField
    {
        get;
        private set;
    } = 85f;
    
    [field: SerializeField]
    public int AngleToLeaveCitadel // When the difference between the targetPoint and ship angle will be less than this value
    {
        get;
        private set;
    } = 40;

    [field: SerializeField]
    public int AngleToEnterCitadelPort // When the difference between the citadel port and ship angle will be less than this value
    {
        get;
        private set;
    } = 10;

    [field: SerializeField]
    public float StartSpeed
    {
        get;
        private set;
    } = 30f;

    [field: SerializeField]
    public float StartRotationDistance
    {
        get;
        private set;
    } = 20f;

    [field: SerializeField]
    public float RotationSpeed
    {
        get;
        private set;
    } = 100f;

    [field: SerializeField]
    public EnemyShip Prefab
    {
        get;
        private set;
    }
}

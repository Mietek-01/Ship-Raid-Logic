using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

[ExecuteInEditMode]
public class CitadelPortsManager : MonoBehaviour
{
    [SerializeField]
    private int citadelPortDistance = 50;
    [SerializeField]
    private int numberOfPortsPerTile = 3;

    private List<CitadelPort> citadelPorts = new List<CitadelPort>();
    private List<EnemyShip> waitingEnemyShips = new List<EnemyShip>();

    private void OnDrawGizmos()
    {
        foreach (var citadelPort in citadelPorts)
        {
            if (citadelPort == null)
            {
                break;
            }

            Gizmos.color = citadelPort.IsFree ? Color.green : Color.red;
            Gizmos.DrawSphere(citadelPort.Position, 4);
        }
    }

    public void CreateCitadelPorts()
    {
        citadelPorts.Clear();
        float angleDifference = 360f / ((float)(numberOfPortsPerTile) * 6f);

        for (int i = 0; i < numberOfPortsPerTile * 6; i++)
        {
            var port = new CitadelPort(PlatformTilePathfinder.CountPointFromAngle(i * angleDifference, citadelPortDistance));
            port.AvailabilityRestored += OnAvailabilityRestored;
            citadelPorts.Add(port);
        }
    }

    public CitadelPort FindFreeCitadelPort(Vector3 forPosition)
    {
        int nearestCitadelPort = 0;
        float minDistance = PlatformTilePathfinder.Count2DDistance(citadelPorts[nearestCitadelPort].Position, forPosition);

        for (int i = 1; i < citadelPorts.Count; i++)
        {
            float citadelPortDistance = PlatformTilePathfinder.Count2DDistance(citadelPorts[i].Position, forPosition);
            if (citadelPortDistance < minDistance)
            {
                minDistance = citadelPortDistance;
                nearestCitadelPort = i;
            }
        }

        if (!citadelPorts[nearestCitadelPort].IsFree)
        {
            for (int i = 1; i <= citadelPorts.Count; i++)
            {
                int foundIndex = PlatformTilePathfinder.FindNextIndex(citadelPorts.Count, nearestCitadelPort, i, true, (index) => citadelPorts[index].IsFree);

                if (citadelPorts[foundIndex].IsFree)
                {
                    return citadelPorts[foundIndex];
                }
            }

            return null;
        }

        return citadelPorts[nearestCitadelPort];
    }

    public void RegisterWaitingEnemyShip(EnemyShip enemyShip)
    {
        if (!waitingEnemyShips.Contains(enemyShip))
        {
            waitingEnemyShips.Add(enemyShip);
        }
        else
        {
            Assert.IsTrue(false);
        }
    }

    public void UnregisterWaitingEnemyShip(EnemyShip enemyShip)
    {
        if (waitingEnemyShips.Contains(enemyShip))
        {
            waitingEnemyShips.Remove(enemyShip);
        }
    }

    private void OnAvailabilityRestored(CitadelPort obj)
    {
        if(waitingEnemyShips.Count > 0)
        {
            waitingEnemyShips[0].SetMyCurrentPort(obj);
            waitingEnemyShips.RemoveAt(0);
        }
    }
}

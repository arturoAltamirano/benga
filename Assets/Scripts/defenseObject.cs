using TMPro;
using UnityEngine;
using System.Collections.Generic;

public class defenseObject : MonoBehaviour
{
    [SerializeField] private RoundManager RoundManager;

    private void OnTriggerEnter(Collider other)
    /*
    this script is attached to the defenseObject, and will alert round manager to an 
    attacker win by switching the round. defenseObject tells round manager it hit the killzone.
    */
    {
        if (other.CompareTag("KillZone"))
        {
            RoundManager.attackerScore += 1;

            //hypothetically...a defense object could hit during defender building
            RoundManager.SwitchPhase(RoundManager.Phase.Defender);
        }
    }
}
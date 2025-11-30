using TMPro;
using UnityEngine;
using System.Collections.Generic;

public class killZone : MonoBehaviour
{
    [SerializeField] private RoundManager RoundManager;

    private void OnTriggerEnter(Collider other)
    /*
    this script is attached to the killzone, and will alert round manager to an 
    attacker win via the boolean attackerWin on the even that a tagged defense object 
    enters it's collider (which covers the entire floor)
    */
    {
        /*
        if (other.CompareTag("DefenseObject"))
        {

            RoundManager.attackerScore += 1;

            //hypothetically...a defense object could hit during defender building
            RoundManager.SwitchPhase(RoundManager.Phase.Defender);
        }
        */

        if (other.CompareTag("Projectile"))
        {
            //Debug.Log($"Object entered kill zone: {other.name}");

            Destroy(other.gameObject);
        }
    }
}
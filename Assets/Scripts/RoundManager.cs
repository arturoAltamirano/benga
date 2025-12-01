using TMPro;
using UnityEngine;
using System.Collections.Generic;

public class RoundManager : MonoBehaviour
{
    public enum Phase
    {
        Defender,
        Attacker
    }

    [Header("Phase and Rounds")]
    [SerializeField] private float defenderPhaseDuration;
    [SerializeField] private float attackerPhaseDuration;
    [HideInInspector] public Phase currentPhase;
    [HideInInspector] public int attackerScore = 0;
    [HideInInspector] public int defenderScore = 0;

    [Header("Script References")]
    [SerializeField] private DefendManager DefendManager;
    [SerializeField] private AttackManager AttackManager;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI scoreboardText;
    [SerializeField] private TextMeshProUGUI phaseTimerText;
    [SerializeField] private TextMeshProUGUI navigationAid;
    private float scoreboardTimer;

    void Start()
    {
        //we want defender to start the game, so use our switch phase to designate accordingly
        SwitchPhase(Phase.Defender);
    }

    public void SwitchPhase(Phase desiredPhase)
    /**
    We need to switch phases and disable/enable the correct player abilities.
    We set the navigation aid UI, the respective timer, and the access permissions.
    This will be called by many scripts when a change is needed.
    **/
    {
        currentPhase = desiredPhase;

        //switch from attacker to defender
        if (currentPhase == Phase.Defender)
        {
            //Debug.Log("Switch to defense.");

            //Debug.Log($"Start Phase: {currentPhase} to {desiredPhase}");
            navigationAid.text = $"1 - Vertical Pillar \n2 - Horizontal Pillar \n3 - Defense Object \nSpace - Finished Building \nRight Click - Place Object";

            //set the defenders designated timer
            scoreboardTimer = defenderPhaseDuration;

            //set the enabled access params to be checked during update
            AttackManager.IsEnabledAccess = false;
            DefendManager.IsEnabledAccess = true;

            //wipe out all of the wrecking balls at the end of the attacker cycle
            foreach (var ball in AttackManager.spawnedBalls)
            {
                Destroy(ball);
            }

            //and placed objects - we are 'wiping the slate clean'
            foreach (var obj in DefendManager.placedObjects)
            {
                Destroy(obj);
            }

            //if we are going from attacker to defender - the defense object has been destroyed
            DefendManager.defenseObjectPlaced = false;
        }

        //vice versa...defender to attacker
        else if (currentPhase == Phase.Attacker)
        {
            //Debug.Log("Switch to offense.");

            //Debug.Log($"Start Phase: {currentPhase} to {desiredPhase}");
            navigationAid.text = $"1 - Fastball \n2 - Scattershot \n3 - Curveball \nRight Click - Shoot Projectile";

            //set the attackers designated timer
            scoreboardTimer = attackerPhaseDuration;

            //set the enabled access params to be checked during update
            AttackManager.IsEnabledAccess = true;
            DefendManager.IsEnabledAccess = false;

            //we want to reset the ammo count in wreckingBall so we can shoot again
            AttackManager.ResetAmmo();
        }

        //anytime we switch phase - update the scoreboard by default
        scoreboardText.text = $"Attacker: {attackerScore} | Defender: {defenderScore}";
    }

    void Update()
    {
        scoreboardTimer -= Time.deltaTime;

        if (scoreboardTimer <= 10)
        {
            if (currentPhase == Phase.Defender) navigationAid.text = $"Hurry! Set your DefenseObject and hit space bar!";
            else navigationAid.text = $"Hurry! Defender will win when timer strikes 0!";

            //if we have exhausted our timer for whatever phase
            if (scoreboardTimer <= 0)
            {
                //defender exhausted their time - give attacker a point and warn them
                if (currentPhase == Phase.Defender) attackerScore++;
                else defenderScore++;

                //no matter what, we want to go back to defender
                SwitchPhase(Phase.Defender);
                navigationAid.text = $"Time expired!";
                scoreboardText.text = $"Attacker: {attackerScore} | Defender: {defenderScore}";
            }
        }

        //update our timer 
        phaseTimerText.text = $"Phase: {currentPhase}\nTime Left: {scoreboardTimer:F1}s";
    }
}


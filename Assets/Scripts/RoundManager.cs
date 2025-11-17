using UnityEngine;
using TMPro;

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
    [HideInInspector] public bool attackerWin = false;
    [HideInInspector] public int attackerScore = 0;
    [HideInInspector] public int defenderScore = 0;

    [Header("Script References")]
    [SerializeField] private BuildingManager buildingManager;
    [SerializeField] private WreckingBallSpawner wreckingBallSpawner;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI scoreboardText;
    [SerializeField] private TextMeshProUGUI currentlyPlacingText;
    [SerializeField] private TextMeshProUGUI phaseTimerText;
    [SerializeField] private TextMeshProUGUI navigationAid;
    private float scoreboardTimer;

    void Start()
    {
        //we want defender to start the game, so use our switch phase to designate accordingly
        SwitchPhase(Phase.Attacker);
    }

    public void SwitchPhase(Phase currentPhase)
    /**
    We need to switch phases and disable/enable the correct player abilities.
    We set the navigation aid UI, the respective timer, and the access permissions.
    This will be called by many scripts when a change is needed.
    **/
    {
        //if we are switching phases,we are going to check this regardless
        if (attackerWin != true)
        {
            defenderScore++;
            attackerWin = false;
        }

        //switch from attacker to defender
        if (currentPhase == Phase.Attacker)
        {
            //Debug.Log($"Start Phase: {currentPhase} to {desiredPhase}");
            navigationAid.text = $"1 - Vertical Pillar \n2 - Horizontal Pillar \n3 - Defense Object";

            //set the defenders designated timer
            scoreboardTimer = defenderPhaseDuration;

            //set the enabled access params to be checked during update
            wreckingBallSpawner.isEnabledAccess = false;
            buildingManager.isEnabledAccess = true;

            //wipe out all of the wrecking balls at the end of the attacker cycle
            foreach (var ball in wreckingBallSpawner.spawnedBalls)
            {
                Destroy(ball);
            }

            //and placed objects - we are 'wiping the slate clean'
            foreach (var obj in buildingManager.placedObjects)
            {
                Destroy(obj);
            }

            //if we are going from attacker to defender - the defense object has been destroyed
            buildingManager.defenseObjectPlaced = false;

            currentPhase = Phase.Defender;
        }

        //vice versa...defender to attacker
        else if (currentPhase == Phase.Defender)
        {
            //Debug.Log($"Start Phase: {currentPhase} to {desiredPhase}");
            navigationAid.text = $"1 - Fastball \n2 - Scattershot \n3 - Curveball";

            //set the attackers designated timer
            scoreboardTimer = attackerPhaseDuration;

            //set the enabled access params to be checked during update
            wreckingBallSpawner.isEnabledAccess = true;
            buildingManager.isEnabledAccess = false;

            //we want to reset the ammo count in wreckingBall so we can shoot again
            wreckingBallSpawner.ResetAmmo();

            currentPhase = Phase.Attacker;
        }

        //anytime we switch phase - update the scoreboard by default
        scoreboardText.text = $"Attacker: {attackerScore} | Defender: {defenderScore}";
    }

    void Update()
    {
        scoreboardTimer -= Time.deltaTime;

        //if we have exhausted our timer for whatever phase
        if (scoreboardTimer <= 0)
        {
            SwitchPhase(currentPhase);
        }

        //update our timer 
        phaseTimerText.text = $"Phase: {currentPhase}\nTime Left: {scoreboardTimer:F1}s";
    }
}


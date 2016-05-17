using UnityEngine;
using System.Collections;

public class SpatialExp : MonoBehaviour {
    public bool shuffleFixedStimulus = false;
    public int numberOfPracticeTrials = 10;
    public int numberOfTestTrials = 30;
    public int numberOfVisualOnlyTrials = 5;
    public int numberOfAudioOnlyTrials = 5;
    public int numberOfZeroOffsetTrials = 5;
    public int numberOfSpeakers = 8;
    public float visualStimulusPresentationTime = 0.05f;
    public float cameraHeight = 57.9f;
    public float fixedDegreeDistributionStep = 0.1f;
    public float[] fixedDegreePositions;
    public float[] degreeOffsets;
    public float fixationTime = 1.0f;

    float[] fixedDegreeDistributions;
    //Vive stuff
    GameObject hmd, controller1, controller2;

    //Visual Stimuli Objects
    GameObject sphereMap, target, direction, visualStimuli;
    MeshRenderer stimuliVisibility;

    //experiment tracking
    int trialN = 0, currentScore = 0;
    float standingRotation = 0.0f, relativeRotation = 0.0f, currentOffset = 0.0f;
    enum STAGE {practice, test };
    enum TRIAL_STATE { wandering, fixated, runningStimuli, acceptingInput, finished };
    STAGE currentStage = STAGE.practice;
    TRIAL_STATE state = TRIAL_STATE.finished;
    float speakerAngle;

    //gaze tracking
    SteamVR_GazeTracker gazeTracker;
    float gazeStartTime = 0.0f;
    TextMesh timeText;
    //ready checks
    bool hmdReady = false, controller1Ready = false, controller2Ready = false, allReady = false;
	// Use this for initialization
	void Start () {
        speakerAngle = (360.0f / ((float)numberOfSpeakers));
        int len = fixedDegreePositions.Length;
        fixedDegreeDistributions = new float[len];
        for (int i = 0; i < len; i++) fixedDegreeDistributions[i] = (1.0f / ((float)len)) * (i+1);
        printDistributions();

        getDevices();
        sphereMap = GameObject.Find("SphereMap");
        target = GameObject.Find("SphereMap/direction/Target");
        direction = GameObject.Find("SphereMap/direction");
        visualStimuli = GameObject.Find("SphereMap/direction/offsetDirection");
        stimuliVisibility = GameObject.Find("SphereMap/direction/offsetDirection/Visual Stimulus").GetComponent<MeshRenderer>();
        gazeTracker = target.GetComponent<SteamVR_GazeTracker>();
        timeText = GameObject.Find("SphereMap/direction/Target/textHolder/timeText").GetComponent<TextMesh>();
        //set up target based on height input
        target.transform.localPosition = new Vector3(0.0f, cameraHeight, -2.064f);

        //hide the visual stimulus
        stimuliVisibility.enabled = false;
        
	}
	
	// Update is called once per frame
	void Update () {
        if (!allReady) getDevices();
        else runExperiment();
	}

    void getDevices()
    {
        if(!hmdReady)
            hmd = GameObject.Find("[CameraRig]/Camera (head)/Camera (eye)");
        if (hmd) hmdReady = true;
        if(!controller1Ready)
            controller1 = GameObject.Find("[CameraRig]/Controller (left)");
        if (controller1) controller1Ready = true;
        if(!controller2Ready)
            controller2 = GameObject.Find("[CameraRig]/Controller (right)");
        if (controller2) controller2Ready = true;
        if (hmdReady & controller1Ready & controller2Ready) {
            Debug.Log("All devices found!");
            allReady = true; }
    }

    void runExperiment()
    {
        if (trialN > numberOfTestTrials && currentStage == STAGE.test) endExperiment();
        else if (trialN > numberOfPracticeTrials && currentStage == STAGE.practice) currentStage = STAGE.test;
        if (currentStage == STAGE.practice) runPracticeTrial();
        else runTestTrial();
    }
    
    void runPracticeTrial()
    {
        //0 offset
        if(state == TRIAL_STATE.finished)
        {
            //set up for next trial

            
            //offset
            currentOffset = 0.0f;
            //where the participant and target will rotate to:
            standingRotation = speakerAngle* ((float)Random.Range(0, numberOfSpeakers));
            //rotation of the "original" source:
            relativeRotation = chooseRandomDegreePosition();

            //set up environment
            Debug.Log("Rotating target to " + standingRotation +" degrees.");
            direction.transform.localEulerAngles = new Vector3(0.0f, standingRotation, 0.0f);

            Debug.Log("Rotating source to " + (relativeRotation+currentOffset) +" degrees.");
            visualStimuli.transform.localEulerAngles = new Vector3(0.0f, relativeRotation+currentOffset, 0.0f);

            
            //all set up, proceed
            state = TRIAL_STATE.wandering;
        }
        else if(state == TRIAL_STATE.wandering)
        {
            //wait for user's gaze
            if (gazeTracker.isInGaze) {
                if(gazeStartTime == 0.0f) gazeStartTime = Time.time;
                else
                {
                    if((Time.time - gazeStartTime) > fixationTime)
                    {
                        //ready to roll
                        timeText.text = "LOOK AT ME WISAM";
                        gazeStartTime = 0.0f;
                        state = TRIAL_STATE.fixated;
                    }
                    else
                    {
                        //do something cool here
                        timeText.text = (fixationTime - (Time.time - gazeStartTime)).ToString();
                    }
                }
            }
            else
            {
                if (gazeStartTime > 0.0f)
                {
                    gazeStartTime = 0.0f;
                    timeText.text = fixationTime.ToString();
                }
            }
            
        }
        else if(state == TRIAL_STATE.fixated)
        {
            showVisualStimulus();
            //stub for playing sound
            int speakerNumber = (int)Mathf.Floor((standingRotation + relativeRotation) / (speakerAngle));
            state = TRIAL_STATE.runningStimuli;
        }
        else if(state == TRIAL_STATE.acceptingInput)
        {
            stimuliVisibility.enabled = false;
            //pointing  happens here
            Debug.Log("Trial "+trialN + ": ");
            trialN++;
            state = TRIAL_STATE.finished;
        }


    }

    void showVisualStimulus()
    {
        //stub, 
        Debug.Log("Showing Stimulus");
        stimuliVisibility.enabled = true;
        Invoke("hideVisualStimulus", visualStimulusPresentationTime);
    }

    void hideVisualStimulus()
    {
        Debug.Log("Hiding Stimulus");
        stimuliVisibility.enabled = false;
        state = TRIAL_STATE.acceptingInput;
    }

    void runTestTrial()
    {

    }


    void endExperiment()
    {

    }

    //helpers
    float chooseRandomDegreePosition()
    {
        float rnd = Random.value;
        int i = 0;
        while ((fixedDegreeDistributions[i] < rnd))
            i++;
        
        redistributeDegrees(i);
        return fixedDegreePositions[i];
    }

    void redistributeDegrees(int idx)
    {
        int len = fixedDegreeDistributions.Length-1;
        for (int i = 0; i < len; i++)
        {
            if (i == idx) fixedDegreeDistributions[i] += fixedDegreeDistributionStep;
            else fixedDegreeDistributions[i] -= fixedDegreeDistributionStep / ((float)len);
        }
    }

    void printDistributions() { for (int i = 0; i < fixedDegreeDistributions.Length; i++) Debug.Log("distribution " + i + ": " + fixedDegreeDistributions[i]); }
}

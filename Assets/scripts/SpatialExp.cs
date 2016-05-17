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
    public float cameraHeight = 57.9f;
    public float[] fixedDegreePositions;
    public float[] degreeOffsets;
    //Vive stuff
    GameObject hmd, controller1, controller2;

    //Visual Stimuli Objects
    GameObject sphereMap, target, direction, visualStimuli;

    //experiment tracking
    int trialN = 0, currentScore = 0;
    float standingRotation = 0.0f, currentOffset = 0.0f;
    enum STAGE {practice, test };
    enum TRIAL_STATE { wandering, staring, runningStimuli, acceptingInput, finished };
    STAGE currentStage = STAGE.practice;
    TRIAL_STATE state = TRIAL_STATE.finished;

    SteamVR_GazeTracker gazeTracker;

    //ready checks
    bool hmdReady = false, controller1Ready = false, controller2Ready = false, allReady = false;
	// Use this for initialization
	void Start () {
        getDevices();
        sphereMap = GameObject.Find("SphereMap");
        target = GameObject.Find("SphereMap/direction/Target");
        direction = GameObject.Find("SphereMap/direction");
        visualStimuli = GameObject.Find("SphereMap/direction/offsetDirection");

        gazeTracker = target.GetComponent<SteamVR_GazeTracker>();

        //set up target based on height input
        target.transform.localPosition = new Vector3(0.0f, cameraHeight, -2.064f);

        //hide the visual stimulus
        visualStimuli.active = false;
        visualStimuli.transform.localEulerAngles = new Vector3(0.0f, 45.0f, 0.0f);
        visualStimuli.active = true;
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
            
        }

    }

    void runTestTrial()
    {

    }


    void endExperiment()
    {

    }
}

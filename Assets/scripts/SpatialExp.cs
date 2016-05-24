using UnityEngine;
using System.Collections;
using System.IO;

public class SpatialExp : MonoBehaviour {
    public bool shuffleFixedStimulus = false;
    public int numberOfPracticeTrials = 10;
    public int numberOfTestTrials = 30;
    public int numberOfVisualOnlyTrials = 5;
    public int numberOfAudioOnlyTrials = 5;
    public int numberOfZeroOffsetTrials = 5;
    public int numberOfSpeakers = 8;
    public float globalSpeakerRotation = 12;
    public float visualStimulusPresentationTime = 0.05f;
    public float cameraHeight = 57.9f;
    public float fixedDegreeDistributionStep = 0.1f;
    public float percentOfDegreesErrorDeemedCorrect = 10.0f;
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
    enum STAGE { practice, test };
    enum TRIAL_STATE { wandering, fixated, runningStimuli, acceptingInput, finished };
    STAGE currentStage = STAGE.practice;
    TRIAL_STATE state = TRIAL_STATE.finished;
    float speakerAngle;
    private bool currentTrialVisualOnly, currentTrialAudioOnly, currentTrialCongruent;
    float baselineProbability = 0.2f;
    float speakerNumber = 0;

    //gaze tracking
    expGazeTracker gazeTracker;
    float gazeStartTime = 0.0f, inputStartTime = 0.0f;
    TextMesh timeText, uiText;
    //ready checks
    bool hmdReady = false, controller1Ready = false, controller2Ready = false, allReady = false;
    // Use this for initialization

    ExpPointer pointer1, pointer2;
    OscOut osc;

    //logging
    int experimentN = 0;
    StreamWriter log;
    ExperimentMetadata metadata;

    void Start() {
        osc = GetComponent<OscOut>();

        speakerAngle = (360.0f / ((float)numberOfSpeakers));
        int len = fixedDegreePositions.Length;
        fixedDegreeDistributions = new float[len];
        for (int i = 0; i < len; i++) fixedDegreeDistributions[i] = (1.0f / ((float)len)) * (i + 1);

        metadata = GetComponent<ExperimentMetadata>();

        getDevices();
        sphereMap = GameObject.Find("SphereMap");
        target = GameObject.Find("SphereMap/direction/Target");
        direction = GameObject.Find("SphereMap/direction");
        visualStimuli = GameObject.Find("SphereMap/direction/offsetDirection");
        stimuliVisibility = GameObject.Find("SphereMap/direction/offsetDirection/Visual Stimulus").GetComponent<MeshRenderer>();
        gazeTracker = target.GetComponent<expGazeTracker>();
        timeText = GameObject.Find("SphereMap/direction/Target/textHolder/timeText").GetComponent<TextMesh>();
        
        //set up target based on height input
        target.transform.localPosition = new Vector3(0.0f, cameraHeight, -2.064f);

        //hide the visual stimulus
        stimuliVisibility.enabled = false;
        setUpLogFile();
    }

    // Update is called once per frame
    void Update() {
        if (!allReady) getDevices();
        else runExperiment();
    }

    void getDevices()
    {
        if (!hmdReady)
            hmd = GameObject.Find("[CameraRig]/Camera (head)/Camera (eye)");
        if (hmd) hmdReady = true;
        if (!controller1Ready)
            controller1 = GameObject.Find("[CameraRig]/Controller (left)");
        if (controller1)
        {
            pointer1 = controller1.GetComponent<ExpPointer>();
            controller1Ready = true;
        }
        if (!controller2Ready)
            controller2 = GameObject.Find("[CameraRig]/Controller (right)");
        if (controller2)
        {
            pointer2 = controller2.GetComponent<ExpPointer>();
            controller2Ready = true;
        }
        if (hmdReady & controller1Ready & controller2Ready) {
            Debug.Log("All devices found!");
            allReady = true;
            osc.Send("start");
            //get UI text
            uiText = GameObject.Find("[CameraRig]/Camera (head)/uiText").GetComponent<TextMesh>();
            uiText.text = "";
            timeText.text = "Please stare at the bullseye.";
        }
    }

    void showUIText(string txt, float seconds)
    {
        uiText.text = txt;
        Invoke("hideUIText", seconds);
    }

    void hideUIText()
    {
        uiText.text = "";
    }

    void runExperiment()
    {
        if (trialN > numberOfTestTrials && currentStage == STAGE.test)
        {
            if (numberOfAudioOnlyTrials > 0 || numberOfVisualOnlyTrials > 0 || numberOfZeroOffsetTrials > 0)
            {
                Debug.Log("Still need to do baseline trials. Running extra trials.");
                baselineProbability = 1.1f;
                runTrial();
            }
            else endExperiment();
        }
        else if (trialN > numberOfPracticeTrials && currentStage == STAGE.practice)
        {
            Debug.Log("Practice done. Starting Test trials...");
            currentStage = STAGE.test;
            trialN = 0;
            timeText.gameObject.active = false;
        }
        else runTrial();
    }

    void setUpTrial()
    {
        //offset
        if (currentStage == STAGE.practice) currentOffset = 0.0f;
        else
        {
            currentOffset = degreeOffsets[degreeOffsets.Length - (currentScore % degreeOffsets.Length) - 1];
            if (Random.value < baselineProbability && (numberOfAudioOnlyTrials > 0))
            {
                Debug.Log("Starting audio only trial.");
                numberOfAudioOnlyTrials--;
                currentTrialAudioOnly = true;
                currentOffset = 0.0f;
            }
            else if (Random.value < baselineProbability && (numberOfVisualOnlyTrials > 0))
            {
                Debug.Log("Starting visual only trial.");
                numberOfVisualOnlyTrials--;
                currentOffset = 0.0f;
                currentTrialVisualOnly = true;
            }
            else if (Random.value > baselineProbability && (numberOfZeroOffsetTrials > 0))
            {
                Debug.Log("Starting congruent trial.");
                numberOfZeroOffsetTrials--;
                currentOffset = 0.0f;
                currentTrialCongruent = true;
            }
        }
        //where the participant and target will rotate to:
        speakerNumber = ((float)Random.Range(0, numberOfSpeakers));
        standingRotation = speakerAngle *speakerNumber  - globalSpeakerRotation;
        //rotation of the "original" source:
        relativeRotation = chooseRandomDegreePosition();
        //eh. We'll fix this for non discretely positioned speaker sources.
        if (relativeRotation > 0) speakerNumber++;
        else if (relativeRotation < 0) speakerNumber--;
        //set up environment
        direction.transform.localEulerAngles = new Vector3(0.0f, standingRotation, 0.0f);

        visualStimuli.transform.localEulerAngles = new Vector3(0.0f, relativeRotation + currentOffset, 0.0f);



        //all set up, proceed
        state = TRIAL_STATE.wandering;
    }

    void waitForGaze()
    {
        if (gazeTracker.isInGaze)
        {
            if (gazeStartTime == 0.0f) gazeStartTime = Time.time;
            else
            {
                if ((Time.time - gazeStartTime) > fixationTime)
                {
                    //ready to roll
                    timeText.text = "LOOK\nAT\nME";
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
                timeText.text = "Please stare at the bullseye.";
            }
        }
    }

    void triggerStimulus()
    {
        showVisualStimulus();
        //stub for playing sound
        if (!currentTrialVisualOnly) osc.Send("spkr", speakerNumber);
        state = TRIAL_STATE.runningStimuli;
    }

    void getPointerInput()
    {
        stimuliVisibility.enabled = false;
        //pointing  happens here
        if (pointer1.newInput)
        {
            if (currentStage == STAGE.test) logTrial(pointer1.lastAzimuth, pointer1.lastElevation, pointer1.lastTime - inputStartTime);
            if (currentStage == STAGE.practice)
            {
                float targetAz = (standingRotation + relativeRotation);
                float azError = pointer1.lastAzimuth - targetAz;
                azError = Mathf.Min(azError, 360.0f - azError);
                showUIText("Got it!\nYou were " + azError + " off.", 4.0f);
            }
            trialN++;
            timeText.text = "Please stare at the bullseye.";
            state = TRIAL_STATE.finished;
        }
        else if (pointer2.newInput)
        {
            if (currentStage == STAGE.test) logTrial(pointer2.lastAzimuth, pointer2.lastElevation, pointer2.lastTime - inputStartTime);
            if (currentStage == STAGE.practice)
            {
                float targetAz = (standingRotation + relativeRotation);
                float azError = pointer2.lastAzimuth - targetAz;
                azError = Mathf.Min(azError, 360.0f - azError);
                showUIText("Cool!\nYou were " + Mathf.RoundToInt(azError) + "\ndegrees off.", 4.0f);
            }
            trialN++;
            timeText.text = "Please stare at the bullseye.";
            state = TRIAL_STATE.finished;
        }
    }
    void runTrial()
    {
        //0 offset
        if (state == TRIAL_STATE.finished)
        {
            //set up for next trial
            setUpTrial();

        }
        else if (state == TRIAL_STATE.wandering)
        {
            //wait for user's gaze
            waitForGaze();

        }
        else if (state == TRIAL_STATE.fixated)
        {
            triggerStimulus();
        }
        else if (state == TRIAL_STATE.acceptingInput)
        {

            getPointerInput();
        }


    }

    void setUpLogFile()
    {
        string fileName = "results/space/SpaceExperiment" + experimentN + ".csv";
        while (File.Exists(fileName)) {
            experimentN++;
            fileName = "results/space/SpaceExperiment" + experimentN + ".csv";
        }
        log = new StreamWriter(fileName);
        log.WriteLine("TrialNumber,StandingRotation,SourceOffset,VisualOffset,azimuthInput,ElevationInput,inputTime,baseline");
        //create metadata log
        StreamWriter meta = new StreamWriter("results/space/SpaceExperiment" + experimentN + "metadata.json");
        meta.WriteLine(JsonUtility.ToJson(metadata));
        meta.Close();
    }

    void logTrial(float az, float el, float t)
    {
        float targetAz = (standingRotation + relativeRotation);
        float azError = az - targetAz;
        azError = Mathf.Min(azError, 360.0f - azError);
        //criteria right?
        if (Mathf.Abs(azError) <= currentOffset * percentOfDegreesErrorDeemedCorrect && 
            (!(currentTrialCongruent||currentTrialAudioOnly||currentTrialVisualOnly))) currentScore++;

        string status = trialN + "," +
            standingRotation +
            "," + relativeRotation +
            "," + currentOffset +
            "," + azError +
            "," + el +
            "," + t;
        if (currentTrialAudioOnly) status += ",audio";
        else if (currentTrialVisualOnly) status += ",visual";
        else if (currentTrialCongruent) status += ",congruent";
        else status += ",null";
        log.WriteLine(status);
        Debug.Log("Trial " + trialN + ". Offset " + currentOffset + "Pointing Error: " + azError);
        
        currentTrialVisualOnly = false; currentTrialAudioOnly = false; currentTrialCongruent = false;
    }

    void showVisualStimulus()
    {

        if(!currentTrialAudioOnly) stimuliVisibility.enabled = true;
        Invoke("hideVisualStimulus", visualStimulusPresentationTime);
    }

    void hideVisualStimulus()
    {
        stimuliVisibility.enabled = false;

        //clear pointers
        pointer1.clearInput();
        pointer2.clearInput();
        //get start time
        inputStartTime = Time.time;
        timeText.text = "Please point at the source.";
        state = TRIAL_STATE.acceptingInput;
    }

    void endExperiment()
    {
        timeText.gameObject.active = true;
        timeText.text = "EXPERIMENT\nDONE";
        log.Close();
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

    void onDestroy()
    {
        log.Close();
    }

    void printDistributions() { for (int i = 0; i < fixedDegreeDistributions.Length; i++) Debug.Log("distribution " + i + ": " + fixedDegreeDistributions[i]); }
}

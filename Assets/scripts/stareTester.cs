using UnityEngine;
using System.Collections;

public class stareTester : MonoBehaviour {
    //config
    public float errorRadius = 2.0f; //in degrees

    public bool isLookingAtTarget = false;
    public float timeSpentLookingAtTarget = 0.0f;

    //game objects
    GameObject target;
    Ray ray;
	void Start () {
        target = GameObject.Find("SphereMap/direction/Target");
        ray = new Ray(transform.position, transform.eulerAngles);
	}
	

	void Update () {
	    
	}
}

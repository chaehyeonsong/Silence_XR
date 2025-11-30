using SoftKitty.LiquidContainer;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Communicator : MonoBehaviour
{
    //Communication between external scripts and internal scripts should only be done indirectly
    //via accessing this script, do not access scripts or variables directly
    [Tooltip("Should be indentical to X, Y, Z scale of Linetracer_Game")]
    public float GameScale = 1f;
    public GameObject PlaygroundHitDetector_left;
    public GameObject PlaygroundHitDetector_right;
    public GameObject PlaygroundHitDetector_horizontal;
    public GameObject Playground;
    public GameObject unhitdetector;
    [HideInInspector]
    public LiquidControl flask;
    public NewCurveVisualizer newCurveVisualizer;
    public BoundMover boundMover;
    [HideInInspector]
    public Vector3 normal;
    
    //Status board
    [HideInInspector]
    public bool out_of_playground = false;
    [HideInInspector]
    public bool missed_line = false;
    [HideInInspector]
    public bool is_game_running = false; 
    [HideInInspector]
    public bool finish_broadcast = false;

    void Start() //this would run once when linetracer game is instantiated
    {
        //initialize detectors playground and unhitdetector
        unhitdetector.GetComponent<UnhitSphereDetector>().Init(this);
        //Debug.Log("Initialized UnhitSphere");
        PlaygroundHitDetector_left.GetComponent<PlaygroundHitDetector>().Init(this);
        //Debug.Log("Initialized PlaygroundHitDetector_left");
        PlaygroundHitDetector_right.GetComponent<PlaygroundHitDetector>().Init(this);
        //Debug.Log("Initialized PlaygroundHitDetector_right");
        PlaygroundHitDetector_horizontal.GetComponent<PlaygroundHitDetector>().Init(this);
        //Debug.Log("Initialized PlaygroundHitDetector_horizontal");

    }

    public void Prepare_game(string difficulty)
    {
        newCurveVisualizer.game_scale = GameScale;
        newCurveVisualizer.Prepare(difficulty);
        boundMover.flask_to_track = flask;
        boundMover.normal = normal;
    }
    void Update()
    {
        
    }
}

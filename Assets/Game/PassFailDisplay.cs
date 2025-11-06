using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PassFailDisplay : MonoBehaviour
{
    //status input objects
    public GameObject PlaygroundHitDetector_left;
    public GameObject PlaygroundHitDetector_right;
    public GameObject PlaygroundHitDetector_horizontal;
    public GameObject Playground;
    public GameObject unhitdetector;
    public CurveVisualizer curve_script;
    
    //materials to change into
    public Material pass_shader;
    public Material fail_shader;

    //status indicator lights
    public GameObject passlight;
    public GameObject faillight;

    //conditional input booleans
    [HideInInspector]
    public bool out_of_playground = false;
    [HideInInspector]
    public bool missed_line = false;

    private bool did_you_pass = false;

    

    void Start()
    {
        //initialize detectors in playground and unhitdetector
        unhitdetector.GetComponent<UnhitSphereDetector>().Init(this);
        PlaygroundHitDetector_left.GetComponent<PlaygroundHitDetector>().Init(this);
        PlaygroundHitDetector_right.GetComponent<PlaygroundHitDetector>().Init(this);
        PlaygroundHitDetector_horizontal.GetComponent<PlaygroundHitDetector>().Init(this);
    }

    
    void Update()
    {
        if (out_of_playground == true & did_you_pass == false)
        {
            MeshRenderer fail = faillight.GetComponent<MeshRenderer>();
            fail.material = fail_shader;
        }

        if (missed_line == true & did_you_pass == false)
        {
            MeshRenderer fail = faillight.GetComponent<MeshRenderer>();
            fail.material = fail_shader;         
        }

        if (out_of_playground == false & missed_line == false & curve_script.finish_broadcast == true)
        {
            MeshRenderer pass = passlight.GetComponent<MeshRenderer>();
            pass.material = pass_shader;
            did_you_pass = true;            
        }
    }
}

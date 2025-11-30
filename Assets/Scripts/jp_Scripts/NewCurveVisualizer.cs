using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class NewCurveVisualizer : MonoBehaviour
{
    [Tooltip("easy, normal, hard, insane")]
    public string difficulty = "normal";

    [Tooltip("Curve size multiplier")]
    public float scaler = 1f;

    [Tooltip("Prefab for point colliders (should have SphereCollider, IsTrigger = true)")]
    public GameObject pointPrefab;

    [Tooltip("Material for the normal line")]
    public Material lineMaterial;

    [Tooltip("Material applied to collided line sections")]
    public Material hitMaterial;

    [Tooltip("Line width in Unity units, keep identical to pointPrefab scale")]
    public float lineWidth = 0.05f;

    [Tooltip("Max height of curve allowed on screen")]
    public int cutoff = 300;

    [Tooltip("The object to detect collision with, must have collider")]
    public GameObject targetObject;

    public Communicator communicator;
    [HideInInspector]
    public float game_scale;

    private float scale_multiplier = 0.006f;
    private LineRenderer lr;
    private List<Vector3> points = new List<Vector3>();
    private List<GameObject> sphere_points = new List<GameObject>();

    [HideInInspector]
    public List<int> collided_spheres = new List<int>();

    private Vector3 Original_position;
    private Vector3 Current_position;
    private Vector3 Displacement;
    private float curr_displacement;
    private int steps;

    public void Prepare(string difficulty)//prepare setup for new curve game session
    {
        scale_multiplier *= game_scale;
        //Curve coordinates generation
        Debug.Log(difficulty);
        (List<float> x, List<float> y) = CurveGenerator.GenerateCurve(difficulty, scaler);

        //Convert to Vector3 list and add to points
        for (int i = 0; i < x.Count; i++)
        {
            points.Add(new Vector3(x[i], y[i], 0f));
        }

        collided_spheres = Enumerable.Repeat(0, points.Count).ToList();

        //Spawn all point spheres in advance for performance, store them in sphere_points
        for (int i = 0; i < points.Count; i++)
        {
            Vector3 worldPos = transform.TransformPoint(points[i]);
            GameObject p = Instantiate(pointPrefab, worldPos, Quaternion.identity, transform);
            p.SetActive(true);
            //p.hideFlags = HideFlags.HideInHierarchy;
            MeshRenderer m = p.GetComponent<MeshRenderer>();
            m.material = hitMaterial;
            var detector = p.GetComponent<CollisionDetector>();
            detector.Init(this, i);

            sphere_points.Add(p);

            //Initial position set
            Original_position = transform.position;

        }
    }

    void Update()
    {
        if (communicator.is_game_running == true) //run the curve game session
        {
            //deciding where and how much of curve to draw
            Current_position = transform.position;
            Displacement = Current_position - Original_position;
            curr_displacement = Displacement.magnitude;
            steps = Mathf.FloorToInt(curr_displacement / scale_multiplier) + 1;

            if (steps <= points.Count + cutoff)
            {
                Destroy(lr);
                DrawCurve(points, steps, cutoff);
            }
            else
            {
                Destroy(lr);
                communicator.finish_broadcast = true;
            }

            //rendering collision spheres(keep out of DrawCurve)
            if ((steps - cutoff) <= points.Count)
            {
                for (int i = 0; i < points.Count; i++)
                {
                    if (collided_spheres[i] == 1)
                    {
                        MeshRenderer m = sphere_points[i].GetComponent<MeshRenderer>();
                        m.enabled = true;
                    }
                }
            }

            //removing spheres that left playground
            if (steps > cutoff & (steps - cutoff) <= points.Count)
            {
                for (int i = 0; i < steps - cutoff; i++)
                {
                    MeshRenderer m = sphere_points[i].GetComponent<MeshRenderer>();
                    m.enabled = false;
                }
                //Debug.Log("Removed sphere " + steps.ToString());
            }
            //steps tend to skip numbers, so this removes any redundant spheres when curve visualization is 
            //over
            else if (steps - cutoff > points.Count)
            {
                for (int i = 0; i < 3; i++)
                {
                    MeshRenderer m = sphere_points[points.Count - 3 + i].GetComponent<MeshRenderer>();
                    m.enabled = false;
                }
            }
        }
        else //linetracer game stopped for whatever reason
        {
            //destroy any remaining line segment
            Destroy(lr);
            //destroy all instantiated point spheres
            try
            {
                foreach (GameObject trash in sphere_points)
                {
                    if (trash != null)
                    {
                        Destroy(trash);
                    }
                }
            }
            catch
            {
                //do nothing if any exceptions occur
            }
            finally //ensure data destruction
            {
                sphere_points.Clear();
            }
        }
    }


    void DrawCurve(List<Vector3> pts, int limit, int cutoff)
    {
        lr = new GameObject("CurveLine").AddComponent<LineRenderer>();
        lr.transform.parent = transform; //the parent transform of lr is transform of CurveVisualizer
        
        lr.material = lineMaterial;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.useWorldSpace = false; //ensures spawned points follow CurveVisualzier coordinates, not world
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        lr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        //lr.hideFlags = HideFlags.HideInHierarchy;


        if (limit > cutoff & limit <= pts.Count)
        {
            //determine curve region to be drawn
            Vector3[] localPoints = new Vector3[cutoff];
            lr.positionCount = cutoff;
            int bias = limit - cutoff;

            for (int i = 0; i < cutoff; i++)
            {
                localPoints[i] = transform.TransformPoint(pts[i + bias]);
            }
            lr.SetPositions(localPoints);

        }
        else if (limit > pts.Count)
        {
            //determine curve region to be drawn
            int bias = limit - pts.Count;
            Vector3[] localPoints = new Vector3[cutoff - bias];
            lr.positionCount = cutoff - bias;

            for (int i = 0; i < cutoff - bias; i++)
            {
                localPoints[i] = transform.TransformPoint(pts[i + limit - cutoff]);
            }
            lr.SetPositions(localPoints);


        }
        else
        {
            //determine curve region to be drawn
            Vector3[] localPoints = new Vector3[limit];
            lr.positionCount = limit;

            for (int i = 0; i < limit; i++)
            {
                localPoints[i] = transform.TransformPoint(pts[i]);
            }
            lr.SetPositions(localPoints);

        }

    }
}

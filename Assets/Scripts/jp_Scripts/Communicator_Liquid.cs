using SoftKitty.LiquidContainer;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class Communicator_Liquid : MonoBehaviour
{
    [Tooltip("Prefab for empty LiquidControl flask")]
    public LiquidControl empty_flask; //prefab of empty flask
    public GameObject sign_prefab;
    [HideInInspector]
    public GameController gameController;
    [Tooltip("How much angular change for next source flask placement")]
    public float flask_adjustment;
    [Tooltip("Where to place mixing flask")]
    public Vector3 mixing_flask_position;
    [HideInInspector]
    public Color target = Color.black;
    [HideInInspector]
    public LiquidControl target_flask;
    [HideInInspector]
    public List<LiquidControl> source_flask_list;

    private const float pi = Mathf.PI;
    private float flask_adjustment_rad = 0f;


    void Start()
    {
        
    }
        

    void Update()
    {
        
    }

    public void ShuffleList<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }

    public void GenerateNewGame(string difficulty)
    {
        //difficulty presets: num_flasks, similarity
        var preset = new Dictionary<string, float[]>
        {
            { "easy",   new float[]{3f, 0.80f} },
            { "normal", new float[]{4f, 0.85f} },
            { "hard",   new float[]{5f, 0.90f} },
            { "insane", new float[]{6f, 0.95f} },
        };

        gameController.similarity_goal = preset[difficulty][1];

        //creating mixing ratios
        int num_flask = (int)preset[difficulty][0];
        List<float> ratios = new List<float>();
        List<float> hues = new List<float>();
        List<float> saturations = new List<float>();
        List<float> values = new List<float>();

        //creating ratios
        float sum_ratio = 0f;
        //create proto-ratio values to be used
        for (int i = 0; i < num_flask; i++)
        {
            float ratio = Random.Range(-1f, 1f) * 1.5f;
            ratio += 5f;
            ratios.Add(ratio);
            sum_ratio += ratio;
        }
        //normalize proto-ratio values to ratio
        for (int i = 0; i < num_flask; i++)
        {
            ratios[i] /= sum_ratio;
        }

        //creating hues, trying to prevent color clustering problem
        float spacingAngle = 360f / num_flask;
        float initialAngle = 360f * Random.Range(0f, 1f);
        for(int i = 0; i < num_flask; i++)
        {
            float hue = initialAngle + spacingAngle * i + spacingAngle * 
                        Random.Range(-1f, 1f) * 0.2f;
            hue = (hue % 360f) / 360f;
            hues.Add(hue);
        }
        ShuffleList(hues);

        //creating saturations
        float spacer = 0.5f / (num_flask + 1);
        for (int i = 0; i < num_flask; i++)
        {
            float saturation = 0.5f + spacer * (i + 1) + spacer *
                               Random.Range(-1f, 1f) * 0.3f;
            saturations.Add(saturation);
        }

        //creating values
        for (int i = 0; i < num_flask; i++)
        {
            float value = 0.5f + spacer * (i + 1) + spacer *
                          Random.Range(-1f, 1f) * 0.3f;
            values.Add(value);
        }

        flask_adjustment_rad = flask_adjustment * Mathf.Deg2Rad;
        float flask_movement = 0f;
        bool isOdd = (num_flask & 1) == 1;
        bool isEven = (num_flask & 1) == 0;

        for (int i = 0; i < num_flask; i++)
        {
            Color randomColor = Color.HSVToRGB(hues[i], saturations[i], values[i]);

            if (isOdd)
            {
                float init_angle = pi / 2 + flask_adjustment_rad * (num_flask - 1) / 2;
                Debug.Log(init_angle * Mathf.Rad2Deg);

                Vector3 temp_displacement = new Vector3(Mathf.Cos(flask_movement + init_angle)
                                                        * Mathf.Abs(mixing_flask_position.z), 
                                                        0f, (Mathf.Sin(flask_movement + init_angle)
                                                        - 1) * Mathf.Abs(mixing_flask_position.z));

                LiquidControl newFlask = Instantiate(empty_flask, transform.position +
                                                 temp_displacement, transform.rotation, transform);
                source_flask_list.Add(newFlask);
                newFlask.GetComponent<FlaskCollisionDetector>().Init(gameController);
                newFlask.FillInLiquid(0.002f, randomColor, randomColor);
            }

            if (isEven)
            {
                float init_angle = pi / 2 + flask_adjustment_rad / 2 + flask_adjustment_rad *
                                   (num_flask / 2 - 1);

                Vector3 temp_displacement = new Vector3(Mathf.Cos(flask_movement + init_angle)
                                                        * Mathf.Abs(mixing_flask_position.z),
                                                        0f, (Mathf.Sin(flask_movement + init_angle)
                                                        - 1) * Mathf.Abs(mixing_flask_position.z));

                LiquidControl newFlask = Instantiate(empty_flask, transform.position +
                                                 temp_displacement, transform.rotation, transform);
                source_flask_list.Add(newFlask);
                newFlask.GetComponent<FlaskCollisionDetector>().Init(gameController);
                newFlask.FillInLiquid(0.002f, randomColor, randomColor);
            }

            flask_movement -= flask_adjustment_rad; //move instantiation position so no overlap

            target += randomColor * ratios[i];
        }
        
        gameController.target_color = target;

        Vector3 goal_offset = new Vector3(0f, 0.2f, 0.2f);
        GameObject color_sign = Instantiate(sign_prefab, transform);
        color_sign.transform.position += goal_offset;
        color_sign.GetComponent<Renderer>().material.color = target;
        
        //Instantiate target flask
        target_flask = Instantiate(empty_flask, transform.position + mixing_flask_position, 
                                   transform.rotation, transform);
        target_flask.FillInLiquid(0.00001f, Color.white, Color.white);
        target_flask.GetComponent<FlaskCollisionDetector>().Init(gameController);
        //for testing purposes only
        target_flask.FillInLiquid(0.002f, target, target);
    }

    public void NowChecking()
    {
        foreach (var flask in source_flask_list)
        {
            Destroy(flask.gameObject);
        }
    }
}

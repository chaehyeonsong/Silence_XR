using SoftKitty.LiquidContainer;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Communicator_Checker : MonoBehaviour
{
    [HideInInspector]
    public LiquidControl target_flask;
    [HideInInspector]
    public GameController gameController;
    [HideInInspector]
    public Color answer;
    [HideInInspector]
    public bool flask_in_checkbox = false;
    [HideInInspector]
    public bool result = false;
    [HideInInspector]
    public bool check_complete = false;
    public GameObject checkBox;
    //public List<GameObject> BoxFrame = new List<GameObject>();
    public Material inMaterial;

    void Start()
    {
        inMaterial.DisableKeyword("_EMISSION");
        answer = gameController.target_color;
        checkBox.GetComponent<CheckboxHitDetector>().Init(this);

    }

    private void OnDisable()
    {
        inMaterial.DisableKeyword("EMISSION");
    }

    public void Check_answer()
    {
        if (flask_in_checkbox)
        {
            Color finalColor = target_flask.LiquidMeshFilter.GetComponent<MeshRenderer>()
                .material.GetColor("_TopColor");
            float similarity = ColorSimilarity(finalColor, answer);
            result = similarity >= gameController.similarity_goal;
            check_complete = true;

        }
        //get target flask color and compare it with answer, and difficulty
    }

    void Update()
    {
        if (flask_in_checkbox)
        {
            inMaterial.EnableKeyword("_EMISSION");
        }
        else
        {
            inMaterial.DisableKeyword("_EMISSION");
        }
    }

    private float ColorSimilarity(Color a, Color b)
    {
        Color.RGBToHSV(a, out float ha, out float sa, out float va);
        Color.RGBToHSV(b, out float hb, out float sb, out float vb);

        float hueDiff = Mathf.Min(Mathf.Abs(ha - hb), 1f - Mathf.Abs(ha - hb)); // hue wraps around
        float satDiff = Mathf.Abs(sa - sb);
        float valDiff = Mathf.Abs(va - vb);

        float distance = (hueDiff * 0.6f) + (satDiff * 0.2f) + (valDiff * 0.2f);
        return Mathf.Clamp01(1f - distance);
    }
}

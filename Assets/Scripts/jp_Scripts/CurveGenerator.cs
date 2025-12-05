using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CurveGenerator
{
    private const float PI = Mathf.PI;

    public static (List<float> x, List<float> y) GenerateCurve(string difficulty, float scaler)
    {
        // difficulty presets: num_nodes, min_radius, lower_st, upper_st
        var preset = new Dictionary<string, float[]>
        {
            { "easy",   new float[]{10f, scaler * 5f, 0.4f, 0.5f} },
            { "normal", new float[]{16f, scaler * 4f, 0.5f, 0.6f} },
            { "hard",   new float[]{22f, scaler * 3f, 0.6f, 0.7f} },
            { "insane", new float[]{30f, scaler * 2f, 0.7f, 1.0f} },
        };

        if (!preset.ContainsKey(difficulty))
        {
            Debug.LogError($"Unknown difficulty '{difficulty}'");
            return (new List<float>(), new List<float>());
        }

        int numNodes = Mathf.RoundToInt(preset[difficulty][0]);
        float minRadius = preset[difficulty][1];
        float maxRadius = scaler * 8f;
        float zero_mean = (maxRadius + minRadius) / 2;
        float maxStdev = (maxRadius - minRadius) / 2f;
        float lowerStdev = maxStdev * preset[difficulty][2];
        float upperStdev = maxStdev * preset[difficulty][3];

        int numInterNodes = 60;

        List<float> baseRadii = new List<float>();
        System.Random rand = new System.Random();

        // --- helper functions ---
        float CosLink(float startVal, float endVal, float x, float startX, float endX)
        {
            float t = (x - startX) / (endX - startX);
            return ((startVal - endVal) / 2f) * Mathf.Cos(PI * t) + (startVal + endVal) / 2f;
        }

        float StdDev(List<float> list)
        {
            float mean = 0f;
            foreach (float v in list) mean += v;
            mean /= list.Count;

            float variance = 0f;
            foreach (float v in list)
                variance += (v - mean) * (v - mean);

            return Mathf.Sqrt(variance / list.Count);
        }

        // --- generate base radii ---
        int tries = 0;
        float baseStdev = 0f;

        do
        {
            baseRadii.Clear();
            for (int i = 0; i < numNodes; i++)
                baseRadii.Add(UnityEngine.Random.Range(minRadius, maxRadius));

            baseStdev = StdDev(baseRadii);
            tries++;
        }
        while ((baseStdev < lowerStdev || baseStdev > upperStdev) && tries < 2000);

        // --- interpolate ---
        List<float> interpRadii = new List<float>();

        for (int i = 0; i < baseRadii.Count - 1; i++)
        {
            interpRadii.Add(baseRadii[i]);

            for (int j = 0; j < numInterNodes; j++)
            {
                float x = i + (j + 1f) / (numInterNodes + 1f);
                float val = CosLink(baseRadii[i], baseRadii[i + 1], x, i, i + 1);
                interpRadii.Add(val);
            }
        }

        // no closure — open curve

        // --- convert to XY ---
        List<float> xList = new List<float>();
        List<float> yList = new List<float>();

        //for line curve
        int init_straight_len = 160;

        for (int i = 0; i < init_straight_len; i ++)
        {
            xList.Add(interpRadii[0] - zero_mean);
            yList.Add((float)i * 0.006f);
        }

        int N = interpRadii.Count;
        for (int i = 0; i < N; i++)
        {
            xList.Add(interpRadii[i] - zero_mean);
            float scaled_i = ((float)i + (float)init_straight_len) * 0.006f;
            yList.Add(scaled_i);
        }


        //for circle curve
        //for (int i = 0; i < N; i++)
        //{
         //   float theta = 2f * PI * i / (numNodes * numInterNodes);
           // xList.Add(interpRadii[i] * Mathf.Cos(theta));
          //  yList.Add(interpRadii[i] * Mathf.Sin(theta));
        //}

        return (xList, yList);
    }
}
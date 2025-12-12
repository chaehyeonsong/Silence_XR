using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LiquidColorManager : MonoBehaviour
{

    public GameObject EasyTrim;
    public GameObject NormalTrim;
    public GameObject HardTrim;
    public GameObject InsaneTrim;

    void Start()
    {

        Reset();

    }

    void Awake()
    {

        Reset();

    }

    public void Reset()
    {

        EasyTrim.SetActive(false);
        NormalTrim.SetActive(false);
        HardTrim.SetActive(false);
        InsaneTrim.SetActive(false);

    }

    public void SelectedEasy()
    {

        EasyTrim.SetActive(true);
        NormalTrim.SetActive(false);
        HardTrim.SetActive(false);
        InsaneTrim.SetActive(false);

    }

    public void SelectedNormal()
    {

        EasyTrim.SetActive(false);
        NormalTrim.SetActive(true);
        HardTrim.SetActive(false);
        InsaneTrim.SetActive(false);

    }

    public void SelectedHard()
    {

        EasyTrim.SetActive(false);
        NormalTrim.SetActive(false);
        HardTrim.SetActive(true);
        InsaneTrim.SetActive(false);

    }

    public void SelectedInsane()
    {

        EasyTrim.SetActive(false);
        NormalTrim.SetActive(false);
        HardTrim.SetActive(false);
        InsaneTrim.SetActive(true);

    }

}

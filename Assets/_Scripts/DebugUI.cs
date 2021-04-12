using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UI;

public class DebugUI : MonoBehaviour
{
    public int FPS;
    public float MS;

    public TextMeshProUGUI LUIText;
    public TextMeshProUGUI RUIText;
    public GameObject Holder;

    public float Interval = 0.2f;
    float TimeToChange = 0;

    // Start is called before the first frame update
    void Start()
    {
        DontDestroyOnLoad(this);
    }

    // Update is called once per frame
    void Update()
    {
        TimeToChange += Time.deltaTime;
        if(TimeToChange > Interval)
		{
            FPS = (int)(1f / Time.deltaTime);
            MS = Time.deltaTime * 1000;

            LUIText.text = 
                $"DEBUG: \n" +
                $"{FPS} FPS, {MS.ToString("0.0 ms")}";

            RUIText.text =
                $"INFO: \n" +
                $"RAM: {(GC.GetTotalMemory(true) + Profiler.GetTotalAllocatedMemoryLong() + Profiler.GetTotalReservedMemoryLong()) / 1024 / 1024}MB\n" +
                $"SCREEN: {Screen.width}x{Screen.height}@{Screen.currentResolution.refreshRate}Hz";

            TimeToChange = 0;
        }

		if (Input.GetKeyDown(KeyCode.F3))
		{
            Holder.SetActive(!Holder.activeSelf);
		}
    }
}
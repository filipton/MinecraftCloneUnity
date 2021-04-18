using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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
    PerformanceCounter ramCounter;

    // Start is called before the first frame update
    void Start()
    {
        DontDestroyOnLoad(this);

        ramCounter = new PerformanceCounter("Memory", "Available MBytes");
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
                $"{FPS} FPS, {MS.ToString("0.0ms")}\n" +
                $"XYZ: {(GeneratorCore.singleton != null ? $"{GeneratorCore.singleton.player.position.x}, {GeneratorCore.singleton.player.position.y}, {GeneratorCore.singleton.player.position.z}" : "NULL")}\n" +
                $"CHUNK: {(GeneratorCore.singleton != null ? $"{GeneratorCore.singleton._offset.x}, {GeneratorCore.singleton._offset.y}" : "NULL")}\n" +
                $"SEED: {(GeneratorCore.singleton != null ? GeneratorCore.singleton.Seed.ToString() : "NULL")}\n" +
                $"ACTIVE CHUNKS: {(GeneratorCore.singleton != null ? GeneratorCore.singleton.generatorChunks.Count.ToString() : "NULL")}\n" +
                $"RENDER DISTANCE: {(GeneratorCore.singleton != null ? GeneratorCore.singleton.RenderDistance.ToString() : "NULL")}";

            RUIText.text =
                $"INFO: \n" +
                $"RAM: {(GC.GetTotalMemory(true) + Profiler.GetTotalAllocatedMemoryLong()) / 1024 / 1024}MB\n" +
                $"SCREEN: {Screen.width}x{Screen.height}@{Screen.currentResolution.refreshRate}Hz";

            TimeToChange = 0;
        }

		if (Input.GetKeyDown(KeyCode.F3))
		{
            Holder.SetActive(!Holder.activeSelf);
		}
    }
}
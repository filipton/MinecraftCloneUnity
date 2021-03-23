using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FastNoiseExtension : ScriptableObject
{

    [Header("Main Settings")]
    public FastNoiseLite.NoiseType mainType = FastNoiseLite.NoiseType.OpenSimplex2;
    public float frequency = 0.01f;

    [Header("Fractal Settings")]
    public int octaves = 3;
    public float lacunarity = 2.0f;
    public float gain = 0.5f;
    public FastNoiseLite.FractalType fractalType = FastNoiseLite.FractalType.FBm;

    [Header("Cellular Settings")]
    public FastNoiseLite.CellularDistanceFunction distanceFunction = FastNoiseLite.CellularDistanceFunction.Euclidean;
    public FastNoiseLite.CellularReturnType returnType = FastNoiseLite.CellularReturnType.CellValue;
    public FastNoiseLite lookupNoise;
    [Range(0, 3)] public int dstIndicieLow = 0;
    [Range(1, 4)] public int dstIndicieHigh = 1;
    public float jitter = 0.45f;

    [Header("Gradient Perturbation Settings")]
    public float gradientPerturbation = 1.0f;

    public FastNoiseLite GetLibInstance (int seed)
    {
        FastNoiseLite fn = new FastNoiseLite();

        fn.SetSeed(seed);
        fn.SetFrequency(frequency);
        fn.SetNoiseType(mainType);
        fn.SetFractalOctaves(octaves);
        fn.SetFractalLacunarity(lacunarity);
        fn.SetFractalGain(gain);
        fn.SetFractalType(fractalType);
        fn.SetCellularDistanceFunction(distanceFunction);
        fn.SetCellularReturnType(returnType);
        fn.SetCellularJitter(jitter);

        return fn;
    }
}
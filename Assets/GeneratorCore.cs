using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class GeneratorCore : MonoBehaviour
{
    public static GeneratorCore singleton { get; set; }

    public double Seed;

    [Header("Generator Settings")]
    public Simplex simplex;
    public AnimationCurve GeneratorCurve;

    public int RenderDistance = 8;

    public int ChunkSizeXZ = 16;
    public int ChunkSizeY = 256;

    [Header("Noise Settings")]
    public float NoiseScaleXZ = 1;
    public float NoiseScaleY = 1;

    [Header("Textures Settings")]
    public Material TextureMaterial;
    public int atlasSize = 4;

    [Header("Generator Fields")]
    public List<GeneratorChunk> generatorChunks = new List<GeneratorChunk>();
    public Transform player;
    public Vector2Int _offset = new Vector2Int();

    Thread regenThread;

    //public bool Test = false;

    private void Awake()
	{
        singleton = this;
        if(Seed == 0)
		{
            Seed = UnityEngine.Random.Range(-320000000, 320000000);
		}
	}

	private void Start()
	{
        simplex = new Simplex();
        simplex.OctaveCount = 8;
        simplex.Seed = Seed;

        StartCoroutine(GenerateWorld());
    }

	IEnumerator GenerateWorld()
	{
        for(int x = -RenderDistance; x <= RenderDistance; x++)
		{
            for (int z = -RenderDistance; z <= RenderDistance; z++)
            {
                GameObject chunkGb = new GameObject();

                GeneratorChunk gc = chunkGb.AddComponent<GeneratorChunk>();
                MeshRenderer mr = chunkGb.AddComponent<MeshRenderer>();

                mr.material = TextureMaterial;
                chunkGb.AddComponent<MeshCollider>();

                gc.Blocks = new BlockType[ChunkSizeXZ, ChunkSizeY, ChunkSizeXZ];
                gc.meshFilter = chunkGb.AddComponent<MeshFilter>();
                gc.meshRenderer = mr;

                generatorChunks.Add(gc);
                Task.Run(() =>
                {
                    gc.GenerateChunk(x + _offset.x, z + _offset.y);
                });
                yield return new WaitForSeconds(0.010f);
            }
        }
    }

	private void Update()
	{
        Vector2Int v2int = new Vector2Int(Mathf.FloorToInt((player.position.x + 0.5f) / ChunkSizeXZ), Mathf.FloorToInt((player.position.z + 0.5f) / ChunkSizeXZ));

        if (v2int != _offset)
        {
            _offset = v2int;

            RegenrateChunks();
        }
    }

    public void RegenrateChunks()
	{
        if (regenThread != null && regenThread.IsAlive) regenThread.Abort();

        regenThread = new Thread(() =>
        {
            Queue<GeneratorChunk> ChunksToRegenerate = new Queue<GeneratorChunk>();

            foreach (GeneratorChunk gc in generatorChunks.GroupBy(x => new { x.ChunkX, x.ChunkZ }).Where(g => g.Count() > 1).Select(d => d.First()).ToArray())
            {
                ChunksToRegenerate.Enqueue(gc);
            }

            for (int i = 0; i < generatorChunks.Count; i++)
            {
                if (Math.Abs(generatorChunks[i].ChunkX - _offset.x) > RenderDistance || Math.Abs(generatorChunks[i].ChunkZ - _offset.y) > RenderDistance)
                {
                    ChunksToRegenerate.Enqueue(generatorChunks[i]);
                }
            }

            int spiralLength = (RenderDistance * 2) + 1;

            int sx, sy, dx, dy;
            sx = sy = dx = 0;
            dy = -1;
            int t = spiralLength;
            int maxI = t * t;
            for (int i = 0; i < maxI; i++)
            {
                if ((-spiralLength / 2 <= sx) && (sx <= spiralLength / 2) && (-spiralLength / 2 <= sy) && (sy <= spiralLength / 2))
                {
                    int x = sx + _offset.x;
                    int z = sy + _offset.y;

                    if (generatorChunks.FindIndex(f => f.ChunkX == x && f.ChunkZ == z) == -1 && ChunksToRegenerate.Count > 0)
                    {
                        Task.Run(() =>
                        {
                            ChunksToRegenerate.Dequeue().GenerateChunk(x, z);
                        });

                        Thread.Sleep(10);
                    }
                }
                if ((sx == sy) || ((sx < 0) && (sx == -sy)) || ((sx > 0) && (sx == 1 - sy)))
                {
                    t = dx;
                    dx = -dy;
                    dy = t;
                }
                sx += dx;
                sy += dy;
            }
        });

        regenThread.Start();
    }
}

public enum BlockType
{
    Air,
    Stone,
    Dirt,
    Grass,
    Water,
    Leaves
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public class GeneratorCore : MonoBehaviour
{
    public static GeneratorCore singleton { get; set; }

    public string Seed;

    [Header("Generator Settings")]
    public float NoiseScale = 1f;
    public FastNoiseExtension Noise;
    public static FastNoiseLite fn;

    public AnimationCurve GeneratorCurve;

    public int RenderDistance = 8;

    public int ChunkSizeXZ = 16;
    public int ChunkSizeY = 256;


    [Header("Textures Settings")]
    public Material TextureMaterial;
    public int atlasSize = 4;

    [Header("Generator Fields")]
    public List<GeneratorChunk> generatorChunks = new List<GeneratorChunk>();
    public Vector2Int offset = new Vector2Int();
    Vector2Int _offset = new Vector2Int();

    public bool Test = false;

    private void Awake()
	{
        singleton = this;
	}

	private void Start()
	{
        GenerateWorld(Seed);
    }

	void GenerateWorld(string seed)
	{
        int _seed = 0;
        if(seed == "")
		{
            seed = UnityEngine.Random.Range(-900000, 900000).ToString();
		}

        MD5 md5 = MD5.Create();
        byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(seed));
        _seed = BitConverter.ToInt32(hash, 0);

        fn = Noise.GetLibInstance(_seed);

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
                gc.GenerateChunk(x + offset.x, z + offset.y);
            }
        }
    }

	private void Update()
	{
        if (Input.GetKeyDown(KeyCode.W)) offset += new Vector2Int(1, 0);
        else if (Input.GetKeyDown(KeyCode.S)) offset += new Vector2Int(-1, 0);
        else if (Input.GetKeyDown(KeyCode.A)) offset += new Vector2Int(0, 1);
        else if (Input.GetKeyDown(KeyCode.D)) offset += new Vector2Int(0, -1);

        if (offset != _offset)
        {
            _offset = offset;

            StopAllCoroutines();
            StartCoroutine(RegenrateChunks());
        }
    }

    public IEnumerator RegenrateChunks()
	{
        Queue<GeneratorChunk> ChunksToRegenerate = new Queue<GeneratorChunk>();

        foreach(GeneratorChunk gc in generatorChunks.GroupBy(x => new { x.ChunkX, x.ChunkZ }).Where(g => g.Count() > 1).Select(d => d.First()))
        {
            ChunksToRegenerate.Enqueue(gc);
        }

        for (int i = 0; i < generatorChunks.Count; i++)
        {
            if (Math.Abs(generatorChunks[i].ChunkX - offset.x) > RenderDistance || Math.Abs(generatorChunks[i].ChunkZ - offset.y) > RenderDistance)
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
                int x = sx + offset.x;
                int z = sy + offset.y;

                if (generatorChunks.FindIndex(f => f.ChunkX == x && f.ChunkZ == z) == -1 && ChunksToRegenerate.Count > 0)
                {
                    yield return null;
                    ChunksToRegenerate.Dequeue().GenerateChunk(x, z);
                    yield return null;
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
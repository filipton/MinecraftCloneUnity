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
    [HideInInspector] public float[] GenCurve;

    public int RenderDistance = 8;

    public int ChunkSizeXZ = 16;
    public int ChunkSizeY = 256;

    public int ChunkLoadingIntervalMs = 10;

    [Header("Noise Settings")]
    public float NoiseScaleXZ = 1;
    public float NoiseScaleY = 1;

    [Header("Textures Settings")]
    public Material TextureMaterial;
    public Material WaterMaterial;
    public int atlasSize = 4;

    [Header("Generator Fields")]
    public List<GeneratorChunk> generatorChunks = new List<GeneratorChunk>();
    public List<AdvBlocksGenObj> AdvancedBlocksGeneration = new List<AdvBlocksGenObj>();
    public Transform player;
    public Vector2Int _offset = new Vector2Int();

    Thread regenThread;

    private void Awake()
	{
        Seed = FindObjectOfType<SaveManager>().Seed;

        GenCurve = GeneratorCurve.GenerateCurveArray(ChunkSizeY);

        singleton = this;
        if(Seed == 0)
		{
            Seed = UnityEngine.Random.Range(-320000000, 320000000);
		}

        DiscordController.ChangeDetails("Playing Singleplayer");
        DiscordController.ChangeState($"DEBUG: W:{SaveManager.singleton.CurrentSave},S:{Seed}");
    }

    private void Start()
	{
        Shader.SetGlobalFloat("minGlobalLightLevel", 0f);
        Shader.SetGlobalFloat("maxGlobalLightLevel", 1f);
        Shader.SetGlobalFloat("GlobalLightLevel", 1f);

        simplex = new Simplex();
        simplex.OctaveCount = 8;
        simplex.Seed = Seed;

        StartCoroutine(GenerateWorld());
    }

	private void Update()
	{
        Vector2Int v2int = GetChunkCords(player.transform.position.x, player.transform.position.z);

        if (v2int != _offset)
        {
            _offset = v2int;

            RegenrateChunks();
        }


        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Equals))
        {
            RenderDistance++;
            ChangeRenderDistance();
        }
        else if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Minus))
        {
            RenderDistance--;
            ChangeRenderDistance();
        }
    }

	public static Vector2Int GetChunkCords(float x, float z)
	{
        return new Vector2Int(Mathf.FloorToInt((x + 0.5f) / singleton.ChunkSizeXZ), Mathf.FloorToInt((z + 0.5f) / singleton.ChunkSizeXZ));
    }

    public static void SetBlock(int globalX, int globalY, int globalZ, BlockType block, bool Regen = true, bool RegenOtherChunks = false)
	{
        Vector2Int chunkCords = new Vector2Int(Mathf.FloorToInt((globalX + 0.5f) / singleton.ChunkSizeXZ), Mathf.FloorToInt((globalZ + 0.5f) / singleton.ChunkSizeXZ));
        SaveManager.singleton.EditBlock(globalX, globalY, globalZ, block);

        GeneratorChunk gc = singleton.generatorChunks.Find(x => x.ChunkX == chunkCords.x && x.ChunkZ == chunkCords.y);
        if (gc != null)
		{
            gc.SetBlockGlobal(globalX, globalY, globalZ, block, Regen);

			if (RegenOtherChunks)
			{
                RegenChunk(chunkCords.x + 1, chunkCords.y);
                RegenChunk(chunkCords.x - 1, chunkCords.y);
                RegenChunk(chunkCords.x, chunkCords.y + 1);
                RegenChunk(chunkCords.x, chunkCords.y - 1);
            }
		}
    }

    public static void RegenChunk(int cX, int cZ)
    {
        GeneratorChunk gc = singleton.generatorChunks.Find(x => x.ChunkX == cX && x.ChunkZ == cZ);
        if (gc != null)
        {
            Task.Run(() =>
            {
                gc.RegenerateChunk();
            });
        }
    }

    public static void SetTree(int x, int y, int z)
	{
        List<Vector2Int> ChunksToRegen = new List<Vector2Int>();

        for(int lx = -2; lx <= 2; lx++)
		{
            for (int ly = 3; ly <= 5; ly++)
            {
                for (int lz = -2; lz <= 2; lz++)
                {
                    SetBlock(x + lx, y + ly, z + lz, BlockType.Leaves, false);
                    Vector2Int chunkCords = new Vector2Int(Mathf.FloorToInt((x + lx + 0.5f) / singleton.ChunkSizeXZ), Mathf.FloorToInt((z + lz + 0.5f) / singleton.ChunkSizeXZ));

					if (!ChunksToRegen.Contains(chunkCords))
					{
                        ChunksToRegen.Add(chunkCords);
					}
                }
            }
        }

        SetBlock(x, y, z, BlockType.Wood, false);
        SetBlock(x, y + 1, z, BlockType.Wood, false);
        SetBlock(x, y + 2, z, BlockType.Wood, false);
        SetBlock(x, y + 3, z, BlockType.Wood, false);
        SetBlock(x, y + 4, z, BlockType.Wood, false);

        foreach(Vector2Int v in ChunksToRegen)
		{
            RegenChunk(v.x, v.y);
        }
    }

    IEnumerator GenerateWorld()
    {
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

                if (isInsideCIrcle(0, 0, RenderDistance, sx, sy) && generatorChunks.FindIndex(d => d.ChunkX == x && d.ChunkZ == z) == -1)
                {
                    GameObject chunkGb = new GameObject();
                    GameObject waterChunkGb = new GameObject();
                    chunkGb.layer = 6;
                    waterChunkGb.layer = 4;

                    waterChunkGb.transform.parent = chunkGb.transform;
                    waterChunkGb.name = "Water";

                    GeneratorChunk gc = chunkGb.AddComponent<GeneratorChunk>();

                    gc.waterMeshFilter = waterChunkGb.AddComponent<MeshFilter>();
                    gc.waterMeshCollider = waterChunkGb.AddComponent<MeshCollider>();
                    waterChunkGb.AddComponent<MeshRenderer>().material = WaterMaterial;

                    chunkGb.AddComponent<MeshRenderer>().material = TextureMaterial;
                    gc.meshCollider = chunkGb.AddComponent<MeshCollider>();

                    gc.Blocks = new byte[ChunkSizeXZ * ChunkSizeY * ChunkSizeXZ];
                    gc.meshFilter = chunkGb.AddComponent<MeshFilter>();

                    generatorChunks.Add(gc);

                    Task.Run(() =>
                    {
                        gc.GenerateChunk(x, z);
                    });
                    yield return new WaitForSeconds(0.010f);
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

    public void ChangeRenderDistance()
	{
        //CHANGING RENDER DISTANCE TO LOWER VALUE
        foreach(GeneratorChunk gc in generatorChunks.ToArray())
		{
            if (!isInsideCIrcle(0, 0, RenderDistance, gc.ChunkX - _offset.x, gc.ChunkZ - _offset.y))
            {
                generatorChunks.Remove(gc);
                Destroy(gc.gameObject);
            }
        }

        //CHANGING RENDER DISTANCE TO GREATER VALUE
        StartCoroutine(GenerateWorld());
    }

    static bool isInsideCIrcle(int circle_x, int circle_y,
                              int rad, int x, int y)
    {
        // Compare radius of circle with
        // distance of its center from
        // given point
        if ((x - circle_x) * (x - circle_x) +
            (y - circle_y) * (y - circle_y) <= rad * rad)
            return true;
        else
            return false;
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
                if (!isInsideCIrcle(0, 0, RenderDistance, generatorChunks[i].ChunkX - _offset.x, generatorChunks[i].ChunkZ - _offset.y))
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

                    if(isInsideCIrcle(0, 0, RenderDistance, sx, sy))
                    {
                        if (generatorChunks.FindIndex(f => f.ChunkX == x && f.ChunkZ == z) == -1 && ChunksToRegenerate.Count > 0)
                        {
                            Task.Run(() =>
                            {
                                GeneratorChunk gc = ChunksToRegenerate.Dequeue();

                                gc.GenerateChunk(x, z);
                            });

                            Thread.Sleep(ChunkLoadingIntervalMs);
                        }
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

[Serializable]
public struct AdvBlocksGenObj
{
    public BlockType blockType;

    [Range(0, 255)]
    public int MinY;

    [Range(0, 255)]
    public int MaxY;

    public float NoiseScale;

    [Range(-1, 1)]
    public float Threshold;
}

public static class Extensions
{
    public static float[] GenerateCurveArray(this AnimationCurve self, int size)
    {
        float[] returnArray = new float[size];
        for (int j = 0; j <= size-1; j++)
        {
            returnArray[j] = self.Evaluate(j / (float)size);
        }
        return returnArray;
    }
}

public enum BlockType
{
    Air = 0,
    Stone = 1,
    Dirt = 2,
    Grass = 3,
    Water = 4,
    Wood = 5,
    Leaves = 6,
    IronOre = 7,
    DiamondOre = 8,
    GoldOre = 9,
    CoalOre = 10
}
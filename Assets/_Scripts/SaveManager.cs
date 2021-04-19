using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager singleton;

    public string CurrentSave = "New World";

    [Header("SaveSystem Settings")]
    public int RegionSizeInChunks = 32;
    public double Seed;
    public string SavesPath = @"D:\Worlds";
    public string worldPath = "";
    public string regionsPath = "";

    public Dictionary<Vector2Int, Region> regions = new Dictionary<Vector2Int, Region>();


    private void Awake()
	{
        Application.wantsToQuit += ApplicationWantsToQuit;
        DontDestroyOnLoad(this);

        SavesPath = Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "MCClone", "Worlds");
        Directory.CreateDirectory(SavesPath);

        //LoadWorld();
        singleton = this;

    }

	void Start()
    {
        //GetAllWorlds();
        //File.WriteAllBytes(@"D:\test.txt", ChunkDataToBytesArray(b));
    }

    bool ApplicationWantsToQuit()
    {
        SaveWorld(true);

        return false;
    }

    void Update()
    {
		if (Input.GetKeyDown(KeyCode.Return))
		{
            SaveWorld();
		}
    }

    public void SaveBlocks(int cX, int cZ, byte[] blocksToSave)
	{
        Vector2Int chunkC = new Vector2Int(cX, cZ);
        Vector2Int regC = GetRegion(cX, cZ);

        if (!regions.ContainsKey(regC)) regions[regC] = new Region(regC, new Dictionary<Vector2Int, SerializedChunk>());

        regions[regC].editedChunks[chunkC] = new SerializedChunk(blocksToSave);
    }

    public bool TryGetSavedBlocks(int cX, int cZ, out byte[] blocks)
	{
        Vector2Int chunkC = new Vector2Int(cX, cZ);
        Vector2Int regC = GetRegion(cX, cZ);

        if (regions.ContainsKey(regC) && regions[regC].editedChunks.ContainsKey(chunkC))
		{
            blocks = regions[regC].editedChunks[chunkC].ChunkData;
            return true;
        }

        blocks = new byte[GeneratorCore.singleton.ChunkSizeXZ * GeneratorCore.singleton.ChunkSizeY * GeneratorCore.singleton.ChunkSizeXZ];
        return false;
	}

    public bool TryGetBlock(int x, int y, int z, out BlockType blockType)
    {
        Vector2Int chunkC = GeneratorCore.GetChunkCords(x, z);
        Vector2Int regC = GetRegion(chunkC.x, chunkC.y);

        if(regions.ContainsKey(regC) && regions[regC].editedChunks.ContainsKey(chunkC))
		{
            Vector3 local = GeneratorChunk.GetLocalChunksBlockCords(x, y, z, chunkC.x, chunkC.y);
            int index = GetArrayCords((int)local.x, (int)local.y, (int)local.z);

            blockType = (BlockType)regions[regC].editedChunks[chunkC].ChunkData[index];
            return true;
        }

        blockType = BlockType.Air;
        return false;
    }

    public bool EditBlock(int x, int y, int z, BlockType blockType)
    {
        Vector2Int chunkC = GeneratorCore.GetChunkCords(x, z);
        Vector2Int regC = GetRegion(chunkC.x, chunkC.y);

        if (regions.ContainsKey(regC) && regions[regC].editedChunks.ContainsKey(chunkC))
        {
            Vector3 local = GeneratorChunk.GetLocalChunksBlockCords(x, y, z, chunkC.x, chunkC.y);
            int index = GetArrayCords((int)local.x, (int)local.y, (int)local.z);

            regions[regC].editedChunks[chunkC].ChunkData[index] = (byte)blockType;
            return true;

            //regions[regC].editedChunks[chunkC].ChunkBlocks[(int)local.x, (int)local.y, (int)local.z] = blockType;
            //return true;
        }

        return false;
    }

    public void SaveWorld(bool QuitAfterSave = false)
	{
        //BinaryFormatter bf = new BinaryFormatter();
        //FileStream file = File.Open(@"D:\save.dat", FileMode.Open);
        //BlockType[,,] b = (BlockType[,,])bf.Deserialize(file);
        //file.Close();

        Task.Run(() =>
        {
            try
            {
                SerializedWorld serializedWorld = new SerializedWorld(GeneratorCore.singleton.Seed);

                if (!Directory.Exists(worldPath)) Directory.CreateDirectory(worldPath);
                if (!Directory.Exists(regionsPath)) Directory.CreateDirectory(regionsPath);

                int curr = 0;

                BinaryFormatter bf = new BinaryFormatter();
                FileStream file = File.Open(Path.Combine(worldPath, "world.dat"), FileMode.OpenOrCreate);
                bf.Serialize(file, serializedWorld);
                file.Close();

                foreach (Region reg in regions.Values)
                {
                    Task.Run(() =>
                    {
                        Dictionary<SerializedCords, SerializedChunk> tmpDict = new Dictionary<SerializedCords, SerializedChunk>();

                        foreach (var chunkKVP in reg.editedChunks)
                        {
                            tmpDict[new SerializedCords(chunkKVP.Key)] = new SerializedChunk(Compress(chunkKVP.Value.ChunkData));
                        }

                        BinaryFormatter bf = new BinaryFormatter();
                        FileStream file = File.Open(Path.Combine(regionsPath, $@"reg.{reg.RegionCords.x}.{reg.RegionCords.y}.dat"), FileMode.OpenOrCreate);
                        bf.Serialize(file, tmpDict);
                        file.Close();
                        curr++;

                        if(curr == regions.Values.Count && QuitAfterSave)
						{
                            Application.Quit();
						}
                    });
                }
            }
            catch (Exception e) { Debug.Log(e); }
        });
    }

    public void LoadWorld()
	{
        worldPath = Path.Combine(SavesPath, CurrentSave);
        regionsPath = Path.Combine(SavesPath, CurrentSave, "regions");

        if (!Directory.Exists(worldPath) || !Directory.Exists(regionsPath)) return;

        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Open(Path.Combine(worldPath, "world.dat"), FileMode.Open);
        SerializedWorld serializedWorld = (SerializedWorld)bf.Deserialize(file);
        file.Close();

        Seed = serializedWorld.Seed;

        foreach(string f in Directory.GetFiles(regionsPath, "reg.*.dat"))
		{
            string[] fc = f.Replace(regionsPath + @"\", "").Replace("reg.", "").Replace(".dat", "").Split('.');
            Vector2Int rCord = new Vector2Int(int.Parse(fc[0]), int.Parse(fc[1]));

            BinaryFormatter b = new BinaryFormatter();
            FileStream fi = File.Open(f, FileMode.Open);
            Dictionary<SerializedCords, SerializedChunk> sc = (Dictionary<SerializedCords, SerializedChunk>)b.Deserialize(fi);
            fi.Close();

            if (!regions.ContainsKey(rCord))
			{
                regions[rCord] = new Region(rCord, new Dictionary<Vector2Int, SerializedChunk>());
			}

            foreach(var dkvp in sc)
			{
                regions[rCord].editedChunks[new Vector2Int(dkvp.Key.x, dkvp.Key.z)] = new SerializedChunk(Decompress(dkvp.Value.ChunkData));
            }
        }
    }

    public byte[] ChunkDataToBytesArray(BlockType[,,] blocks, bool compress = true)
    {
        List<byte> tmp = new List<byte>();

        int ChunkXZ = GeneratorCore.singleton.ChunkSizeXZ;
        int ChunkY = GeneratorCore.singleton.ChunkSizeY;

        for (int x = 0; x < ChunkXZ; x++)
        {
            for (int y = 0; y < ChunkY; y++)
            {
                for (int z = 0; z < ChunkXZ; z++)
                {
                    tmp.Add((byte)blocks[x, y, z]);
                }
            }
        }

		if (compress)
		{
            return Compress(tmp.ToArray());
        }
		else
		{
            return tmp.ToArray();
        }
    }

    public BlockType[,,] ByteArrayToChunkData(byte[] compresedData, bool compresed = true)
    {
        byte[] data = new byte[0];
		if (compresed)
		{
            data = Decompress(compresedData);
        }
		else
		{
            data = compresedData;
		}

        BlockType[,,] tmp = new BlockType[0, 0, 0];

        if (data.Length > 0)
        {
            int ChunkXZ = GeneratorCore.singleton.ChunkSizeXZ;
            int ChunkY = GeneratorCore.singleton.ChunkSizeY;

            tmp = new BlockType[ChunkXZ, ChunkY, ChunkXZ];

            int index = 0;
            for (int x = 0; x < ChunkXZ; x++)
            {
                for (int y = 0; y < ChunkY; y++)
                {
                    for (int z = 0; z < ChunkXZ; z++)
                    {
                        tmp[x, y, z] = (BlockType)data[index];
                        index++;
                    }
                }
            }
        }

        return tmp;
    }

    public static byte[] Compress(byte[] data)
    {
        MemoryStream output = new MemoryStream();
        using (DeflateStream dstream = new DeflateStream(output, System.IO.Compression.CompressionLevel.Fastest))
        {
            dstream.Write(data, 0, data.Length);
        }
        return output.ToArray();
    }

    public static byte[] Decompress(byte[] data)
    {
        MemoryStream input = new MemoryStream(data);
        MemoryStream output = new MemoryStream();
        using (DeflateStream dstream = new DeflateStream(input, CompressionMode.Decompress))
        {
            dstream.CopyTo(output);
        }
        return output.ToArray();
    }

    public Vector2Int GetRegion(int cX, int cZ)
	{
        return new Vector2Int(Mathf.FloorToInt(cX / RegionSizeInChunks), Mathf.FloorToInt(cZ / RegionSizeInChunks));
	}

    public int GetArrayCords(int x, int y, int z)
    {
        return (x * GeneratorCore.singleton.ChunkSizeXZ * GeneratorCore.singleton.ChunkSizeY) + (y * GeneratorCore.singleton.ChunkSizeXZ) + z;
    }
}

public struct Region
{
    public Vector2Int RegionCords;
    public Dictionary<Vector2Int, SerializedChunk> editedChunks;

    public Region(Vector2Int rcords, Dictionary<Vector2Int, SerializedChunk> edChunks)
    {
        RegionCords = rcords;
        editedChunks = edChunks;
    }
}

[Serializable]
public struct SerializedWorld
{
    public double Seed;

    public SerializedWorld(double seed)
	{
        Seed = seed;
	}
}

[Serializable]
public struct SerializedRegion
{
    public Dictionary<SerializedCords, SerializedChunk> editedChunks;

    public SerializedRegion(Dictionary<SerializedCords, SerializedChunk> edChunks)
    {
        editedChunks = edChunks;
    }
}

[Serializable]
public struct SerializedChunk
{
    public byte[] ChunkData;

    public SerializedChunk(byte[] data)
    {
        ChunkData = data;
    }
}

[Serializable]
public struct SerializedCords
{
    public int x;
    public int z;

    public SerializedCords(int x, int z)
	{
        this.x = x;
        this.z = z;
	}

    public SerializedCords(Vector2Int vec)
	{
        x = vec.x;
        z = vec.y;
	}

    public Vector2Int GetVector2Int()
	{
        return new Vector2Int(x, z);
	}
}
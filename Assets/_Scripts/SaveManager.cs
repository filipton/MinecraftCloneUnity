using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager singleton;

    [Header("SaveSystem Settings")]
    public int RegionSizeInChunks = 32;
    public Dictionary<Vector2Int, Region> regions = new Dictionary<Vector2Int, Region>();


    private void Awake()
	{
        singleton = this;
    }

	void Start()
    {
        //File.WriteAllBytes(@"D:\test.txt", ChunkDataToBytesArray(b));
    }

    void Update()
    {
		if (Input.GetKeyDown(KeyCode.Return))
		{
            SaveWorld();
		}
    }

    public void SaveBlocks(int cX, int cZ, BlockType[,,] blocksToSave)
	{
        Vector2Int chunkC = new Vector2Int(cX, cZ);
        Vector2Int regC = GetRegion(cX, cZ);

        if (!regions.ContainsKey(regC)) regions[regC] = new Region(regC, new Dictionary<Vector2Int, SerializedChunk>());

        regions[regC].editedChunks[chunkC] = new SerializedChunk(ChunkDataToBytesArray(blocksToSave, false));
    }

    public bool TryGetSavedBlocks(int cX, int cZ, out BlockType[,,] blocks)
	{
        Vector2Int chunkC = new Vector2Int(cX, cZ);
        Vector2Int regC = GetRegion(cX, cZ);

        if (regions.ContainsKey(regC) && regions[regC].editedChunks.ContainsKey(chunkC))
		{
            blocks = ByteArrayToChunkData(regions[regC].editedChunks[chunkC].ChunkData, false);
            return true;
        }

        blocks = new BlockType[GeneratorCore.singleton.ChunkSizeXZ, GeneratorCore.singleton.ChunkSizeY, GeneratorCore.singleton.ChunkSizeXZ];
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

    public void SaveWorld()
	{
        //BinaryFormatter bf = new BinaryFormatter();
        //FileStream file = File.Open(@"D:\save.dat", FileMode.Open);
        //BlockType[,,] b = (BlockType[,,])bf.Deserialize(file);
        //file.Close();

        Task.Run(() =>
        {
            SerializedWorld serializedWorld = new SerializedWorld(GeneratorCore.singleton.Seed);

            foreach (Region reg in regions.Values)
			{
                Task.Run(() =>
                {
                    Dictionary<SerializedCords, SerializedChunk> tmpDict = new Dictionary<SerializedCords, SerializedChunk>();

                    foreach (var chunkKVP in reg.editedChunks)
                    {
                        tmpDict[new SerializedCords(chunkKVP.Key)] = new SerializedChunk(Ionic.Zlib.DeflateStream.CompressBuffer(chunkKVP.Value.ChunkData));
                    }

                    BinaryFormatter bf = new BinaryFormatter();
                    FileStream file = File.Open($@"D:\World\reg.{reg.RegionCords.x}.{reg.RegionCords.y}.dat", FileMode.OpenOrCreate);
                    bf.Serialize(file, tmpDict);
                    file.Close();
                    print($@"SAVED REGION -> D:\World\reg.{reg.RegionCords.x}.{reg.RegionCords.y}.dat");
                });
            }
        });
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
            return Ionic.Zlib.DeflateStream.CompressBuffer(tmp.ToArray());
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
            data = Ionic.Zlib.DeflateStream.UncompressBuffer(compresedData);
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

public struct EditedChunk
{
    public Vector2Int ChunkCords;
    public BlockType[,,] ChunkBlocks;

    public EditedChunk(Vector2Int ccords, BlockType[,,] chuBlocks)
	{
        ChunkCords = ccords;
        ChunkBlocks = chuBlocks;
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
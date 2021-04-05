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
    public Dictionary<KeyValuePair<int, int>, EditedChunk> chunks = new Dictionary<KeyValuePair<int, int>, EditedChunk>();


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
        KeyValuePair<int, int> chunkKVP = new KeyValuePair<int, int>(cX, cZ);

        chunks[chunkKVP] = new EditedChunk(chunkC, blocksToSave);
    }

    public bool TryGetSavedBlocks(int cX, int cZ, out BlockType[,,] blocks)
	{
        KeyValuePair<int, int> chunkKVP = new KeyValuePair<int, int>(cX, cZ);

		if (chunks.ContainsKey(chunkKVP))
		{
            blocks = chunks[chunkKVP].ChunkBlocks;
            return true;
        }

        blocks = new BlockType[GeneratorCore.singleton.ChunkSizeXZ, GeneratorCore.singleton.ChunkSizeY, GeneratorCore.singleton.ChunkSizeXZ];
        return false;
	}

    /*public bool TryGetBlock(int x, int y, int z, out BlockType blockType)
    {
        Vector2Int chunkC = GeneratorCore.GetChunkCords(x, z);
        Vector2Int regC = GetRegion(chunkC.x, chunkC.y);

        int index = regions.FindIndex(g => g.RegionCords == regC);
        if (regions.FindIndex(g => g.RegionCords == regC) > -1)
        {
            int ei = regions[index].editedChunks.FindIndex(o => o.ChunkCords == chunkC);
            if (ei > -1)
            {
                if(regions[index].editedChunks[ei].ChunkBlocks.Length > 0)
				{
                    Vector3 local = GeneratorChunk.GetLocalChunksBlockCords(x, y, z, chunkC.x, chunkC.y);
                    blockType = regions[index].editedChunks[ei].ChunkBlocks[(int)local.x, (int)local.y, (int)local.z];
                    return true;
                }
            }
        }

        blockType = BlockType.Air;
        return false;
    }*/

    public bool EditBlock(int x, int y, int z, BlockType blockType)
    {
        Vector2Int chunkC = GeneratorCore.GetChunkCords(x, z);
        KeyValuePair<int, int> chunkKVP = new KeyValuePair<int, int>(chunkC.x, chunkC.y);

        if (chunks.ContainsKey(chunkKVP))
        {
            Vector3 local = GeneratorChunk.GetLocalChunksBlockCords(x, y, z, chunkC.x, chunkC.y);
        chunks[chunkKVP].ChunkBlocks[(int)local.x, (int)local.y, (int)local.z] = blockType;

        return true;
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
            List<SerializedChunk> serializedChunks = new List<SerializedChunk>();

            foreach (EditedChunk edChunk in chunks.Values)
            {
                serializedChunks.Add(new SerializedChunk(new SerializedChunkCords(edChunk.ChunkCords), ChunkDataToBytesArray(edChunk.ChunkBlocks)));
            }

            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Open($@"D:\World\r.0.0.dat", FileMode.OpenOrCreate);
            bf.Serialize(file, serializedChunks);
            file.Close();

            print("DONE!!!");
        });
    }

    public byte[] ChunkDataToBytesArray(BlockType[,,] blocks)
    {
        List<byte> tmp = new List<byte>();

        byte ChunkXZ = (byte)GeneratorCore.singleton.ChunkSizeXZ;
        byte ChunkY = (byte)(GeneratorCore.singleton.ChunkSizeY - 1);

        tmp.Add(ChunkXZ); //CHUNK XZ
        tmp.Add(ChunkY);  //CHUNK Y

        for (int x = 0; x < ChunkXZ; x++)
        {
            for (int y = 0; y <= ChunkY; y++)
            {
                for (int z = 0; z < ChunkXZ; z++)
                {
                    tmp.Add((byte)blocks[x, y, z]);
                }
            }
        }

        return Ionic.Zlib.DeflateStream.CompressBuffer(tmp.ToArray());
    }

    public BlockType[,,] ByteArrayToChunkData(byte[] compresedData)
    {
        byte[] data = Ionic.Zlib.DeflateStream.UncompressBuffer(compresedData);
        BlockType[,,] tmp = new BlockType[0, 0, 0];

        if (data.Length > 2)
        {
            byte ChunkXZ = data[0];
            byte ChunkY = data[1];

            tmp = new BlockType[ChunkXZ, ChunkY + 1, ChunkXZ];

            int index = 2;
            for (int x = 0; x < ChunkXZ; x++)
            {
                for (int y = 0; y <= ChunkY; y++)
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
}

[Serializable]
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
public struct SerializedChunk
{
    public SerializedChunkCords ChunkCords;
    public byte[] ChunkData;

    public SerializedChunk(SerializedChunkCords ccords, byte[] data)
    {
        ChunkCords = ccords;
        ChunkData = data;
    }
}

[Serializable]
public struct SerializedChunkCords
{
    public int x;
    public int z;

    public SerializedChunkCords(int x, int z)
	{
        this.x = x;
        this.z = z;
	}

    public SerializedChunkCords(Vector2Int vec)
	{
        x = vec.x;
        z = vec.y;
	}

    public Vector2Int GetVector2Int()
	{
        return new Vector2Int(x, z);
	}
}
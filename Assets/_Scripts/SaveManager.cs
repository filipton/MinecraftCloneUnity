using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager singleton;

    [Header("SaveSystem Settings")]
    public int RegionSizeInChunks = 32;
    public List<RegionData> regions = new List<RegionData>();


    private void Awake()
	{
        singleton = this;
	}

	void Start()
    {
        
    }

    void Update()
    {
        
    }

    public void SaveBlock(int x, int y, int z, BlockType block)
	{
        Vector2Int chunkC = GeneratorCore.GetChunkCords(x, z);
        Vector2Int regC = GetRegion(chunkC.x, chunkC.y);

        int index = regions.FindIndex(g => g.RegionCords == regC);
        if (regions.FindIndex(g => g.RegionCords == regC) < 0)
		{
            regions.Add(new RegionData(regC, new List<EditedChunk>()));
            index = regions.Count - 1;
		}

        RegionData regData = regions[index];
        int ei = regData.editedChunks.FindIndex(o => o.ChunkCords == chunkC);
        if (ei < 0)
		{
            regData.editedChunks.Add(new EditedChunk(chunkC, new List<EditedBlock>()));
            ei = regData.editedChunks.Count - 1;
		}

        EditedChunk edChunk = regData.editedChunks[ei];
        edChunk.editedBlocks.Add(new EditedBlock(x, y, z, block));

        regions[index] = regData;
    }

    public bool IsBlockSaved(int x, int y, int z)
	{
        Vector2Int chunkC = GeneratorCore.GetChunkCords(x, z);
        Vector2Int regC = GetRegion(chunkC.x, chunkC.y);

        int index = regions.FindIndex(g => g.RegionCords == regC);
        if (regions.FindIndex(g => g.RegionCords == regC) > -1)
        {
            int ei = regions[index].editedChunks.FindIndex(o => o.ChunkCords == chunkC);
            if (ei < -1)
            {
                if (regions[index].editedChunks[ei].editedBlocks.FindIndex(e => e.x == x && e.y == y && e.z == z) > -1)
				{
                    return true;
				}
            }
        }

        return false;
    }

    public bool TryGetEditedChunk(int cX, int cZ, out EditedChunk editedChunk)
	{
        Vector2Int regionCords = GetRegion(cX, cZ);

        int regIndex = regions.FindIndex(r => r.RegionCords == regionCords);
        if(regIndex > -1)
		{
            int chunkIndex = regions[regIndex].editedChunks.FindIndex(c => c.ChunkCords.x == cX && c.ChunkCords.y == cZ);
            if(chunkIndex > -1)
			{
                editedChunk = regions[regIndex].editedChunks[chunkIndex];
                return true;
			}
		}

        editedChunk = new EditedChunk();
        return false;
	}

    public bool GetBlock(int x, int y, int z, out BlockType blockType)
    {
        Vector2Int chunkC = GeneratorCore.GetChunkCords(x, z);
        Vector2Int regC = GetRegion(chunkC.x, chunkC.y);

        int index = regions.FindIndex(g => g.RegionCords == regC);
        if (regions.FindIndex(g => g.RegionCords == regC) > -1)
        {
            int ei = regions[index].editedChunks.FindIndex(o => o.ChunkCords == chunkC);
            if (ei > -1)
            {
                int eb = regions[index].editedChunks[ei].editedBlocks.FindIndex(e => e.x == x && e.y == y && e.z == z);
                if (eb > -1)
                {
                    blockType = regions[index].editedChunks[ei].editedBlocks[eb].blockType;
                    return true;
                }
            }
        }

        blockType = BlockType.Air;
        return false;
    }

    public Vector2Int GetRegion(int cX, int cZ)
	{
        return new Vector2Int(Mathf.FloorToInt(cX / RegionSizeInChunks), Mathf.FloorToInt(cX / RegionSizeInChunks));
	}
}

[System.Serializable]
public struct RegionData
{
    public Vector2Int RegionCords;
    public List<EditedChunk> editedChunks;

    public RegionData(Vector2Int rcords, List<EditedChunk> edC)
    {
        RegionCords = rcords;
        editedChunks = edC;
    }
}

[System.Serializable]
public struct EditedChunk
{
    public Vector2Int ChunkCords;
    public List<EditedBlock> editedBlocks;

    public EditedChunk(Vector2Int ccords, List<EditedBlock> edB)
	{
        ChunkCords = ccords;
        editedBlocks = edB;
	}
}

[System.Serializable]
public struct EditedBlock
{
    public int x;
    public int y;
    public int z;
    public BlockType blockType;

    public EditedBlock(int _x, int _y, int _z, BlockType btype)
	{
        x = _x;
        y = _y;
        z = _z;
        blockType = btype;
	}
}
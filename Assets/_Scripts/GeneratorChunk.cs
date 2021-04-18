using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class GeneratorChunk : MonoBehaviour
{
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;
    public MeshCollider meshCollider;

    public int ChunkX = 0;
    public int ChunkZ = 0;

    public byte[] Blocks = new byte[0];

    public void GenerateChunk(int cX, int cZ)
	{
        ChunkX = cX;
        ChunkZ = cZ;

        if (!SaveManager.singleton.TryGetSavedBlocks(cX, cZ, out Blocks))
		{
            int depthY = 0;

            for (int x = cX * GeneratorCore.singleton.ChunkSizeXZ; x < (cX * GeneratorCore.singleton.ChunkSizeXZ) + GeneratorCore.singleton.ChunkSizeXZ; x++)
            {
                for (int z = cZ * GeneratorCore.singleton.ChunkSizeXZ; z < (cZ * GeneratorCore.singleton.ChunkSizeXZ) + GeneratorCore.singleton.ChunkSizeXZ; z++)
                {
                    for (int y = GeneratorCore.singleton.ChunkSizeY - 1; y >= 0; y--)
                    {
                        if (ChunkX != cX || ChunkZ != cZ) return;

                        float noiseValue = (float)GeneratorCore.singleton.simplex.GetValue(x * GeneratorCore.singleton.NoiseScaleXZ, y * GeneratorCore.singleton.NoiseScaleY, z * GeneratorCore.singleton.NoiseScaleXZ);

                        Vector3 local = GetLocalChunksBlockCords(x, y, z, cX, cZ);
                        int lx = (int)local.x;
                        int ly = (int)local.y;
                        int lz = (int)local.z;

                        if (noiseValue >= GeneratorCore.singleton.GenCurve[y])
                        {
                            if (depthY == 0)
                            {
                                if (lx == 2 && lz == 8)
                                {
                                    for (int tlx = -2; tlx <= 2; tlx++)
                                    {
                                        for (int tly = 3; tly <= 5; tly++)
                                        {
                                            for (int tlz = -2; tlz <= 2; tlz++)
                                            {
                                                Blocks[GetArrayCords(tlx + lx, tly + ly, tlz + lz)] = (byte)BlockType.Leaves;
                                                //Vector2Int chunkCords = new Vector2Int(Mathf.FloorToInt((x + lx + 0.5f) / GeneratorCore.singleton.ChunkSizeXZ), Mathf.FloorToInt((z + lz + 0.5f) / GeneratorCore.singleton.ChunkSizeXZ));
                                            }
                                        }
                                    }

                                    Blocks[GetArrayCords(lx, ly, lz)] = (byte)BlockType.Wood;
                                    Blocks[GetArrayCords(lx, ly + 1, lz)] = (byte)BlockType.Wood;
                                    Blocks[GetArrayCords(lx, ly + 2, lz)] = (byte)BlockType.Wood;
                                    Blocks[GetArrayCords(lx, ly + 3, lz)] = (byte)BlockType.Wood;
                                    Blocks[GetArrayCords(lx, ly + 4, lz)] = (byte)BlockType.Wood;
                                }

                                Blocks[GetArrayCords(lx, ly, lz)] = (byte)BlockType.Grass;
                            }
                            else if (depthY < 5)
                            {
                                Blocks[GetArrayCords(lx, ly, lz)] = (byte)BlockType.Dirt;
                            }
                            else
                            {
                                Blocks[GetArrayCords(lx, ly, lz)] = (byte)BlockType.Stone;

                                foreach (AdvBlocksGenObj BlockGen in GeneratorCore.singleton.AdvancedBlocksGeneration)
                                {
                                    float PerlinValue = Perlin.Noise(x * BlockGen.NoiseScale, y * BlockGen.NoiseScale, z * BlockGen.NoiseScale);
                                    if (ly <= BlockGen.MaxY && ly >= BlockGen.MinY && PerlinValue > BlockGen.Threshold)
                                    {
                                        Blocks[GetArrayCords(lx, ly, lz)] = (byte)BlockGen.blockType;
                                    }
                                }
                            }

                            depthY++;
                        }
                        else
                        {
                            if (y <= 63)
                            {
                                Blocks[GetArrayCords(lx, ly, lz)] = (byte)BlockType.Water;
                                depthY++;
                            }
                        }
                    }

                    depthY = 0;
                }
            }

            SaveManager.singleton.SaveBlocks(cX, cZ, Blocks);
        }

        GenerateChunkMesh();
    }

    public void SetBlockLocal(int localX, int localY, int localZ, BlockType blockToSet, bool Regen = true)
	{
        Blocks[GetArrayCords(localX, localY, localZ)] = (byte)blockToSet;

        if (Regen)
        {
            Task.Run(() =>
            {
                RegenerateChunk();
            });
        }
    }
    public void SetBlockGlobal(int gloablX, int gloablY, int gloablZ, BlockType blockToSet, bool Regen = true)
    {
        Vector3 local = GetLocalChunksBlockCords(gloablX, gloablY, gloablZ, ChunkX, ChunkZ);
        Blocks[GetArrayCords((int)local.x, (int)local.y, (int)local.z)] = (byte)blockToSet;

        if (Regen)
		{
            Task.Run(() =>
            {
                RegenerateChunk();
            });
        }
    }

    public void RegenerateChunk(byte[] blocks = null)
	{
        if(blocks != null) Blocks = blocks;

        GenerateChunkMesh();
	}

    public void GenerateChunkMesh()
	{
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<int> Wtriangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        List<Color> colors = new List<Color>();

        Vector2 atlasCoords;
        float grid = 1f / GeneratorCore.singleton.atlasSize;

        for (int x = 0; x < GeneratorCore.singleton.ChunkSizeXZ; x++)
        {
            for (int z = 0; z < GeneratorCore.singleton.ChunkSizeXZ; z++)
            {
                for (int y = 0; y < GeneratorCore.singleton.ChunkSizeY; y++)
                {
                    BlockType currBlock = (BlockType)Blocks[GetArrayCords(x, y, z)];

                    if (currBlock != BlockType.Air)
                    {
                        float lightLevel = 1f;
                        if(CheckIfShadow(x, y, z))
						{
                            lightLevel = 0.4f;
						}

                        //X+
                        if (CheckIfFaceVisible(currBlock, x + 1, y, z))
                        {
                            vertices.Add(new Vector3(x + .5f, y - .5f, z - .5f));
                            vertices.Add(new Vector3(x + .5f, y + .5f, z - .5f));
                            vertices.Add(new Vector3(x + .5f, y + .5f, z + .5f));
                            vertices.Add(new Vector3(x + .5f, y - .5f, z + .5f));

                            atlasCoords = Atlas.Cords[(int)currBlock - 1, 0];
                            uvs.Add(new Vector2(atlasCoords.x * grid, atlasCoords.y * grid));
                            uvs.Add(new Vector2(atlasCoords.x * grid, (atlasCoords.y + 1) * grid));
                            uvs.Add(new Vector2((atlasCoords.x + 1) * grid, (atlasCoords.y + 1) * grid));
                            uvs.Add(new Vector2((atlasCoords.x + 1) * grid, atlasCoords.y * grid));

                            if(currBlock == BlockType.Water)
							{
                                Wtriangles.Add(vertices.Count - 4);
                                Wtriangles.Add(vertices.Count - 3);
                                Wtriangles.Add(vertices.Count - 1);
                                
                                Wtriangles.Add(vertices.Count - 3);
                                Wtriangles.Add(vertices.Count - 2);
                                Wtriangles.Add(vertices.Count - 1);
                            }
							else
							{
                                triangles.Add(vertices.Count - 4);
                                triangles.Add(vertices.Count - 3);
                                triangles.Add(vertices.Count - 1);

                                triangles.Add(vertices.Count - 3);
                                triangles.Add(vertices.Count - 2);
                                triangles.Add(vertices.Count - 1);
                            }

                            colors.Add(new Color(0, 0, 0, 1));
                            colors.Add(new Color(0, 0, 0, 1));
                            colors.Add(new Color(0, 0, 0, 1));
                            colors.Add(new Color(0, 0, 0, 1));
                        }

                        //X-
                        if (CheckIfFaceVisible(currBlock, x - 1, y, z))
                        {
                            vertices.Add(new Vector3(x - .5f, y - .5f, z - .5f));
                            vertices.Add(new Vector3(x - .5f, y + .5f, z - .5f));
                            vertices.Add(new Vector3(x - .5f, y + .5f, z + .5f));
                            vertices.Add(new Vector3(x - .5f, y - .5f, z + .5f));

                            atlasCoords = Atlas.Cords[(int)currBlock - 1, 1];
                            uvs.Add(new Vector2(atlasCoords.x * grid, atlasCoords.y * grid));
                            uvs.Add(new Vector2(atlasCoords.x * grid, (atlasCoords.y + 1) * grid));
                            uvs.Add(new Vector2((atlasCoords.x + 1) * grid, (atlasCoords.y + 1) * grid));
                            uvs.Add(new Vector2((atlasCoords.x + 1) * grid, atlasCoords.y * grid));

                            if (currBlock == BlockType.Water)
                            {
                                Wtriangles.Add(vertices.Count - 4);
                                Wtriangles.Add(vertices.Count - 1);
                                Wtriangles.Add(vertices.Count - 3);
                                
                                Wtriangles.Add(vertices.Count - 3);
                                Wtriangles.Add(vertices.Count - 1);
                                Wtriangles.Add(vertices.Count - 2);
                            }
                            else
                            {
                                triangles.Add(vertices.Count - 4);
                                triangles.Add(vertices.Count - 1);
                                triangles.Add(vertices.Count - 3);

                                triangles.Add(vertices.Count - 3);
                                triangles.Add(vertices.Count - 1);
                                triangles.Add(vertices.Count - 2);
                            }

                            colors.Add(new Color(0, 0, 0, 1));
                            colors.Add(new Color(0, 0, 0, 1));
                            colors.Add(new Color(0, 0, 0, 1));
                            colors.Add(new Color(0, 0, 0, 1));
                        }

                        //Y+
                        if (CheckIfFaceVisible(currBlock, x, y + 1, z))
                        {
                            vertices.Add(new Vector3(x + .5f, y + .5f, z - .5f));
                            vertices.Add(new Vector3(x - .5f, y + .5f, z - .5f));
                            vertices.Add(new Vector3(x - .5f, y + .5f, z + .5f));
                            vertices.Add(new Vector3(x + .5f, y + .5f, z + .5f));

                            atlasCoords = Atlas.Cords[(int)currBlock - 1, 2];
                            uvs.Add(new Vector2(atlasCoords.x * grid, atlasCoords.y * grid));
                            uvs.Add(new Vector2(atlasCoords.x * grid, (atlasCoords.y + 1) * grid));
                            uvs.Add(new Vector2((atlasCoords.x + 1) * grid, (atlasCoords.y + 1) * grid));
                            uvs.Add(new Vector2((atlasCoords.x + 1) * grid, atlasCoords.y * grid));

                            if (currBlock == BlockType.Water)
                            {
                                Wtriangles.Add(vertices.Count - 4);
                                Wtriangles.Add(vertices.Count - 3);
                                Wtriangles.Add(vertices.Count - 1);

                                Wtriangles.Add(vertices.Count - 3);
                                Wtriangles.Add(vertices.Count - 2);
                                Wtriangles.Add(vertices.Count - 1);
                            }
                            else
                            {
                                triangles.Add(vertices.Count - 4);
                                triangles.Add(vertices.Count - 3);
                                triangles.Add(vertices.Count - 1);

                                triangles.Add(vertices.Count - 3);
                                triangles.Add(vertices.Count - 2);
                                triangles.Add(vertices.Count - 1);
                            }

                            colors.Add(new Color(0, 0, 0, lightLevel));
                            colors.Add(new Color(0, 0, 0, lightLevel));
                            colors.Add(new Color(0, 0, 0, lightLevel));
                            colors.Add(new Color(0, 0, 0, lightLevel));
                        }

                        //Y-
                        if (CheckIfFaceVisible(currBlock, x, y - 1, z))
                        {
                            vertices.Add(new Vector3(x + .5f, y - .5f, z - .5f));
                            vertices.Add(new Vector3(x - .5f, y - .5f, z - .5f));
                            vertices.Add(new Vector3(x - .5f, y - .5f, z + .5f));
                            vertices.Add(new Vector3(x + .5f, y - .5f, z + .5f));

                            atlasCoords = Atlas.Cords[(int)currBlock - 1, 3];
                            uvs.Add(new Vector2(atlasCoords.x * grid, atlasCoords.y * grid));
                            uvs.Add(new Vector2(atlasCoords.x * grid, (atlasCoords.y + 1) * grid));
                            uvs.Add(new Vector2((atlasCoords.x + 1) * grid, (atlasCoords.y + 1) * grid));
                            uvs.Add(new Vector2((atlasCoords.x + 1) * grid, atlasCoords.y * grid));

                            if (currBlock == BlockType.Water)
                            {
                                Wtriangles.Add(vertices.Count - 4);
                                Wtriangles.Add(vertices.Count - 1);
                                Wtriangles.Add(vertices.Count - 3);

                                Wtriangles.Add(vertices.Count - 3);
                                Wtriangles.Add(vertices.Count - 1);
                                Wtriangles.Add(vertices.Count - 2);
                            }
                            else
                            {
                                triangles.Add(vertices.Count - 4);
                                triangles.Add(vertices.Count - 1);
                                triangles.Add(vertices.Count - 3);

                                triangles.Add(vertices.Count - 3);
                                triangles.Add(vertices.Count - 1);
                                triangles.Add(vertices.Count - 2);
                            }

                            colors.Add(new Color(0, 0, 0, 1));
                            colors.Add(new Color(0, 0, 0, 1));
                            colors.Add(new Color(0, 0, 0, 1));
                            colors.Add(new Color(0, 0, 0, 1));
                        }

                        //Z+
                        if (CheckIfFaceVisible(currBlock, x, y, z + 1))
                        {
                            vertices.Add(new Vector3(x + .5f, y - .5f, z + .5f));
                            vertices.Add(new Vector3(x + .5f, y + .5f, z + .5f));
                            vertices.Add(new Vector3(x - .5f, y + .5f, z + .5f));
                            vertices.Add(new Vector3(x - .5f, y - .5f, z + .5f));

                            atlasCoords = Atlas.Cords[(int)currBlock - 1, 4];
                            uvs.Add(new Vector2(atlasCoords.x * grid, atlasCoords.y * grid));
                            uvs.Add(new Vector2(atlasCoords.x * grid, (atlasCoords.y + 1) * grid));
                            uvs.Add(new Vector2((atlasCoords.x + 1) * grid, (atlasCoords.y + 1) * grid));
                            uvs.Add(new Vector2((atlasCoords.x + 1) * grid, atlasCoords.y * grid));

                            if (currBlock == BlockType.Water)
                            {
                                Wtriangles.Add(vertices.Count - 4);
                                Wtriangles.Add(vertices.Count - 3);
                                Wtriangles.Add(vertices.Count - 1);

                                Wtriangles.Add(vertices.Count - 3);
                                Wtriangles.Add(vertices.Count - 2);
                                Wtriangles.Add(vertices.Count - 1);
                            }
                            else
                            {
                                triangles.Add(vertices.Count - 4);
                                triangles.Add(vertices.Count - 3);
                                triangles.Add(vertices.Count - 1);

                                triangles.Add(vertices.Count - 3);
                                triangles.Add(vertices.Count - 2);
                                triangles.Add(vertices.Count - 1);
                            }

                            colors.Add(new Color(0, 0, 0, 1));
                            colors.Add(new Color(0, 0, 0, 1));
                            colors.Add(new Color(0, 0, 0, 1));
                            colors.Add(new Color(0, 0, 0, 1));
                        }

                        //Z-
                        if (CheckIfFaceVisible(currBlock, x, y, z - 1))
                        {
                            vertices.Add(new Vector3(x + .5f, y - .5f, z - .5f));
                            vertices.Add(new Vector3(x + .5f, y + .5f, z - .5f));
                            vertices.Add(new Vector3(x - .5f, y + .5f, z - .5f));
                            vertices.Add(new Vector3(x - .5f, y - .5f, z - .5f));

                            atlasCoords = Atlas.Cords[(int)currBlock - 1, 5];
                            uvs.Add(new Vector2(atlasCoords.x * grid, atlasCoords.y * grid));
                            uvs.Add(new Vector2(atlasCoords.x * grid, (atlasCoords.y + 1) * grid));
                            uvs.Add(new Vector2((atlasCoords.x + 1) * grid, (atlasCoords.y + 1) * grid));
                            uvs.Add(new Vector2((atlasCoords.x + 1) * grid, atlasCoords.y * grid));

                            if (currBlock == BlockType.Water)
                            {
                                Wtriangles.Add(vertices.Count - 4);
                                Wtriangles.Add(vertices.Count - 1);
                                Wtriangles.Add(vertices.Count - 3);

                                Wtriangles.Add(vertices.Count - 3);
                                Wtriangles.Add(vertices.Count - 1);
                                Wtriangles.Add(vertices.Count - 2);
                            }
                            else
                            {
                                triangles.Add(vertices.Count - 4);
                                triangles.Add(vertices.Count - 1);
                                triangles.Add(vertices.Count - 3);

                                triangles.Add(vertices.Count - 3);
                                triangles.Add(vertices.Count - 1);
                                triangles.Add(vertices.Count - 2);
                            }

                            colors.Add(new Color(0, 0, 0, 1));
                            colors.Add(new Color(0, 0, 0, 1));
                            colors.Add(new Color(0, 0, 0, 1));
                            colors.Add(new Color(0, 0, 0, 1));
                        }
                    }
                }
            }
        }

        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            name = $"Chunk [{ChunkX},{ChunkZ}]";

            meshFilter.mesh.Clear();

            meshFilter.mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            meshFilter.mesh.subMeshCount = 2;

            meshFilter.mesh.SetVertices(vertices);
            meshFilter.mesh.SetTriangles(triangles, 0);
            meshFilter.mesh.SetTriangles(Wtriangles, 1);
            meshFilter.mesh.SetUVs(0, uvs);
            meshFilter.mesh.SetColors(colors);

            meshFilter.mesh.RecalculateNormals();
            meshCollider.sharedMesh = meshFilter.mesh;

            transform.position = new Vector3(ChunkX * GeneratorCore.singleton.ChunkSizeXZ, 0, ChunkZ * GeneratorCore.singleton.ChunkSizeXZ);
        });
    }

    public bool CheckIfShadow(int x, int y, int z)
	{
        for(int i = y + 1; i < GeneratorCore.singleton.ChunkSizeY; i++)
		{
            if (Blocks[GetArrayCords(x, i, z)] != (byte)BlockType.Air) return true;
		}

        return false;
	}

    public bool CheckIfFaceVisible(BlockType currBlock, int x, int y, int z)
	{
        try
        {
            if (x >= GeneratorCore.singleton.ChunkSizeXZ) return CheckIfRenderBlockInChunk(currBlock, ChunkX + 1, ChunkZ, 0, y, z);
            else if (x < 0) return CheckIfRenderBlockInChunk(currBlock, ChunkX - 1, ChunkZ, GeneratorCore.singleton.ChunkSizeXZ - 1, y, z);
            if (z >= GeneratorCore.singleton.ChunkSizeXZ) return CheckIfRenderBlockInChunk(currBlock, ChunkX, ChunkZ + 1, x, y, 0);
            else if (z < 0) return CheckIfRenderBlockInChunk(currBlock, ChunkX, ChunkZ - 1, x, y, GeneratorCore.singleton.ChunkSizeXZ - 1);
            else if (y >= GeneratorCore.singleton.ChunkSizeY || y < 0) return true;
        }
        catch (Exception e)
        {
			UnityEngine.Debug.LogError(e);
        }

        return Blocks[GetArrayCords(x, y, z)] == (byte)BlockType.Air || (currBlock != BlockType.Water && Blocks[GetArrayCords(x, y, z)] == (byte)BlockType.Water);
    }

    public bool CheckIfRenderBlockInChunk(BlockType currBlock, int cX, int cZ, int x, int y, int z)
	{
        try
        {
            x += cX * GeneratorCore.singleton.ChunkSizeXZ;
            z += cZ * GeneratorCore.singleton.ChunkSizeXZ;

            if (SaveManager.singleton.TryGetBlock(x, y, z, out BlockType bType))
			{
                return bType == BlockType.Air || (currBlock != BlockType.Water && bType == BlockType.Water);
            }
			else
			{
                float noiseValue = (float)GeneratorCore.singleton.simplex.GetValue(x * GeneratorCore.singleton.NoiseScaleXZ, y * GeneratorCore.singleton.NoiseScaleY, z * GeneratorCore.singleton.NoiseScaleXZ);

                if (!(noiseValue >= GeneratorCore.singleton.GenCurve[y]))
                {
                    if (y > 63)
                    {
                        return true;
                    }
                    else
                    {
                        if (currBlock != BlockType.Water)
                        {
                            return true;
                        }
                    }
                }
            }
        }
        catch(Exception e) { UnityEngine.Debug.LogWarning(e); }

        return false;
    }

    public static Vector3 GetLocalChunksBlockCords(int x, int y, int z, int cX, int cZ)
    {
        x = x - (cX * GeneratorCore.singleton.ChunkSizeXZ);
        z = z - (cZ * GeneratorCore.singleton.ChunkSizeXZ);

        return new Vector3(x, y, z);
    }

    public int GetArrayCords(int x, int y, int z)
    {
        return (x * GeneratorCore.singleton.ChunkSizeXZ * GeneratorCore.singleton.ChunkSizeY) + (y * GeneratorCore.singleton.ChunkSizeXZ) + z;
    }
}

public class Atlas
{
    public static Vector2[,] Cords = new Vector2[,] {
        { new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0) },
        { new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0) },
        { new Vector2(2, 0), new Vector2(2, 0), new Vector2(3, 0), new Vector2(1, 0), new Vector2(2, 0), new Vector2(2, 0) },
        { new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1) },
        { new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1) },
        { new Vector2(2, 1), new Vector2(2, 1), new Vector2(2, 1), new Vector2(2, 1), new Vector2(2, 1), new Vector2(2, 1) },
        { new Vector2(3, 1), new Vector2(3, 1), new Vector2(3, 1), new Vector2(3, 1), new Vector2(3, 1), new Vector2(3, 1) },
        { new Vector2(0, 2), new Vector2(0, 2), new Vector2(0, 2), new Vector2(0, 2), new Vector2(0, 2), new Vector2(0, 2) },
        { new Vector2(1, 2), new Vector2(1, 2), new Vector2(1, 2), new Vector2(1, 2), new Vector2(1, 2), new Vector2(1, 2) },
        { new Vector2(2, 2), new Vector2(2, 2), new Vector2(2, 2), new Vector2(2, 2), new Vector2(2, 2), new Vector2(2, 2) }
    };
}
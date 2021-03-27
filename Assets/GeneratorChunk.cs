using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class GeneratorChunk : MonoBehaviour
{
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;
    public MeshCollider meshCollider;

    public int ChunkX;
    public int ChunkZ;

    public BlockType[,,] Blocks = new BlockType[0, 0, 0];

    public void GenerateChunk(int cX, int cZ)
	{
        AnimationCurve genCurve = new AnimationCurve();
        genCurve.keys = GeneratorCore.singleton.GeneratorCurve.keys;

        ChunkX = cX;
        ChunkZ = cZ;

        int depthY = 0;

        for (int x = cX * GeneratorCore.singleton.ChunkSizeXZ; x < (cX * GeneratorCore.singleton.ChunkSizeXZ) + GeneratorCore.singleton.ChunkSizeXZ; x++)
        {
            for (int z = cZ * GeneratorCore.singleton.ChunkSizeXZ; z < (cZ * GeneratorCore.singleton.ChunkSizeXZ) + GeneratorCore.singleton.ChunkSizeXZ; z++)
            {
                for (int y = GeneratorCore.singleton.ChunkSizeY - 1; y >= 0; y--)
                {
                    float noiseValue = (float)GeneratorCore.singleton.simplex.GetValue(x * GeneratorCore.singleton.NoiseScaleXZ, y * GeneratorCore.singleton.NoiseScaleY, z * GeneratorCore.singleton.NoiseScaleXZ);

                    Vector3 local = GetLocalChunksBlockCords(x, y, z, cX, cZ);
                    int lx = (int)local.x;
                    int ly = (int)local.y;
                    int lz = (int)local.z;

                    if (noiseValue >= genCurve.Evaluate(y / ((float)GeneratorCore.singleton.ChunkSizeY)))
                    {
                        if (depthY == 0)
                        {
                            Blocks[lx, ly, lz] = BlockType.Grass;
                        }
                        else if (depthY < 5)
                        {
                            Blocks[lx, ly, lz] = BlockType.Dirt;
                        }
                        else
                        {
                            Blocks[lx, ly, lz] = BlockType.Stone;
                        }

                        depthY++;
                    }
                    else
                    {
                        if (y > 63)
                        {
                            Blocks[lx, ly, lz] = BlockType.Air;
                        }
                        else
                        {
                            Blocks[lx, ly, lz] = BlockType.Water;
                            depthY++;
                        }
                    }
                }

                depthY = 0;
            }
        }

        GenerateChunkMesh();
    }

    public void SetBlockLocal(int localX, int localY, int localZ, BlockType blockToSet)
	{
        Blocks[localX,localY,localZ] = blockToSet;

        Task.Run(() =>
        {
            RegenerateChunk();
        });
	}
    public void SetBlockGlobal(int gloablX, int gloablY, int gloablZ, BlockType blockToSet)
    {
        Vector3 local = GetLocalChunksBlockCords(gloablX, gloablY, gloablZ, ChunkX, ChunkZ);
        Blocks[(int)local.x, (int)local.y, (int)local.z] = blockToSet;

        Task.Run(() =>
        {
            RegenerateChunk();
        });
    }

    public void GenerateChunkFromBlocks(int cX, int cZ, BlockType[,,] blocks)
    {
        Blocks = blocks;
        ChunkX = cX;
        ChunkZ = cZ;

        GenerateChunkMesh();
    }

    public void RegenerateChunk(BlockType[,,] blocks = null)
	{
        if(blocks != null) Blocks = blocks;

        GenerateChunkMesh();
	}

    public void GenerateChunkMesh()
	{
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
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
                    BlockType currBlock = Blocks[x, y, z];

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

                            triangles.Add(vertices.Count - 4);
                            triangles.Add(vertices.Count - 3);
                            triangles.Add(vertices.Count - 1);

                            triangles.Add(vertices.Count - 3);
                            triangles.Add(vertices.Count - 2);
                            triangles.Add(vertices.Count - 1);

                            colors.Add(new Color(0, 0, 0, lightLevel));
                            colors.Add(new Color(0, 0, 0, lightLevel));
                            colors.Add(new Color(0, 0, 0, lightLevel));
                            colors.Add(new Color(0, 0, 0, lightLevel));
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

                            triangles.Add(vertices.Count - 4);
                            triangles.Add(vertices.Count - 1);
                            triangles.Add(vertices.Count - 3);

                            triangles.Add(vertices.Count - 3);
                            triangles.Add(vertices.Count - 1);
                            triangles.Add(vertices.Count - 2);

                            colors.Add(new Color(0, 0, 0, lightLevel));
                            colors.Add(new Color(0, 0, 0, lightLevel));
                            colors.Add(new Color(0, 0, 0, lightLevel));
                            colors.Add(new Color(0, 0, 0, lightLevel));
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

                            triangles.Add(vertices.Count - 4);
                            triangles.Add(vertices.Count - 3);
                            triangles.Add(vertices.Count - 1);

                            triangles.Add(vertices.Count - 3);
                            triangles.Add(vertices.Count - 2);
                            triangles.Add(vertices.Count - 1);

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

                            triangles.Add(vertices.Count - 4);
                            triangles.Add(vertices.Count - 1);
                            triangles.Add(vertices.Count - 3);

                            triangles.Add(vertices.Count - 3);
                            triangles.Add(vertices.Count - 1);
                            triangles.Add(vertices.Count - 2);

                            colors.Add(new Color(0, 0, 0, lightLevel));
                            colors.Add(new Color(0, 0, 0, lightLevel));
                            colors.Add(new Color(0, 0, 0, lightLevel));
                            colors.Add(new Color(0, 0, 0, lightLevel));
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

                            triangles.Add(vertices.Count - 4);
                            triangles.Add(vertices.Count - 3);
                            triangles.Add(vertices.Count - 1);

                            triangles.Add(vertices.Count - 3);
                            triangles.Add(vertices.Count - 2);
                            triangles.Add(vertices.Count - 1);

                            colors.Add(new Color(0, 0, 0, lightLevel));
                            colors.Add(new Color(0, 0, 0, lightLevel));
                            colors.Add(new Color(0, 0, 0, lightLevel));
                            colors.Add(new Color(0, 0, 0, lightLevel));
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

                            triangles.Add(vertices.Count - 4);
                            triangles.Add(vertices.Count - 1);
                            triangles.Add(vertices.Count - 3);

                            triangles.Add(vertices.Count - 3);
                            triangles.Add(vertices.Count - 1);
                            triangles.Add(vertices.Count - 2);

                            colors.Add(new Color(0, 0, 0, lightLevel));
                            colors.Add(new Color(0, 0, 0, lightLevel));
                            colors.Add(new Color(0, 0, 0, lightLevel));
                            colors.Add(new Color(0, 0, 0, lightLevel));
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

            meshFilter.mesh.SetVertices(vertices);
            meshFilter.mesh.SetTriangles(triangles, 0);
            meshFilter.mesh.SetUVs(0, uvs);
            meshFilter.mesh.SetColors(colors);

            Task.Run(() =>
            {
                meshFilter.mesh.RecalculateNormals();
                meshCollider.sharedMesh = meshFilter.mesh;
            });

            transform.position = new Vector3(ChunkX * GeneratorCore.singleton.ChunkSizeXZ, 0, ChunkZ * GeneratorCore.singleton.ChunkSizeXZ);
        });
    }

    public bool CheckIfShadow(int x, int y, int z)
	{
        for(int i = y + 1; i < GeneratorCore.singleton.ChunkSizeY; i++)
		{
            if (Blocks[x, i, z] != BlockType.Air) return true;
		}

        return false;
	}

    public bool CheckIfFaceVisible(BlockType currBlock, int x, int y, int z)
	{
        if (x >= GeneratorCore.singleton.ChunkSizeXZ || y >= GeneratorCore.singleton.ChunkSizeY || z >= GeneratorCore.singleton.ChunkSizeXZ) return true;
        if (x < 0 || y < 0 || z < 0) return true;

        return Blocks[x, y, z] == BlockType.Air || (currBlock != BlockType.Water && Blocks[x, y, z] == BlockType.Water);
    }

    public Vector3 GetLocalChunksBlockCords(int x, int y, int z, int cX, int cZ)
    {
        x = x - (cX * GeneratorCore.singleton.ChunkSizeXZ);
        z = z - (cZ * GeneratorCore.singleton.ChunkSizeXZ);

        return new Vector3(x, y, z);
    }
}
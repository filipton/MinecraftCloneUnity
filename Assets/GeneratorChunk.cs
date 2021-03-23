using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class GeneratorChunk : MonoBehaviour
{
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;

    public int ChunkX;
    public int ChunkZ;

    public BlockType[,,] Blocks = new BlockType[0, 0, 0];

    public void GenerateChunk(int cX, int cZ)
	{
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            meshRenderer.enabled = false;

            transform.position = new Vector3(cX * GeneratorCore.singleton.ChunkSizeXZ, 0, cZ * GeneratorCore.singleton.ChunkSizeXZ);
            name = $"Chunk [{cX},{cZ}]";
        });

        int depthY = 0;

        for (int x = cX * GeneratorCore.singleton.ChunkSizeXZ; x < (cX * GeneratorCore.singleton.ChunkSizeXZ) + GeneratorCore.singleton.ChunkSizeXZ; x++)
        {
            for (int z = cZ * GeneratorCore.singleton.ChunkSizeXZ; z < (cZ * GeneratorCore.singleton.ChunkSizeXZ) + GeneratorCore.singleton.ChunkSizeXZ; z++)
            {
                for (int y = GeneratorCore.singleton.ChunkSizeY - 1; y >= 0; y--)
                {
                    //float noiseValue = GeneratorCore.fn.GetNoise(x * GeneratorCore.singleton.NoiseScale, y * GeneratorCore.singleton.NoiseScale, z * GeneratorCore.singleton.NoiseScale);

                    //float noiseValue = Noise3D(x * GeneratorCore.singleton.NoiseScale, y * GeneratorCore.singleton.NoiseScale, z * GeneratorCore.singleton.NoiseScale, GeneratorCore.singleton.NoiseFrequency, GeneratorCore.singleton.NoiseAmplitude, GeneratorCore.singleton.NoisePersistance, GeneratorCore.singleton.NoiseOctaves, GeneratorCore.singleton.NoiseSeed);
                    //float noiseValue = (float)Simplex.SimplexNoise3D(x * GeneratorCore.singleton.NoiseScale, y * GeneratorCore.singleton.NoiseScale, z * GeneratorCore.singleton.NoiseScale);
                    float noiseValue = (float)GeneratorCore.singleton.simplex.GetValue(x * GeneratorCore.singleton.NoiseScale, y * GeneratorCore.singleton.NoiseScale, z * GeneratorCore.singleton.NoiseScale);

                    Vector3 local = GetLocalChunksBlockCords(x, y, z, cX, cZ);
                    int lx = (int)local.x;
                    int ly = (int)local.y;
                    int lz = (int)local.z;

                    if (noiseValue >= GeneratorCore.singleton.GeneratorCurve.Evaluate(y / ((float)GeneratorCore.singleton.ChunkSizeY)))
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

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        Vector2 atlasCoords;
        float grid = 1f / GeneratorCore.singleton.atlasSize;

        for (int x = 0; x < GeneratorCore.singleton.ChunkSizeXZ; x++)
        {
            for (int z = 0; z < GeneratorCore.singleton.ChunkSizeXZ; z++)
            {
                for (int y = 0; y < GeneratorCore.singleton.ChunkSizeY; y++)
                {
                    if (Blocks[x, y, z] != BlockType.Air)
                    {
                        //X+
                        if (IfAir(x + 1, y, z) || CheckIfWater(Blocks[x, y, z], x + 1, y, z))
                        {
                            vertices.Add(new Vector3(x + .5f, y - .5f, z - .5f));
                            vertices.Add(new Vector3(x + .5f, y + .5f, z - .5f));
                            vertices.Add(new Vector3(x + .5f, y + .5f, z + .5f));
                            vertices.Add(new Vector3(x + .5f, y - .5f, z + .5f));

                            atlasCoords = Atlas.Cords[(int)Blocks[x, y, z] - 1, 0];
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

                            /*colors.Add(new Color(255, 0, 0, Light));
                            colors.Add(new Color(255, 0, 0, Light));
                            colors.Add(new Color(255, 0, 0, Light));
                            colors.Add(new Color(255, 0, 0, Light));*/
                        }

                        //X-
                        if (IfAir(x - 1, y, z) || CheckIfWater(Blocks[x, y, z], x - 1, y, z))
                        {
                            vertices.Add(new Vector3(x - .5f, y - .5f, z - .5f));
                            vertices.Add(new Vector3(x - .5f, y + .5f, z - .5f));
                            vertices.Add(new Vector3(x - .5f, y + .5f, z + .5f));
                            vertices.Add(new Vector3(x - .5f, y - .5f, z + .5f));

                            atlasCoords = Atlas.Cords[(int)Blocks[x, y, z] - 1, 1];
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
                        }

                        //Y+
                        if (IfAir(x, y + 1, z) || CheckIfWater(Blocks[x, y, z], x, y + 1, z))
                        {
                            vertices.Add(new Vector3(x + .5f, y + .5f, z - .5f));
                            vertices.Add(new Vector3(x - .5f, y + .5f, z - .5f));
                            vertices.Add(new Vector3(x - .5f, y + .5f, z + .5f));
                            vertices.Add(new Vector3(x + .5f, y + .5f, z + .5f));

                            atlasCoords = Atlas.Cords[(int)Blocks[x, y, z] - 1, 2];
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
                        }

                        //Y-
                        if (IfAir(x, y - 1, z) || CheckIfWater(Blocks[x, y, z], x, y - 1, z))
                        {
                            vertices.Add(new Vector3(x + .5f, y - .5f, z - .5f));
                            vertices.Add(new Vector3(x - .5f, y - .5f, z - .5f));
                            vertices.Add(new Vector3(x - .5f, y - .5f, z + .5f));
                            vertices.Add(new Vector3(x + .5f, y - .5f, z + .5f));

                            atlasCoords = Atlas.Cords[(int)Blocks[x, y, z] - 1, 3];
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
                        }

                        //Z+
                        if (IfAir(x, y, z + 1) || CheckIfWater(Blocks[x, y, z], x, y, z + 1))
                        {
                            vertices.Add(new Vector3(x + .5f, y - .5f, z + .5f));
                            vertices.Add(new Vector3(x + .5f, y + .5f, z + .5f));
                            vertices.Add(new Vector3(x - .5f, y + .5f, z + .5f));
                            vertices.Add(new Vector3(x - .5f, y - .5f, z + .5f));

                            atlasCoords = Atlas.Cords[(int)Blocks[x, y, z] - 1, 4];
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
                        }

                        //Z-
                        if (IfAir(x, y, z - 1) || CheckIfWater(Blocks[x, y, z], x, y, z - 1))
                        {
                            vertices.Add(new Vector3(x + .5f, y - .5f, z - .5f));
                            vertices.Add(new Vector3(x + .5f, y + .5f, z - .5f));
                            vertices.Add(new Vector3(x - .5f, y + .5f, z - .5f));
                            vertices.Add(new Vector3(x - .5f, y - .5f, z - .5f));

                            atlasCoords = Atlas.Cords[(int)Blocks[x, y, z] - 1, 5];
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
                        }
                    }
                }
            }
        }

        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            Mesh tmpMesh = new Mesh();

            tmpMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            tmpMesh.SetVertices(vertices);
            tmpMesh.SetUVs(0, uvs);
            tmpMesh.SetTriangles(triangles, 0);
            tmpMesh.RecalculateNormals();

            meshFilter.mesh = tmpMesh;

            meshRenderer.enabled = true;

            ChunkX = cX;
            ChunkZ = cZ;
        });
    }

	public bool IfAir(int x, int y, int z)
    {
        if (x >= GeneratorCore.singleton.ChunkSizeXZ || y >= GeneratorCore.singleton.ChunkSizeY || z >= GeneratorCore.singleton.ChunkSizeXZ) return true;
        if (x < 0 || y < 0 || z < 0) return true;

        return Blocks[x, y, z] == BlockType.Air;
    }

    public bool CheckIfWater(BlockType blockType, int x, int y, int z)
    {
        if (blockType != BlockType.Water)
        {
            if (x >= GeneratorCore.singleton.ChunkSizeXZ || y >= GeneratorCore.singleton.ChunkSizeY || z >= GeneratorCore.singleton.ChunkSizeXZ) return true;
            if (x < 0 || y < 0 || z < 0) return true;

            if (Blocks[x, y, z] == BlockType.Water)
            {
                return true;
            }
        }

        return false;
    }

    public Vector3 GetLocalChunksBlockCords(int x, int y, int z, int cX, int cZ)
	{
        x = x - (cX * GeneratorCore.singleton.ChunkSizeXZ);
        z = z - (cZ * GeneratorCore.singleton.ChunkSizeXZ);

        return new Vector3(x, y, z);
	}
}
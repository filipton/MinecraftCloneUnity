using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

public class TestCube : MonoBehaviour
{
    public static TestCube instance;

    public FastNoiseExtension noise;
    public Simplex simplex;

    public AnimationCurve terrainElev;
    public Material materialTerrain;

    public float Light = 1f;

    public int SizeXZ = 16;
    public int SizeY = 16;

    public float scale = 0.05f;
    public float freq = 0.0522f;
    public float ampl = 1f;
    public float persistance = 1f;
    public int Octave = 6;
    public int seed = 321;

    public BlockType[,,] Blocks = new BlockType[0,0,0];

    public float atlasSize = 4;

    public bool GenAgain = false;
    public bool done = false;

	public void Start()
	{
        StartGen();
	}

	// Start is called before the first frame update
	private void StartGen()
	{
        done = false;
        instance = this;

        DeleteChunks();

        FastNoiseLite fn = noise.GetLibInstance(3245654);
        simplex = new Simplex();
        simplex.Frequency = 0.00522D;

        new Thread(() =>
        {
            Blocks = new BlockType[SizeXZ, SizeY, SizeXZ];

            int depthY = 0;

            for (int x = 0; x < SizeXZ; x++)
            {
                for (int z = 0; z < SizeXZ; z++)
                {
                    for (int y = SizeY - 1; y >= 0; y--)
                    {
                        //float noiseValue = Noise3D(x, y, z, 0.005f, 1.2f, 1, 3, 0);
                        float noiseValue = (float)simplex.GetValue(x * scale, y * scale, z * scale);

                        if (noiseValue >= terrainElev.Evaluate(y / ((float)SizeY)))
                        {
                            if (depthY == 0)
                            {
                                Blocks[x, y, z] = BlockType.Grass;
                            }
                            else if (depthY < 5)
                            {
                                Blocks[x, y, z] = BlockType.Dirt;
                            }
                            else
                            {
                                Blocks[x, y, z] = BlockType.Stone;
                            }

                            depthY++;
                        }
                        else
                        {
                            if (y > 96)
                            {
                                Blocks[x, y, z] = BlockType.Air;
                            }
                            else
                            {
                                Blocks[x, y, z] = BlockType.Water;
                                depthY++;
                            }
                        }
                    }

                    depthY = 0;
                }
            }

            done = true;
        }).Start();
    }

    public void GenerateChunks()
	{
        Mesh mesh = new Mesh();

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        List<Color> colors = new List<Color>();

        Vector2 atlasCoords;
        float grid = 1f / atlasSize;

        for (int x = 0; x < SizeXZ; x++)
        {
            for (int y = 0; y < SizeY; y++)
            {
                for (int z = 0; z < SizeXZ; z++)
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

                            colors.Add(new Color(255, 0, 0, Light));
                            colors.Add(new Color(255, 0, 0, Light));
                            colors.Add(new Color(255, 0, 0, Light));
                            colors.Add(new Color(255, 0, 0, Light));
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

                            colors.Add(new Color(255, 0, 0, Light));
                            colors.Add(new Color(255, 0, 0, Light));
                            colors.Add(new Color(255, 0, 0, Light));
                            colors.Add(new Color(255, 0, 0, Light));
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

                            colors.Add(new Color(255, 0, 0, Light));
                            colors.Add(new Color(255, 0, 0, Light));
                            colors.Add(new Color(255, 0, 0, Light));
                            colors.Add(new Color(255, 0, 0, Light));
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

                            colors.Add(new Color(255, 0, 0, Light));
                            colors.Add(new Color(255, 0, 0, Light));
                            colors.Add(new Color(255, 0, 0, Light));
                            colors.Add(new Color(255, 0, 0, Light));
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

                            colors.Add(new Color(255, 0, 0, Light));
                            colors.Add(new Color(255, 0, 0, Light));
                            colors.Add(new Color(255, 0, 0, Light));
                            colors.Add(new Color(255, 0, 0, Light));
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

                            colors.Add(new Color(255, 0, 0, Light));
                            colors.Add(new Color(255, 0, 0, Light));
                            colors.Add(new Color(255, 0, 0, Light));
                            colors.Add(new Color(255, 0, 0, Light));
                        }

                        /*if (vertices.Count > 65000)
                        {
                            mesh = new Mesh();
                            mesh.SetVertices(vertices);
                            mesh.SetUVs(0, uvs);
                            mesh.SetTriangles(triangles, 0);
                            mesh.SetColors(colors);
                            mesh.RecalculateNormals();
                            mesh.Optimize();

                            vertices.Clear();
                            triangles.Clear();
                            uvs.Clear();
                            colors.Clear();

                            GameObject gbt = new GameObject("Mesh");
                            gbt.AddComponent<MeshRenderer>().material = materialTerrain;
                            gbt.AddComponent<MeshFilter>().mesh = mesh;
                            gbt.AddComponent<MeshCollider>();
                        }*/
                    }
                }
            }
        }

        mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.SetColors(colors);
        mesh.RecalculateNormals();
        mesh.Optimize();

        GameObject gb = new GameObject("Mesh");
        gb.AddComponent<MeshRenderer>().material = materialTerrain;
        gb.AddComponent<MeshFilter>().mesh = mesh;
        gb.AddComponent<MeshCollider>();
    }

    public static void DeleteChunks()
	{
        foreach(MeshFilter gb in FindObjectsOfType<MeshFilter>().ToArray())
		{
            Destroy(gb.gameObject);
		}
	}

    public bool IfAir(int x, int y, int z)
	{
        if (x >= SizeXZ || y >= SizeY || z >= SizeXZ) return true;
        if (x < 0 || y < 0 || z < 0) return true;

        return Blocks[x, y, z] == BlockType.Air;
	}

    public bool CheckIfWater(BlockType blockType, int x, int y, int z)
	{
        if(blockType != BlockType.Water)
		{
            if (x >= SizeXZ || y >= SizeY || z >= SizeXZ) return true;
            if (x < 0 || y < 0 || z < 0) return true;

            if (Blocks[x,y,z] == BlockType.Water)
			{
                return true;
			}
		}

        return false;
	}

    // Update is called once per frame
    void Update()
    {
		if (done)
		{
            done = false;
            GenerateChunks();
        }
        else if (GenAgain)
		{
            GenAgain = false;
            StartGen();
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
}

public class Atlas
{
    public static Vector2[,] Cords = new Vector2[,] {
        { new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0) },
        { new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0) },
        { new Vector2(2, 0), new Vector2(2, 0), new Vector2(3, 0), new Vector2(1, 0), new Vector2(2, 0), new Vector2(2, 0) },
        { new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1) },
        { new Vector2(2, 1), new Vector2(2, 1), new Vector2(2, 1), new Vector2(2, 1), new Vector2(2, 1), new Vector2(2, 1) }
    };
}
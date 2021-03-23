using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    public LayerMask layerMask;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        RaycastHit Hit;
        Ray dir = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        if (Physics.Raycast(dir, out Hit, 1000, layerMask))
        {
            //break block
            Vector3 hitCoord = new Vector3(Hit.point.x, Hit.point.y, Hit.point.z);
            hitCoord += (new Vector3(Hit.normal.x, Hit.normal.y, Hit.normal.z)) * -0.5f;

            int x = Mathf.RoundToInt(hitCoord.x);
            int y = Mathf.RoundToInt(hitCoord.y);
            int z = Mathf.RoundToInt(hitCoord.z);

            if (Input.GetKey(KeyCode.Mouse0))
            {
                print($"{x} {y} {z} {TestCube.instance.Blocks[x, y, z]}");
                TestCube.instance.Blocks[x, y, z] = TestCube.BlockType.Air;

                TestCube.DeleteChunks();
                TestCube.instance.GenerateChunks();
            }
            /*else if (Input.GetKeyDown(KeyCode.Mouse1))
            {
                Vector3 ChunkCoords = Generator.GetChunkCords(px, py, pz);
                Chunk c = Generator.Chunks.Find(u => u.ChunkX == ChunkCoords.x && u.ChunkY == ChunkCoords.y && u.ChunkZ == ChunkCoords.z);
                if (c == null)
                {
                    GameObject chunkI = Instantiate(Generator.ChunkPrefab, Generator.ChunksParent.transform);
                    chunkI.name = $"Chunk ({ChunkCoords.x}, {ChunkCoords.y}, {ChunkCoords.z}) (With Placing)";

                    Chunk chunk = chunkI.GetComponent<Chunk>();
                    chunk.Generator = Generator;

                    Generator.Chunks.Add(chunk);

                    chunk.ChunkX = (int)ChunkCoords.x;
                    chunk.ChunkY = (int)ChunkCoords.y;
                    chunk.ChunkZ = (int)ChunkCoords.z;

                    chunk.SetBlock(px, py, pz, 1);
                }
                else
                {
                    c.SetBlock(px, py, pz, 1);
                }
            }*/
        }
    }
}
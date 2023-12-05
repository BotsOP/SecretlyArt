using System;
using System.Collections.Generic;
using Managers;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using EventType = Managers.EventType;

namespace MeshGen
{
    public class MeshGenManager : MonoBehaviour
    {
        public static ComputeShader caveGenerationShader;
        public static int threadGroupSize0;
        public static int threadGroupSizeOut1;
        public static float isoLevel = 0.5f;
        public static int chunkSizeStatic = 32;
        public static ComputeBuffer amountVertsBuffer;
        public static GraphicsBuffer appendTrianglesBuffer;

        [SerializeField] private GameObject meshContainer;
        [Range(1, 32)] public int amountChunksX = 16;
        [Range(1, 32)] public int amountChunksY = 8;
        [Range(1, 32)] public int amountChunksZ = 16;
        [Range(0, 1)] public float chunkScale = 0.5f;
        [Range(1, 128)] public int chunkSize = 32;

        public RenderTexture test;
        
        public LayerMask caveMask;
        [NonSerialized] public Chunk[,,] chunks;
        [NonSerialized] public Vector3[] caveBounds;


        private void OnEnable()
        {
            UnsafeUtility.SetLeakDetectionMode(NativeLeakDetectionMode.Disabled);
            
            chunkSizeStatic = chunkSize;
            float stepSize = (chunkSizeStatic - 1) * chunkScale;

            BoxCollider boxCollider = meshContainer.GetComponent<BoxCollider>();
            boxCollider.center = new Vector3(stepSize / 2f, stepSize / 2f, stepSize / 2f);
            boxCollider.size = new Vector3(stepSize, stepSize, stepSize);
            
            amountVertsBuffer = new ComputeBuffer(1, sizeof(uint));
            amountVertsBuffer.SetData(new [] { 0 });
            int amountMaxVerts = (int)Mathf.Pow(chunkSize, 3) * 3;
            appendTrianglesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Append, amountMaxVerts, sizeof(float) * (3 + 3));

            caveGenerationShader = Resources.Load<ComputeShader>("CaveGeneration");

            caveGenerationShader.GetKernelThreadGroupSizes(0, out uint threadGroupSizeX, out uint threadGroupSizeY, out uint threadGroupSizeZ);
            threadGroupSize0 = Mathf.CeilToInt((float)chunkSizeStatic / threadGroupSizeX);
            caveGenerationShader.GetKernelThreadGroupSizes(1, out threadGroupSizeX, out threadGroupSizeY, out threadGroupSizeZ);
            threadGroupSizeOut1 = (int)threadGroupSizeX;

            caveBounds = new Vector3[2];
            caveBounds[0] = transform.position;
            caveBounds[1] = new Vector3(amountChunksX * stepSize, amountChunksY * stepSize, amountChunksZ * stepSize) + caveBounds[0];

            chunks = new Chunk[amountChunksX, amountChunksY, amountChunksX];

            for (int i = 0; i < chunks.GetLength(0); i++)
            for (int j = 0; j < chunks.GetLength(1); j++)
            for (int k = 0; k < chunks.GetLength(2); k++)
            {
                Vector3 index = new Vector3(i, j, k);
                Vector3 pos = index * stepSize + transform.position;
                GameObject meshObject = Instantiate(meshContainer, pos, Quaternion.identity, transform);
                chunks[(int)index.x, (int)index.y, (int)index.z] = new Chunk(pos, meshObject);
            }

            test = chunks[0, 0, 0].noiseTex;
        }

        private void OnDisable()
        {
            for (int i = 0; i < chunks.GetLength(0); i++)
            for (int j = 0; j < chunks.GetLength(1); j++)
            for (int k = 0; k < chunks.GetLength(2); k++)
            {
                chunks[i, j, k].OnDestroy();
            }
            amountVertsBuffer?.Dispose();
            appendTrianglesBuffer?.Dispose();
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.8f, 0.1f, 0.1f, 0.3f);
            float stepSize = (chunkSizeStatic - 1) * chunkScale;
            Vector3 minCorner = transform.position;
            Vector3 maxCorner = new Vector3(amountChunksX * stepSize, amountChunksY * stepSize, amountChunksZ * stepSize) + minCorner;
            Gizmos.DrawCube(minCorner + (maxCorner - minCorner) / 2, maxCorner - minCorner);
        }
        
        public Vector3Int GetChunkIndex(Vector3 _playerPos)
        {
            Vector3 chunkIndex = _playerPos.Remap(caveBounds[0], caveBounds[1], Vector3.zero, 
                                                  new Vector3(amountChunksX, amountChunksY, amountChunksX));
            return new Vector3Int((int)chunkIndex.x, (int)chunkIndex.y, (int)chunkIndex.z);
        }

        public bool IsChunkIndexWithinRange(Vector3Int _index)
        {
            return !(_index.x < 0 || _index.x > amountChunksX - 1 || _index.y < 0 || _index.y > amountChunksY - 1 || _index.z < 0 || _index.z > amountChunksZ - 1);
        }

    }
}

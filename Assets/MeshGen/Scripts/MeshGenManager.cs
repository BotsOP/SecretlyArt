using System;
using System.Collections.Generic;
using Managers;
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

        [SerializeField] private GameObject meshContainer;
        [Range(1, 32)] public int amountChunksHorizontal = 16;
        [Range(1, 16)] public int amountChunksVertical = 8;
        [Range(1, 128)] public int chunkSize = 32;

        public RenderTexture test;
        
        public LayerMask caveMask;
        [NonSerialized] public Chunk[,,] chunks;
        [NonSerialized] public Vector3[] caveBounds;

        private float caveWidth;
        private int stepSize;

        private void OnEnable()
        {
            chunkSizeStatic = chunkSize;
            stepSize = chunkSizeStatic - 1;

            caveWidth = amountChunksHorizontal * stepSize;

            // BoxCollider boxCollider = meshContainer.GetComponent<BoxCollider>();
            // boxCollider.center = new Vector3(stepSize / 2f, stepSize / 2f, stepSize / 2f);
            // boxCollider.size = new Vector3(stepSize, stepSize, stepSize);

            caveGenerationShader = Resources.Load<ComputeShader>("CaveGeneration");

            caveGenerationShader.GetKernelThreadGroupSizes(0, out uint threadGroupSizeX, out uint threadGroupSizeY, out uint threadGroupSizeZ);
            threadGroupSize0 = Mathf.CeilToInt((float)chunkSizeStatic / threadGroupSizeX);
            caveGenerationShader.GetKernelThreadGroupSizes(1, out threadGroupSizeX, out threadGroupSizeY, out threadGroupSizeZ);
            threadGroupSizeOut1 = (int)threadGroupSizeX;

            caveBounds = new Vector3[2];
            caveBounds[0] = transform.position;
            caveBounds[1] = new Vector3(caveWidth, amountChunksVertical * stepSize, caveWidth) + transform.position;

            chunks = new Chunk[amountChunksHorizontal, amountChunksVertical, amountChunksHorizontal];

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
                if (ReferenceEquals(chunks[i, j, k], null))
                    continue;

                chunks[i, j, k].OnDestroy();
            }
        }

        private void Update()
        {
            // if (Input.GetKeyDown(KeyCode.B))
            // {
            //     for (int i = 0; i < chunks.GetLength(0); i++)
            //     for (int j = 0; j < chunks.GetLength(1); j++)
            //     for (int k = 0; k < chunks.GetLength(2); k++)
            //     {
            //         if (ReferenceEquals(chunks[i, j, k], null))
            //             continue;
            //             
            //         chunks[i, j, k].GenerateMesh();
            //     }
            // }
        }

    }
}

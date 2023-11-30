using System;
using System.Collections;
using System.Collections.Generic;
using Managers;
using UnityEngine;

namespace MeshGen
{
    [RequireComponent(typeof(MeshGenManager)), RequireComponent(typeof(GPUPhysicsManager))]
    public class MeshCarver : MonoBehaviour
    {
        private ComputeShader caveCarveShader;
        private Vector3 threadGroupSize;
        private MeshGenManager meshGenManager;
        private GPUPhysicsManager physicsManager;
        private Chunk[,,] chunks => meshGenManager.chunks;
        private Vector3[] caveBounds => meshGenManager.caveBounds;
        private int amountChunksHorizontal => meshGenManager.amountChunksHorizontal;
        private int amountChunksVertical => meshGenManager.amountChunksVertical;
        private LayerMask caveMask => meshGenManager.caveMask;

        private int chunkSize;

        private void OnEnable()
        {
            meshGenManager = GetComponent<MeshGenManager>();
            physicsManager = GetComponent<GPUPhysicsManager>();
            
            caveCarveShader = Resources.Load<ComputeShader>("SDFCarver");
            caveCarveShader.GetKernelThreadGroupSizes(0, out uint threadGroupSizeX, out uint threadGroupSizeY, out uint threadGroupSizeZ);
        
            threadGroupSize.x = threadGroupSizeX;
            threadGroupSize.y = threadGroupSizeY;
            threadGroupSize.z = threadGroupSizeZ;

            chunkSize = MeshGenManager.chunkSizeStatic;

            EventSystem<MyRay, float, float>.Subscribe(Managers.EventType.CARVE_TERRAIN, RemoveTerrain);
            EventSystem<Vector3, float, float>.Subscribe(Managers.EventType.CARVE_TERRAIN, RemoveTerrain);
            EventSystem<Vector3, float, float>.Subscribe(Managers.EventType.FILL_TERRAIN, FillTerrain);
        }

        private void OnDisable()
        {
            EventSystem<MyRay, float, float>.Unsubscribe(Managers.EventType.CARVE_TERRAIN, RemoveTerrain);
            EventSystem<Vector3, float, float>.Unsubscribe(Managers.EventType.CARVE_TERRAIN, RemoveTerrain);
            EventSystem<Vector3, float, float>.Unsubscribe(Managers.EventType.FILL_TERRAIN, FillTerrain);
        }

        private void RemoveTerrain(MyRay _ray, float _carveSize, float _carveSpeed)
        {
            if (physicsManager.Raycast(_ray.origin, _ray.direction, out var rayOutput))
            {
                // raycastCursor.position = rayOutput.position;
                // raycastCursor.rotation = Quaternion.LookRotation(rayOutput.normal);
                RemoveTerrain(rayOutput.position, _carveSize, _carveSpeed);
            }
        }

        //These functions do not yet work with other isolevels
        private void RemoveTerrain(Vector3 _pos, float _carveSize, float _carveSpeed)
        {
            Collider[] chunksHit = Physics.OverlapSphere(_pos, _carveSize, caveMask);
            foreach (var chunkCollider in chunksHit)
            {
                Vector3 chunkIndex = GetChunkIndex(chunkCollider.transform.position);
                Chunk chunk = chunks[(int)chunkIndex.x, (int)chunkIndex.y, (int)chunkIndex.z];
                Vector3 carvePos = _pos - chunk.position;

                Vector3[] areaHitCorners = { _pos - new Vector3(_carveSize, _carveSize, _carveSize) - chunk.position, _pos + new Vector3(_carveSize, _carveSize, _carveSize) - chunk.position};
            
                areaHitCorners[0].x = Mathf.Clamp(areaHitCorners[0].x, 0, chunkSize);
                areaHitCorners[0].y = Mathf.Clamp(areaHitCorners[0].y, 0, chunkSize);
                areaHitCorners[0].z = Mathf.Clamp(areaHitCorners[0].z, 0, chunkSize);
            
                areaHitCorners[1].x = Mathf.Clamp(areaHitCorners[1].x, 0, chunkSize);
                areaHitCorners[1].y = Mathf.Clamp(areaHitCorners[1].y, 0, chunkSize);
                areaHitCorners[1].z = Mathf.Clamp(areaHitCorners[1].z, 0, chunkSize);

                float areaWidth = Mathf.Abs(areaHitCorners[0].x - areaHitCorners[1].x);
                float areaHeight = Mathf.Abs(areaHitCorners[0].y - areaHitCorners[1].y);
                float areaDepth = Mathf.Abs(areaHitCorners[0].z - areaHitCorners[1].z);

                int dispatchWidth = Mathf.CeilToInt(areaWidth / threadGroupSize.x) + 1;
                int dispatchHeight = Mathf.CeilToInt(areaHeight / threadGroupSize.y) + 1;
                int dispatchDepth = Mathf.CeilToInt(areaDepth / threadGroupSize.z) + 1;

                caveCarveShader.SetTexture(0, "noiseTex", chunk.noiseTex);
                caveCarveShader.SetVector("carvePos", carvePos);
                caveCarveShader.SetVector("startPos", areaHitCorners[0]);
                caveCarveShader.SetFloat("carveSize", _carveSize);
                caveCarveShader.SetFloat("carveSpeed", _carveSpeed);
            
                caveCarveShader.Dispatch(0, dispatchWidth, dispatchHeight, dispatchDepth);
                chunk.GenerateMesh();
            }
        }

        private void FillTerrain(Vector3 _pos, float _carveSize, float _carveSpeed)
        {
            Collider[] chunksHit = Physics.OverlapSphere(_pos, _carveSize);
            foreach (var chunkCollider in chunksHit)
            {
                Vector3 chunkIndex = GetChunkIndex(chunkCollider.transform.position);
                Chunk chunk = chunks[(int)chunkIndex.x, (int)chunkIndex.y, (int)chunkIndex.z];
                Vector3 carvePos = _pos - chunk.position;

                Vector3[] areaHitCorners = { _pos - new Vector3(_carveSize, _carveSize, _carveSize) - chunk.position, _pos + new Vector3(_carveSize, _carveSize, _carveSize) - chunk.position};
            
                areaHitCorners[0].x = Mathf.Clamp(areaHitCorners[0].x, 0, chunkSize);
                areaHitCorners[0].y = Mathf.Clamp(areaHitCorners[0].y, 0, chunkSize);
                areaHitCorners[0].z = Mathf.Clamp(areaHitCorners[0].z, 0, chunkSize);
            
                areaHitCorners[1].x = Mathf.Clamp(areaHitCorners[1].x, 0, chunkSize);
                areaHitCorners[1].y = Mathf.Clamp(areaHitCorners[1].y, 0, chunkSize);
                areaHitCorners[1].z = Mathf.Clamp(areaHitCorners[1].z, 0, chunkSize);

                float areaWidth = Mathf.Abs(areaHitCorners[0].x - areaHitCorners[1].x);
                float areaHeight = Mathf.Abs(areaHitCorners[0].y - areaHitCorners[1].y);
                float areaDepth = Mathf.Abs(areaHitCorners[0].z - areaHitCorners[1].z);

                int dispatchWidth = Mathf.CeilToInt(areaWidth / threadGroupSize.x) + 1;
                int dispatchHeight = Mathf.CeilToInt(areaHeight / threadGroupSize.y) + 1;
                int dispatchDepth = Mathf.CeilToInt(areaDepth / threadGroupSize.z) + 1;

                caveCarveShader.SetTexture(1, "noiseTex", chunk.noiseTex);
                caveCarveShader.SetVector("carvePos", carvePos);
                caveCarveShader.SetVector("startPos", areaHitCorners[0]);
                caveCarveShader.SetFloat("carveSize", _carveSize);
                caveCarveShader.SetFloat("carveSpeed", _carveSpeed);
            
                caveCarveShader.Dispatch(1, dispatchWidth, dispatchHeight, dispatchDepth);
                chunk.GenerateMesh();
            }
        }
    
        private Vector3 GetChunkIndex(Vector3 _playerPos)
        {
            return _playerPos.Remap(caveBounds[0], caveBounds[1], Vector3.zero, 
                                    new Vector3(amountChunksHorizontal, amountChunksVertical, amountChunksHorizontal));
        }
    }

    public struct MyRay
    {
        public Vector3 origin;
        public Vector3 direction;

        public MyRay(Vector3 _origin, Vector3 _direction)
        {
            origin = _origin;
            direction = _direction;
        }
    }

    public struct ChangedChunk
    {
        public RenderTexture renderTexture;
        public Vector3 startPos;
        public Vector3Int dimensions;

        public ChangedChunk(RenderTexture _renderTexture, Vector3 _startPos, Vector3Int _dimensions)
        {
            renderTexture = _renderTexture;
            startPos = _startPos;
            dimensions = _dimensions;
        }
    }
}
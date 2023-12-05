using System;
using System.Collections;
using System.Collections.Generic;
using MeshGen;
using UnityEngine;

[RequireComponent(typeof(MeshGenManager))]
public class GPUPhysicsManager : MonoBehaviour
{
    public static GPUPhysicsManager instance;
    private MeshGenManager meshGenManager;
    private Chunk[,,] chunks => meshGenManager.chunks;
    private Vector3[] caveBounds => meshGenManager.caveBounds;
    private int AmountChunksX => meshGenManager.amountChunksX;
    private int AmountChunksY => meshGenManager.amountChunksY;
    private LayerMask caveMask => meshGenManager.caveMask;

    private void OnEnable()
    {
        meshGenManager = GetComponent<MeshGenManager>();
        instance = this;
    }

    public bool Sphere(Vector3 _spherePos, float _sphereRadius, out RayOutput _closestPoint)
    {
        _closestPoint = new RayOutput();
        
        Collider[] chunksHit = Physics.OverlapSphere(_spherePos, _sphereRadius, caveMask);
        int amountChunksHit = chunksHit.Length;

        if (amountChunksHit == 0)
        {
            return false;
        }

        float lowestDist = float.MaxValue;
        RayOutput closestRay =  new RayOutput();
        
        foreach (var chunkCollider in chunksHit)
        {
            Vector3 chunkIndex = meshGenManager.GetChunkIndex(chunkCollider.transform.position);
            Chunk chunk = chunks[(int)chunkIndex.x, (int)chunkIndex.y, (int)chunkIndex.z];

            if (GPUPhysics.SphereIntersectMesh(chunk.vertexBuffer, chunk.indexBuffer, chunk.position, _spherePos,
                    _sphereRadius, out RayOutput closestPoint))
            {
                //Switch to box average instead of closest
                float dist = Vector3.Distance(closestPoint.position, _spherePos);
                if (dist < lowestDist)
                {
                    lowestDist = dist;
                    closestRay.position = closestPoint.position;
                    closestRay.normal = closestPoint.normal;
                }
            }
        }

        if (closestRay.position == Vector3.zero)
        {
            return false;
        }

        _closestPoint = closestRay;
        
        return true;
    }

    public bool Raycast(Vector3 _rayOrigin, Vector3 _rayDirection, out RayOutput _rayOutput)
    {
        _rayOutput = new RayOutput();
        int index = 0;
        int amountChunksToCheck = 10;
        
        Vector3 localRayOrigin = _rayOrigin;
        while (true)
        {
            Vector3 chunkIndex = meshGenManager.GetChunkIndex(localRayOrigin);
            chunkIndex = new Vector3((int)chunkIndex.x, (int)chunkIndex.y, (int)chunkIndex.z);
            
            if (chunkIndex.x < 0 || chunkIndex.y < 0 || chunkIndex.z < 0 || 
                chunkIndex.x > AmountChunksX - 1 || chunkIndex.y > AmountChunksY - 1 || chunkIndex.z > AmountChunksX - 1)
            {
                break;
            }
            
            Chunk chunk = chunks[(int)chunkIndex.x, (int)chunkIndex.y, (int)chunkIndex.z];
            if (GPUPhysics.RayIntersectMesh(chunk.vertexBuffer, chunk.indexBuffer, chunk.position, _rayOrigin, _rayDirection, out var rayOutput))
            {
                _rayOutput = rayOutput;
                return true;
            }
            
            RaycastHit hit;
            if (Physics.Raycast(localRayOrigin, _rayDirection, out hit, Mathf.Infinity))
            {
                localRayOrigin = hit.point + _rayDirection.normalized / 10;
            }
            else
            {
                return false;
            }
            
            index++;
            if (index > amountChunksToCheck)
            {
                return false;
            }
        }

        return false;
    }
}

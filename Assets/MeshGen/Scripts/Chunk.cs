using System.Collections.Generic;
using MeshGen;
using UnityEngine;
using UnityEngine.Rendering;

public struct Chunk
{
    public Mesh caveMesh;
    public RenderTexture noiseTex;
    public Vector3 position;
    public MeshFilter meshFilter;
    public GraphicsBuffer vertexBuffer;
    public GraphicsBuffer indexBuffer;

    public Chunk(Vector3 _position, GameObject _gameObject)
    {
        position = _position;
        meshFilter = _gameObject.GetComponent<MeshFilter>();

        caveMesh = new Mesh();
        caveMesh.vertexBufferTarget |= GraphicsBuffer.Target.Structured;
        caveMesh.indexBufferTarget |= GraphicsBuffer.Target.Structured;
        caveMesh.AddVertexAttribute(new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, dimension: 3));
        caveMesh.indexFormat = IndexFormat.UInt32;

        meshFilter.mesh = caveMesh;

        int chunkSize = MeshGenManager.chunkSizeStatic + 2;
        noiseTex = new RenderTexture(chunkSize, chunkSize, 0, RenderTextureFormat.R8)
        {
            filterMode = FilterMode.Point,
            dimension = TextureDimension.Tex3D,
            volumeDepth = chunkSize,
            enableRandomWrite = true,
        };

        vertexBuffer = null;
        indexBuffer = null;
    }

    public void OnDestroy()
    {
        vertexBuffer?.Dispose();
        indexBuffer?.Dispose();
        noiseTex.Release();
    }

    public void GenerateMesh()
    {
        MeshGenManager.appendTrianglesBuffer.SetCounterValue(0);

        MeshGenManager.caveGenerationShader.SetBuffer(0, "appendTriangleBuffer", MeshGenManager.appendTrianglesBuffer);
        MeshGenManager.caveGenerationShader.SetBuffer(0, "amountTrianglesBuffer", MeshGenManager.amountVertsBuffer);
        MeshGenManager.caveGenerationShader.SetTexture(0, "noiseTex", noiseTex);
        MeshGenManager.caveGenerationShader.SetInt("chunkSize", MeshGenManager.chunkSizeStatic);
        MeshGenManager.caveGenerationShader.SetFloat("isoLevel", MeshGenManager.isoLevel);
        MeshGenManager.caveGenerationShader.Dispatch(0, MeshGenManager.threadGroupSize0, MeshGenManager.threadGroupSize0, MeshGenManager.threadGroupSize0);

        int[] amountTrianglesArray = new int[1];
        MeshGenManager.amountVertsBuffer.GetData(amountTrianglesArray);
        int amountTriangles = amountTrianglesArray[0];
        int amountVerts = amountTriangles * 3;

        caveMesh.SetVertices(new Vector3[amountVerts]);
        caveMesh.SetIndices(new int[amountVerts], MeshTopology.Triangles, 0);

        if (amountVerts == 0)
        {
            return;
        }

        vertexBuffer?.Dispose();
        indexBuffer?.Dispose();
        vertexBuffer = caveMesh.GetVertexBuffer(0);
        indexBuffer = caveMesh.GetIndexBuffer();

        int amountTriThreadGroupX = Mathf.CeilToInt((float)amountTriangles / MeshGenManager.threadGroupSizeOut1);
        MeshGenManager.caveGenerationShader.SetBuffer(1, "vertexBuffer", vertexBuffer);
        MeshGenManager.caveGenerationShader.SetBuffer(1, "triangleBuffer", MeshGenManager.appendTrianglesBuffer);
        MeshGenManager.caveGenerationShader.SetBuffer(1, "indexBuffer", indexBuffer);
        MeshGenManager.caveGenerationShader.Dispatch(1, amountTriThreadGroupX, 1, 1);

        int chunkSize = MeshGenManager.chunkSizeStatic;
        float boundsSize = chunkSize / 2f;
        caveMesh.bounds = new Bounds(new Vector3(boundsSize, boundsSize, boundsSize), new Vector3(chunkSize, chunkSize, chunkSize));
        MeshGenManager.amountVertsBuffer.SetData(new[] { 0 });
    }

    public void FillEdges(Chunk[,,] _chunks, Vector3Int _index)
    {
        NeighbourData[] neighbourData = new NeighbourData[(MeshGenManager.chunkSizeStatic + 2) * 8];

        int amountChunksX = _chunks.GetLength(0);
        int amountChunksY = _chunks.GetLength(1);
        int amountChunksZ = _chunks.GetLength(2);

        int lastIndex = MeshGenManager.chunkSizeStatic + 1;

        Vector3Int offset = new Vector3Int(-1, -1, -1);
        if (IsChunkIndexWithinRange(_index + offset, amountChunksX, amountChunksY, amountChunksZ))
        {
            neighbourData[0] = new NeighbourData(new Vector3Int(0, 0, 0), new Vector3Int(lastIndex, lastIndex, lastIndex), 0);
        }
        else
        {
            neighbourData[0] = new NeighbourData(new Vector3Int(0, 0, 0), new Vector3Int(0, 0, 0), -1);
        }
        
        offset = new Vector3Int(-1, -1, 0);
        if (IsChunkIndexWithinRange(_index + offset, amountChunksX, amountChunksY, amountChunksZ))
        {
            for (int i = 1; i < lastIndex; i++)
            {
                neighbourData[i] = new NeighbourData(new Vector3Int(i, 0, 0), new Vector3Int(i, lastIndex, lastIndex), 1);
            }
        }
        else
        {
            for (int i = 1; i < lastIndex; i++)
            {
                neighbourData[i] = new NeighbourData(new Vector3Int(0, 0, 0), new Vector3Int(0, 0, 0), -1);
            }
        }
        
        offset = new Vector3Int(-1, -1, 1);
        if (IsChunkIndexWithinRange(_index + offset, amountChunksX, amountChunksY, amountChunksZ))
        {
            neighbourData[lastIndex] = new NeighbourData(new Vector3Int(lastIndex, lastIndex, lastIndex), new Vector3Int(0, 0, 0), 2);
        }
        else
        {
            neighbourData[lastIndex] = new NeighbourData(new Vector3Int(0, 0, 0), new Vector3Int(0, 0, 0), -1);
        }

        offset = new Vector3Int(0, -1, -1);
        if (IsChunkIndexWithinRange(_index + offset, amountChunksX, amountChunksY, amountChunksZ))
        {
            for (int i = 1; i < lastIndex; i++)
            {
                neighbourData[i] = new NeighbourData(new Vector3Int(i, 0, 0), new Vector3Int(i, lastIndex, lastIndex), 1);
            }
        }
        else
        {
            for (int i = 1; i < lastIndex; i++)
            {
                neighbourData[i] = new NeighbourData(new Vector3Int(0, 0, 0), new Vector3Int(0, 0, 0), -1);
            }
        }
    }
    
    private bool IsChunkIndexWithinRange(Vector3Int _index, int _amountChunksX, int _amountChunksY, int _amountChunksZ)
    {
        return !(_index.x < 0 || _index.x > _amountChunksX - 1 || _index.y < 0 || _index.y > _amountChunksY - 1 || _index.z < 0 || _index.z > _amountChunksZ - 1);
    }

    struct NeighbourData
    {
        public Vector3Int indexBase;
        public Vector3Int indexNeighbour;
        public int rtToUse;
        public NeighbourData(Vector3Int _indexBase, Vector3Int _indexNeighbour, int _rtToUse)
        {
            indexBase = _indexBase;
            indexNeighbour = _indexNeighbour;
            rtToUse = _rtToUse;
        }
    }
}

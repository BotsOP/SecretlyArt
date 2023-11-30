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
    private ComputeBuffer amountVertsBuffer;
    private GraphicsBuffer appendTrianglesBuffer;

    public Chunk(Vector3 _position, GameObject _gameObject)
    {
        position = _position;
        meshFilter = _gameObject.GetComponent<MeshFilter>();
        
        caveMesh = new Mesh();
        caveMesh.vertexBufferTarget |= GraphicsBuffer.Target.Structured;
        caveMesh.indexBufferTarget |= GraphicsBuffer.Target.Structured;
        caveMesh.AddVertexAttribute(new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, dimension: 3));
        caveMesh.indexFormat = IndexFormat.UInt32;

        int chunkSize = MeshGenManager.chunkSizeStatic;
        
        meshFilter.mesh = caveMesh;

        noiseTex = new RenderTexture(chunkSize, chunkSize, 0, RenderTextureFormat.R8)
        {
            filterMode = FilterMode.Point,
            dimension = TextureDimension.Tex3D,
            volumeDepth = chunkSize,
            enableRandomWrite = true,
        };
        
        amountVertsBuffer = new ComputeBuffer(1, sizeof(uint));
        amountVertsBuffer.SetData(new [] { 0 });
        int amountMaxVerts = (int)Mathf.Pow(chunkSize, 3) * 3;
        appendTrianglesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Append, amountMaxVerts, sizeof(float) * (3 + 3));
        
        vertexBuffer = null;
        indexBuffer = null;
    }

    public void OnDestroy()
    {
        appendTrianglesBuffer?.Dispose();
        vertexBuffer?.Dispose();
        indexBuffer?.Dispose();
        amountVertsBuffer?.Dispose();
        noiseTex.Release();
    }

    public void GenerateMesh()
    {
        appendTrianglesBuffer.SetCounterValue(0);

        MeshGenManager.caveGenerationShader.SetBuffer(0, "appendTriangleBuffer", appendTrianglesBuffer);
        MeshGenManager.caveGenerationShader.SetBuffer(0, "amountTrianglesBuffer", amountVertsBuffer);
        MeshGenManager.caveGenerationShader.SetTexture(0, "noiseTex", noiseTex);
        MeshGenManager.caveGenerationShader.SetInt("chunkSize", MeshGenManager.chunkSizeStatic);
        MeshGenManager.caveGenerationShader.SetFloat("isoLevel", MeshGenManager.isoLevel);
        MeshGenManager.caveGenerationShader.Dispatch(0, MeshGenManager.threadGroupSize0, MeshGenManager.threadGroupSize0, MeshGenManager.threadGroupSize0);
        
        int[] amountTrianglesArray = new int[1];
        amountVertsBuffer.GetData(amountTrianglesArray);
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
        MeshGenManager.caveGenerationShader.SetBuffer(1, "triangleBuffer", appendTrianglesBuffer);
        MeshGenManager.caveGenerationShader.SetBuffer(1, "indexBuffer", indexBuffer);
        MeshGenManager.caveGenerationShader.Dispatch(1, amountTriThreadGroupX, 1, 1);

        int chunkSize = MeshGenManager.chunkSizeStatic;
        float boundsSize = chunkSize / 2f;
        caveMesh.bounds = new Bounds(new Vector3(boundsSize, boundsSize, boundsSize), new Vector3(chunkSize, chunkSize, chunkSize));
        amountVertsBuffer.SetData(new [] { 0 });
    }
}

using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Jobs;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;


public class WaveGenerator : MonoBehaviour
{
    public MeshFilter waterMeshFilter;
    public float waveScale;
    public float waveOffsetSpeed;
    public float waveHeight;

    private NativeArray<Vector3> waterVertices;
    private NativeArray<Vector3> waterNormals;

    private Mesh _waterMesh;
    private void Start()
    {
        _waterMesh = waterMeshFilter.mesh;
        _waterMesh.MarkDynamic();
        
        waterVertices = new NativeArray<Vector3>(_waterMesh.vertices,Allocator.Persistent);
        waterNormals = new NativeArray<Vector3>(_waterMesh.normals,Allocator.Persistent);
    }

    private void OnDestroy()
    {
        waterVertices.Dispose();
        waterNormals.Dispose();
    }
    //[BurstCompile]
    private struct UpdateMeshJob:IJobParallelFor
    {
        public NativeArray<Vector3> vertices;
        [ReadOnly]
        public NativeArray<Vector3> normals;

        public float offsetSpeed;
        public float scale;
        public float height;
        public float time;

        float Noise(float x, float y)
        {
            float2 pos = math.float2(x,y);
            return noise.snoise(pos);
        }
        public void Execute(int index)
        {
            if (normals[index].z > 0f)
            {
                var vertex = vertices[index];
                float noiseValue = Noise(vertex.x * scale + offsetSpeed * time, vertex.y * scale + offsetSpeed * time);
                vertices[index] = new Vector3(vertex.x,vertex.y,noiseValue * height + 0.3f);
            }
        }
    }
    
    private JobHandle meshModificationJobHandle;
    private UpdateMeshJob meshModificationJob;
    void Update()
    {
        meshModificationJob = new UpdateMeshJob()
        {
            vertices = waterVertices,
            normals = waterNormals,
            offsetSpeed = waveOffsetSpeed,
            time = Time.time,
            scale = waveScale,
            height = waveHeight
        };

        meshModificationJobHandle = meshModificationJob.Schedule(waterVertices.Length, 64);
    }

    private void LateUpdate()
    {
        meshModificationJobHandle.Complete();
        _waterMesh.SetVertices(meshModificationJob.vertices.ToList());
        _waterMesh.RecalculateNormals();
    }
}

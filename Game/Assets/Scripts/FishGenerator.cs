using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;
using random = Unity.Mathematics.Random;
using math = Unity.Mathematics.math;
using Random = UnityEngine.Random;

public class FishGenerator : MonoBehaviour
{
    public int amountOfFish;
    public Vector3 spawnBounds;
    public float spawnHeight;
    public GameObject objectPrefab;
    public float swimSpeed;
    public float turnSpeed;
    public Transform waterObject;
    public int swimChangeFrenquency;
    
    private NativeArray<Vector3> _velocities;
    private TransformAccessArray _transformAccessArray;

    void Start()
    {
        _velocities = new NativeArray<Vector3>(amountOfFish,Allocator.Persistent);
        
        _transformAccessArray = new TransformAccessArray(amountOfFish);

        for (int i = 0; i < amountOfFish; i++)
        {
            float distanceX = Random.Range(-spawnBounds.x / 2, spawnBounds.x / 2);
            float distanceZ = Random.Range(-spawnBounds.z / 2, spawnBounds.z / 2);

            Vector3 spawnPoint = (transform.position + Vector3.up * spawnHeight) + new Vector3(distanceX, 0, distanceZ);
            Transform t = Instantiate(objectPrefab,spawnPoint,quaternion.identity).transform;
            
            _transformAccessArray.Add(t);
        }
    }

    private void OnDestroy()
    {
        _transformAccessArray.Dispose();
        _velocities.Dispose();
    }

    private PositionUpdateJob _positionUpdateJob;
    private JobHandle _positionUpdateJobHandle;

    private void Update()
    {
        _positionUpdateJob = new PositionUpdateJob()
        {
            objectVelocities = _velocities,
            bounds =  spawnBounds,
            center = waterObject.position,
            jobDeltaTime = Time.deltaTime,
            time = Time.time,
            swimSpeed = swimSpeed,
            turnSpeed = turnSpeed,
            swimChangeFrenquency = swimChangeFrenquency
        };

        _positionUpdateJobHandle = _positionUpdateJob.Schedule(_transformAccessArray);
    }

    private void LateUpdate()
    {
        _positionUpdateJobHandle.Complete();
    }
    //[BurstCompile]
    struct PositionUpdateJob:IJobParallelForTransform
    {
        public NativeArray<Vector3> objectVelocities;
        public Vector3 bounds;
        public Vector3 center;
        public float jobDeltaTime;
        public float time;
        public float swimSpeed;
        public float turnSpeed;
        public int swimChangeFrenquency;
        public float seed;
        public void Execute(int index, TransformAccess t)
        {
            Vector3 currentVelocity = objectVelocities[index];
            
            random  randomGen = new random((uint)(index*time + 1 + seed));

            t.position += t.rotation * Vector3.forward * swimSpeed * jobDeltaTime * randomGen.NextFloat(0.3f, 1.0f);

            if (currentVelocity != Vector3.zero)
            {
                t.rotation = Quaternion.Lerp(t.rotation, Quaternion.LookRotation(currentVelocity),
                    turnSpeed * jobDeltaTime);
            }

            Vector3 currentPosition = t.position;
            bool randomise = true;
            if (currentPosition.x > center.x + bounds.x / 2 ||
                currentPosition.x < center.x - bounds.x / 2 ||
                currentPosition.z > center.z + bounds.z / 2 ||
                currentPosition.z < center.z - bounds.z / 2)
            {
                Vector3 internalPostion = new Vector3(center.x + randomGen.NextFloat(-bounds.x/2,bounds.x/2)/1.3f,0,
                    center.z + randomGen.NextFloat(-bounds.z/2,bounds.z/2)/1.3f);

                currentVelocity = (internalPostion - currentPosition).normalized;

                objectVelocities[index] = currentVelocity;

                t.rotation = Quaternion.Lerp(t.rotation,
                    Quaternion.LookRotation(currentVelocity) , turnSpeed * jobDeltaTime * 2);
                randomise = false;
            }

            if (randomise)
            {
                if (randomGen.NextInt(0, swimChangeFrenquency) <= 2)
                {
                    objectVelocities[index] = new Vector3(randomGen.NextFloat(-1f,1f),0f, randomGen.NextFloat(-1f,1f));
                }
            }
        }
    }
}

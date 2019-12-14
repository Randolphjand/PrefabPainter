﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Yapp
{
    public class PhysicsSimulation : ScriptableObject
    {
        public enum ForceApplyType
        {
            /// <summary>
            /// Apply the force at the start of a simulation.
            /// Like an explosion when you have a lot of prefabs at the same location.
            /// </summary>
            Initial,

            /// <summary>
            /// Apply the force continuously during the simulation.
            /// Like wind blowing the prefabs away
            /// </summary>
            Continuous
        }

        #region Public Editor Fields

        public int maxIterations = 1000;
        public Vector2 forceMinMax = Vector2.zero;
        public float forceAngleInDegrees = 0f;
        public bool randomizeForceAngle = false;

        public ForceApplyType forceApplyType = ForceApplyType.Initial;
        #endregion Public Editor Fields

        private SimulatedBody[] simulatedBodies;

        private List<Rigidbody> generatedRigidbodies;
        private List<Collider> generatedColliders;

        #region Simulate Once
        public void RunSimulationOnce(Transform[] gameObjects)
        {
            AutoGenerateComponents(gameObjects);

            simulatedBodies = gameObjects.Select(rb => new SimulatedBody(rb, forceAngleInDegrees, randomizeForceAngle)).ToArray();

            SimulateOnce(simulatedBodies);

            RemoveAutoGeneratedComponents();

        }

        private void SimulateOnce(SimulatedBody[] simulatedBodies)
        {
            // apply force if necessary
            if (forceApplyType == ForceApplyType.Initial)
            {
                ApplyForce();
            }

            // Run simulation for maxIteration frames, or until all child rigidbodies are sleeping
            Physics.autoSimulation = false;
            for (int i = 0; i < maxIterations; i++)
            {
                // apply force if necessary
                if (forceApplyType == ForceApplyType.Continuous)
                {
                    ApplyForce();
                }

                Physics.Simulate(Time.fixedDeltaTime);

                if (simulatedBodies.All(body => body.rigidbody.IsSleeping()))
                {
                    break;
                }
            }
            Physics.autoSimulation = true;
        }
        #endregion Simulate Once

        private void ApplyForce()
        {
            // Add force to bodies
            foreach (SimulatedBody body in simulatedBodies)
            {
                float randomForceAmount = Random.Range(forceMinMax.x, forceMinMax.y);
                float forceAngle = body.forceAngle;
                Vector3 forceDir = new Vector3(Mathf.Sin(forceAngle), 0, Mathf.Cos(forceAngle));
                body.rigidbody.AddForce(forceDir * randomForceAmount, ForceMode.Impulse);
            }
        }

        #region Simulate Continuously
        public bool simulationRunning = false;
        private bool simulationStopTriggered = false;
        public int simulationStepCount = 0;

        public void StartSimulation(Transform[] gameObjects)
        {
            if (simulationRunning)
            {
                Debug.Log("Simulation already running");
                return;
            }
            
            Debug.Log("Simulation started");

            AutoGenerateComponents(gameObjects);

            simulatedBodies = gameObjects.Select(rb => new SimulatedBody(rb, forceAngleInDegrees, randomizeForceAngle)).ToArray();

            simulationRunning = true;
            simulationStepCount = 0;
            simulationStopTriggered = false;

            // Run simulation for maxIteration frames, or until all child rigidbodies are sleeping
            Physics.autoSimulation = false;

            // apply force if necessary
            if (forceApplyType == ForceApplyType.Initial)
            {
                ApplyForce();
            }

            EditorCoroutines.Execute(SimulateContinuously());

        }

        public void StopSimulation()
        {
            simulationStopTriggered = true;

            Debug.Log("Simulation stopp triggered");

        }

        private void PerformSimulateStep(SimulatedBody[] simulatedBodies)
        {

            // apply force if necessary
            if (forceApplyType == ForceApplyType.Continuous)
            {
                ApplyForce();
            }

            Physics.Simulate(Time.fixedDeltaTime);

        }

        IEnumerator SimulateContinuously()
        {

            while (!simulationStopTriggered)
            {

                simulationStepCount++;

                PerformSimulateStep(simulatedBodies);

                yield return 0;
            }

            Physics.autoSimulation = true;

            RemoveAutoGeneratedComponents();

            simulationRunning = false;

            Debug.Log("Simulation stopped");


        }
        #endregion Simulate Continuously

        // Automatically add rigidbody and box collider to object if it doesn't already have
        void AutoGenerateComponents(Transform[] gameObjects)
        {
            generatedRigidbodies = new List<Rigidbody>();
            generatedColliders = new List<Collider>();

            foreach (Transform child in gameObjects)
            {
                if (!child.GetComponent<Rigidbody>())
                {

                    Rigidbody rb = child.gameObject.AddComponent<Rigidbody>();

                    rb.useGravity = true;
                    rb.mass = 1;

                    generatedRigidbodies.Add(rb);

                }
                if (!child.GetComponent<Collider>())
                {
                    MeshCollider collider = child.gameObject.AddComponent<MeshCollider>();

                    collider.convex = true;

                    generatedColliders.Add(collider);
                }
            }
        }

        // Remove the components which were generated at start of simulation
        void RemoveAutoGeneratedComponents()
        {
            foreach (Rigidbody rb in generatedRigidbodies)
            {
                DestroyImmediate(rb);
            }
            foreach (Collider c in generatedColliders)
            {
                DestroyImmediate(c);
            }
        }

        public void UndoSimulation()
        {
            if (simulatedBodies != null)
            {
                foreach (SimulatedBody body in simulatedBodies)
                {
                    body.Undo();
                }
            }
        }

        struct SimulatedBody
        {
            public readonly Rigidbody rigidbody;

            readonly Geometry geometry;
            readonly Transform transform;

            public readonly float forceAngle;

            public SimulatedBody(Transform transform, float forceAngleInDegrees, bool randomizeForceAngle)
            {
                this.transform = transform;
                this.rigidbody = transform.GetComponent<Rigidbody>();

                this.geometry = new Geometry(transform);

                this.forceAngle = ((randomizeForceAngle) ? Random.Range(0, 360f) : forceAngleInDegrees) * Mathf.Deg2Rad;
            }

            public void Undo()
            {
                // check if the transform was removed manually
                if (transform == null)
                    return;

                transform.position = geometry.getPosition();
                transform.rotation = geometry.getRotation();

                if (rigidbody != null)
                {
                    rigidbody.velocity = Vector3.zero;
                    rigidbody.angularVelocity = Vector3.zero;
                }
            }
        }
    }
}
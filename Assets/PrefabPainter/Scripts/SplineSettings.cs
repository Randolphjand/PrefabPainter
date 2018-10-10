﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PrefabPainter
{
    [System.Serializable]
    public class SplineSettings
    {

        public enum AttachMode
        {
            Bounds,
            Between
        }

        public enum Rotation
        {
            Prefab,
            Spline,
            Identity
        }



        [Range (0,10)]
        public int curveResolution = 0;
        public bool loop = false;
        public float distanceBetweenObjects = 1f;
        public Rotation instanceRotation = Rotation.Prefab;

        public AttachMode attachMode = AttachMode.Bounds;
        public bool controlPointRotation = false;

        // internal properties
        public bool dirty = false;
        public List<GameObject> prefabInstances = new List<GameObject>();
        public List<ControlPoint> controlPoints = new List<ControlPoint>();
    }
}
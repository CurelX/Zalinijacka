using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionMap : MonoBehaviour {

    public static List<Vector3> collisionPositions = new List<Vector3>();

    private void Start() {
        collisionPositions.Clear();
    }
}

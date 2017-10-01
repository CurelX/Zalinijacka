using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Player_Movement : MonoBehaviour {

    public Vector2 startVector;
    public Color color;
    public Material mat;

    private Rigidbody2D rb;
    private EdgeCollider2D eCol;
    private Vector3 currentVector;
    private Vector3 lastPosition;
    private Quaternion rotation;
    private LineRenderer lr;
    private int vertexCount;

    private void Start() {
        rb = GetComponent<Rigidbody2D>();
        lr = GetComponent<LineRenderer>();
        eCol = GetComponent<EdgeCollider2D>();
        lr.startColor = color;
        lr.endColor = color;
        lr.startWidth = 0.05f;
        lr.endWidth = 0.05f;
        lr.receiveShadows = false;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        if (!name.Contains("Instance")) {
            startVector = new Vector2(Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f));
            startVector.Normalize();
            currentVector = startVector;
            InstantiateNew();
            Destroy(this.gameObject);
            return;
        }

        currentVector = new Vector3(startVector.x, startVector.y, 0);
        vertexCount = 0;

        rb.velocity = startVector;
        rb.isKinematic = true;
        RoutineManager.Core.startRoutine(Go());
    }
    IEnumerable Go() {

        while (true) {

            if (Input.GetKey(KeyCode.LeftArrow)) {
                currentVector = Quaternion.Euler(0, 0, 2) * currentVector;
            } else if (Input.GetKey(KeyCode.RightArrow)) {
                currentVector = Quaternion.Euler(0, 0, -2) * currentVector;
            }

            CheckInput();

            rb.velocity = currentVector;
            vertexCount++;
            lr.positionCount = vertexCount;
            lr.SetPosition(vertexCount - 1, transform.position);

            if (transform.position != lastPosition) {
                if (CheckCollision()) {
                    print("DEAD");
                    break;
                }
            }

            if (!CollisionMap.collisionPositions.Contains(transform.position))
                CollisionMap.collisionPositions.Add(transform.position);

            if (vertexCount % 500 == 0) {
                InstantiateNew();
                rb.velocity = Vector2.zero;
                break;
            }

            lastPosition = transform.position;
            yield return null;
        }
    }
    private bool CheckCollision() {
        for (int i = 0; i < CollisionMap.collisionPositions.Count; i++) {
            if (V3Equals(transform.position, CollisionMap.collisionPositions[i]))
                return true;
        }

        if (transform.position.x >= Bounds_Draw.xPosBound)
            return true;
        if (transform.position.x <= Bounds_Draw.xNegBound)
            return true;
        if (transform.position.y >= Bounds_Draw.yPosBound)
            return true;
        if (transform.position.y <= Bounds_Draw.yNegBound)
            return true;

        return false;
    }
    private bool V3Equals(Vector3 a, Vector3 b) {
        return Vector3.SqrMagnitude(a - b) < 0.0001f;
    }
    private void InstantiateNew() {
        GameObject g = new GameObject();
        g.name = "PlayerInstance";
        g.tag = "Player";
        g.AddComponent<LineRenderer>();
        g.AddComponent<Rigidbody2D>();
        g.AddComponent<Player_Movement>();
        g.AddComponent<EdgeCollider2D>();

        g.GetComponent<LineRenderer>().material = mat;
        g.GetComponent<EdgeCollider2D>().isTrigger = true;
        g.GetComponent<Player_Movement>().color = this.color;
        g.GetComponent<Player_Movement>().startVector = this.currentVector;
        g.GetComponent<Player_Movement>().mat = this.mat;
        g.transform.position = transform.position + (currentVector / 4);
    }
    private void CheckInput() {
        if(Input.GetMouseButton(0)) {
            if(Input.mousePosition.x >= Screen.width / 2)
                currentVector = Quaternion.Euler(0, 0, -2) * currentVector;
            else if (Input.mousePosition.x < Screen.width / 2)
                currentVector = Quaternion.Euler(0, 0, 2) * currentVector;
        }
    }
  
}

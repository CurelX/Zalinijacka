using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bounds_Draw : MonoBehaviour {

    private LineRenderer lr;
    public static float screenW;
    public static float screenH;

    private float offset;

    public static float yPosBound;
    public static float yNegBound;
    public static float xPosBound;
    public static float xNegBound;

    private void Start() {
        lr = GetComponent<LineRenderer>();
        screenH = Camera.main.orthographicSize * 2.0f;
        screenW = screenH * Screen.width / Screen.height;

        offset = screenW / 60;

        yPosBound = screenH / 2 - offset;
        yNegBound = -screenH / 2 + offset;
        xPosBound = screenW / 2 - offset;
        xNegBound = -screenW / 2 + offset;

        lr.positionCount = 5;
        lr.SetPosition(0, new Vector3(xNegBound, yPosBound, 0));
        lr.SetPosition(1, new Vector3(xNegBound, yNegBound, 0));
        lr.SetPosition(2, new Vector3(xPosBound, yNegBound, 0));
        lr.SetPosition(3, new Vector3(xPosBound, yPosBound, 0));
        lr.SetPosition(4, new Vector3(xNegBound, yPosBound, 0));
    }
}

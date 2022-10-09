using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CylPanoControlHeight : MonoBehaviour
{
    public float CylPanoAngularHeight = 50.671f; // to be set depending on the cylindrical panorama picture used
    public float CylPanoRadius = 48.0f; // set to this just because the Hololens 2 back clipping place is stuck at 50m. Idealy, should by a big number
    public float CylPanoOffset = 0;
    [ReadOnlyInspector]
    public float CylPanoNorth = 0;

    int counter = 0;

    GameObject lCylPanoControlHeight;

    // Start is called before the first frame update
    void Start()
    {
        lCylPanoControlHeight = GameObject.Find("CylinderForPanorama");
    }

    // Update is called once per frame
    void Update()
    {
        counter++;
        if (counter >= 64) // no need to compute it more often than every second
        {
            float scalez = 2 * CylPanoRadius * Mathf.Tan(0.5f * Mathf.Deg2Rad * CylPanoAngularHeight);
            lCylPanoControlHeight.transform.localScale = new Vector3(-CylPanoRadius, CylPanoRadius, scalez);
            lCylPanoControlHeight.transform.localEulerAngles = new Vector3(-90, CylPanoOffset + CylPanoNorth, 0);
            counter = 0;
        }
    }

    public void cylPanoSetNorth(float offset)
    {
        CylPanoNorth = offset;
    }
}

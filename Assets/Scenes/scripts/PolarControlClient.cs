//#define BRIGHTNESS_CALIB

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using TMPro;
using ComStructures;
using TrajectoryStructures; // only for limitAngles now


public class PolarControlClient : PolarControlBase
{
    LookAtConstraint lookat; // in order to be able to activate it

    TextMeshProUGUI textmeshPro;

    GameObject lPolarPanYawPitch;
    GameObject lDistanceZ;
    GameObject lPanP2D;
    GameObject lPanC2D;
    GameObject lPanP3D;
    GameObject lPanC3D;
    GameObject lcamera;
//    Skybox lSkybox;
//    Material lSkyboxMaterial;
    GameObject lSkyboxCamera;
//    GameObject lQuadBaseP;
    GameObject lQuadBaseC;
    GameObject lCartesian;
    GameObject lNorth;
    GameObject lNorthDirectionT;
    GameObject lSights;
    CylPanoControlHeight lCylPanoControlHeight;

#if BRIGHTNESS_CALIB
    GameObject lTriplet;
    SpriteRenderer lOnePix;
    SpriteRenderer lTwoPix;
    SpriteRenderer lMoon;
#endif

    // Start is called before the first frame update
    void Start()
    {
        // trajectory player
        t.kp_list = new List<keypoint>();


        lkp.set = true;

        ref_allcfg = allcfg;
        ref_kp = lkp;

        lookat = GameObject.Find("QuadbaseC").GetComponent<LookAtConstraint>();

        textmeshPro = GameObject.Find("HoloHUDText").GetComponent<TextMeshProUGUI>();

        lPolarPanYawPitch = GameObject.Find("PolarPanYawPitch");
        lDistanceZ = GameObject.Find("DistanceZ");
        lCartesian = GameObject.Find("Cartesian");
        lPanP2D = GameObject.Find("PanP2D");
        lPanC2D = GameObject.Find("PanC2D");
        lPanP3D = GameObject.Find("PanP3D");
        lPanC3D = GameObject.Find("PanC3D");
        lcamera = GameObject.Find("Main Camera");
//        lQuadBaseP = GameObject.Find("QuadbaseP");
        lQuadBaseC = GameObject.Find("QuadbaseC");
        lNorth = GameObject.Find("North");
        lNorthDirectionT = GameObject.Find("NorthDirectionT");
        lSights = GameObject.Find("Sights");
        lCylPanoControlHeight = GameObject.Find("CylPanoControlHeight").GetComponent<CylPanoControlHeight>();

#if BRIGHTNESS_CALIB
        lTriplet = GameObject.Find("Triplet"); // object parented to the cam to show a moon for brightness calibration
        lOnePix = GameObject.Find("OnePix").GetComponent<SpriteRenderer>();
        lTwoPix = GameObject.Find("TwoPix").GetComponent<SpriteRenderer>();
        lMoon = GameObject.Find("Moon").GetComponent<SpriteRenderer>();
#endif

        //        lSkyboxCamera = GameObject.Find("SkyboxCamera");
        //        lSkybox = lcamera.GetComponent<Skybox>();
        //        lSkyboxMaterial = lSkybox.material;

    }

    // Update is called once per frame
    void Update()
    {
        lkp.pinfo.distanceP = (float)(((int)(lkp.pinfo.distanceP * 100)) / 100.0f); // avoid rounding errors using the interface
        lkp.cinfo.distanceC = (float)(((int)(lkp.cinfo.distanceC * 100)) / 100.0f); // avoid rounding errors using the interface

        processRx("Client");

        switch ((commands) allcfg.commonConf.command)
        {
            case commands.CM_play: playing = true; allcfg.commonConf.command = 0;  break;
            case commands.CM_pause: playing = false; allcfg.commonConf.command = 0;  break;
            case commands.CM_rewind: // rewind
                {
                    lkp.cminfo.time = 0;
                    if (t.kp_list.Count>0)
                        lkp = t.kp_list[0];
                    allcfg.commonConf.command = 0;
                }
                break;
        }

        if (playing)  // this is for play under remote control. But time may also be updated by the server directly.
        {
            if ((allcfg.commonConf.loop) && (t.kp_list.Count > 0) && (lkp.cminfo.time >= t.kp_list[t.kp_list.Count - 1].cminfo.time))// loop mode in remote control
            { // we are already at the end of the traj we should loop back at the start
                lkp.cminfo.time = t.kp_list[0].cminfo.time;
            }
            else
                lkp.cminfo.time += Time.deltaTime;  // doing it before the interpolate ensures lkp.cminfo.time will remain in the times of the traj
            lkp = t.interpolate(lkp.cminfo.time); // may change the time to go back into the traj. 
        }



        if (lkp.cminfo.alpha >= 1)
            lkp.cminfo.alpha = 1;
        if (lkp.cminfo.alpha <0)
            lkp.cminfo.alpha = 0;


        /*******************************************************************/
        // Witness Direction info

        /*
         * 
         * 
        B child of A
        compute localtransform of C, child of D so that it has the same world transform as B

        localRot = Quaternion.Inverse(theTargetParent.transform.rotation) * theTargetChild.transform.rotation);

        public static Vector3 GetTranslation(this Matrix4x4 m)
        {
            var col = m.GetColumn(3);
            return new Vector3(col.x, col.y, col.z);
        }
        
        Transform.InverseTransformPoint

        Vector3 cameraRelative = cam.InverseTransformPoint(transform.position);

        localTranslation = theTargetParent.InverseTransformPoint(transform.position);


        See https://forum.unity.com/threads/how-to-calculate-the-new-transform-values-relative-to-parent.447609/
        B child of A (nota we don't really care A, we only use the world transform of B)
        compute localtransform of C, child of D so that it has the same world transform as B

        localRotC = Quaternion.Inverse(D.transform.rotation) * B.transform.rotation);
        localTransC = D.InverseTransformPoint(B.transform.position);
         */

        Vector3 camheading = lcamera.transform.localEulerAngles;
        camheading = TrajectoryStructures.trajectory.limitAngles(camheading);

        wdinfo.az = camheading[1];
        wdinfo.alt = -camheading[0];


        /*******************************************************************/
        // Common Control

        if (allcfg.commonConf.showSight)
        {
            lSights.SetActive(true);
            lNorthDirectionT.SetActive(true);
        }
        else
        {
            lSights.SetActive(false);
            lNorthDirectionT.SetActive(false);
        }


        if (allcfg.commonConf.command == (int) commands.CM_setNorthByLook) // process explicit request by server to use the current viewing direction as the north direction
        {
            allcfg.commonConf.north = wdinfo.az;
            allcfg.commonConf.command = 0;
 //           lSkyboxCamera.transform.localEulerAngles = new Vector3(0, -wdinfo.az, 0);

            RenderSettings.skybox.SetFloat("_Rotation", 180 + wdinfo.az); // marche pas
            //            lSkyboxMaterial.SetFloat("_Rotation", 180 + wdinfo.az); // marche pas

            lCylPanoControlHeight.cylPanoSetNorth(wdinfo.az); // also rotate the panorama accordingly
        }


        lNorth.transform.localEulerAngles = new Vector3(0, allcfg.commonConf.north, 0);

        /*******************************************************************/
        // Polar Control

        if (lkp.pinfo.sizeP <= 0)
            lkp.pinfo.sizeP = 1;

        if (allcfg.commonConf.ControlAngularSize)
        {
            if (lkp.pinfo.distanceP != ref_kp.pinfo.distanceP) // detects a changed distance, compute the new scale
                lkp.pinfo.sizeP = 2 * Mathf.Tan(Mathf.Deg2Rad * lkp.pinfo.AngularSizeP / 2) * lkp.pinfo.distanceP;
            else // changed the scale or the angular size, compute the distance
            {
                lkp.pinfo.distanceP = (lkp.pinfo.sizeP / 2) / Mathf.Tan(Mathf.Deg2Rad * lkp.pinfo.AngularSizeP / 2);
                lkp.pinfo.distanceP = (float)(((int)(lkp.pinfo.distanceP * 100))/100.0f); // avoid rounding errors using the interface, which creates a loop blocking distance to certain values.
            }
        }
        else // changed size or distance, compute angular size
        {
            lkp.pinfo.AngularSizeP = 2* Mathf.Rad2Deg* Mathf.Atan((lkp.pinfo.sizeP / 2) / lkp.pinfo.distanceP);
        }

        //        lastdistanceP = lkp.pinfo.distanceP; // now managed auto with ref_kp

        // update CG

        lDistanceZ.transform.localPosition = new Vector3(0, 0, lkp.pinfo.distanceP);
        lPolarPanYawPitch.transform.localRotation = Quaternion.Euler(-lkp.pinfo.alt, lkp.pinfo.az, 0);

        lPanP2D.transform.localEulerAngles = new Vector3(0, 180, lkp.cminfo.lroll);
        lPanP2D.transform.localScale = new Vector3(lkp.pinfo.sizeP, lkp.pinfo.sizeP, lkp.pinfo.sizeP);
        lPanP2D.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, lkp.cminfo.alpha);

        lPanP3D.transform.localEulerAngles = new Vector3(-lkp.cminfo.lpitch, -lkp.cminfo.lyaw, -lkp.cminfo.lroll);
        lPanP3D.transform.localScale = new Vector3(lkp.pinfo.sizeP, lkp.pinfo.sizeP, lkp.pinfo.sizeP);

        lPanP2D.SetActive(false);
        lPanP3D.SetActive(false);

        if (allcfg.commonConf.PanPolarActive)
        {
            if (allcfg.commonConf.useDrawing)
                lPanP2D.SetActive(true);
            else
                lPanP3D.SetActive(true);
        }


        /*******************************************************************/ 
        // cartesian Control
        Vector3 heading;

        if (allcfg.commonConf.useDrawing)
            heading = lPanC2D.transform.position - lcamera.transform.position;
        else
            heading = lPanC3D.transform.position - lcamera.transform.position;


        lkp.cinfo.distanceC = heading.magnitude;
        lkp.cinfo.distanceC = (float)(((int)(lkp.cinfo.distanceC * 100)) / 100.0f); // avoid rounding errors using the interface. Precision 1cm


        if (lkp.cinfo.sizeC <= 0)
            lkp.cinfo.sizeC = 1;

        if (allcfg.commonConf.ControlAngularSize)
        {
            lkp.cinfo.sizeC = 2 * Mathf.Tan(Mathf.Deg2Rad * lkp.cinfo.AngularSizeC / 2) * lkp.cinfo.distanceC;
        }
        else //control by scale
        {
            lkp.cinfo.AngularSizeC = 2 * Mathf.Rad2Deg * Mathf.Atan((lkp.cinfo.sizeC / 2) / lkp.cinfo.distanceC);
        }

        // update CG

        lCartesian.transform.localPosition = new Vector3(-lkp.cinfo.posy, lkp.cinfo.posz, lkp.cinfo.posx);

        if (allcfg.commonConf.PanBillboard)
        {
            lookat.enabled = true;
        }
        else
        {
            lookat.enabled = false;
            lQuadBaseC.transform.localEulerAngles = new Vector3(0, 180, 0);
        }


        lPanC2D.transform.localEulerAngles = new Vector3(0, 180, lkp.cminfo.lroll);
        lPanC2D.transform.localScale = new Vector3(lkp.cinfo.sizeC, lkp.cinfo.sizeC, lkp.cinfo.sizeC);

        lPanC2D.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, lkp.cminfo.alpha);

#if BRIGHTNESS_CALIB
        // Brightness Calibration of the screen, using the moon and stars for comparison
        lTriplet.transform.localEulerAngles = new Vector3(0, lkp.cminfo.lroll, 0);
        lOnePix.color = new Color(1, 1, 1, lkp.cminfo.alpha);
        lTwoPix.color = new Color(1, 1, 1, lkp.cminfo.alpha);
        lMoon.color = new Color(1, 1, 1, lkp.cminfo.alpha);
#endif

        lPanC3D.transform.localEulerAngles = new Vector3(-lkp.cminfo.lpitch, -lkp.cminfo.lyaw, -lkp.cminfo.lroll);
        lPanC3D.transform.localScale = new Vector3(lkp.cinfo.sizeC, lkp.cinfo.sizeC, lkp.cinfo.sizeC);


        lPanC2D.SetActive(false);
        lPanC3D.SetActive(false);

        if (!allcfg.commonConf.PanPolarActive) 
        {
            if (allcfg.commonConf.useDrawing)
            {
                lPanC2D.SetActive(true);
            }
            else
            {
                lPanC3D.SetActive(true);
            }
        }

//        textmeshPro.SetText("alpha = {0:2}", lkp.cminfo.alpha);
        textmeshPro.SetText("dt = {0:4}", Time.deltaTime);

        if (allcfg.commonConf.command == (int)commands.CM_getWitnessDirection) // process explicit request by server for witness direction
        {
            wdinfo.byrequest = true; // that is the way I use in order to inform the server that this wdinfo is comming back by request and is not an auto update
            processTxWitnessDirection();
            wdinfo.byrequest = false;
            allcfg.commonConf.command = 0; // command processed
            counter = 0;
        }

        processTx();

        counter++;
        if (counter % 30 == 0)
            processTxWitnessDirection(); // auto update every 0.5sec
    }
}

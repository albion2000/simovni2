using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using TMPro;
using ComStructures;
using UnityEngine.UI;
using TrajectoryStructures;

public class PolarControlServer : PolarControlBase
{
    /* [SerializeField] private  */
    Slider sliderAlpha;
    TextMeshProUGUI sliderAlphaText;
    TextMeshProUGUI sliderDistanceText;
    TextMeshProUGUI sliderAngularSizeText;
    TextMeshProUGUI sliderSizeText;

    TextMeshProUGUI sliderAzText;
    TextMeshProUGUI sliderAltText;

    Slider sliderAz;
    Slider sliderAlt;
    Slider sliderRoll;

    Slider sliderPosx;
    Slider sliderPosy;
    Slider sliderPosz;

    Slider sliderDistance;
    Slider sliderAngularSize;
    Slider sliderSize;

    Slider sliderTime;

    Toggle togglePolar;
    Toggle toggleControlAngularSize;

    Toggle toggleUseDrawing;
    Toggle toggleBillboard;
    Toggle toggleShowSight;

    Toggle toggleRemoteControl;
    Toggle toggleLoop;
    Toggle toggleConnected;


    TextMeshProUGUI sliderTimeText;
    TextMeshProUGUI kpIdxText;

    Button bPlay;
    Button bPause;
    Button bRewind;
    Button bCreateKp;
    Button bPrevKp;
    Button bNextKp;
    Button bRemoveKp;
    Button bLoad;
    Button bSave;
    Button bSetNorthByGaze;
    Button bGetWitnessDirection;

    Button bSendTrajectory;

    bool remoteControl;

    // currently, the trajectory is on the server and interpolation is done on the server.
    // that could change for a more fluid interpolation

    int idx; // index of currently selected keypoint

    // Start is called before the first frame update
    void Start()
    {
        sliderAlphaText = GameObject.Find("SliderAlphaTextValue").GetComponent<TextMeshProUGUI>();
        sliderTimeText = GameObject.Find("SliderTimeTextValue").GetComponent<TextMeshProUGUI>();
        kpIdxText = GameObject.Find("kpIdxTextValue").GetComponent<TextMeshProUGUI>();

        sliderDistanceText = GameObject.Find("SliderDistanceTextValue").GetComponent<TextMeshProUGUI>();
        sliderAngularSizeText = GameObject.Find("SliderAngularSizeTextValue").GetComponent<TextMeshProUGUI>();
        sliderSizeText = GameObject.Find("SliderSizeTextValue").GetComponent<TextMeshProUGUI>();

        sliderAzText = GameObject.Find("SliderAzTextValue").GetComponent<TextMeshProUGUI>();
        sliderAltText = GameObject.Find("SliderAltTextValue").GetComponent<TextMeshProUGUI>();
        //-----------------------------------------------------------------------

        sliderAlpha = GameObject.Find("SliderAlpha").GetComponent<Slider>();

        sliderAz = GameObject.Find("SliderAz").GetComponent<Slider>();
        sliderAlt = GameObject.Find("SliderAlt").GetComponent<Slider>();
        sliderRoll = GameObject.Find("SliderRoll").GetComponent<Slider>();
        sliderDistance = GameObject.Find("SliderDistance").GetComponent<Slider>();
        sliderAngularSize = GameObject.Find("SliderAngularSize").GetComponent<Slider>();
        sliderSize = GameObject.Find("SliderSize").GetComponent<Slider>();

        sliderPosx = GameObject.Find("SliderPosx").GetComponent<Slider>();
        sliderPosy = GameObject.Find("SliderPosy").GetComponent<Slider>();
        sliderPosz = GameObject.Find("SliderPosz").GetComponent<Slider>();

        sliderTime = GameObject.Find("SliderTime").GetComponent<Slider>();

        //-----------------------------------------------------------------------

        sliderAlpha.onValueChanged.AddListener(delegate { UpdateAlpha(); });

        sliderAz.onValueChanged.AddListener(delegate { UpdateYaw(); });
        sliderAlt.onValueChanged.AddListener(delegate { UpdatePitch(); });
        sliderRoll.onValueChanged.AddListener(delegate { UpdateRoll(); });
        sliderDistance.onValueChanged.AddListener(delegate { UpdateDistance(); });
        sliderAngularSize.onValueChanged.AddListener(delegate { UpdateAngularSize(); });
        sliderSize.onValueChanged.AddListener(delegate { UpdateSize(); });

        sliderPosx.onValueChanged.AddListener(delegate { UpdatePosx(); });
        sliderPosy.onValueChanged.AddListener(delegate { UpdatePosy(); });
        sliderPosz.onValueChanged.AddListener(delegate { UpdatePosz(); });

        sliderTime.onValueChanged.AddListener(delegate { UpdateTime(); });

        //-----------------------------------------------------------------------

        togglePolar = GameObject.Find("Toggle Polar").GetComponent<Toggle>();
        toggleControlAngularSize = GameObject.Find("Toggle ControlAngularSize").GetComponent<Toggle>();
        toggleUseDrawing = GameObject.Find("Toggle UseDrawing").GetComponent<Toggle>();
        toggleBillboard = GameObject.Find("Toggle Billboard").GetComponent<Toggle>();
        toggleShowSight = GameObject.Find("Toggle ShowSight").GetComponent<Toggle>();

        toggleRemoteControl = GameObject.Find("Toggle RemoteControl").GetComponent<Toggle>();
        toggleLoop = GameObject.Find("Toggle Loop").GetComponent<Toggle>();
        toggleConnected = GameObject.Find("Toggle Connected").GetComponent<Toggle>();

        togglePolar.onValueChanged.AddListener(TogglePolar);
        toggleControlAngularSize.onValueChanged.AddListener(ToggleControlAngularSize);
        toggleUseDrawing.onValueChanged.AddListener(ToggleUseDrawing);
        toggleBillboard.onValueChanged.AddListener(ToggleBillboard);
        toggleShowSight.onValueChanged.AddListener(ToggleShowSight);
        toggleLoop.onValueChanged.AddListener(ToggleLoop);

        toggleRemoteControl.onValueChanged.AddListener(ToggleRemoteControl);
//        toggleConnected.onValueChanged.AddListener(ToggleConnected); // not under control of user
        //-----------------------------------------------------------------------

        bPlay = GameObject.Find("Button Play").GetComponent<Button>();
        bPlay.onClick.AddListener(play);

        bPause = GameObject.Find("Button Pause").GetComponent<Button>();
        bPause.onClick.AddListener(pause);

        bRewind = GameObject.Find("Button Rewind").GetComponent<Button>();
        bRewind.onClick.AddListener(rewind);

        bCreateKp = GameObject.Find("Button CreateKp").GetComponent<Button>();
        bCreateKp.onClick.AddListener(createkp);

        bPrevKp = GameObject.Find("Button PrevKp").GetComponent<Button>();
        bPrevKp.onClick.AddListener(prevkp);

        bNextKp = GameObject.Find("Button NextKp").GetComponent<Button>();
        bNextKp.onClick.AddListener(nextkp);

        bRemoveKp = GameObject.Find("Button RemoveKp").GetComponent<Button>();
        bRemoveKp.onClick.AddListener(removekp);

        bLoad = GameObject.Find("Button Load").GetComponent<Button>();
        bLoad.onClick.AddListener(load);

        bSave = GameObject.Find("Button Save").GetComponent<Button>();
        bSave.onClick.AddListener(save);

        bSetNorthByGaze = GameObject.Find("Button SetNorthByGaze").GetComponent<Button>();
        bSetNorthByGaze.onClick.AddListener(setNorthByLook);

        bGetWitnessDirection = GameObject.Find("Button GetWitnessDirection").GetComponent<Button>();
        bGetWitnessDirection.onClick.AddListener(getWitnessDirection);

        bSendTrajectory = GameObject.Find("Button SendTrajectory").GetComponent<Button>();
        bSendTrajectory.onClick.AddListener(sendTrajectory);


#if UNITY_EDITOR
        //     QualitySettings.vSyncCount = 0;  // VSync must be disabled
        Application.targetFrameRate = 60;
#endif

        remoteControl = false;

        idx = 0;

        t.kp_list = new List<keypoint>();

        lkp.set = true;

        ref_allcfg = allcfg;
        ref_kp = lkp;

    }

    // Update is called once per frame
    void Update()
    {
        processRx("Server");

        if (playing)
        {
            // if looping
            if ((allcfg.commonConf.loop) && (t.kp_list.Count > 0) && (lkp.cminfo.time >= t.kp_list[t.kp_list.Count - 1].cminfo.time))
            {  // we are already at the end of the traj we should loop back at the start
                lkp.cminfo.time = t.kp_list[0].cminfo.time;
            }
            else   
                lkp.cminfo.time += Time.deltaTime; // doing it before the interpolate ensures lkp.cminfo.time will remain in the times of the traj

            lkp = t.interpolate(lkp.cminfo.time); // may change the time to go back into the traj. 

            int kp_left = t.findKpByTime(lkp.cminfo.time);
            if (kp_left != -1)
            {
                kpIdxText.text = kp_left.ToString()+"-"+ (kp_left+1).ToString();
                idx = kp_left;
            }
        }

        // we requested witness direction (command 2), we just received the answer
        // we use it in order to place the UAP in that direction.

        if (wdinfo.byrequest)
        {
            // not supposed to do that while playing or in remote control.
            // ignore it
            if ((!playing) && (!remoteControl))
            {
                lkp.pinfo.az = wdinfo.az - allcfg.commonConf.north;
                lkp.pinfo.alt = wdinfo.alt;
            }
            wdinfo.byrequest = false;
        }

        if (lkp.cminfo.alpha >= 1)
            lkp.cminfo.alpha = 1;
        if (lkp.cminfo.alpha < 0)
            lkp.cminfo.alpha = 0;

        if (lkp.pinfo.sizeP <= 0)
            lkp.pinfo.sizeP = 1;

        if (lkp.cinfo.sizeC <= 0)
            lkp.cinfo.sizeC = 1;

        // move sliders if updated somewhere else : by IDE or computations
        sliderAlpha.value = lkp.cminfo.alpha * 255;
        sliderRoll.value = lkp.cminfo.lroll;

        sliderAz.value = lkp.pinfo.az;
        sliderAlt.value = lkp.pinfo.alt;

        sliderPosx.value = lkp.cinfo.posx;
        sliderPosy.value = lkp.cinfo.posy;
        sliderPosz.value = lkp.cinfo.posz;

        if (allcfg.commonConf.PanPolarActive)
        {
            togglePolar.isOn = true;
        }
        else
        {
            togglePolar.isOn = false;
        }

        toggleControlAngularSize.isOn = allcfg.commonConf.ControlAngularSize;


        if (allcfg.commonConf.PanPolarActive)
        {
            sliderDistance.value = lkp.pinfo.distanceP;
            sliderAngularSize.value = lkp.pinfo.AngularSizeP;
            sliderSize.value = lkp.pinfo.sizeP;
        }
        else
        {
            sliderDistance.value = lkp.cinfo.distanceC;
            sliderAngularSize.value = lkp.cinfo.AngularSizeC;
            sliderSize.value = lkp.cinfo.sizeC;
        }

        toggleBillboard.isOn = allcfg.commonConf.PanBillboard;
        toggleUseDrawing.isOn = allcfg.commonConf.useDrawing;
        toggleShowSight.isOn = allcfg.commonConf.showSight;
        toggleLoop.isOn = allcfg.commonConf.loop;
        toggleConnected.isOn = isConnected();

        sliderTime.value = lkp.cminfo.time;

        sliderAngularSize.interactable = allcfg.commonConf.ControlAngularSize;

        if (allcfg.commonConf.PanPolarActive)
        {
            sliderAz.gameObject.SetActive(true);
            sliderAlt.gameObject.SetActive(true);
            sliderPosx.gameObject.SetActive(false);
            sliderPosy.gameObject.SetActive(false);
            sliderPosz.gameObject.SetActive(false);
        }
        
        if (!allcfg.commonConf.PanPolarActive)
        {
            sliderAz.gameObject.SetActive(false);
            sliderAlt.gameObject.SetActive(false);
            sliderPosx.gameObject.SetActive(true);
            sliderPosy.gameObject.SetActive(true);
            sliderPosz.gameObject.SetActive(true);
        }


        counter++;

        if (counter % 4 == 0) // need 2 frames for receiving the response copy. Else won't be stable. Else we need to manage a messages queue. Not the case right now
        {
            processTx(remoteControl);
        }

    }
    //see also  https://www.youtube.com/watch?v=nTLgzvklgU8

    //---------------------------------------------------------------------

    // START SLIDERS. Called when sliders move
    public void UpdateAlpha()
    {
        lkp.cminfo.alpha = sliderAlpha.value / 255.0f;
        sliderAlphaText.text = sliderAlpha.value.ToString("000");
    }

    public void UpdateRoll()
    {
       lkp.cminfo.lroll = sliderRoll.value; 
    }

    public void UpdateYaw()
    {
        lkp.pinfo.az = sliderAz.value;
        sliderAzText.text = sliderAz.value.ToString("000.0");
    }

    public void UpdatePitch()
    {
        lkp.pinfo.alt = sliderAlt.value;
        sliderAltText.text = sliderAlt.value.ToString("000.0");
    }

    public void UpdatePosx()
    {
        lkp.cinfo.posx = sliderPosx.value;
    }

    public void UpdatePosy()
    {
        lkp.cinfo.posy = sliderPosy.value;
    }

    public void UpdatePosz()
    {
        lkp.cinfo.posz = sliderPosz.value;
    }


    public void UpdateDistance()
    {
        // can move slider manually only in polar mode.
        if (allcfg.commonConf.PanPolarActive)
        {
            lkp.pinfo.distanceP = sliderDistance.value;
        }

        sliderDistanceText.text = sliderDistance.value.ToString("000.0");
    }

    public void UpdateAngularSize()
    {
        if (allcfg.commonConf.PanPolarActive) // risque de mélange... XXX
            lkp.pinfo.AngularSizeP = sliderAngularSize.value;
        else
            lkp.cinfo.AngularSizeC = sliderAngularSize.value;

        sliderAngularSizeText.text = sliderAngularSize.value.ToString("000.0");
    }

    public void UpdateSize()
    {
        if (allcfg.commonConf.PanPolarActive) // risque de mélange... XXX
            lkp.pinfo.sizeP = sliderSize.value;
        else
            lkp.cinfo.sizeC = sliderSize.value;

        sliderSizeText.text = sliderSize.value.ToString("000.0");
    }

    public void UpdateTime()
    {
        lkp.cminfo.time = sliderTime.value;
        sliderTimeText.text = sliderTime.value.ToString("000.0");
    }
    // END SLIDERS

    //---------------------------------------------------------------------

    // START TOGGLES

    public void TogglePolar(bool value)
    {
        allcfg.commonConf.PanPolarActive = value;
        if (value)
        {
            sliderDistance.value = lkp.pinfo.distanceP;
            sliderSize.value = lkp.pinfo.sizeP;
            sliderAngularSize.value = lkp.pinfo.AngularSizeP;
        }
        else
        {
            sliderDistance.value = lkp.cinfo.distanceC;
            sliderSize.value = lkp.cinfo.sizeC;
            sliderAngularSize.value = lkp.cinfo.AngularSizeC;
        }
    }

    public void ToggleControlAngularSize(bool value)
    {
        allcfg.commonConf.ControlAngularSize = value;
    }

    public void ToggleUseDrawing(bool value)
    {
        allcfg.commonConf.useDrawing = value;
    }

    public void ToggleBillboard(bool value)
    {
        allcfg.commonConf.PanBillboard = value;
    }

    public void ToggleShowSight(bool value)
    {
        allcfg.commonConf.showSight = value;
    }

    public void ToggleLoop(bool value)
    {
        allcfg.commonConf.loop = value;
    }

    public void ToggleRemoteControl(bool value)
    {
        // if we stop the remote control, put also the client HMD in pause, in case it was playing.
        // else, we would continue to receive messages with updates
        if ((!value)&&(remoteControl)) 
        {
            pause();
        }
        // if we start remote control, put also the server (us) in pause, in case it was playing
        // else, we would continue to send messages with updates, which would create sort of a conflict
        if ((value) && (!remoteControl))
        {
            sendTrajectory(); // as an helper, in case you forget to update the client with the trajectory
            playing = false;
        }


        remoteControl = value;
    }

    

    // END TOGGLES


    //---------------------------------------------------------------------

    // START BUTTONS
    public void play()
    {
        if (remoteControl)
            allcfg.commonConf.command = (int) commands.CM_play;
        else
            playing = true;
    }
    public void pause()
    {
        if (remoteControl)
            allcfg.commonConf.command = (int) commands.CM_pause;
        else
            playing = false;
    }
    public void rewind()
    {
        if (remoteControl)
            allcfg.commonConf.command = (int) commands.CM_rewind;
        else
        {
            idx = 0;
            kpIdxText.text = idx.ToString();
            getKp();
        }
    }

    void getKp()
    {
        if ((idx>=0)&&(idx< t.kp_list.Count))
        {
            lkp = t.kp_list[idx];
        }
    }


    public void createkp()
    {
        t.InsertAtTime(lkp);

        int kp_left = t.findKpByTime(lkp.cminfo.time);
        if (kp_left != -1)
        {
            idx = kp_left;
            kpIdxText.text = idx.ToString();
        }
    }

    public void prevkp()
    {
        if (idx < t.kp_list.Count)
        {
            if (idx>0)
            {
                if ((playing) || (lkp.cminfo.time == t.kp_list[idx].cminfo.time)) // makes that if we are between 1-2 we stay back at 2.
                    idx--;
            }

            kpIdxText.text = idx.ToString();
            getKp();
        }
    }
    public void nextkp()
    {
        if (idx < t.kp_list.Count)
        {
            if (idx < t.kp_list.Count - 1)
                idx++;
            kpIdxText.text = idx.ToString();
            getKp();
        }
    }

    public void removekp()
    {
        if (playing)
            return;

        if ((idx >= 0) && (idx < t.kp_list.Count))
        {
            if (lkp.cminfo.time != t.kp_list[idx].cminfo.time) // forbits removal of keypoint if we are not exactly on it.
                return;

            t.kp_list.RemoveAt(idx);
            if (idx != 0)
            {
                idx--;
            }
            kpIdxText.text = idx.ToString();
            getKp();            
        }
    }

    public void setNorthByLook() // explicit request to use the current viewing direction as the north direction
    {
        allcfg.commonConf.command = (int) commands.CM_setNorthByLook;
    }

    public void getWitnessDirection() // explicit request to get the direction
    {
        // not supposed to do that while playing or in remote control.
        // ignore it
        if ((!playing) && (!remoteControl))
            allcfg.commonConf.command = (int) commands.CM_getWitnessDirection;
    }

    public void load()
    {
        t.loadFromFile("trajectoire.txt");
        // il faut au moins appliquer la config pour avoir quelque chose d'affiché et sélectionner le kp 0
        allcfg = t.allcfg;
        rewind();

    }

    public void save()
    {
        t.allcfg = allcfg;
        t.saveToFile("trajectoire.txt");
    }

    // END BUTTONS

}

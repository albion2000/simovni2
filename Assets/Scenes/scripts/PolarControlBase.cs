using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using TMPro;
using ComStructures;
using TrajectoryStructures;

public class PolarControlBase : MonoBehaviour
{

    public messaging messager;

//    [Header("Config")]
    // current state
    public AllConfig allcfg;
    public keypoint lkp;
    protected WitnessDirection wdinfo;

    // copy of ^ in order to auto detect updates to be mirrored to the mate
    protected AllConfig ref_allcfg;
    protected keypoint ref_kp;
    protected WitnessDirection ref_wdinfo;

    protected int counter = 0; // useful for actions every N frames
    protected bool playing = false;
    protected trajectory t = new trajectory();

    protected void processRx(string comment, bool debug = true)
    {

        while (messager.messageReceived)
        {
            tcpMessage rx = messager.getRxMessage();
            if (debug)
            {
              Debug.Log("rx "+comment+" Type " + rx.ctype + "... json:" + rx.json_as_string);
            }

            switch (rx.ctype)
            {
                case classType.CT_CommonInfo:
                    {
                        lkp.cminfo = translation.retrieveCommonInfoClassFromReceivedTCPMessage(rx);
                        //                        Debug.Log("message fom client has been processed");
                        break;
                    }
                case classType.CT_PolarInfo:
                    {
                        lkp.pinfo = translation.retrievePolarInfoClassFromReceivedTCPMessage(rx);
                        //                        Debug.Log("message fom client has been processed");
                        break;
                    }
                case classType.CT_CartesianInfo:
                    {
                        lkp.cinfo = translation.retrieveCartesianInfoClassFromReceivedTCPMessage(rx);
                        //                        Debug.Log("message fom client has been processed");
                        break;
                    }
                case classType.CT_AllConfig:
                    {
                        allcfg = translation.retrieveAllConfigClassFromReceivedTCPMessage(rx);
                        //                        Debug.Log("message fom client has been processed");
                        break;
                    }
                case classType.CT_WitnessDirection: // should only happen on server side
                    {
                        wdinfo = translation.retrieveWitnessDirectionClassFromReceivedTCPMessage(rx);
                        //                        Debug.Log("message fom client has been processed");
                        break;
                    }
                case classType.CT_Trajectory:
                    {
                        t = translation.retrieveTrajectoryClassFromReceivedTCPMessage(rx);  // BOOM
                        //                        Debug.Log("message fom client has been processed");
                        break;
                    }



                default:  Debug.Log("unexpected message type " + rx.ctype); break; 
            }
        }
    }

    protected void processTx(bool remoteControl = false)
    {
        // always need this one for the commands to be passed even in remote control
        if (!AllConfig.Equals(allcfg, ref_allcfg)) 
        {
            tcpMessage tx_message = translation.preparetoSendTCPMessage(classType.CT_AllConfig, allcfg);
            messager.txMessage(tx_message);
            allcfg.commonConf.command = 0;
            ref_allcfg = allcfg;
        }

        // this way, when traj is remotely played on the client, only the client will send updates. the server will not ping pong the updates
        // as a consequence, in remote_control, one cannot modify time from the server. (time is in cminfo struct)
        if (!remoteControl) 
        {
            if (!CommonInfo.Equals(lkp.cminfo, ref_kp.cminfo))
            {
                tcpMessage tx_message = translation.preparetoSendTCPMessage(classType.CT_CommonInfo, lkp.cminfo);
                messager.txMessage(tx_message);
                ref_kp.cminfo = lkp.cminfo;
            }
            if (!PolarInfo.Equals(lkp.pinfo, ref_kp.pinfo))
            {
                tcpMessage tx_message = translation.preparetoSendTCPMessage(classType.CT_PolarInfo, lkp.pinfo);
                messager.txMessage(tx_message);
                ref_kp.pinfo = lkp.pinfo;
            }
            if (!CartesianInfo.Equals(lkp.cinfo, ref_kp.cinfo))
            {
                tcpMessage tx_message = translation.preparetoSendTCPMessage(classType.CT_CartesianInfo, lkp.cinfo);
                messager.txMessage(tx_message);
                ref_kp.cinfo = lkp.cinfo;
            }
        }
    }

    // used only by the client in order to send the witness' HMD direction
    protected void processTxWitnessDirection()
    {
        if (!WitnessDirection.Equals(wdinfo, ref_wdinfo))
        {
            tcpMessage tx_message = translation.preparetoSendTCPMessage(classType.CT_WitnessDirection, wdinfo);
            messager.txMessage(tx_message);
            ref_wdinfo = wdinfo;
        }
    }

    // used only by the server in order to send the trajectory
    protected void sendTrajectory()
    {
            tcpMessage tx_message = translation.preparetoSendTCPMessage(classType.CT_Trajectory, t);
            messager.txMessage(tx_message);
    }

    protected bool isConnected()
    {
        return messager.isConnected();
    }
}

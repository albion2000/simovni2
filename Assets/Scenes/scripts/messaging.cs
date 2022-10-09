using System;
using System.Text;
using UnityEngine;
using Unity.Collections;
using TinyJson;
using Unity.Jobs;
using System.Collections.Generic;
using System.Collections.Concurrent;
using TrajectoryStructures;

// [System.Serializable] to be able to have structure members display in the inspector
// If the ReadOnlyInspector attribute does not work, then see link below for what's missing in your scripts
// https://www.patrykgalach.com/2020/01/20/readonly-attribute-in-unity-editor/

namespace ComStructures
{
    [System.Serializable]
    public struct CommonConfig
    {
        [Tooltip("Use a drawing or a 3D model for the UAP")]
        [ReadOnlyInspector]
        public bool useDrawing;
        [Tooltip("Is the UAP always facing the witness ?")]
        [ReadOnlyInspector]
        public bool PanBillboard;
        [Tooltip("Correction for North Direction")]
        [ReadOnlyInspector]
        public float north;
        [Tooltip("Show to the user the sighting direction")]
        [ReadOnlyInspector]
        public bool showSight;
        [Tooltip("Loop the trajectory")]
        [ReadOnlyInspector]
        public bool loop;
        [ReadOnlyInspector]
        public int command; // for passing commands to the HMD (client)
        [Tooltip("User can directly control the angular size")]
        [ReadOnlyInspector]
        public bool ControlAngularSize;
        [Tooltip("Activates the Polar coordinates control mode")]
        [ReadOnlyInspector]
        public bool PanPolarActive;
    }

/*
    [System.Serializable]
    public struct PolarConfig
    {
    }

    [System.Serializable]
    public struct CartesianConfig
    {
        [Tooltip("Activates the Cartesian coordinates control mode")]
        [ReadOnlyInspector]
        public bool PanCartesianActive;
    }
*/

    [System.Serializable]
    public struct AllConfig
    {
        [Tooltip("Configuration Common to all modes")]
        public CommonConfig commonConf;
//        public PolarConfig polarConf;
//        public CartesianConfig cartesianConf;
    }

    [System.Serializable]
    public struct CommonInfo
    {
        [Tooltip("UAP Orientation")]
        public float lyaw, lpitch, lroll;
        [Tooltip("UAP Luminosity")]
        public float alpha;
        [ReadOnlyInspector]
        public float time;
    }

    [System.Serializable]
    public struct PolarInfo
    {
        [Tooltip("UAP Azimuth")]
        public float az;
        [Tooltip("UAP Angular Altitude")]
        public float alt;
        [Tooltip("UAP Angular Size in degrees")]
        public float AngularSizeP;
        [Tooltip("UAP Distance in meters")]
        public float distanceP;
        [Tooltip("UAP Size in meters")]
        public float sizeP;

        public static bool Equals(PolarInfo a, PolarInfo b)
        {
            const float aPrecision = 0.1f;
            const float dPrecision = 0.0101f;

            if (Math.Abs(a.az - b.az) > 0)
                return false;
            if (Math.Abs(a.alt - b.alt) > 0)
                return false;
            // the Equals override is needed because of instability in the ping pong of the info through the network.
            // it can get stuck a certain unwanted values, or never stabilizes.
            if (Math.Abs(a.AngularSizeP - b.AngularSizeP) > aPrecision)
                return false;
            if (Math.Abs(a.distanceP - b.distanceP) > dPrecision)
                return false;
            if (Math.Abs(a.sizeP - b.sizeP) > dPrecision)
                return false;
            return true;
        }

    }

    [System.Serializable]
    public struct CartesianInfo
    {
        [Tooltip("UAP Position in meters (X is forward, Y to the left, Z up)")]
        public float posx, posy, posz;
        [Tooltip("UAP Angular Size in degrees")]
        public float AngularSizeC;
        [Tooltip("UAP Distance in meters")]
        [ReadOnlyInspector]
        public float distanceC;
        [Tooltip("UAP Size in meters")]
        public float sizeC;

        public static bool Equals(CartesianInfo a, CartesianInfo b)
        {
            const float aPrecision = 0.1f;
            const float dPrecision = 0.0101f;

            if (Math.Abs(a.posx - b.posx) > 0)
                return false;
            if (Math.Abs(a.posy - b.posy) > 0)
                return false;
            if (Math.Abs(a.posz - b.posz) > 0)
                return false;
            // the Equals override is needed because of instability in the ping pong of the info through the network.
            // it can get stuck a certain unwanted values, or never stabilizes.
            if (Math.Abs(a.AngularSizeC - b.AngularSizeC) > aPrecision)
                return false;
            if (Math.Abs(a.distanceC - b.distanceC) > dPrecision)
                return false;
            if (Math.Abs(a.sizeC - b.sizeC) > dPrecision)
                return false;
            return true;
        }

    }

    // This is also a message, but contents are not intended to be displayed in the IDE, not really useful.
//    [System.Serializable]
    public struct WitnessDirection
    {
//        [Tooltip("Witness Direction")]
        public float az, alt;
        public bool byrequest;
    }

    public enum classType
    {
        CT_AllConfig = 'A',
        CT_CommonInfo = 'B',
        CT_PolarInfo = 'C',
        CT_CartesianInfo = 'D',
        CT_Undefined = 'E',
        CT_WitnessDirection = 'F',
        CT_Keypoint = 'G', // to be done. not used
        CT_Trajectory = 'H' // Warning, WILL BE bigger than 1500 bytes ! 
    }

    public enum commands
    {
        CM_noCommand = 0,
        CM_setNorthByLook = 1,
        CM_getWitnessDirection = 2,
        CM_play = 3,
        CM_pause = 4,
        CM_rewind = 5
    }

    struct messages_sizes
    {
        public const int HEADER_SIZE = 3;
        public const int MAX_JSON_MESSAGE_SIZE = 1500-3;
    }

    public class tcpMessage
    {
        public classType ctype;  // classType, one byte in the message
        public int size; // size = sizeL + sizeH*256, sizeL then sizeH in message. Max Size = 65535
                         // then followed by the json text as bytes of this size. (only basic ascii allowed for more compatibility)
        public byte[] json; // = new byte[messages_sizes.MAX_JSON_MESSAGE_SIZE];  // ascii version, the one sent on the network
        public string json_as_string;  // string version

        public tcpMessage()
        {
            ctype = classType.CT_Undefined;
//            json = new byte[messages_sizes.MAX_JSON_MESSAGE_SIZE];
        }

        // not used anymore : 0 references
/*        
        public tcpMessage(tcpMessage x)
        {
            ctype = x.ctype;
            size = x.size;
            json = new byte[size];
            Array.Copy(x.json, json, size);
            json_as_string = x.json_as_string.ToString(); // ensures we make a full copy, with no more reference. right ?
        }
*/        
    }

    public static class translation // translation json <-> class
    {
        public static tcpMessage preparetoSendTCPMessage(classType ctype, object x)
        {
            tcpMessage tx_message = new tcpMessage();

            tx_message.ctype = ctype;
            tx_message.json_as_string = x.ToJson(); // this part won't be sent.
            tx_message.size = Encoding.ASCII.GetByteCount(tx_message.json_as_string); // tx_message.json_as_string.Length;
            tx_message.json = new byte[tx_message.size];     
            tx_message.json = Encoding.ASCII.GetBytes(tx_message.json_as_string);  // this part will be sent. 
            return tx_message;
        }


        public static AllConfig retrieveAllConfigClassFromReceivedTCPMessage(tcpMessage rx_message)
        {
            return rx_message.json_as_string.FromJson<AllConfig>();
        }

        public static CommonInfo retrieveCommonInfoClassFromReceivedTCPMessage(tcpMessage rx_message)
        {
            return rx_message.json_as_string.FromJson<CommonInfo>();
        }
        public static PolarInfo retrievePolarInfoClassFromReceivedTCPMessage(tcpMessage rx_message)
        {
            return rx_message.json_as_string.FromJson<PolarInfo>();
        }
        public static CartesianInfo retrieveCartesianInfoClassFromReceivedTCPMessage(tcpMessage rx_message)
        {
            return rx_message.json_as_string.FromJson<CartesianInfo>();
        }
        public static WitnessDirection retrieveWitnessDirectionClassFromReceivedTCPMessage(tcpMessage rx_message)
        {
            return rx_message.json_as_string.FromJson<WitnessDirection>();
        }
        public static trajectory retrieveTrajectoryClassFromReceivedTCPMessage(tcpMessage rx_message)
        {
            return rx_message.json_as_string.FromJson<trajectory>();
        }
        
    }


    public class messaging : MonoBehaviour
    {
        TCPServer m_tcpserver; 
        TCPClient m_tcpclient; 

        public bool server; // helper for the code below. to be defined in the unity3d IDE

        void Start()
        {
            if (server)
            {
                m_tcpserver = GameObject.Find("TCPServer").GetComponent<TCPServer>();
                if (m_tcpserver != null)
                    m_tcpserver.setMessager(this);
                else
                    Debug.Log("class messaging MessagingServer: init problem you need one TCPServer object in the unity scene");
            }
            else
            {
                m_tcpclient = GameObject.Find("TCPClient").GetComponent<TCPClient>();
                if (m_tcpclient != null)
                    m_tcpclient.setMessager(this);
                else
                    Debug.Log("class messaging MessagingClient: init problem you need one TCPClient object in the unity scene");
            }

            if ((m_tcpserver != null) && (m_tcpclient != null))
                Debug.Log("class messaging : init problem : double init");
            if ((m_tcpserver == null) && (m_tcpclient == null))
                Debug.Log("class messaging : init problem : null");
        }

        ConcurrentQueue<tcpMessage> rx_messages_list = new ConcurrentQueue<tcpMessage>(); // in order to be  thread safe. this is a FIFO
   //        List<tcpMessage> rx_messages_list = new List<tcpMessage>(); // basic list is NOT THREAD SAFE : produit des null reference de temps à autre.

        public bool messageReceived
        {
            get
            {
                if (rx_messages_list.Count == 0) // this is thread safe
                    return false;
                return true;
            }
        }

        byte[] brokenFrame; // to save no yet used data (broken frame leftover) between calls 
        int brokenFrameLength = 0;

        // Handles broken frames, but will not recover from a loss of synchro (aka loss of data).
        private int consummeMessage(byte[] bytes, int offset, int remainingsize)
        {
            int sizeconsummed;
            int bytesRecovered;
            byte[] frame;
            int frameLength = 0;

            if (brokenFrameLength>0) // there is pending broken frame from a previous call, prepend it 
            {
                frameLength = brokenFrameLength + remainingsize;
                frame = new byte[frameLength];
                Array.Copy(brokenFrame, frame, brokenFrameLength);
                Array.Copy(bytes, offset, frame, brokenFrameLength, remainingsize);
                bytesRecovered = brokenFrameLength;
                brokenFrameLength = 0; // a priori...
            }
            else // no previous broken frame, take all remaining as is (even if it is too much).
            {
                frameLength = remainingsize;
                frame = new byte[frameLength];
                Array.Copy(bytes, offset, frame, 0, remainingsize);
                bytesRecovered = 0;
            }



            if (frameLength <= 3) // can't potentially even get the size, this has to be a broken frame
            {
                brokenFrameLength = frameLength;
                brokenFrame = new byte[brokenFrameLength];
                Array.Copy(frame, brokenFrame, brokenFrameLength);
                return remainingsize; // consummed all, but broken
            }

            tcpMessage rx_message = new tcpMessage();
            rx_message.ctype = (classType)(frame[0]);
            rx_message.size = (frame[1] + frame[2] * 256);

            if (frameLength < rx_message.size + messages_sizes.HEADER_SIZE) // still not a complete message
            {
                // this is not a real problem, but good to inform
                Debug.Log("inconsistent message size too small, potentially broken tcp frame :" + frameLength + " vs " + rx_message.size + messages_sizes.HEADER_SIZE);
                brokenFrameLength = frameLength;
                brokenFrame = new byte[brokenFrameLength];
                Array.Copy(frame, brokenFrame, brokenFrameLength); // save for next pass
                return remainingsize;  // consummed all, but broken
            }

            if (rx_message.size > messages_sizes.MAX_JSON_MESSAGE_SIZE)
                Debug.Log("TCP message size very big. Is it OK ?" + rx_message.size);

            // now, we have received enough data for that frame. not a broken frame
            // But we may not consumme everything
            if (rx_message.size < 65000) // for security
            {
                rx_message.json = new byte[rx_message.size];
                Array.Copy(frame, messages_sizes.HEADER_SIZE, rx_message.json, 0, rx_message.size);
                rx_message.json_as_string = Encoding.ASCII.GetString(rx_message.json, 0, (int)rx_message.size);
                rx_messages_list.Enqueue(rx_message);
                sizeconsummed = rx_message.size + messages_sizes.HEADER_SIZE - bytesRecovered; 
                return sizeconsummed;
                // Debug.Log("tcp rx message : json = "+ rx_message.json_as_string);
            }
            return remainingsize; // if >=65000 we have a big problem, trash the rest.
        }

        // is called by the tcp server or client reception thread we are attached to
        // potentially, can include more than one message in a frame and/or a broken frame at the end
        public void newRxMessage(byte[] bytes, int size) 
        {
            int offset;
            int sizeconsummed;
            int remainingsize = size;

            offset = 0;
            do
            {
                sizeconsummed = consummeMessage(bytes, offset, remainingsize);
                offset = offset + sizeconsummed;
                remainingsize = remainingsize - sizeconsummed;
            }
            while (remainingsize > 0);
        }

        public tcpMessage getRxMessage() // to be called by the processRx() / from server or client by polarControlBase
        {
            tcpMessage lrx_message;
            if (!messageReceived)
            {
                Debug.Log("tcp rx underflow. You are Reading Dummy data");
                return new tcpMessage(); // message with CT_Undefined type, will be ignored with a logged warning
            }
            else
            {
                if (rx_messages_list.TryDequeue(out lrx_message)) // don't need to make a full copy. Ref is enough. Well not sure.
                    return lrx_message; // makes a full copy, separate : no, just ref
                else
                {
                    Debug.Log("internal error on pop. Very weird. You are Reading Dummy data");
                    return new tcpMessage();
                }
            }
        }
        
        public void txMessage(tcpMessage tx) //  to be called by the API (polarControlClient-Server by polarControlBase)
        {
            int size_on_inet = messages_sizes.HEADER_SIZE + tx.size;
            byte[] data = new byte[size_on_inet];
            data[0] = (byte) tx.ctype;
            data[1] = (byte) (tx.size % 256);
            data[2] = (byte) ((tx.size-data[1]) / 256);
            Array.Copy(tx.json, 0, data, messages_sizes.HEADER_SIZE, tx.size);
            if (m_tcpserver != null)
                m_tcpserver.SendMessage(data, size_on_inet);
            else
            {
                if (m_tcpclient != null)
                    m_tcpclient.SendMessage(data, size_on_inet);
            }
        }

        public bool isConnected()
        {
            if (m_tcpserver != null)
                return m_tcpserver.isConnected();
            else
            {
                if (m_tcpclient != null)
                    m_tcpclient.isConnected();
            }
            return false;

        }
    }

}


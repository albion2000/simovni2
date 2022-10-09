using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using ComStructures;
using System.IO;

public class TCPClient : MonoBehaviour
{
    #region private members 	
    private TcpClient socketConnection;
    private Thread clientReceiveThread;
    #endregion

    private messaging m_messager;
    private bool lostConnection = true;
    private int counter = 0;
    public bool forceCloseForTest = false;

    public void setMessager(messaging messager)
    {
        m_messager = messager;
    }

    // Use this for initialization 	
    void Start()
    {
 //       ConnectToTcpServer();
    }
    // Update is called once per frame
    void Update()
    {
        if (counter == 0)
        {
            if ((lostConnection)&&(!forceCloseForTest))
            {
                Debug.Log("Client tries to connect");
                lostConnection = false;
                ConnectToTcpServer();
            }
        }
        counter++;
        if (counter == 100)
            counter = 0;

    }
    /// <summary> 	
    /// Setup socket connection. 	
    /// </summary> 	
    private void ConnectToTcpServer()
    {
        try
        {
            clientReceiveThread = new Thread(new ThreadStart(ListenForData));
            clientReceiveThread.IsBackground = true;
            clientReceiveThread.Start();
        }
        catch (Exception e)
        {
            Debug.Log("On client connect exception " + e);
            lostConnection = true;
        }
    }

    private Byte[] bytes = new Byte[65000]; // messages_sizes.MAX_JSON_MESSAGE_SIZE + messages_sizes.HEADER_SIZE];

    /// <summary> 	
    /// Runs in background clientReceiveThread; Listens for incomming data. 	
    /// </summary>     
    private void ListenForData()
    {
        try
        {
            socketConnection = new TcpClient("192.168.43.121", 9005); // 192.168.0.15  127.0.0.1  --- 10.0.1.34 pc bureau --- 10.0.1.53 portable au bureau ---  x360 maison 192.168.0.25 -- x360 par point d'acces mobile 192.168.43.121
            Debug.Log("Client seems to be connected");
            while (!forceCloseForTest)
            {
                // Get a stream object for reading 				
                using (NetworkStream stream = socketConnection.GetStream())
                {
                    int length;
                    // Read incomming stream into byte arrary. 					
                    while ((!forceCloseForTest) && ((length = stream.Read(bytes, 0, bytes.Length)) != 0))
                    {
                        m_messager.newRxMessage(bytes, length);
                    }
                }
            }
            socketConnection.Close();
            socketConnection = null;
            lostConnection = true;
        }
        // all the complexity here is in order to be able to recover the connection if the server goes off/on
        catch (SocketException socketException)
        {
            Debug.Log("Socket exception: " + socketException);
            if (socketConnection != null)
            {
                if (socketConnection.Connected)
                    socketConnection.Close();
            }
            socketConnection = null;
            lostConnection = true;
        }
        catch (InvalidOperationException invalidOpException)
        {
            Debug.Log("My InvalidOperationException: " + invalidOpException);
            if (socketConnection != null)
            {
                if (socketConnection.Connected)
                    socketConnection.Close();
            }
            socketConnection = null;
            lostConnection = true;
        }
        catch (IOException ioexcept)
        {
            Debug.Log("My IOException: " + ioexcept);
            if (socketConnection != null)
            {
                if (socketConnection.Connected)
                    socketConnection.Close();
            }
            socketConnection = null;
            lostConnection = true;
        }
    }
    /// <summary> 	
    /// Send message to server using socket connection. 	
    /// all the complexity here is in order to be able to recover the connection if the server goes off/on
    /// </summary> 	
    public void SendMessage(byte[] data, int size)
    {
        if (forceCloseForTest)
        {
            if (socketConnection != null)
            {
                if (socketConnection.Connected)
                    socketConnection.Close();
            }
            socketConnection = null;
            lostConnection = true;
            return;
        }

        if (socketConnection == null)
        {
            return;
        }

        if ((lostConnection)||(!socketConnection.Connected))
        {
            return;
        }

        try
        {
            // Get a stream object for writing. 			
            NetworkStream stream = socketConnection.GetStream();
            if (stream.CanWrite)
            {
                stream.Write(data, 0, size);
 //               Debug.Log("Client sent his message - should be received by server");
            }
        }
        catch (SocketException socketException)
        {
            Debug.Log("Socket exception: " + socketException);
        }
    }
    public bool isConnected()
    {
        return !lostConnection;
    }

}

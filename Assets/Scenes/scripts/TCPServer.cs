using System;
using System.Collections; 
using System.Collections.Generic; 
using System.Net; 
using System.Net.Sockets; 
using System.Text; 
using System.Threading; 
using UnityEngine;
using ComStructures;
using System.IO;


public class TCPServer : MonoBehaviour {  	
	#region private members 	
	/// <summary> 	
	/// TCPListener to listen for incomming TCP connection 	
	/// requests. 	
	/// </summary> 	
	private TcpListener tcpListener; 
	/// <summary> 
	/// Background thread for TcpServer workload. 	
	/// </summary> 	
	private Thread tcpListenerThread;  	
	/// <summary> 	
	/// Create handle to connected tcp client. 	
	/// </summary> 	
	private TcpClient connectedTcpClient;
	#endregion

	private messaging m_messager;
	private bool lostConnection = true;
	// public
	public bool forceCloseForTest = false;
	private int counter = 0;

	public void setMessager(messaging messager)
    {
		m_messager = messager;
	}

	// Use this for initialization
	void Start () {
		// Start TcpServer background thread 		
/*
		Debug.Log("Server Start");
		tcpListenerThread = new Thread (new ThreadStart(ListenForIncommingRequests)); 		
		tcpListenerThread.IsBackground = true; 		
		tcpListenerThread.Start(); 	
*/
	}  	
	
	// Update is called once per frame
	void Update () 
	{
		if (counter == 0) // so that the server starts quickly
		{
			if ((lostConnection) && (!forceCloseForTest))
			{
				// Start TcpServer background thread 		
				Debug.Log("Server Restart");
				lostConnection = false;
				// ReStart TcpServer background thread 		
				tcpListenerThread = new Thread(new ThreadStart(ListenForIncommingRequests));
				tcpListenerThread.IsBackground = true;
				tcpListenerThread.Start();
			}
		}

		counter++;
		if (counter==100)
			counter = 0;

	}

	private Byte[] bytes = new Byte[65000]; //messages_sizes.MAX_JSON_MESSAGE_SIZE+ messages_sizes.HEADER_SIZE];

	/// <summary> 	
	/// Runs in background TcpServerThread; Handles incomming TcpClient requests 	
	/// </summary> 	
	private void ListenForIncommingRequests () 
	{ 		
		try 
		{ 			
			// Create listener on localhost port 8052. 			
			tcpListener = new TcpListener(IPAddress.Parse("192.168.43.121"), 9005);   //  127.0.0.1   192.168.0.15 asnieres	10.0.1.34 bureau	x360 par point d'acces mobile 192.168.43.121 --- 192.168.0.25 x360 maison
			tcpListener.Start();              
			Debug.Log("Server is listening");              
			while (!forceCloseForTest) 
			{ 				
				using (connectedTcpClient = tcpListener.AcceptTcpClient()) 
				{ 					
					// Get a stream object for reading 					
					using (NetworkStream stream = connectedTcpClient.GetStream())  // blocking until someone connected
					{ 						
						int length;

						Debug.Log("Server is connected to a client");

						if (stream.CanRead)
						{
							// Read incomming stream into byte arrary. 						
							while ((!forceCloseForTest)&&((length = stream.Read(bytes, 0, bytes.Length)) != 0)) // blocking mode
							{
								m_messager.newRxMessage(bytes, length);
							}
						}
					} 				
				} 			
			}
			tcpListener.Stop();
			lostConnection = true;
			connectedTcpClient = null;
		} 		
		catch (SocketException socketException) 
		{
			lostConnection = true;
			connectedTcpClient = null;
			Debug.Log("My SocketException " + socketException.ToString()); 		
		}
		catch (IOException ioException)
		{
			tcpListener.Stop();
			lostConnection = true;
			connectedTcpClient = null;
			Debug.Log("My ioException " + ioException.ToString());
		}
	}
	/// <summary> 	
	/// Send message to client using socket connection. 
	/// all the complexity here is in order to be able to recover the connection if the client goes off/on
	/// </summary> 	
	public void SendMessage(byte[] data, int size)
    {
		if (forceCloseForTest)
		{
			if (connectedTcpClient != null)
			{
				if (connectedTcpClient.Connected)
					connectedTcpClient.Close();
			}
			connectedTcpClient = null;
			lostConnection = true;
			return;
		}

		try
		{
			if (connectedTcpClient == null)
			{
				Debug.Log("Server : no client connected yet or lost connection, can't com");
				return;
			}
		}
		catch(ObjectDisposedException ODException)
        {
			Debug.Log("My ObjectDisposedException: " + ODException);
		}
		catch (SocketException socketException)
		{
			Debug.Log("My Second Socket exception: " + socketException);
		}

		try 
		{ 			
			// Get a stream object for writing. 			
			NetworkStream stream = connectedTcpClient.GetStream(); 			
			if (stream.CanWrite) 
			{                 
				stream.Write(data, 0, size); 
//				Debug.Log("Server sent his message - should be received by client");           
			}
			else
				Debug.Log("Server Write : can't write on socket");
		}
		catch (SocketException socketException) 
		{             
			Debug.Log("Socket exception: " + socketException);         
		}
		catch (ObjectDisposedException ODException)
		{
			if (counter==0)
				Debug.Log("My ObjectDisposedException: " + ODException);
		}
	} 

	public bool isConnected()
    {
		return !lostConnection;
	}
}
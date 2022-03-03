using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using VinteR.Model.Gen;

public class VinterReciver : MonoBehaviour
{
    //private readonly float roomscale = 0.001f;
   
	[Tooltip("The Port to listen on. Must be identical to configured receiver port in VinteR")]
	public int port = 3457;
    private IPEndPoint OptiTRackEndPoint;
    private UdpClient OptiTrackClient;
    private Thread OptiTrackListener;
    private MocapFrame currentMocapFrame;

    private CancellationTokenSource _cancellationToken;

    void Start()
    {
        Debug.Log("Starting OptiTrack Listener...");
		OptiTRackEndPoint = new IPEndPoint(IPAddress.Any, port);
        OptiTrackClient = new UdpClient(OptiTRackEndPoint);
        OptiTrackListener = new Thread(new ThreadStart(ReceiveOptiTrackData));
        OptiTrackListener.IsBackground = true;
        OptiTrackListener.Start();
        Debug.Log("Done!");
        _cancellationToken = new CancellationTokenSource();
    }
    
    void ReceiveOptiTrackData()
    {
        Debug.Log("Listening...");
        while (!_cancellationToken.IsCancellationRequested)
        {
            try
            {
                //Debug.Log(OptiTrackClient.ToString());
                var data = OptiTrackClient.Receive(ref OptiTRackEndPoint);
                currentMocapFrame = MocapFrame.Parser.ParseFrom(data);
            }
            catch (Exception e)
            {
                Debug.LogError("Receive data error " + e.Message);
                OptiTrackClient.Close();
                return;
            }
            Thread.Sleep(1);
        }
    }

    public MocapFrame getCurrentMocapFrame()
    {
        if (currentMocapFrame != null)
        {
            return currentMocapFrame.Clone();
        }
        else
        {
            return null;
        }
        
    }

    private void OnDestroy()
    {   
        _cancellationToken.Cancel();
        OptiTrackListener.Abort();
        if (OptiTrackClient != null)
            OptiTrackClient.Close();
        Debug.Log("Disconnected from server");
    }

    private void OnQuitApplication()
    {
        OptiTrackListener.Abort();
        OptiTrackClient.Close();
    }
}

using UnityEngine;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Text;

public class UAVClient : MonoBehaviour
{
    public Camera uavCamera;
    public string serverIP = "192.168.1.104"; // OrangePI server IP address
    public int port = 5000;

    private RenderTexture renderTexture;
    private Texture2D texture2D;
    private TcpClient client;
    private NetworkStream stream;
    private BinaryReader reader;
    private BinaryWriter writer;
    
    private string lastCommand;

    // UAV movement
    private Rigidbody rb;
    private float moveSpeed = 2f;
    private float smoothing = 0.1f;

    // mission
    public GameObject box;
    private bool boxDropped;
    private bool missionComplete;
    private Vector3 initialPosition;

    
    // wander
    private float zigzagAngle = 25f;
    private float zigzagTime = 2.5f;
    private float wanderSpeed = 2f;

    private float timer = 0f;
    private bool isZigzaggingLeft = true;


    void Start()
    {
        SetupConnection();
        renderTexture = new RenderTexture(256, 256, 24);
        uavCamera.targetTexture = renderTexture;
        texture2D = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);

        rb = GetComponent<Rigidbody>();
        timer = zigzagTime;
        initialPosition = transform.position;
    }

    void Update()
    {
        if (boxDropped && !missionComplete)
        {
            SendFrameToServer();
            ReturnToBase();
        }
        else if (client != null && client.Connected)
        {
            SendFrameToServer();
            ReceiveControlCommand();
            
            if (!missionComplete)
            {
                ProcessControlCommand(lastCommand);
            }

        }
        
    }

    void SetupConnection()
    {
        try
        {
            client = new TcpClient(serverIP, port);
            stream = client.GetStream();
            reader = new BinaryReader(stream);
            writer = new BinaryWriter(stream);
            Debug.Log("Connected to server");
        }
        catch (SocketException e)
        {
            Debug.LogError("SocketException: " + e.Message);
        }
    }

    void SendFrameToServer()
    {
        // Read the Render Texture into the Texture2D
        RenderTexture.active = renderTexture;
        texture2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        texture2D.Apply();

        // Encode Texture2D to JPEG
        byte[] frameBytes = texture2D.EncodeToJPG();

        // Send the length of the data followed by the actual image data
        writer.Write(frameBytes.Length);
        writer.Write(frameBytes);
    }

    void ReceiveControlCommand()
    {
        byte[] commandBuffer = new byte[1024];
        int bytesRead = stream.Read(commandBuffer, 0, commandBuffer.Length);
        if (bytesRead > 0)
        {
            lastCommand = Encoding.UTF8.GetString(commandBuffer, 0, bytesRead).Trim();
            // Debug.Log($"Received command: {lastCommand}");
        }
        else
        {
            Debug.Log("No command received.");
        }
    }
   
    void ProcessControlCommand(string command)
    {
        var targetVelocity = Vector3.zero;

        switch (command)
        {
            case "FORWARD":
                targetVelocity += transform.forward * moveSpeed;
                break;
            case "BACK":
                targetVelocity -= transform.forward * moveSpeed;
                break;
            case "LEFT":
                targetVelocity -= transform.right * moveSpeed;
                break;
            case "RIGHT":
                targetVelocity += transform.right * moveSpeed;
                break;
            case "UP":
                targetVelocity += transform.up * moveSpeed;
                break;
            case "DOWN":
                targetVelocity -= transform.up * moveSpeed;
                break;
            case "DROP":
                DropBox();
                break;
            case "WANDER":
                ZigzagWander();
                break;
        }

        rb.velocity = Vector3.Lerp(rb.velocity, targetVelocity, smoothing);
    }

    void DropBox()
    {
        Debug.Log("DROP BOX");
        boxDropped = true;

        box.GetComponent<Rigidbody>().isKinematic = false;
        
        box.transform.SetParent(null);
    }

    
    void ReturnToBase()
    {
        Vector3 directionToBase = initialPosition - transform.position;
        float distanceToBase = directionToBase.magnitude;

        if (distanceToBase > 1.0f) // Adjust tolerance as needed
        {
            directionToBase.Normalize();
            rb.velocity = directionToBase * moveSpeed;
        }
        else
        {
            rb.velocity = Vector3.zero;
            missionComplete = true;
        }
    }


    void ZigzagWander()
    {
        // Update the timer
        timer -= Time.deltaTime;

        // Switch directions when the timer runs out
        if (timer <= 0f)
        {
            isZigzaggingLeft = !isZigzaggingLeft;
            timer = zigzagTime;
        }

        // Calculate zigzag direction
        Vector3 zigzagDirection;
        if (isZigzaggingLeft)
        {
            zigzagDirection = Quaternion.Euler(0, -zigzagAngle, 0) * transform.forward;
        }
        else
        {
            zigzagDirection = Quaternion.Euler(0, zigzagAngle, 0) * transform.forward;
        }

        // Move the drone in the zigzag direction
        rb.velocity = zigzagDirection * wanderSpeed;
    }


    void OnApplicationQuit()
    {
        if (reader != null) reader.Close();
        if (writer != null) writer.Close();
        if (stream != null) stream.Close();
        if (client != null) client.Close();
    }
}

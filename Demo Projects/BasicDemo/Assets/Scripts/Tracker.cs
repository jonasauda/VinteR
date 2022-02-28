using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Instrumentation;
//using Assets;
using UnityEngine;
using UnityEngine.iOS;
using VinteR.Model.Gen;

public class Tracker : MonoBehaviour
{
	[Tooltip("Gameobject with Vinter Client Script")]
	public VinterReciver VinterReciver;
	[Tooltip("Position offset from the tracked Centroid")]
	public Vector3 offset;
	
	[Tooltip("If and how to dampen position and roation")]
	public DampeningFunktion dampeningFunktion = DampeningFunktion.none;
	[Tooltip("Number of Frames to dampen over")]
	public int dampeningBufferSize;

	public enum DampeningFunktion
	{
		none,
		mean,
		median
	}
	
	[Tooltip("If true, the Map will be initially set to the position of this object")]
	public bool isInitPoint = false;
	[Tooltip("Initial map offset")]
	public Vector3 initOffset;
	[Tooltip("The Map to initiate")]
	public GameObject Map;
	
	[Tooltip("If the tracked object should be rotated")]
	public bool positionOnly = false;
	[Tooltip("If the tracked object is a LeapMotion Hand Rig")]
	public bool isLeapHands = false;
	[Tooltip("Motive Name of the RigidBody")]
	public String MotiveName;
	
	private Vector3 _position;
	private Vector3 _rotation;
	
	private int frameCounter;
	private Vector3[] positionBuffer;
	private Vector3[] rotationBuffer;
	private int lastdampeningBufferSize;
	
	private bool _initMapPosition = true;
	private bool _isStartSet = false;

	// Use this for initialization
	void Start () {
	
		frameCounter = 0;
		
		positionBuffer = new Vector3[dampeningBufferSize];
		rotationBuffer = new Vector3[dampeningBufferSize];
	}

	void FixedUpdate()
	{
		var mocapFrame = VinterReciver.getCurrentMocapFrame();
		if (mocapFrame != null)
		{
			var body = mocapFrame.Bodies.SingleOrDefault(b => b.Name.Equals(MotiveName));
			if (body != null)
			{
				// Extract position and rotation from MocapFrame
				_position = new Vector3(-body.Centroid.X * 0.001f + offset.x, body.Centroid.Y * 0.001f + offset.y,
					body.Centroid.Z * 0.001f + offset.z);

				var quaternion = new Quaternion(-body.Rotation.X, body.Rotation.Y, body.Rotation.Z, -body.Rotation.W);
				_rotation = new Vector3(quaternion.eulerAngles.x, quaternion.eulerAngles.y, quaternion.eulerAngles.z);


				if (isInitPoint && _initMapPosition)
				{
					// init the Map to the position of the tracked object
					_initMapPosition = false;

					Map.transform.position = _position + initOffset;
					Map.transform.rotation = Quaternion.Euler(0, quaternion.eulerAngles.y, 0);
				}

				switch (dampeningFunktion)
				{
					case DampeningFunktion.mean:
						wirteToBuffer();
						mean();
						break;
					case DampeningFunktion.median:
						wirteToBuffer();
						median();
						break;
					default:
						setTransform();
						break;
				}
			}
		}
	}

	private void wirteToBuffer()
	{
		// Reset the arrays when the number of frames to damp ofer is changed
		if (dampeningBufferSize != lastdampeningBufferSize)
		{
			positionBuffer = new Vector3[dampeningBufferSize];
			rotationBuffer = new Vector3[dampeningBufferSize];
		}
			
		// insert new position and rotation in the buffer
		frameCounter = frameCounter % dampeningBufferSize;
		positionBuffer[frameCounter] = _position;
		rotationBuffer[frameCounter] = _rotation;
		lastdampeningBufferSize = dampeningBufferSize;
		frameCounter++;
	}
	
	private void setTransform()
	{
		transform.position = _position;
		if (!positionOnly && !isLeapHands)
		{
			transform.rotation = Quaternion.Euler(_rotation);
		}
		else if (isLeapHands)
		{
			transform.position = new Vector3(_position.x,0, _position.z);
			transform.rotation = Quaternion.Euler(0, _rotation.y, 0);
		}
		else if (_isStartSet)
		{
			transform.position = _position;
			transform.rotation = Quaternion.Euler(0, _rotation.y, 0);
			_isStartSet = false;
		}
	}

	private void mean()
	{
		float posX = 0f;
		float posY = 0f;
		float posZ = 0f;
		foreach (Vector3 position in positionBuffer)
		{
			posX += position.x;
			posY += position.y;
			posZ += position.z;
		}
		_position = new Vector3(posX, posY, posZ) / dampeningBufferSize;
                				
		float rotX = 0f;
		float rotY = 0f;
		float rotZ = 0f;
		foreach (Vector3 rotation in rotationBuffer)
		{
			rotX += rotation.x;
			rotY += rotation.y;
			rotZ += rotation.z;
		}
		_rotation = new Vector3(rotX, rotY, rotZ) / dampeningBufferSize;
		setTransform();
	}

	private void median()
	{
		float[] posX = new float[positionBuffer.Length];
		float[] posY = new float[positionBuffer.Length];
		float[] posZ = new float[positionBuffer.Length];
		
		float[] rotX = new float[positionBuffer.Length];
		float[] rotY = new float[positionBuffer.Length];
		float[] rotZ = new float[positionBuffer.Length];
		
		for (int i = 0; i < positionBuffer.Length; i++)
		{
			posX[i] = positionBuffer[i].x;
			posY[i] = positionBuffer[i].y;
			posZ[i] = positionBuffer[i].z;
			
			rotX[i] = rotationBuffer[i].x;
			rotY[i] = rotationBuffer[i].y;
			rotZ[i] = rotationBuffer[i].z;
		}
		
		Array.Sort(posX);
		Array.Sort(posY);
		Array.Sort(posZ);
		
		Array.Sort(rotX);
		Array.Sort(rotY);
		Array.Sort(rotZ);
		
		
		int m = (int) (positionBuffer.Length / 2);
		
		if (positionBuffer.Length % 2 == 0)
		{
			_position = new Vector3(
				(posX[m] + posX[m-1])/2,
				(posY[m] + posY[m-1])/2,
				(posZ[m] + posZ[m-1])/2
				);
			
			_rotation =new Vector3(
				(rotX[m] + rotX[m-1])/2,
				(rotY[m] + rotY[m-1])/2,
				(rotZ[m] + rotZ[m-1])/2
				);
		}
		else
		{
			_position = new Vector3(posX[m], posY[m], posZ[m]);
			_rotation = new Vector3(rotX[m], rotY[m], rotZ[m]);
		}
		setTransform();
	}
}

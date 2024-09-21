using UnityEngine;
using System.Collections;
using UnityEngine.Audio;

public class Casing : MonoBehaviour {

	[Header("Force X")]
	[Tooltip("Minimum force on X axis")]
	public float minimumXForce = 25f;		
	[Tooltip("Maimum force on X axis")]
	public float maximumXForce = 40f;
	[Header("Force Y")]
	[Tooltip("Minimum force on Y axis")]
	public float minimumYForce = 10f;
	[Tooltip("Maximum force on Y axis")]
	public float maximumYForce = 20f;
	[Header("Force Z")]
	[Tooltip("Minimum force on Z axis")]
	public float minimumZForce = -12f;
	[Tooltip("Maximum force on Z axis")]
	public float maximumZForce = 12f;
	[Header("Rotation Force")]
	[Tooltip("Minimum initial rotation value")]
	public float minimumRotation = -360f;
	[Tooltip("Maximum initial rotation value")]
	public float maximumRotation = 360f;
	[Header("Despawn Time")]
	[Tooltip("How long after spawning that the casing is destroyed")]
	public float despawnTime = 1f;

	[Header("Audio")]
	public AudioClip[] casingSounds;
	public AudioSource audioSource;

	[Header("Spin Settings")]
	//How fast the casing spins
	[Tooltip("How fast the casing spins over time")]
	public float speed = 2500.0f;

	//Launch the casing at start
	private void Awake () 
	{
		//Random rotation of the casing
		GetComponent<Rigidbody>().AddRelativeTorque (
			Random.Range(minimumRotation, maximumRotation), //X Axis
			Random.Range(minimumRotation, maximumRotation), //Y Axis
			Random.Range(minimumRotation, maximumRotation)  //Z Axis
			* Time.deltaTime);

		//Random direction the casing will be ejected in
		GetComponent<Rigidbody>().AddRelativeForce (
			Random.Range (minimumXForce, maximumXForce),  //X Axis
			Random.Range (minimumYForce, maximumYForce),  //Y Axis
			Random.Range (minimumZForce, maximumZForce)); //Z Axis		     
	}

	private void Start ()
    {
		//Destroy casings after some time
        Destroy(gameObject, despawnTime);

		//Set random rotation at start
		transform.rotation = Random.rotation;

        //Start play sound
        PlaySound();
	}

	private void FixedUpdate () 
	{
		//Spin the casing based on speed value
		transform.Rotate (Vector3.right, speed * Time.deltaTime);
		transform.Rotate (Vector3.down, speed * Time.deltaTime);
	}

	private void PlaySound () 
	{
		//Get a random casing sound from the array 
		audioSource.clip = casingSounds
			[Random.Range(0, casingSounds.Length)];

		//play after short time
		audioSource.PlayDelayed(Random.Range(0.25f, 0.85f));
	}
}
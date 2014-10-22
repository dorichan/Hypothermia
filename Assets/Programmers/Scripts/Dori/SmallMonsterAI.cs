﻿/* This script is responsible for controlling the small monster.
 * Will "pop" out of the ground when the player is near, will give chase
 * up to a certain distance, but will eventually fall back down into a
 * hole when the player is out of range. Will also fall back down into a 
 * hole when it's killed, and the game object will destroy itself after
 * a certain period of time.
 */ 

using UnityEngine;
using System.Collections;

public class SmallMonsterAI : MonoBehaviour 
{
	// Declare variables here, and set up values in Start()
	public bool isJump;
	public bool isAwake;
	public bool isDead;
	public float health;
	public float speed;
	public float pop;
	public float maxPop;
	public float power;
	public float radius;
	public float deadTimer;

	public GameObject cubes;
	public Transform target;

	private State currentState;
	private float chaseDistance;
	private float attackDistance;

	// Private references
	private SphereCollider sphereCollider;
	private SmallMonsterSpawner spawner;
	private Health player;

	// Set to public, but still not showing up in the inspector - what's up with that
	public enum State {
		Idle,
		Chase,
		Attack
	};

	void Start()
	{
		// Set up variables and references
		isAwake = false;
		health = 100.0f;
		speed = 3.0f;
		pop = 15.0f;
		maxPop = 0.3f;
		power = 3.0f;
		radius = 5.0f;
		chaseDistance = 10.0f;
		attackDistance = 5.0f;
		deadTimer = 0.0f;

		gameObject.rigidbody.isKinematic = true;
		gameObject.collider.enabled = false;

		sphereCollider = GetComponent<SphereCollider> ();
		sphereCollider.radius = chaseDistance;
		spawner = GameObject.FindWithTag ("Spawner").GetComponent<SmallMonsterSpawner> ();
		player = GameObject.FindWithTag ("Player").GetComponent<Health> ();
	}

	void Update()
	{
		// Popping out of the ground 
		if(isJump && !isDead) {
			// Set rigidbody.isKinematic to false, so that gravity can be used. Previously turned false to keep below surface.
			gameObject.rigidbody.isKinematic = false;
			gameObject.rigidbody.AddForce(Vector3.up * pop);

			if(!isAwake){
				// Creating ice blocks that the monster shoots out of the ground
				GameObject clone = Instantiate(cubes, transform.position, Quaternion.identity) as GameObject;
				clone.tag = "IceBlock";

				GameObject[] clones = GameObject.FindGameObjectsWithTag("IceBlock");
				foreach(var cube in clones) {
					cube.transform.localScale = new Vector3(Random.Range (0.2f, 0.5f), Random.Range (0.2f, 0.5f), Random.Range (0.2f, 0.5f));
					cube.rigidbody.AddExplosionForce (power, Vector3.up, radius, 3.0f, ForceMode.Impulse);
					Destroy(cube, 3.0f);
				}
			}

			if(gameObject.transform.position.y > maxPop) {
				gameObject.collider.enabled = true;
				gameObject.rigidbody.AddForce(-Vector3.up * pop);
				isJump = false;
				isAwake = true;
			}
		}

		// Fall back into the ground if the player is out of range or the monster is dead
		if (currentState == State.Idle || isDead) {
			gameObject.collider.enabled = false;
			isAwake = false;

			if(gameObject.transform.position.y < -1.0f) {
				gameObject.rigidbody.isKinematic = true;
			}
		}
	
		// Chase State (if the player are within range)
		if(currentState == State.Chase && !isDead) {
			target = GameObject.FindGameObjectWithTag ("Player").transform;

			// Get the direction to travel in by subtracting the target's position with the monster's position. Set y to a negative value
			Vector3 direction = target.transform.position - transform.position;
			direction.y = -1.0f;

			// Look in the direction of the player
			transform.LookAt(target);

			// Keep moving if the player is out of attack range
			if(direction.magnitude > attackDistance) {
				Vector3 moveDirection = direction.normalized * speed * Time.deltaTime;
				transform.position += moveDirection;
			}

			// Stop moving and switch to attack mode
			if(direction.magnitude <= attackDistance) {
				isJump = true;
				currentState = State.Attack;
				direction.x = 0.0f;
			}
		}

		if(currentState == State.Attack) {
			// Look at the player
			transform.LookAt(target);

			// Shoot a raycast forward, and if it's a hit - blow ice onto the player
			RaycastHit hit;
			if(Physics.Raycast(transform.position, Vector3.forward, out hit, attackDistance)) {
				if(hit.transform.gameObject == GameObject.FindWithTag("Player")) {
					player.SendMessage("OnDamage", 10.0f);
				}
			}
		}

		// If the monster is dead, set isDead to true - this will keep the monster from popping back up
		if(health <= 0.0f) {
			isDead = true;
			Debug.Log (gameObject.name + " is dead"); 
			spawner.Remove(gameObject.name);
		}

		if(isDead) {
			deadTimer += 1.0f * Time.deltaTime;

			if (deadTimer > 3.0f) {
				deadTimer = 0;
				Destroy (gameObject); // Destroy the dead monster and free up space for a new monster
			}
		}
	}

	void FixedUpdate()
	{
		// Ignore collisions with the ice blocks on layer 8
		Physics.IgnoreLayerCollision (0, 8, true);
	}

	void OnTriggerStay (Collider other) 
	{
		if (other.gameObject == GameObject.FindWithTag ("Player")) {
			currentState = State.Chase;
		}
	}

	void OnTriggerExit (Collider other) 
	{
		if (other.gameObject == GameObject.FindWithTag ("Player")) {
			currentState = State.Idle;
		}
	}
}
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Data/MovementStats")]
public class MovementStats : ScriptableObject {
	public float runSpeed;
	public float groundAccel;
	public float airAccel;
	public float jumpSpeed;
	public float airFriction;
	public float shortHopCutoff;
	public float maxFallSpeed;
}

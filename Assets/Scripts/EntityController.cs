using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(PlayerInput))]
public class EntityController : EntityBase {
	public MovementStats movement;
	PlayerInput input;

	#pragma warning disable 0649
	[SerializeField] GameObject playerRig;
	[SerializeField] AudioResource jumpNoise;
	#pragma warning restore 0649

	float inputX;
	bool inAttack;
	bool speeding;
	const float bufferDuration = 0.1f;
	const float fModRecoveryTime = 1.5f;

	float airControlMod = 1;
	float fMod = 1;

	bool movingForwards;
	bool movingBackwards;
	bool inputBackwards;
	bool inputForwards;

	float angleLastStep = 0;

	float jumpTime = 0;

	ToonMotion toonMotion;
	float landingRecovery = 0;
	
	public bool frozeInputs { 
		get {
			return _frozeInputs || stunned;
		} 
		private set {
			_frozeInputs = value;
		}
	}
	[SerializeField] private bool _frozeInputs;

	protected override void Awake() {
		base.Awake();
		input = GetComponent<PlayerInput>();
		toonMotion = GetComponentInChildren<ToonMotion>();
	}

	protected override void Update() {
		base.Update();
		Move();
		Jump();
		CheckFlip();
		RotateToGround();
		UpdateAnimator();
	}

	void FixedUpdate() {
		groundCheck.Check();
		ApplyMovement();
		angleLastStep = groundData.normalRotation;
	}


	void Move() {
		inputX = input.HorizontalInput();
		inputBackwards = input.HasHorizontalInput() && input.HorizontalInput()*Forward() < 0;
		inputForwards = input.HasHorizontalInput() && !inputBackwards;
		movingBackwards = Mathf.Abs(rb2d.velocity.x) > 0.01 && rb2d.velocity.x * -transform.localScale.x < 0;
		movingForwards = input.HasHorizontalInput() && ((facingRight && rb2d.velocity.x > 0) || (!facingRight && rb2d.velocity.x < 0));
		airControlMod = Mathf.MoveTowards(airControlMod, 1, 0.5f * Time.deltaTime);

		// allow moving during air attacks
		if (frozeInputs && !(inAttack && !groundData.grounded)) {
			inputX = 0;
		}

		if (groundData.leftGround) {
			// the player can initiate walltouch on the ground
			// and ground movement can override the wallflip
			if (wallData.touchingWall) {
				FlipToWall();
			}
        }

		// stop at the end of ledges (but allow edge canceling)
		if (groundData.ledgeStep && !speeding && !input.HasHorizontalInput()) {
			rb2d.velocity = new Vector2(0, rb2d.velocity.y);
		}

		// don't slide on slopes
		if (!movingBackwards && !movingForwards && groundData.normalRotation != 0) {
			if (rb2d.velocity.sqrMagnitude < 0.1) {
				rb2d.velocity = Vector2.zero;
			}
		}
	}

	void Jump() {
		if (groundData.grounded && input.ButtonDown(Buttons.JUMP)) {
			rb2d.velocity = new Vector2(rb2d.velocity.x, movement.jumpSpeed);
			JumpDust();
			jumpTime = Time.unscaledTime;
			animator.SetTrigger("Jump");
		}
	}

	void ApplyMovement() {
		speeding = Mathf.Abs(rb2d.velocity.x) > movement.runSpeed;

		void SlowOnFriction() {
            float f = groundData.grounded ? groundData.groundCollider.friction : movement.airFriction;
            rb2d.velocity = new Vector2(rb2d.velocity.x * (1 - (f*f*fMod)), rb2d.velocity.y);
        }

		if ((groundData.grounded || groundData.leftGround) && (angleLastStep != groundData.normalRotation) && (Time.unscaledTime - jumpTime > 0.5f)) {
			// if they've just moved onto a lower slope
			// todo: check if they haven't just jumped
			float a = groundData.normalRotation - angleLastStep;
			rb2d.velocity = rb2d.velocity.Rotate(a);
		}

        if (inputX!=0) {
			if (!speeding || (movingForwards && inputBackwards) || (movingBackwards && inputForwards)) {
				if (groundData.grounded) {
					// if ground is a platform that's been destroyed/disabled
					float f = groundData.groundCollider != null ? groundData.groundCollider.friction : movement.airFriction;
					Vector2 v = Vector2.right * rb2d.mass * movement.groundAccel * inputX * f*f;
					v = v.Rotate(groundData.normalRotation);
					rb2d.AddForce(v);
				} else {	
					float attackMod = inAttack ? 0.5f : 1f;
					Vector2 v = Vector2.right * rb2d.mass * movement.airAccel * inputX * airControlMod * attackMod;
					rb2d.AddForce(v);
				}
			}
        } else {
            // if no input, slow player
            if (groundData.grounded) {
                if (Mathf.Abs(rb2d.velocity.x) > 0.05f) {
                    SlowOnFriction();
                } else {
                    rb2d.velocity = new Vector2(0, rb2d.velocity.y);
                }
            }
        }

		Debug.DrawLine(transform.position, transform.position + (Vector3) Vector2.up.Rotate(groundData.normalRotation), Color.red);

        if (speeding) {
            SlowOnFriction();
        }

		if (!groundData.grounded && rb2d.velocity.y < movement.maxFallSpeed && !inAttack) {
			rb2d.velocity = new Vector2(rb2d.velocity.x, movement.maxFallSpeed);
		}

		// if falling down through a platform (ground distance above feet)
		// that distance can vary due to physics and/or float precision
		if (rb2d.velocity.y<0 && (groundData.distance)<collider2d.bounds.extents.y) {
			// then snap to its top
			float diff = collider2d.bounds.extents.y - groundData.distance;
			// rb2d.MovePosition(rb2d.position + ((diff+0.1f) * Vector2.up));
			// cancel downward velocity
			rb2d.velocity = new Vector2(
				rb2d.velocity.x,
				0.1f
			);
		}
	}

	void UpdateAnimator() {
        animator.SetBool("Grounded", groundData.grounded);
        animator.SetFloat("YSpeed", rb2d.velocity.y);
        animator.SetFloat("XSpeedMagnitude", Mathf.Abs(rb2d.velocity.x));

		if (groundData.hitGround) {
			landingRecovery = -1;
		}

		landingRecovery = Mathf.MoveTowards(landingRecovery, 0, 4f * Time.deltaTime);
		animator.SetFloat("LandingRecovery", landingRecovery);
    }

	void CheckFlip() {
		if (frozeInputs) return;
		if (inputBackwards && movingForwards) return;

        if (facingRight && inputX<0) {
            Flip();
        } else if (!facingRight && inputX>0) {
            Flip();
        }
    }

	void RotateToGround() {
		// if they flip on a slope, we want the rotation to instantly snap
		if (groundData.grounded) {
			float curr = playerRig.transform.eulerAngles.z;
			float dest = groundData.normalRotation;
			float a = Mathf.MoveTowardsAngle(curr, dest, 360 * Time.deltaTime);

			if (Time.unscaledTime < flipTime+0.2f) {
				a = groundData.normalRotation;
			}
			playerRig.transform.rotation = Quaternion.Euler(new Vector3(0, 0, a));
		} else {
			playerRig.transform.rotation = Quaternion.identity;
		}
	}
}

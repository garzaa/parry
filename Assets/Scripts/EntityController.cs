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

	ToonMotion toonMotion;
	
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
	}

	void Jump() {
		if (groundData.grounded && input.ButtonDown(Buttons.JUMP)) {
			rb2d.velocity = new Vector2(rb2d.velocity.x, movement.jumpSpeed);
		}
	}

	void FixedUpdate() {
		ApplyMovement();
	}

	void ApplyMovement() {
		speeding = Mathf.Abs(rb2d.velocity.x) > movement.runSpeed;

		void SlowOnFriction() {
            float f = groundData.grounded ? groundData.groundCollider.friction : movement.airFriction;
            rb2d.velocity = new Vector2(rb2d.velocity.x * (1 - (f*f*fMod)), rb2d.velocity.y);
        }

        if (inputX!=0) {
			if (!speeding || (movingForwards && inputBackwards) || (movingBackwards && inputForwards)) {
				if (groundData.grounded) {
					// if ground is a platform that's been destroyed/disabled
					float f = groundData.groundCollider != null ? groundData.groundCollider.friction : movement.airFriction;
					rb2d.AddForce(Vector2.right * rb2d.mass * movement.groundAccel * inputX * f*f);
				} else {	
					float attackMod = inAttack ? 0.5f : 1f;
					rb2d.AddForce(Vector2.right * rb2d.mass * movement.airAccel * inputX * airControlMod * attackMod);
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

        if (speeding) {
            SlowOnFriction();
        }

		if (rb2d.velocity.y < movement.maxFallSpeed && !inAttack) {
			rb2d.velocity = new Vector2(rb2d.velocity.x, movement.maxFallSpeed);
		}

		// if falling down through a platform (ground distance above feet)
		// that distance can vary due to physics and/or float precision
		if (rb2d.velocity.y<0 && (groundData.distance)<collider2d.bounds.extents.y) {
			// then snap to its top
			float diff = collider2d.bounds.extents.y - groundData.distance;
			rb2d.MovePosition(rb2d.position + ((diff+0.1f) * Vector2.up));
			// cancel downward velocity
			rb2d.velocity = new Vector2(
				rb2d.velocity.x,
				0.1f
			);
		}
	}

	void FlipToWall() {
		if (inAttack) return;
		if (facingRight && wallData.direction>0) {
			Flip();
		} else if (!facingRight && wallData.direction<0) {
			Flip();
		}
		toonMotion?.ForceUpdate();
	}
}

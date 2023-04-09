using UnityEngine;
using System.Collections.Generic;
using Rewired;
using System.Linq;

public class PlayerInput : MonoBehaviour {
    Player player = null;

    Controller lastActiveController;

    [SerializeField] bool humanControl = false;
    public bool isHuman => humanControl;

    public ComputerController comControl { get; private set; }

	void Awake() {
        player = ReInput.players.GetPlayer(0);
        comControl = new ComputerController();
    }

    void Start() {
        player.AddInputEventDelegate(ShowHideMouse, UpdateLoopType.Update);
        player.controllers.hasKeyboard = true;
        player.controllers.AddController(ControllerType.Joystick, 0, true);
    }

    void LateUpdate() {
        comControl.LateUpdate();
    }

    public Player GetPlayer() {
        return player;
    }

    public void EnableHumanControl() {
        humanControl = true;
    }

    public void DisableHumanControl() {
        humanControl = false;
    }

    void ShowHideMouse(InputActionEventData actionData) {
        if (!humanControl) return;
        lastActiveController = player.controllers.GetLastActiveController();
        Cursor.visible = (lastActiveController?.type == Rewired.ControllerType.Mouse);
    }

    public float GetAxis(int axisId) {
        if (humanControl) return player.GetAxis(axisId);
        else return comControl.GetAxis(axisId);
    }

    public bool ButtonDown(int b) {
        if (humanControl) return player.GetButtonDown(b);
        else return comControl.GetButtonDown(b);
    }

    public bool Button(int b) {
        if (humanControl) return player.GetButton(b);
        else return comControl.GetButton(b);
    }

    public bool ButtonUp(int b) {
        if (humanControl) return player.GetButtonUp(b);
        else return comControl.GetButtonUp(b);
    }

    public bool HasHorizontalInput() {
        return HorizontalInput() != 0;
    }

    public float HorizontalInput() {
        return GetAxis(Buttons.H_AXIS);
    }

    public bool GenericContinueInput() {
        return (
            ButtonDown(Buttons.JUMP)
        );
    }

    public static float GetInputBufferDuration() {
		return 2f/10f;
        // return SaveManager.save.options.inputBuffer * (1f/16f);
    }

    public static bool IsHorizontal(int actionID) {
		return actionID == RewiredConsts.Action.Horizontal;
	}

    public static bool IsAttack(int actionID) {
        return 
            actionID == RewiredConsts.Action.Attack
        ;
    }

    public static PlayerInput GetPlayerOneInput() {
		return GameObject.FindObjectsOfType<PlayerInput>()
			.Where(x => x.humanControl)
			.First();
	}
}

public static class Buttons {
    public static readonly int H_AXIS = RewiredConsts.Action.Horizontal;
    public static readonly int JUMP   = RewiredConsts.Action.Jump;
    public static readonly int PARRY  = RewiredConsts.Action.Parry;

}

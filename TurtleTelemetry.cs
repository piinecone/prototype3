using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TurtleTelemetry : MonoBehaviour {

  [SerializeField]
  private TurtleStateController stateController;
  [SerializeField]
  private TurtleMovementController movementController;

  public bool Enabled = false;

  private Vector3 lastKnownPosition = Vector3.zero;

  private GUIStyle style = new GUIStyle();
  private int width = 160;
  private int height = 64;
  private string description = "";

  void Start(){
    lastKnownPosition = transform.position;
    style.normal.textColor = Color.gray;
  }

  void OnGUI(){
    if (!Enabled) return;

    GUI.Box(new Rect(8, 8, 340, 140), "Telemetry (T to hide)");
    GUI.Label(new Rect(16, 40, 400, 140), description, style);
  }

  void Update(){
    if (Input.GetKeyDown(KeyCode.T)) Enabled = !Enabled;
  }

  void FixedUpdate(){
    description =  string.Format("Velocity:              {0}", velocity());
    //description += String.Format("\nSpeed: {0}", speed());
    description += string.Format("\nAcceleration:        {0}", acceleration());
    description += string.Format("\nState:                  {0}", state());
    description += string.Format("\nPrevious State:    {0}", previousState());
    description += string.Format("\nSpecial Move:      {0}", move());
  }

  private float velocity(){
    float velocity = Vector3.Distance(transform.position, lastKnownPosition) / Time.deltaTime;
    lastKnownPosition = transform.position;
    return velocity;
  }

  //private float speed(){
  //  float speed;
  //  return speed;
  //}

  private float acceleration(){
    return movementController.CurrentAcceleration();
  }

  private string state(){
    return stateController.CurrentState();
  }

  private string previousState(){
    return stateController.PreviousState();
  }

  private string move(){
    return movementController.CurrentSpecialMove();
  }
}

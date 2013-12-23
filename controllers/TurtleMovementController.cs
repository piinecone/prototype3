using UnityEngine;
using System.Collections;

[RequireComponent(typeof (Animator))]
[RequireComponent(typeof (CapsuleCollider))]
[RequireComponent(typeof (Rigidbody))]
[RequireComponent(typeof (TurtleStateController))]

public class TurtleMovementController : MonoBehaviour {
  private Animator animator;
  private CapsuleCollider capsuleCollider;
  private CharacterController characterController;
  private TurtleStateController stateController;

  // configuration options
  [SerializeField]
  private bool invertedYAxis = false;
  [SerializeField]
  private float waterSurfaceLevel = 160f;
  [SerializeField]
  private float gravity = 15f;

  // stabilize the look position when the avatar is partially submerged
  private float currentYValueOfLookPosition = 0f;

  // swim

  // walk
  Vector3 defaultTerrainRay = Vector3.down;
  float defaultSlope = 90f;
  private Vector3 moveDirection = Vector3.zero;

  // input
  private Vector3 mouseInput;
  private Vector3 keyboardInput;
  private float rawForwardValue = 0f;
  private float rawHorizontalValue = 0f;
  private float rawPitchValue = 0f;
  private float rawYawValue = 0f;
  private float rawRollValue = 0f;

  void Start () {
    animator = GetComponent<Animator>();
    capsuleCollider = GetComponent<CapsuleCollider>();
    characterController = GetComponent<CharacterController>();
    stateController = GetComponent<TurtleStateController>();
  }

  void Update () {
    mapInputParameters();

    if (stateController.PlayerIsInWater())
      handleMovementInWater();
    else if (stateController.PlayerIsOnLand())
      walk(slope: defaultSlope, terrainRay: defaultTerrainRay);
  }

  private void mapInputParameters(){
    // mouse input
    Vector3 mousePosition = Input.mousePosition;
    Ray mouseRay = Camera.main.ScreenPointToRay(mousePosition);
    mouseInput = mouseRay.direction;

    // keyboard input
    Vector3 rawKeyboardInput = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
    keyboardInput = transform.TransformDirection(rawKeyboardInput);

    rawForwardValue = keyboardInput.y;       // forward thrust
    rawHorizontalValue = keyboardInput.x;    // lateral thrust
    rawPitchValue = mouseInput.y;            // pitch
    rawRollValue = keyboardInput.x;          // roll
    rawYawValue = mouseInput.x;              // yaw
  }

  private void handleMovementInWater(){
    if (stateController.PlayerIsEmergingFromWater())
      walk(slope: 120f, terrainRay: Vector3.forward);
    else if (stateController.PlayerIsUnderwater())
      swim();
  }

  private void swim(){
    updateRotationInWater();
    updatePositionInWater();
  }

  private void updateRotationInWater(){
    Quaternion targetRotation = determineUnderwaterRotationFromInput();
    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeedInWater());
  }

  private Quaternion determineUnderwaterRotationFromInput(){
    Vector3 mousePosition = Input.mousePosition;
    if (invertedYAxis) mousePosition.y = Screen.height - mousePosition.y;
    Ray mouseRay = Camera.main.ScreenPointToRay(mousePosition);
    Vector3 lookPos = mouseRay.direction;// - transform.position;
    lookPos.y *= currentYAxisMultiplier();

    if (transform.position.y >= waterSurfaceLevel && lookPos.y >= 0f){
      lookPos.y = currentYValueOfLookPosition;
      lookPos = Vector3.Lerp(lookPos, new Vector3(lookPos.x, 0f, lookPos.z), 10f * Time.deltaTime);
    }

    return Quaternion.LookRotation(lookPos);
  }

  private void updatePositionInWater(){
    gravity = 15f;

    moveDirection = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
    if (moveDirection.z < 0f) moveDirection.z = 0f;
    moveDirection = transform.TransformDirection(moveDirection);
    //moveDirection *= speedInMedium;

    if (!stateController.PlayerIsNearSurface()) moveDirection.y -= gravity * Time.deltaTime;
    characterController.Move(moveDirection * Time.deltaTime);

    if (stateController.PlayerIsNearSurface())
      if (transform.position.y > waterSurfaceLevel) transform.position = new Vector3(transform.position.x, waterSurfaceLevel, transform.position.z);
    currentYValueOfLookPosition = transform.forward.y;
  }

  private void walk(float slope, Vector3 terrainRay){
    slope = slope == null ? 90f : slope;
    terrainRay = terrainRay == null ? Vector3.down : terrainRay;
    characterController.slopeLimit = slope;
    alignPlayerWithTerrain(terrainRay: terrainRay);
    movePlayerOnLand();
  }

  private void alignPlayerWithTerrain(Vector3 terrainRay){
    Quaternion targetRotation = rotationForAlignmentWithTerrain(terrainRay: terrainRay);
    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, terrainAlignmentRotationSpeed());
  }

  private void movePlayerOnLand(){
    gravity = 80f;
    moveDirection = new Vector3(0, 0, Input.GetAxis("Vertical"));
    moveDirection = transform.TransformDirection(moveDirection);
    //moveDirection *= speedInMedium;
    moveDirection.y -= gravity * Time.deltaTime;
    characterController.Move(moveDirection * Time.deltaTime);
  }

  private Quaternion rotationForAlignmentWithTerrain(Vector3 terrainRay){
    RaycastHit hit;
    float terrainCheckDistance = 6f;
    Vector3 terrainCheckRay = transform.TransformDirection(terrainRay);
    Vector3 normal = transform.TransformDirection(Vector3.up);
    Vector3 lookDirection = transform.forward + transform.TransformDirection(new Vector3(Input.GetAxis("Horizontal"), 0, 0));
    if (Physics.Raycast(transform.position, terrainCheckRay, out hit, terrainCheckDistance))
      normal = hit.normal;
    Quaternion rotation = Quaternion.FromToRotation(transform.up, normal);
    return (rotation * Quaternion.LookRotation(lookDirection));
  }

  private float terrainAlignmentRotationSpeed(){
    return (currentRotateSpeed() * .05f * Time.deltaTime);
  }

  private float rotationSpeedInWater(){
    return currentRotateSpeed() * Time.deltaTime;
  }

  private float currentRotateSpeed(){
    return 1.0f;
  }

  private float currentYAxisMultiplier(){
    return 1.0f;
  }
}

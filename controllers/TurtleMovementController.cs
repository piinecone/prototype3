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
  private float forwardAccelerationUnderwater = 1.00f;
  private float maximumForwardAccelerationUnderwater = 1.05f;
  private float swimSpeedMultiplier = .65f;
  private float maximumForwardSwimmingSpeed = 10f;
  private Vector3 underwaterMovementVectorInWorldSpace = Vector3.zero;
  private float lowSpeedDragCoefficientInWater = .99f;
  private float highSpeedDragCoefficientInWater = .96f;
  private float appliedRollValue = 0f;
  private float maxRollRotationAngle = 95f;

  // walk
  Vector3 defaultTerrainRay = Vector3.down;
  float defaultSlope = 90f;
  private Vector3 moveDirection = Vector3.zero;
  [SerializeField]
  private float walkSpeedMultiplier = 5f;

  // input
  private Vector3 mouseInput;
  private Vector3 keyboardInput;
  private float rawForwardValue = 0f;
  private float rawHorizontalValue = 0f;
  private float rawPitchValue = 0f;
  private float rawYawValue = 0f;
  private float rawRollValue = 0f;

  // timing
  private float rawRollInputTimeElapsed = 1f;

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

    rawForwardValue = rawKeyboardInput.z;    // forward thrust
    rawHorizontalValue = rawKeyboardInput.x; // lateral thrust
    rawPitchValue = mouseInput.y;            // pitch
    rawRollValue = rawKeyboardInput.x;       // roll
    rawYawValue = mouseInput.x;              // yaw

    rawRollInputTimeElapsed += Time.deltaTime;
    if (rawRollValue == 0) rawRollInputTimeElapsed = 1f;
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
    Quaternion targetRotation = determineUnderwaterRotationFromInput_();
    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeedInWater());
  }

  private Quaternion determineUnderwaterRotationFromInput_(){
    Quaternion rotation = Quaternion.identity;
    calculateAppliedRollValue();
    Quaternion lookRotation = currentLookRotation();
    rotation.eulerAngles = new Vector3(lookRotation.eulerAngles.x, lookRotation.eulerAngles.y, appliedRollValue);

    return rotation;
  }

  private Quaternion currentLookRotation(){
    Vector3 mousePosition = Input.mousePosition;
    Ray mouseRay = Camera.main.ScreenPointToRay(mousePosition);
    Debug.DrawRay(transform.position, mouseRay.direction * 10f, Color.red);
    Vector3 lookDirection = mouseRay.direction;
    lookDirection.y *= currentYAxisMultiplier();

    return Quaternion.LookRotation(lookDirection);
  }

  private void calculateAppliedRollValue(){
    float forwardStep = Time.deltaTime * 6.5f;
    float backwardStep = Time.deltaTime * 2f;
    if (rawRollValue != 0f) {
      // to allow corkscrewing, multiply the max by the active time elapsed
      // appliedRollValue = Mathf.SmoothStep(appliedRollValue, rawRollValue * -maxRollRotationAngle * rawRollInputTimeElapsed, step * 1.5f);
      appliedRollValue = Mathf.SmoothStep(appliedRollValue, rawRollValue * -maxRollRotationAngle, forwardStep);
    } else {
      // to undo a corkscrew: instead of stepping to 0f, step to the next lowest upright rotation (modulo 2pi probably)
      appliedRollValue = Mathf.SmoothStep(appliedRollValue, 0f, backwardStep);
    }
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
    Vector3 positionVector = underwaterThrustVector();
    characterController.Move(positionVector);
  }

  private Vector3 underwaterThrustVector(){
    underwaterMovementVectorInWorldSpace *= currentDragCoefficientInWater();
    float acceleration = calculateForwardAccelerationUnderwater();
    underwaterMovementVectorInWorldSpace += transform.forward * acceleration * Time.deltaTime * swimSpeedMultiplier;
    //underwaterMovementVectorInWorldSpace = Vector3.ClampMagnitude(underwaterMovementVectorInWorldSpace, maximumForwardSwimmingSpeed);

    return underwaterMovementVectorInWorldSpace;
  }

  private float calculateForwardAccelerationUnderwater(){
    if (rawForwardValue > .95f)
      return maximumForwardAccelerationUnderwater;
    else if (rawForwardValue >= .5f)
      forwardAccelerationUnderwater = Mathf.SmoothStep(forwardAccelerationUnderwater, maximumForwardAccelerationUnderwater, Time.deltaTime * 2f);
    else
      forwardAccelerationUnderwater = 0f;

    return forwardAccelerationUnderwater;
  }

  private float currentDragCoefficientInWater(){
    if (rawForwardValue > .75f)
      return highSpeedDragCoefficientInWater;
    else if (rawForwardValue > .35f)
      return (highSpeedDragCoefficientInWater + lowSpeedDragCoefficientInWater) / 2f;
    else
      return lowSpeedDragCoefficientInWater;

    //float accelerationPercentile = forwardAccelerationUnderwater / maximumForwardAccelerationUnderwater;
    //float dragOffset = (1f - accelerationPercentile) * (maximumDragCoefficientInWater - minimumDragCoefficientInWater);
    //float dragCoefficient = minimumDragCoefficientInWater + dragOffset;

    //return dragCoefficient;
  }

  private void updatePositionInWater_legacy(){
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
    return 150.0f;
  }

  private float currentYAxisMultiplier(){
    return 1.5f;
  }
}

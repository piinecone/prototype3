using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ThirdPersonCamera : MonoBehaviour {

  [SerializeField]
  private float distanceAway;
  [SerializeField]
  private float distanceUp;
  [SerializeField]
  private float smooth;
  [SerializeField]
  private Transform follow;
  [SerializeField]
  private float widescreen = 0.2f;
  [SerializeField]
  private float targetingTime = 0.5f;
  [SerializeField]
  private float maxDistanceAway;
  [SerializeField]
  private float minDistanceAway;
  [SerializeField]
  private GameObject testPosition;

  [SerializeField] // just for testing
  private Vector3 targetPosition;
  private Vector3 lookDir;
  private Vector3 velocityCamSmooth = Vector3.zero;
  //private float camSmoothDampTime = 0.1f;
  private float camSmoothDampTime = 0.2f;
  private CamStates camState = CamStates.Behind;
  private float defaultDistanceAway = 0f;
  private float defaultDistanceUp = 0f;
  private List<GameObject> objectsThatShouldAlwaysBeVisible = new List<GameObject>();

  // cut scenes
  private bool currentlyInCutScene = false;
  private GameObject cutSceneTarget;
  private float cutSceneTimeLeft;
  [SerializeField]
  private Vector3 cutSceneOffsetVector;

  public float waterSurfaceLevel = 177.5f;

  public enum CamStates {
    Behind,
    Target,
    Banking
  }

  // state-specific configuration
  private float defaultBankingCameraYDivisor = 2f;
  private float bankingCameraYDivisor;
  private float rawHorizontalInputValue = 0f;

  void Start () {
    follow = GameObject.FindWithTag("Player").transform;
    lookDir = follow.forward;
    minDistanceAway = distanceAway;
    bankingCameraYDivisor = defaultBankingCameraYDivisor;
    defaultDistanceUp = distanceUp;
    defaultDistanceAway = distanceAway;
  }

  void LateUpdate() {
    if (currentlyInCutScene){
      continueToDisplayCutScene();
    } else {
      performNormalCameraMovement();
    }
  }

  private void continueToDisplayCutScene(){
    if (cutSceneTimeLeft > 0f){
      cutSceneTimeLeft -= Time.deltaTime;
      Vector3 offset = cutSceneTarget.transform.position + cutSceneOffsetVector;
      lookDir = offset - this.transform.position;
      lookDir.y = 0;
      lookDir.Normalize();
      targetPosition = offset + cutSceneTarget.transform.up * 0f - lookDir * 0f;
      CompensateForWalls(offset, ref targetPosition);
      this.transform.position = Vector3.Lerp(this.transform.position, targetPosition, .5f * Time.deltaTime);
      //this.transform.position = Vector3.SmoothDamp(this.transform.position, targetPosition, ref velocityCamSmooth, 1f);
      transform.LookAt(cutSceneTarget.transform);
    } else {
      currentlyInCutScene = false;
    }
  }

  private void performNormalCameraMovement(){
    captureInputValues();
    calculateDesiredDistanceAwayBasedOnFollowers();
    Vector3 characterOffset = follow.position + new Vector3(0f, distanceUp, 0f);

    // Determine camera state
    if (Input.GetAxis("Jump") > 0.01f){
      //barEffect.coverage = Mathf.SmoothStep(barEffect.coverage, widescreen, targetingTime);
      camState = CamStates.Target;
    } else if (rawHorizontalInputValue != 0f || (int)bankingCameraYDivisor != defaultBankingCameraYDivisor){
      camState = CamStates.Banking;
    } else {
      //barEffect.coverage = Mathf.SmoothStep(barEffect.coverage, 0f, targetingTime);
      camState = CamStates.Behind;
    }

    switch(camState){
      case CamStates.Behind:
        // calculate direction from camera to player, kill y, and normalize to give a valid direction with unit magnitude
        lookDir = characterOffset - this.transform.position + (10 * follow.forward);
        //lookDir.y = 0;
        lookDir.y = lookDir.y / 2f;
        lookDir.Normalize();
        //Debug.DrawRay(this.transform.position, lookDir, Color.green);
        //Debug.DrawLine(follow.position, targetPosition, Color.magenta);
        break;
      case CamStates.Target:
        lookDir = follow.forward;
        break;
      case CamStates.Banking:
        calculateBankingDivisor();
        lookDir = follow.forward;
        lookDir.y = lookDir.y / bankingCameraYDivisor;
        lookDir.Normalize();
        break;
    }

    targetPosition = characterOffset + follow.up * distanceUp - lookDir * distanceAway;
    AdjustYValue(ref targetPosition);
    CompensateForWalls(characterOffset, ref targetPosition);
    smoothPosition(this.transform.position, targetPosition);
    transform.LookAt(follow);
  }

  private void captureInputValues(){
    rawHorizontalInputValue = Input.GetAxis("Horizontal");
  }

  private void calculateBankingDivisor(){
    float forwardStep = Time.deltaTime * 1.5f;
    float backwardStep = Time.deltaTime * 4f;
    if (rawHorizontalInputValue != 0f)
      bankingCameraYDivisor = Mathf.SmoothStep(bankingCameraYDivisor, defaultBankingCameraYDivisor + Mathf.Abs(rawHorizontalInputValue), forwardStep);
    else
      bankingCameraYDivisor = Mathf.SmoothStep(bankingCameraYDivisor, defaultBankingCameraYDivisor, backwardStep);
  }

  private void AdjustYValue(ref Vector3 toTarget){
    float followY = follow.transform.position.y;
    if (followY >= (waterSurfaceLevel - 1f)){
      if (toTarget.y <= waterSurfaceLevel) toTarget.y += distanceUp;
    } else if (followY <= waterSurfaceLevel){
      if (toTarget.y >= waterSurfaceLevel) toTarget.y -= distanceUp;
    }
  }

  private void calculateDesiredDistanceAwayBasedOnFollowers(){
    if (objectsThatShouldAlwaysBeVisible.Count > 0){
      bool zoomOut = false;
      float desiredDistance = distanceAway;
      foreach(GameObject go in objectsThatShouldAlwaysBeVisible){
        float distance = Vector3.Distance(go.transform.position, follow.position);
        if ((distance + 7.5f) > desiredDistance) zoomOut = true;
      }
      desiredDistance += zoomOut == true ? (15f * Time.deltaTime) : (-5f * Time.deltaTime);
      distanceAway = (desiredDistance > maxDistanceAway || desiredDistance < minDistanceAway) ? distanceAway : desiredDistance;
    } else {
      distanceAway = minDistanceAway;
    }
  }

  private void CompensateForWalls(Vector3 fromObject, ref Vector3 toTarget){
    Debug.DrawLine(fromObject, toTarget, Color.cyan);
    RaycastHit wallHit = new RaycastHit();
    if (Physics.Linecast(fromObject, toTarget, out wallHit)) {
      string hitTag = wallHit.transform.gameObject.tag;
      if (hitTag != "Player" && hitTag != "Fish" && hitTag != "BigTreeRoot" && hitTag != "MainCamera" && hitTag != "CameraShouldIgnore" && !wallHit.transform.collider.isTrigger){ // :|
        //Debug.DrawRay(wallHit.point, Vector3.left, Color.red);
        //toTarget = new Vector3(wallHit.point.x, toTarget.y, wallHit.point.z);
        if (transform.position.y >= waterSurfaceLevel) // shit :| - water surface level... for this level
          toTarget = new Vector3(wallHit.point.x, toTarget.y, wallHit.point.z);
        else
          toTarget = new Vector3(wallHit.point.x, wallHit.point.y, wallHit.point.z); // incorporate Y
      }
    }
  }

  private void smoothPosition(Vector3 fromPos, Vector3 toPos){
    this.transform.position = Vector3.SmoothDamp(fromPos, toPos, ref velocityCamSmooth, camSmoothDampTime);
  }

  public string getCamState(){
    switch(camState){
      case CamStates.Behind:
        return "Behind";
      case CamStates.Target:
        return "Target";
      case CamStates.Banking:
        return "Banking";
      default:
        return "Behind";
    }
  }

  public void addObjectThatMustAlwaysRemainInFieldOfView(GameObject theGameObject){
    objectsThatShouldAlwaysBeVisible.Add(theGameObject);
  }

  public void removeObjectThatMustAlwaysRemainInFieldOfView(GameObject theGameObject){
    objectsThatShouldAlwaysBeVisible.Remove(theGameObject);
  }

  public void cutTo(GameObject aGameObject, float timeToWatch, Vector3 offsetVector){
    if (!currentlyInCutScene){
      currentlyInCutScene = true;
      cutSceneTarget = aGameObject;
      cutSceneTimeLeft = timeToWatch;
      cutSceneOffsetVector = offsetVector;
    }
  }

  public void ResetPosition(){
    distanceUp = defaultDistanceUp;
    distanceAway = defaultDistanceAway;
  }

  public void UpdatePosition(float up, float away){
    distanceUp = up;
    distanceAway = away;
  }

  public float DistanceAway(){
    return distanceAway;
  }

  public float DistanceUp(){
    return distanceUp;
  }
}

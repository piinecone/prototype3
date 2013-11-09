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
  private TurtleState turtleState;
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
  private List<GameObject> objectsThatShouldAlwaysBeVisible = new List<GameObject>();

  // cut scenes
  private bool currentlyInCutScene = false;
  private GameObject cutSceneTarget;
  private float cutSceneTimeLeft;
  [SerializeField]
  private Vector3 cutSceneOffsetVector;

  public enum CamStates {
    Behind,
    Target
  }

  void Start () {
    follow = GameObject.FindWithTag("Player").transform;
    lookDir = follow.forward;
    minDistanceAway = distanceAway;
  }
  
  void Update () {
  }

  //if (turtleState.isUnderwater()){
  //  distanceAway = 15;
  //  distanceUp = 2;
  //} else {
  //  distanceAway = 10;
  //  distanceUp = 4;
  //}

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
      this.transform.position = Vector3.Lerp(this.transform.position, targetPosition, .7f * Time.deltaTime);
      //this.transform.position = Vector3.SmoothDamp(this.transform.position, targetPosition, ref velocityCamSmooth, 1f);
      transform.LookAt(cutSceneTarget.transform);
    } else {
      currentlyInCutScene = false;
    }
  }

  private void performNormalCameraMovement(){
    calculateDesiredDistanceAwayBasedOnFollowers();
    Vector3 characterOffset = follow.position + new Vector3(0f, distanceUp, 0f);

    // Determine camera state
    if (Input.GetAxis("Fire2") > 0.01f){ // FIXME change to space bar
      //barEffect.coverage = Mathf.SmoothStep(barEffect.coverage, widescreen, targetingTime);
      camState = CamStates.Target;
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
    }

    targetPosition = characterOffset + follow.up * distanceUp - lookDir * distanceAway;
    // FIXME turn this back on and just have it respect terrain and terrain-like elements
    // smooth it out a little as well
    CompensateForWalls(characterOffset, ref targetPosition);
    smoothPosition(this.transform.position, targetPosition);
    transform.LookAt(follow);
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
      if (hitTag != "Player" && hitTag != "Fish" && hitTag != "BigTreeRoot"){ // :|
        //Debug.DrawRay(wallHit.point, Vector3.left, Color.red);
        //toTarget = new Vector3(wallHit.point.x, toTarget.y, wallHit.point.z);
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
}

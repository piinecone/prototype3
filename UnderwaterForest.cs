using UnityEngine;
using System.Collections;

public class UnderwaterForest : MonoBehaviour {

  [SerializeField]
  private float highFogDensity = 0.03f;
  [SerializeField]
  private float lowFogDensity = 0.009f;
  [SerializeField]
  private UnderWater underwaterCameraState;

  private float targetDensity;
  private Transform player;
  private Transform camera;
  private Color lowDensityColor;
  private Color highDensityColor;
  private Color targetColor;

  void Start () {
    player = GameObject.FindWithTag("Player").transform;
    camera = GameObject.FindWithTag("MainCamera").transform;
    lowDensityColor = new Color(0.22f, 0.45f, 0.87f, 0.5f);
    highDensityColor = new Color(0.22f, 0.60f, 0.8f, 0.5f);
    targetDensity = lowFogDensity;
    targetColor = lowDensityColor;
  }
  
  void LateUpdate () {
    //if (playerIsNearForest()){
    if (cameraIsNearForest() && underwaterCameraState.currentlyUnderwater()){
      float density = RenderSettings.fogDensity;
      if (density != targetDensity){
        RenderSettings.fogDensity = Mathf.SmoothStep(density, targetDensity, .08f);
      }
      Color fogColor = RenderSettings.fogColor;
      if (fogColor != targetColor){
        RenderSettings.fogColor = Color.Lerp(fogColor, targetColor, .03f);
      }
    }
  }

  void OnTriggerEnter(Collider collider){
    //if (collider.gameObject.tag == "Player"){
    if (collider.gameObject.tag == "MainCamera"){
      increaseFogDensity();
    }
  }

  void OnTriggerExit(Collider collider){
    //if (collider.gameObject.tag == "Player"){
    if (collider.gameObject.tag == "MainCamera"){
      revertFogDensity();
    }
  }

  void increaseFogDensity(){
    targetDensity = highFogDensity;
    targetColor = highDensityColor;
  }

  void revertFogDensity(){
    targetDensity = lowFogDensity;
    targetColor = lowDensityColor;
  }

  bool playerIsNearForest(){
    return (Vector3.Distance(player.position, transform.position) < 300f);
  }

  bool cameraIsNearForest(){
    return (Vector3.Distance(camera.position, transform.position) < 300f);
  }
}

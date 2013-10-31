using UnityEngine;
using System.Collections;

public class UnderWater : MonoBehaviour {

  public float waterLevel;
  private bool isUnderwater;
  private Color normalColor;
  private Color underwaterColor;

  void Start () {
    normalColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
    underwaterColor = new Color(0.22f, 0.65f, 0.77f, 0.5f);
  }

  void Update () {
    if ((transform.position.y < waterLevel) != isUnderwater){
      isUnderwater = transform.position.y < waterLevel;
      if (isUnderwater) setUnderwater();
      if (!isUnderwater) setAboveWater();
    }
  }

  private void setUnderwater(){
    Debug.Log("going underwater");
    RenderSettings.fogColor = underwaterColor;
    RenderSettings.fogDensity = 0.1f;
  }

  private void setAboveWater(){
    Debug.Log("going above water");
    RenderSettings.fogColor = normalColor;
    RenderSettings.fogDensity = 0.002f;
  }
}

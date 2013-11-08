using UnityEngine;
using System.Collections;

public class UnderWater : MonoBehaviour {

  public float waterLevel;
  public float atmosphericVisibility;
  public float waterVisibility;
  private bool isUnderwater;
  private Color normalColor;
  private Color underwaterColor;

  void Start () {
    normalColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
    //underwaterColor = new Color(0.22f, 0.65f, 0.77f, 0.5f); # original
    underwaterColor = new Color(0.22f, 0.45f, 0.87f, 0.5f);
  }

  void Update () {
    if ((transform.position.y < waterLevel) != isUnderwater){
      isUnderwater = transform.position.y < waterLevel;
      if (isUnderwater) setUnderwater();
      if (!isUnderwater) setAboveWater();
    }
  }

  private void setUnderwater(){
    RenderSettings.fogColor = underwaterColor;
    RenderSettings.fogDensity = waterVisibility;
  }

  private void setAboveWater(){
    RenderSettings.fogColor = normalColor;
    RenderSettings.fogDensity = atmosphericVisibility;
  }

  public bool currentlyUnderwater(){
    return isUnderwater;
  }
}

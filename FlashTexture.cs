using UnityEngine;
using System.Collections;

public class FlashTexture : MonoBehaviour {
  void Start(){
    guiTexture.enabled = false;
  }

  public void Flash(float duration) {
    guiTexture.enabled = true;
    Invoke("Cancel", duration);
  }
  
  void Cancel() {
    guiTexture.enabled = false;
  }
}

using UnityEngine;
using System.Collections;

public class Powerup : MonoBehaviour {

  [SerializeField]
  private FlashTexture flashTexture;
  [SerializeField]
  private JetstreamTrigger jetstreams;
  [SerializeField]
  private GameObject proxyBoulders;

  // Use this for initialization
  void Start () {
    renderer.material.color = Color.magenta;
    proxyBoulders.active = false;
  }
  
  // Update is called once per frame
  void Update () {
  
  }

  void OnTriggerEnter(Collider collider){
    if (collider.gameObject.tag == "Player"){
      Destroy(gameObject);
      flashTexture.Flash(0.1f);
      proxyBoulders.active = true;
      jetstreams.PowerUpCollected();
    }
  }
}

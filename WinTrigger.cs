using UnityEngine;
using System.Collections;

public class WinTrigger : MonoBehaviour {

   [SerializeField]
   private GUIText victoryText;
   private AudioSource victoryMusic;

   // Use this for initialization
   void Start () {
     victoryMusic = GetComponent<AudioSource>();
     victoryText.active = false;
   }
   
   // Update is called once per frame
   void Update () {
   }

   void OnTriggerEnter(Collider collider){
     if (collider.gameObject.tag == "Player"){
       victoryMusic.Play();
       victoryText.active = true;
     }
   }
}

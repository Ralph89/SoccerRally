using UnityEngine;
using System.Collections;

public class Game : MonoBehaviour {


	void Start () {
        //entering the game allow the messages
        PhotonNetwork.isMessageQueueRunning = true;
        if (PhotonNetwork.isMasterClient)
        {
            //PhotonNetwork.Instantiate("NetworkBall", new Vector3(0, 5, 0), Quaternion.identity, 0);
        }
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
 
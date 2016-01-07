using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MainMenu : MonoBehaviour {

    [SerializeField] private NetworkManager networkMangerPrefab;
    [SerializeField] private Button joinButton;
   
	// Use this for initialization
	void Start () {
        Application.targetFrameRate = 60;
        //check if we already have a networkmanager
        NetworkManager networkManager =  FindObjectOfType<NetworkManager>();
        if (networkManager == null)
            networkManager = Instantiate(networkMangerPrefab);
        Debug.Log(networkManager);
        //register event for when clicking on the button
        joinButton.onClick.AddListener(networkManager.CreateJoinGame);
	}

}

using UnityEngine;
using System.Collections;
using ExitGames.Client.Photon.Chat;
using ExitGames.Client.Photon;

public class NetworkManager : Photon.MonoBehaviour
{

    ChatClient chatClient;
    void Start () {
        //register serialization
        PhotonNetwork.ConnectUsingSettings("0.1");
        PhotonNetwork.sendRate = 20;
        PhotonNetwork.sendRateOnSerialize = 20;
        Application.targetFrameRate = 60;
        DontDestroyOnLoad(this);
    }
	

    void OnLeftRoom()
    {
        //load menu scene
        Application.LoadLevel(0);
    }

    void OnJoinedRoom()
    {
        //load game scene
        PhotonNetwork.isMessageQueueRunning = false;
        Application.LoadLevel(1);
    }

    public void CreateJoinGame()
    {
        PhotonNetwork.JoinOrCreateRoom("test", new RoomOptions(), TypedLobby.Default);
    }
}

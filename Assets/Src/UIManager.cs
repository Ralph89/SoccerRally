using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UIManager : MonoBehaviour {

    public Button stateInterpolateButton;
    public Button rigidbodySyncButton;
    public Button resetBtn;
    public Slider forceSlider;

    public Button upInput;
    public Button rightInput;
    public Button leftInput;


    void Awake()
    {
#if UNITY_ANDROID
        EnableTouchControls();
#endif
        if (PhotonNetwork.isMasterClient)
        {
            stateInterpolateButton.gameObject.SetActive(false);
            rigidbodySyncButton.gameObject.SetActive(false);
        }
    }


    void EnableTouchControls()
    {
        if (PhotonNetwork.isMasterClient)
            resetBtn.gameObject.SetActive(true);
        upInput.gameObject.SetActive(true);
        leftInput.gameObject.SetActive(true);
        rightInput.gameObject.SetActive(true);
    }

    public void DisableClientButtons()
    {
        stateInterpolateButton.gameObject.SetActive(false);
        rigidbodySyncButton.gameObject.SetActive(false);
#if UNITY_ANDROID
        resetBtn.gameObject.SetActive(true);
#endif
    }
}

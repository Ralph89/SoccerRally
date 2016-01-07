using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UIManager : MonoBehaviour {

    public Button stateInterpolateButton;
    public Button rigidbodySyncButton;
    public Slider forceSlider;

    public Button upInput;
    public Button rightInput;
    public Button leftInput;

    void Awake()
    {
#if UNITY_ANDROID
        EnableTouchControls();
#endif
    }


    void EnableTouchControls()
    {
        upInput.gameObject.SetActive(true);
        leftInput.gameObject.SetActive(true);
        rightInput.gameObject.SetActive(true);
    }
}

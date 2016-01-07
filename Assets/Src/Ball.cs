using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

/// <summary>
/// The host will control the ball
/// </summary>
public class Ball : Photon.MonoBehaviour {

    [Header("Sync Options")]
    [SerializeField] private bool StateInterpolation = false;   // always behind
    [SerializeField] private bool syncRigidbody = false;        // syning the rigidbody values

    //state sync
    public class NetworkState
    {
        public Vector3 pos;
        public Quaternion rot;
        public double time;
        public Vector3 rigidBodyVel;

        public NetworkState(Vector3 p, Quaternion r,  Vector3 v, double t)
        {
            this.pos = p;
            this.rot = r;
            this.rigidBodyVel = v;
            this.time = t;
        }
    }
    private int m_TimestampCount;
    private NetworkState[] states = new NetworkState[20];
    private Rigidbody cachedRB;
    private Transform cachedTF;
    private float force;

    //network 
    private Vector3 nextPos;
    private Quaternion nextRot;
    private Vector3 nextVel;

    //prediction method
    private double lastPacketTime;
    private float packetTimer;
    private NetworkState lastPredictionState;
    private NetworkState prevPredictionState;
    private double lastSimTime;
    private bool firstFrame = true;
    private bool offToMuch;
    private Vector3 offBy;
    UIManager uiManager;

    void Awake()
    {
        cachedRB = GetComponent<Rigidbody>();
        cachedTF = transform;
        if (!PhotonNetwork.isMasterClient)
            cachedRB.isKinematic = true;

    }
    
    void Start()
    {
        //find the uimanager to register the functions to the button
        uiManager = FindObjectOfType<UIManager>();
        if (uiManager != null)
        {
            uiManager.rigidbodySyncButton.onClick.AddListener(EnableRigidbodySync);
            uiManager.stateInterpolateButton.onClick.AddListener(EnableStateInterpolation);
            uiManager.forceSlider.onValueChanged.AddListener(SetForce);

            #if UNITY_ANDROID
            uiManager.upInput.onClick.AddListener(ClickUpBtn);
            uiManager.rightInput.onClick.AddListener(ClickRightBtn);
            uiManager.leftInput.onClick.AddListener(ClickLeftBtn);
            uiManager.resetBtn.onClick.AddListener(ResetBall);
            #endif
        }
        EnableRigidbodySync();
    }

    void Update()
    {
        KeyBoardInput();
        if (!PhotonNetwork.isMasterClient)
        {
            //Predict();
            //PredicationInterpolate();
            if (StateInterpolation)
                StateSyncInterpolate();
        }
    }

    void OnDestroy()
    {
        PhotonNetwork.LeaveRoom();
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {

        if (stream.isWriting) //host owns the ball
        {
            //send pos & rot
            stream.SendNext(cachedTF.position);
            stream.SendNext(cachedTF.rotation);
            stream.SendNext(cachedRB.velocity);
            stream.SendNext(cachedRB.angularVelocity);
        }
        else
        {
            //receive pos and rot in right order
            nextPos = (Vector3)stream.ReceiveNext();
            nextRot = (Quaternion)stream.ReceiveNext();
            nextVel = (Vector3)stream.ReceiveNext();
            Vector3 angVel = (Vector3)stream.ReceiveNext();
            //debug
            Vector3 diff = cachedTF.position - nextPos;
            
            NetworkState state = null;
            if (syncRigidbody)
            {
                cachedRB.velocity = nextVel;
                cachedRB.angularVelocity = angVel;
                if (diff.magnitude > 0.1f)
                {
                    offBy = diff;
                    // so the floating point precision is getting to much lets speed up to get it in the right position again lets try to resolve in .5s second
                    cachedRB.velocity -= (diff * 5);
                }
                if (diff.magnitude > 1)
                {
                    Debug.Log("maybe snaP" + diff.magnitude);
                    cachedTF.position = nextPos;
                }
            }
            else if (StateInterpolation)
            {
                //State sync
                //shift the array. erase oldest data
                state = new NetworkState(nextPos, nextRot, nextVel, info.timestamp);
                AddState(state);
            }

            //prediction states
            lastSimTime = PhotonNetwork.time;
            prevPredictionState = lastPredictionState;
            lastPredictionState = state;
        }
    }

    void AddState(NetworkState state)
    {
        //State sync
        //shift the array. erase oldest data
        for (int i = states.Length - 1; i >= 1; i--)
            states[i] = states[i - 1];
        //new states
        states[0] = state;
        m_TimestampCount = Mathf.Min(m_TimestampCount + 1, states.Length);

        //Check integrity, lowest numbered state in the buffer is the newest and so on
        for (int i = 0; i < m_TimestampCount - 1; i++)
            if (states[i].time < states[i + 1].time)
                Debug.Log("State inconsistent");
    }

    void StateSyncInterpolate()
    {
        //state sync way
        float ping = (float)PhotonNetwork.GetPing() * 0.001f; // ping in seconds
        double currentTime = PhotonNetwork.time;
        //interpolation we get the ping back as an in to make it to ms again.
        double interpolationTime = currentTime - .15f;
        // We have a window of interpolation time that we play
        //By having networkdelay the average ping, you will usually use interpolation
        //And only if no more data arrives will we use extrapolation
        //Use interpolation
        //Check if latest state exceeds interpolation time
        //If this is the case then it is too old and extrapolation should be used

        //when the state time is higher then our interpolation time
        if (m_TimestampCount > 0 && states[0].time > interpolationTime)
        {
            for (int i = 0; i < m_TimestampCount; i++)
            {
                //Find a state that matches the interpolation time (time+.1) or use the last state
                if (states[i].time <= interpolationTime || i == m_TimestampCount - 1)
                {
                    //The state one slot newer (<100ms) than the best playback time
                    NetworkState rhs = states[Mathf.Max(i - 1, 0)];
                    //The best playback state (closest to 100ms old (default time))
                    NetworkState lhs = states[i];

                    //Use the time between the two slots to determine if interpolation is necessary
                    double length = rhs.time - lhs.time;
                    float t = 0;

                    //As the time gets closer to 100ms, t gets closer to 1 in which case only rhs is used
                    if (length > 0.0001)
                    {
                        t = (float)((interpolationTime - lhs.time) / length);
                        // Debug.Log(t);
                    }
                    //distance check
                    //ship.cachedRB.AddRelativeForce(Vector3.forward * states[0].speed);
                    cachedTF.position = Vector3.Lerp(lhs.pos, rhs.pos, t);
                    cachedTF.rotation = Quaternion.Slerp(lhs.rot, rhs.rot, t);
                    return;
                }
            }
        }
        else if (m_TimestampCount > 1)
        {
            Debug.Log("extra");
            double extrapolationLength = (interpolationTime - states[0].time);
            if (extrapolationLength < 1)
            {
                cachedTF.position = states[0].pos + (states[0].rigidBodyVel * (float)extrapolationLength);
                cachedTF.rotation = states[0].rot;
            }
        }
    }

    void ResetBall()
    {
        cachedRB.velocity = Vector3.zero;
        cachedTF.position = new Vector3(0, 5, 0);
    }

    [PunRPC]
    void ApplyBallForce(Vector3 force)
    {
        cachedRB.AddForce(force);
    }

    void SetForce(float value)
    {
        force = value * 100;
    }

    #region Helper functions
    void EnableStateInterpolation()
    {
        StateInterpolation = true;
        syncRigidbody = false;
        cachedRB.isKinematic = true;
        uiManager.rigidbodySyncButton.image.color = Color.white;
        uiManager.stateInterpolateButton.image.color = Color.red;
    }

    void EnableRigidbodySync()
    {
        StateInterpolation = false;
        syncRigidbody = true;
        if (cachedRB.isKinematic)
            cachedRB.isKinematic = false;
        uiManager.rigidbodySyncButton.image.color = Color.red;
        uiManager.stateInterpolateButton.image.color = Color.white;

    }
    #endregion 

    #region temp for input should be in inputmanager but still a test project, kinda lazy
    void KeyBoardInput()
    {
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            photonView.RPC("ApplyBallForce", PhotonTargets.MasterClient, Vector3.down * force);
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            photonView.RPC("ApplyBallForce", PhotonTargets.MasterClient, Vector3.up * force);
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            photonView.RPC("ApplyBallForce", PhotonTargets.MasterClient, Vector3.left * force);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            photonView.RPC("ApplyBallForce", PhotonTargets.MasterClient, Vector3.right * force);
        }
        else if (Input.GetKeyDown(KeyCode.Space) && PhotonNetwork.isMasterClient)
        {
            ResetBall();
        }
    }

    void ClickUpBtn()
    {
        photonView.RPC("ApplyBallForce", PhotonTargets.MasterClient, Vector3.up * force);
    }

    void ClickRightBtn()
    {
        photonView.RPC("ApplyBallForce", PhotonTargets.MasterClient, Vector3.right * force);
    }

    void ClickLeftBtn()
    {
        photonView.RPC("ApplyBallForce", PhotonTargets.MasterClient, Vector3.left * force);
    }

    #endregion

    #region predictions
    void Predict()
    {
        double serverTime = PhotonNetwork.time;
        double timeSinceLastPacket = serverTime - lastPredictionState.time;

        Vector3 lastPos = lastPredictionState.pos;
        float t = 0;
        if (timeSinceLastPacket > 0)
            t = ((float)timeSinceLastPacket / Time.fixedDeltaTime);
        //Debug.Log((float)timeSinceLastPacket);
        Vector3 curPos = lastPos + (lastPredictionState.rigidBodyVel * (float)timeSinceLastPacket);

        cachedTF.position = curPos;
        cachedTF.rotation = lastPredictionState.rot;
    }

    void PredicationInterpolate()
    {
        if (prevPredictionState == null)
            return;
        //2 packets
        //1st is latest
        //2nd is prev
        double serverTime = PhotonNetwork.time;

        float timeSinceLastPacket;
        float timeSinceLastUpdate;
        float timePrevPacket;
        float timeLerp = 0;

        timePrevPacket = (float)(serverTime - prevPredictionState.time);
        timeSinceLastPacket = (float)(serverTime - lastPredictionState.time);
        timeSinceLastUpdate = (float)(serverTime - lastSimTime);

        // Calculate time to interpolate from the previous path
        // to ideal (last) packet's path
        timeLerp = timeSinceLastUpdate / .033333333f;
        float tLerpRaw = timeLerp; // rawTimeLerp before clamping
        timeLerp = Mathf.Clamp(timeLerp, 0, 1);// Clamp to 0..1 - it's just about the previous -> last path so no prediction or history
        // Cache 1-tLerp
        float oneMinusTimeLerp = 1.0f - timeLerp;

        //
        // Linear interpolation - should be perfect for static linear velocity tests with 1 server and 1 client
        // More clients will mean the clock varies a bit from clients -> srv -> other clients
        //

        // Calculate current position based on packet info
        Vector3 posLast, posPrev;

        posLast = lastPredictionState.pos + lastPredictionState.rigidBodyVel * timeSinceLastPacket;

        // Calculate predicted previous position - use time since last interpolation time
        // Calculate interpolated velocity for a bit more smoothness
        Vector3 ipVel;
        ipVel = timeLerp * lastPredictionState.rigidBodyVel + oneMinusTimeLerp * prevPredictionState.rigidBodyVel;

        posPrev = prevPredictionState.pos + ipVel * timePrevPacket;


        // Calculate position, interpolating from posPrev -> posLast as time goes by
        // to correct towards the last received packet
        Vector3 curPos;
        //posLast and posPrev are interpolated to see where we should be.
        //the curpos will be moving from the prevpos towards the lastpos  (already precited towards our time)
        curPos.x = timeLerp * posLast.x + oneMinusTimeLerp * posPrev.x;
        curPos.y = timeLerp * posLast.y + oneMinusTimeLerp * posPrev.y;
        curPos.z = timeLerp * posLast.z + oneMinusTimeLerp * posPrev.z;

        cachedTF.position = curPos;

        // Use a different method for orientation, which interpolates
        // the rotation from the previous to the new quat.
        // Use unclamped lerp from prev->last since we want prediction
        // Calculate interpolant into 'quat' (current value)
        cachedRB.rotation = lastPredictionState.rot;

    }
    #endregion

}

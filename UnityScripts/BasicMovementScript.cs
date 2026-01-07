using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Mono.Cecil.Cil;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.Rendering;

public class NewMonoBehaviourScript : MonoBehaviour
{
    public Rigidbody rb;
    public Camera PlayerCamera;

    InputAction anyKey;
    InputAction forward;
    InputAction backward;
    InputAction leftward;
    InputAction rightward;
    InputAction jump;
    InputAction playerView;

    private enum State { mvng, stnd }
    private State currState = State.stnd;

    private float rotationXnormalized = 0; // 
    public float sens = 14f; // mouse sensetivity
    public float fForce = 0.000001f; // forward force
    public float sForce = 0.5f; // sideways force
    public float jForce = 0.3f; // jump force
    public float RAY_LENGTH = 0.6f;
    public float jumpTimerTime = 10f;
    public float stndTimerTime = 10f;
    public float validTime;
    public float validStndTime;
    private bool buttonPress;

    // Awake is called once before the start function is called, and before any actions or updated occur
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    // Update is called once per frame
    void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        anyKey = InputSystem.actions.FindAction("AnyKey");
        forward = InputSystem.actions.FindAction("forward");
        backward = InputSystem.actions.FindAction("backward");
        leftward = InputSystem.actions.FindAction("leftward");
        rightward = InputSystem.actions.FindAction("rightward");
        jump = InputSystem.actions.FindAction("jump");
        playerView = InputSystem.actions.FindAction("look");

        validTime = Time.time;
        validStndTime = Time.time;
    }

    void Update()
    {
        buttonPress = anyKey.IsPressed();

        Vector3 currRotation = transform.eulerAngles;
        Vector2 mouseDelta = playerView.ReadValue<Vector2>();
        // Mouse delta is the change in position of the mouse?

        UnityEngine.Debug.Log(currState);
        UnityEngine.Debug.Log(buttonPress);

        // State machine
        switch (currState)
        {
            // Player can look with the camera, 
            // Once any input is observed the state changes to mvnt
            case State.stnd:

                if (buttonPress)
                {
                    validStndTime = stndTimer();    
                    currState = State.mvng;
                }

                MoveCamera(mouseDelta);

                break;

            // The player can move and look, 
            // Once input stops and the time is valid, state changes to stnd 
            case State.mvng:

                // Increase the valid time if input occurs
                if (buttonPress)
                {
                    validStndTime = stndTimer();
                }

                MoveCamera(mouseDelta);
                MovePlayer(currRotation);

                // No input and valid time, change state. 
                if (!buttonPress && Time.time >= validStndTime)
                {
                    currState = State.stnd;
                }

                break;
        }
    }

    void MovePlayer(Vector3 currRotation)
    {
        if (isGrounded())
        {
            // convert all boolean info about inputs into 1's or zeros and multiply by the related velocities
            float fVec = (Convert.ToInt32(forward.IsPressed()) - Convert.ToInt32(backward.IsPressed())) * fForce;
            float sVec = (Convert.ToInt32(rightward.IsPressed()) - Convert.ToInt32(leftward.IsPressed())) * sForce; 
            float vVec = 0;

            // Only jump if within a certain time interval of previous jump
            if (Time.time >= validTime && jump.IsPressed())
            {
                vVec = jForce * 9.8f;
                validTime = jumpTimer(); 
            }

            // Make a vector of all movement velocities, multiply everything by time delta time
            Vector3 movementVec = new Vector3(sVec * Time.deltaTime, vVec * Time.deltaTime, fVec * Time.deltaTime);
            // Multiple the movement vector by the player's current vector, so as to make movement relative
            movementVec = Quaternion.AngleAxis(currRotation.y, Vector3.up) * movementVec;

            // Add a force to the rigid body component to move the player capsule
            rb.AddForce(movementVec, ForceMode.VelocityChange);
            // AddForce allegedly better since it pushes the rb rather than forcing it to a location.
        }
    }

    private void MoveCamera(Vector2 mouseDelta)
    {
        float rotationX = mouseDelta.y * sens * Time.deltaTime; // Vertical mouse movement
        float rotationY = mouseDelta.x * sens * Time.deltaTime; // Horizontal mouse movement

        gameObject.transform.Rotate(new Vector3(0, rotationY, 0)); // horoizontal rotation
        rotationXnormalized = Mathf.Clamp(rotationXnormalized - rotationX, -90, 80); // vertical rotation calculation
        PlayerCamera.transform.eulerAngles 
            = new Vector3(rotationXnormalized, gameObject.transform.eulerAngles.y, gameObject.transform.eulerAngles.z); // vertical rotation
    }

    // shoots a raycast a certain length under the player to check for collision
    public bool isGrounded()
    {
        RaycastHit hit;
        float rayLength = RAY_LENGTH;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, rayLength))
        {
            return true;
        }
        return false;
    }

    // Jump and stnd timers add some amount of seconds to the current time. 
    public float jumpTimer()
    {
        float intialTime = Time.time;
        return intialTime + jumpTimerTime;
    }

    public float stndTimer()
    {
        float intialTime = Time.time;
        return intialTime + stndTimerTime;
    }

}

//13157
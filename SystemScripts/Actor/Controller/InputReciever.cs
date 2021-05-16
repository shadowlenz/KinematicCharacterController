using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
public class InputReciever : MonoBehaviour
{
    public PlayerInput playerInput;
    public int PlayerByIndex = 0;

    InputActionMap ThisActionMap;

    InputAction MoveAction;
    public InputActionReference MoveRef;

    InputAction CrouchAction;
    public InputActionReference CrouchRef;

   [HideInInspector()] public InputAction JumpAction;
    public InputActionReference JumpRef;

    bool DelegatesEnabled;
    private void OnEnable()
    {
        StartCoroutine(ControllerEnable_Delay());
    }
    private void OnDisable()
    {
        Controller_Disable();
    }
    private void OnDestroy()
    {
        Controller_Disable();
    }

    IEnumerator ControllerEnable_Delay()
    {
        while (PlayerInput.GetPlayerByIndex(PlayerByIndex) == null)
        {
            yield return null;
        }
        playerInput = PlayerInput.GetPlayerByIndex(PlayerByIndex);
        ThisActionMap = playerInput.currentActionMap;
        yield return new WaitForSeconds(0.2f);

        Controller_Enable();
    }
    /// <summary>
    /// create listeners
    /// </summary>
    void Controller_Enable ()
    {
        //move
        MoveAction = ThisActionMap.FindAction(MoveRef.action.id);
        MoveAction.performed += MoveAxis;
        MoveAction.canceled += MoveAxis;
        //crouch
        CrouchAction = ThisActionMap.FindAction(CrouchRef.action.id);
        CrouchAction.started += OnCrouch;
        CrouchAction.performed += OnCrouch;
        CrouchAction.canceled += OnCrouch;
        //jump
        JumpAction = ThisActionMap.FindAction(JumpRef.action.id);
        JumpAction.started += OnJump;
        JumpAction.performed += OnJump;
        JumpAction.canceled += OnJump;

        DelegatesEnabled = true;
    }

    /// <summary>
    /// avoid memory leak or controller movement when disabled or destory
    /// </summary>
    void Controller_Disable()
    {
        if (!DelegatesEnabled) return;

        //move
        MoveAction.performed -= MoveAxis;
        MoveAction.canceled -= MoveAxis;
        //crouch
        CrouchAction.started -= OnCrouch;
        CrouchAction.performed -= OnCrouch;
        CrouchAction.canceled -= OnCrouch;
        //jump
        JumpAction.started -= OnJump;
        JumpAction.performed -= OnJump;
        JumpAction.canceled -= OnJump;

        DelegatesEnabled = false;
    }


    //==================================================================================

    public Vector2 GetMoveAxis;
    void MoveAxis(InputAction.CallbackContext ctx)
    {
        GetMoveAxis = ctx.ReadValue<Vector2>();
    }


    void OnCrouch(InputAction.CallbackContext ctx)
    {
        //crouchInput = ctx.performed;
    }
    void OnJump(InputAction.CallbackContext ctx)
    {
        /*
       if (ctx.started) print("started " + Time.timeSinceLevelLoad);
      else if (ctx.performed) print("IsHeld " + Time.timeSinceLevelLoad);
       else if (ctx.canceled) print("canceled " + Time.timeSinceLevelLoad);
         */
    }

    bool JumpWasStarted;
    private void Update()
   {
       if (playerInput == null || !DelegatesEnabled) return;

        // jump input debug
        /*
        if (JumpAction.triggered)
        {
            Debug.Log("WasPressedThisFrame " + Time.timeSinceLevelLoad);
            JumpWasStarted = true;
        }
        else if (JumpAction.ReadValue<float>() > 0)
        {
            Debug.Log("IsHeld " + Time.timeSinceLevelLoad);
            JumpWasStarted = true;
        }
        if (JumpWasStarted && JumpAction.ReadValue<float>() == default)
        {
            Debug.Log("WasReleasedThisFrame " + Time.timeSinceLevelLoad);
            JumpWasStarted = false;
        }
        */

   
    }
}



using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class test : MonoBehaviour
{
    private VRControls _controls;
    public TMP_Text textObject;

    void Awake()
    {
        _controls = new VRControls();
        _controls.OculusTouchControllers.Enable();
    }
    void Start()
    {
        textObject.text = "bruh";
    }

    public void SetText(String text)
    {
        textObject.text = text;
    }

    // Update is called once per frame
    void Update()
    {
        bool aButtonPressed = _controls.OculusTouchControllers.AButton.WasPressedThisFrame();
        float rightTriggerValue = _controls.OculusTouchControllers.RightTrigger.ReadValue<float>();
        float leftTriggerValue = _controls.OculusTouchControllers.LeftTrigger.ReadValue<float>();
        Vector2 thumbstick = _controls.OculusTouchControllers.RightJoyStick.ReadValue<Vector2>();
        
        SetText($"AButton pressed : {aButtonPressed}\n" +
                $"Right Trigger Value : {rightTriggerValue:F3}\n" +
                $"Left Trigger Value : {leftTriggerValue:F3}\n" +
                $"Right Joystick Value : X={thumbstick.x:F3}, Y={thumbstick.y:F3}");
    }
    
    void OnDisable()
    {
        _controls.OculusTouchControllers.Disable();
    }
}

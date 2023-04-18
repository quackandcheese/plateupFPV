using Controllers;
using Kitchen;
using KitchenMods;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem;
using UnityEngine;
using System.ComponentModel;
using Unity.Entities.UniversalDelegates;
using UnityEngine.Rendering;

namespace KitchenFirstPersonView
{
    public struct CFirstPersonPlayer : IModComponent
    {
        public bool IsActive;
        public bool IsInitialised;
    }

    public class FirstPersonPlayerView : UpdatableObjectView<FirstPersonPlayerView.ViewData>, ISpecificViewResponse
    {
        public class UpdateView : ResponsiveViewSystemBase<ViewData, ResponseData>, IModSystem
        {
            EntityQuery Query;
            private KeyControl toggleCameraKey;

            protected override void Initialise()
            {
                base.Initialise();

                Query = GetEntityQuery(typeof(CLinkedView), typeof(CFirstPersonPlayer));


                //toggleCameraKey = Keyboard.current.f5Key;
            }

            protected override void OnUpdate()
            {
                if (Query.IsEmpty) return;

                using NativeArray<CLinkedView> linkedViews = Query.ToComponentDataArray<CLinkedView>(Allocator.Temp);
                using NativeArray<CFirstPersonPlayer> components = Query.ToComponentDataArray<CFirstPersonPlayer>(Allocator.Temp);
                using NativeArray<CPlayer> playerComponent = Query.ToComponentDataArray<CPlayer>(Allocator.Temp);
                using var ents = Query.ToEntityArray(Allocator.Temp);

                //  ***************MOVED TO VIEWDATA
                /*for (var i = 0; i < ents.Length; i++)
                {
                    var ent = ents[i];
                    var my_component = components[i];

                    if (!components[i].IsInitialised)
                        break;
                    if (toggleCameraKey.wasPressedThisFrame)
                    {
                        my_component.IsActive = !my_component.IsActive;
                        Set(ent, my_component);
                    }
                }*/

                foreach (CLinkedView view in linkedViews)
                {
                    SendUpdate(view, new ViewData { IsActive = components[0].IsActive, IsInitialised = components[0].IsInitialised, Source = InputSourceIdentifier.Identifier, LookSensitivity = 5.0f, Speed = playerComponent[0].Speed });

                    // protected bool ApplyUpdates(ViewIdentifier identifier, Action<TResp> act, bool only_final_update = false)
                    // As this is a subview, identifier refers to the main view identifier
                    // act is performed for each ResponseData packet received
                    // only_final_update makes act only performed for the latest packet. The rest are ignored.
                    // Set only_final_update to false if you need something to happen for every packet sent, in the event more than 1 packet is received this frame
                    if (ApplyUpdates(view.Identifier, PerformUpdateWithResponse, only_final_update: true))
                    {
                        // Do something if at least one ResponseData packet was processed this frame for the specified view
                        Mod.LogInfo("Received some data!");
                    }
                }

            }

            private void PerformUpdateWithResponse(ResponseData data)
            {
                // Do something for each ResponseData packet received
                // This is ECS only
                //Mod.LogInfo(data.Text);
                if (data == null)
                    return;

                using NativeArray<CLinkedView> linkedViews = Query.ToComponentDataArray<CLinkedView>(Allocator.Temp);
                using NativeArray<CFirstPersonPlayer> components = Query.ToComponentDataArray<CFirstPersonPlayer>(Allocator.Temp);
                using var ents = Query.ToEntityArray(Allocator.Temp);

                // When Camera is initialised in UpdateData, this is called in callback and sets the component
                for (var i = 0; i < ents.Length; i++)
                {
                    var ent = ents[i];
                    var my_component = components[i];

                    my_component.IsInitialised = data.IsInitialised;
                    my_component.IsActive = data.IsActive;
                    Set(ent, my_component);
                }
            }
        }

        [MessagePackObject(false)]
        public class ViewData : ISpecificViewData, IViewData.ICheckForChanges<ViewData>
        {
            
            [Key(0)] public bool IsActive;
            [Key(1)] public bool IsInitialised;
            [Key(2)] public Vector3 MovementVector;
            [Key(4)] public int Source;
            [Key(5)] public float LookSensitivity;
            [Key(6)] public float Speed;

            public IUpdatableObject GetRelevantSubview(IObjectView view)
            {
                GameObject gO = view.GameObject;

                if (gO == null)
                {
                    Mod.LogError("GameObject to add FirstPersonPlayerView subview does not exist");
                    return null;
                }
                if (!gO.GetComponent<FirstPersonPlayerView>())
                {
                    gO.AddComponent<FirstPersonPlayerView>();
                    Mod.LogInfo("Added FirstPersonPlayerView");
                }
                return view.GetSubView<FirstPersonPlayerView>();
            }


            public bool IsChangedFrom(ViewData check)
            {
                return true;
            }
        }


        // Definition of Message Packet that will be sent back to host via a callback
        // This should contain the minimum amount of data necessary to perform the view's function.
        // You MUST mark your ViewData as MessagePackObject
        // If you don't, the game will run locally but fail in multiplayer
        [MessagePackObject(false)]
        public class ResponseData : IResponseData, IViewResponseData
        {
            [Key(0)] public bool IsActive;
            [Key(1)] public bool IsInitialised;
        }


        // Cached callback to send data back to host.
        // First parameter is the ResponseData instance
        // Second parameter is typeof(ResponseData). This is used to identify the view system that will handle the response
        // Callback is initialized after the first ViewData is received
        private Action<IResponseData, Type> Callback;


        // Some private fields used for example. Can be ignored
        private bool wasPressed = false;
        private int counter = 0;
        private KeyControl incrementCounterKey = Keyboard.current.yKey;


        // This runs locally for each client every frame
        public void Update()
        {
            if (incrementCounterKey.isPressed)
            {
                if (!wasPressed)
                {
                    Mod.LogInfo($"Incremented counter to {++counter}");
                }
                wasPressed = true;
            }
            else wasPressed = false;
        }

        private GameObject firstPersonCamera = null;

        List<InputAction> movementAndLookActions = new List<InputAction>();
        //private InputAction rgtStick;
        private InputAction lookAction;
        private InputAction moveAction;
        private float xRotation = 0f;
        private KeyControl toggleCameraKey;

        private const string ITEM_HOLDPOINT_PATH = "MorphmanPlus/Hold Points/Item Hold Point";

        // This is done so some aspects are only run once, instead of every frame TODO: REWORK REWORK REWORK
        private bool active = true;

        protected override void UpdateData(ViewData data)
        {
            if (data.Source != InputSourceIdentifier.Identifier)
                return;


            // Initializing Camera
            if (!data.IsInitialised)
            {
                // Sets initialise on the component.
                Callback.Invoke(new ResponseData
                {
                    IsInitialised = true,
                    IsActive = data.IsActive
                }, typeof(ResponseData));


                // Camera Setup
                firstPersonCamera = Instantiate(Mod.Bundle.LoadAsset<GameObject>("FPV Camera"));
                firstPersonCamera.transform.parent = transform;
                Vector3 pos = new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z);
                firstPersonCamera.transform.SetPositionAndRotation(pos, transform.rotation);


                // Input Init
                foreach (var action in InputSystem.ListEnabledActions())
                {
                    if (action.name == "Movement" || action.name == "Look")
                    {
                        movementAndLookActions.Add(action);
                    }
                }

                lookAction = new InputAction("look", binding: "<Mouse>/delta");
                lookAction.AddBinding("<Gamepad>/rightStick").WithName("Gamepad");

                //rgtStick = new InputAction("RightStick", binding: "<Gamepad>/rightStick");

                moveAction = new InputAction("move", binding: "<Gamepad>/leftStick", processors: "stickDeadzone(min=0.125,max=0.925)");
                moveAction.AddCompositeBinding("Dpad")
                    .With("Up", "<Keyboard>/w")
                    .With("Down", "<Keyboard>/s")
                    .With("Left", "<Keyboard>/a")
                    .With("Right", "<Keyboard>/d");

                toggleCameraKey = Keyboard.current.f5Key;
            }

            // Anything below here requires the camera gameobject to not be null to be activated
            if (firstPersonCamera == null)
                return;

            // Toggle Active
            if (toggleCameraKey.wasPressedThisFrame)
            {
                Callback.Invoke(new ResponseData
                {
                    IsActive = !data.IsActive,
                    IsInitialised = data.IsInitialised
                }, typeof(ResponseData));
            }
            
            if (data.IsActive)
            {
                if (!active)
                {
                    active = true;

                    firstPersonCamera.gameObject.SetActive(true);

                    moveAction.Enable();
                    //rgtStick.Enable();
                    lookAction.Enable();
                    foreach (var action in movementAndLookActions)
                    {
                        action.Disable();
                    }

                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;

                    Vector3 desiredHoldPointPosition = new Vector3(0f, 0.65f, 0.55f);
                    transform.Find(ITEM_HOLDPOINT_PATH).localPosition = desiredHoldPointPosition;
                }
            }
            else
            { 
                if (active)
                {
                    active = false;

                    firstPersonCamera.gameObject.SetActive(false);

                    moveAction.Disable();
                    //rgtStick.Disable();
                    lookAction.Disable();
                    foreach (var action in movementAndLookActions)
                    {
                        action.Enable();
                    }

                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;

                    Vector3 origLocalPos = new Vector3(0f, 1.158f, 0.336f);
                    transform.Find(ITEM_HOLDPOINT_PATH).localPosition = origLocalPos;
                }
            }

            

            // Movement
            float moveSpeed = 60f;
            Vector2 movementDir = moveAction.ReadValue<Vector2>().normalized;
            Vector3 move = transform.right * movementDir.x + transform.forward * movementDir.y;
            GetComponent<Rigidbody>().AddForce(move * moveSpeed * data.Speed * Time.deltaTime, ForceMode.VelocityChange);


            // Look movement
            Vector2 looking = lookAction.ReadValue<Vector2>();
            /*float inputDeviceMultiplier = 0f;
            if (data.IsGamepadPlayer)
            {
                inputDeviceMultiplier = 8f;
            }*/
            float lookX = looking.x * data.LookSensitivity * Time.deltaTime;
            float lookY = looking.y * data.LookSensitivity * Time.deltaTime;

            xRotation -= lookY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);

            firstPersonCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

            transform.Rotate(Vector3.up * lookX);
        }



        // This is automatically called after each UpdateData call
        // Hence, this is when Callback is initialized
        public void SetCallback(Action<IResponseData, Type> callback)
        {
            // Cache callback to send data back to host.
            Callback = callback;
        }
    }
}

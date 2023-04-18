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

                // Cache Entity Queries
                // This should contain ALL IComponentData that will be used in the class
                Query = GetEntityQuery(typeof(CLinkedView), typeof(CFirstPersonPlayer));


                toggleCameraKey = Keyboard.current.f5Key;
            }

            protected override void OnUpdate()
            {
                if (Query.IsEmpty) return;

                using NativeArray<CLinkedView> linkedViews = Query.ToComponentDataArray<CLinkedView>(Allocator.Temp);
                using NativeArray<CFirstPersonPlayer> components = Query.ToComponentDataArray<CFirstPersonPlayer>(Allocator.Temp);
                using var ents = Query.ToEntityArray(Allocator.Temp);


                for (var i = 0; i < ents.Length; i++)
                {
                    var ent = ents[i];
                    var my_component = components[i];

                    //TODO: check if player is local player, maybe move to viewdata
                    if (!components[i].IsInitialised)
                        break;
                    if (toggleCameraKey.wasPressedThisFrame)
                    {
                        my_component.IsActive = !my_component.IsActive;
                        Set(ent, my_component);
                    }
                }

                foreach (CLinkedView view in linkedViews)
                {
                    SendUpdate(view, new ViewData { IsActive = components[0].IsActive, IsInitialised = components[0].IsInitialised, Source = InputSourceIdentifier.Identifier });

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
            // You MUST also and mark each field with a key
            // All players must be running versions of the game with the same assigned keys.
            // It is recommended not to change keys after releasing your mod
            // The specifc key used does not matter, as long as there is no overlap.
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
            // Remember that this is Monobehaviour, not ECS
            // Use this to prepare the response data to be sent
            // You can use Callback here as well. But must perform a null check, since Callback may not be initialized (I'm not sure I recommend this XD)

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
        private InputAction rgtStick;
        private InputAction lookAction;
        private InputAction moveAction;

        // This is done so some aspects are only run once, instead of every frame TODO: REWORK REWORK REWORK
        private bool active = false;

        protected override void UpdateData(ViewData data)
        {
            // Perform any view updates here
            // Remember that this is Monobehaviour, not ECS
            // Eg. You can change whether a GameObject is active or not
            if (data.Source != InputSourceIdentifier.Identifier)
                return;


            // Initializing Camera
            if (!data.IsInitialised)
            {
                // Sets initialise on the component.
                Callback.Invoke(new ResponseData
                {
                    IsInitialised = true
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

                rgtStick = new InputAction("RightStick", binding: "<Gamepad>/rightStick");

                moveAction = new InputAction("move", binding: "<Gamepad>/leftStick", processors: "stickDeadzone(min=0.125,max=0.925)");
                moveAction.AddCompositeBinding("Dpad")
                    .With("Up", "<Keyboard>/w")
                    .With("Down", "<Keyboard>/s")
                    .With("Left", "<Keyboard>/a")
                    .With("Right", "<Keyboard>/d");

                
            }

            // Anything below here requires the camera gameobject to not be null to be activated
            if (firstPersonCamera == null)
                return;

            
            if (data.IsActive)
            {
                if (!active)
                {
                    active = true;

                    firstPersonCamera.gameObject.SetActive(true);

                    moveAction.Enable();
                    rgtStick.Enable();
                    lookAction.Enable();
                    foreach (var action in movementAndLookActions)
                    {
                        action.Disable();
                    }

                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
            else
            { 
                if (active)
                {
                    active = false;

                    firstPersonCamera.gameObject.SetActive(false);

                    moveAction.Disable();
                    rgtStick.Disable();
                    lookAction.Disable();
                    foreach (var action in movementAndLookActions)
                    {
                        action.Enable();
                    }

                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
            }


            // Movement
            float moveSpeed = 60f;
            Vector2 movementDir = moveAction.ReadValue<Vector2>().normalized;
            Vector3 move = transform.right * movementDir.x + transform.forward * movementDir.y;
            GetComponent<Rigidbody>().AddForce(move * moveSpeed * Time.deltaTime, ForceMode.VelocityChange);


            // Mouse movement
            if (rgtStick.ReadValue<Vector2>().x != 0 || rgtStick.ReadValue<Vector2>().y != 0)
            {
                float x = rgtStick.ReadValue<Vector2>().x * 2f;
                float y = rgtStick.ReadValue<Vector2>().y * -2f;
                transform.Rotate(new Vector3(0, x, 0));
                firstPersonCamera.transform.Rotate(new Vector3(y, 0, 0));
            }

            Vector2 mouseMove = lookAction.ReadValue<Vector2>();
            float mouseX = mouseMove.x / 4;
            float mouseY = (mouseMove.y / 8) * -1;

            transform.Rotate(new Vector3(0, mouseX, 0));
            firstPersonCamera.transform.Rotate(new Vector3(mouseY, 0, 0));
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

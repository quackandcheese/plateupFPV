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
using KitchenLib.Preferences;

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

            protected override void Initialise()
            {
                base.Initialise();

                Query = GetEntityQuery(typeof(CLinkedView), typeof(CFirstPersonPlayer));
            }

            protected override void OnUpdate()
            {
                if (Query.IsEmpty) return;

                using NativeArray<CLinkedView> linkedViews = Query.ToComponentDataArray<CLinkedView>(Allocator.Temp);
                using NativeArray<CFirstPersonPlayer> components = Query.ToComponentDataArray<CFirstPersonPlayer>(Allocator.Temp);
                using NativeArray<CPlayer> playerComponent = Query.ToComponentDataArray<CPlayer>(Allocator.Temp);
                using var ents = Query.ToEntityArray(Allocator.Temp);

                using NativeArray<CInputData> inputDataComponent = Query.ToComponentDataArray<CInputData>(Allocator.Temp);

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

                //bool isPaused = base.Time.IsPaused;


                for (var i = 0; i < linkedViews.Length; i++)
                {

                    bool is_active = false;
                    if (inputDataComponent[i].State.Request == GameStateRequest.InLocalMenu)
                    {
                        is_active = false;
                    }
                    else
                    {
                        is_active = components[i].IsActive;
                    }
                    SendUpdate(linkedViews[i], new ViewData { IsActive = is_active, IsInitialised = components[i].IsInitialised, Source = playerComponent[i].InputSource, Speed = playerComponent[i].Speed, PlayerID = playerComponent[0].ID });
                }

                foreach (CLinkedView view in linkedViews)
                {
                    //SendUpdate(view, new ViewData { IsActive = components[0].IsActive, IsInitialised = components[0].IsInitialised, Source = playerComponent[0].InputSource, LookSensitivity = 5.0f, Speed = playerComponent[0].Speed });

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
                if (data == null)
                    return;

                using NativeArray<CLinkedView> linkedViews = Query.ToComponentDataArray<CLinkedView>(Allocator.Temp);
                using NativeArray<CFirstPersonPlayer> fppComponents = Query.ToComponentDataArray<CFirstPersonPlayer>(Allocator.Temp);
                using NativeArray<CPlayer> playerComponents = Query.ToComponentDataArray<CPlayer>(Allocator.Temp);
                using NativeArray<Entity> entities = Query.ToEntityArray(Allocator.Temp);

                // When Camera is initialised in UpdateData, this is called in callback and sets the component

                for (int i = 0; i < playerComponents.Length; i++)
                {
                    Entity ent = entities[i];
                    CPlayer player = playerComponents[i];
                    CFirstPersonPlayer fpp = fppComponents[i];

                    if (player.InputSource != data.Source)
                        continue;

                    fpp.IsInitialised = data.IsInitialised;
                    fpp.IsActive = data.IsActive;
                    Set(ent, fpp);
                    break;
                }
                return;
            }
        }

        [MessagePackObject(false)]
        public class ViewData : ISpecificViewData, IViewData.ICheckForChanges<ViewData>
        {
            [Key(0)] public int Source;
            [Key(1)] public bool IsInitialised;
            [Key(2)] public bool IsActive;
            [Key(3)] public float Speed;
            [Key(4)] public int PlayerID;

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
                return IsActive != check.IsActive || IsInitialised != check.IsInitialised || Source != check.Source || Speed != check.Speed;
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
            [Key(2)] public int Source;
        }


        // Cached callback to send data back to host.
        // First parameter is the ResponseData instance
        // Second parameter is typeof(ResponseData). This is used to identify the view system that will handle the response
        // Callback is initialized after the first ViewData is received
        private Action<IResponseData, Type> Callback;

        public FirstPersonPlayerView.ViewData Data;

        // This runs locally for each client every frame
        public void Update()
        {
            if (Data.Source != InputSourceIdentifier.Identifier)
                return;


            // Toggle Active
            if (toggleCameraKey.wasPressedThisFrame)
            {
                if (Callback != null)
                {
                    Callback.Invoke(new ResponseData
                    {
                        IsActive = !Data.IsActive,
                        IsInitialised = Data.IsInitialised,
                        Source = Data.Source,
                    }, typeof(ResponseData));
                }
                else
                {
                    Mod.LogError("*** CALLBACK IS NULL ***");
                }
            }


            PreferenceInt playerModelVisibilityPreference = Mod.PrefManager.GetPreference<PreferenceInt>(Mod.PLAYER_MODEL_VISIBLE_ID);
            int playerModelVisibility = playerModelVisibilityPreference.Get();


            if (!Data.IsActive)
            {
                transform.Find(PLAYER_MODEL_PATH).gameObject.SetActive(true);
                transform.Find(COSMETICS_PATH).gameObject.SetActive(true);
                return;
            }

            // Player Model Visibility
            if (playerModelVisibility == 0)
            {
                transform.Find(PLAYER_MODEL_PATH).gameObject.SetActive(false);
                transform.Find(COSMETICS_PATH).gameObject.SetActive(false);
            }
            else
            {
                transform.Find(PLAYER_MODEL_PATH).gameObject.SetActive(true);
                transform.Find(COSMETICS_PATH).gameObject.SetActive(true);
            }

            // FOV
            PreferenceInt fovPreference = Mod.PrefManager.GetPreference<PreferenceInt>(Mod.FOV_ID);
            int fov = fovPreference.Get();

            Camera fpvCam = firstPersonCamera.GetComponent<Camera>();
            if (fpvCam != null)
            {
                fpvCam.fieldOfView = fov;
            }

            // Movement
            float moveSpeed = 3000f;
            Vector2 movementDir = moveAction.ReadValue<Vector2>().normalized;
            Vector3 move = transform.right * movementDir.x + transform.forward * movementDir.y;
            GetComponent<Rigidbody>().AddForce(move * moveSpeed * Data.Speed * Time.deltaTime);


            // Look movement
            Vector2 looking = lookAction.ReadValue<Vector2>();
            /*float inputDeviceMultiplier = 0f;
            if (data.IsGamepadPlayer)
            {
                inputDeviceMultiplier = 8f;
            }*/

            PreferenceFloat sensitivityFloat = Mod.PrefManager.GetPreference<PreferenceFloat>(Mod.SENSITIVITY_ID);
            float lookSensitivity = sensitivityFloat.Get();

            float lookX = looking.x * lookSensitivity * Time.deltaTime;
            float lookY = looking.y * lookSensitivity * Time.deltaTime;

            xRotation -= lookY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);

            firstPersonCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

            transform.Rotate(Vector3.up * lookX);


            // Hold Point Rotation
            transform.Find(ITEM_HOLDPOINT_PATH).rotation = firstPersonCamera.transform.Find("HoldPoint").rotation;
            transform.Find(ITEM_HOLDPOINT_PATH).position = firstPersonCamera.transform.Find("HoldPoint").position;
        }

        
        private GameObject firstPersonCamera = null;

        List<InputAction> movementAndLookActions = new List<InputAction>();
        private InputAction lookAction;
        private InputAction moveAction;
        private float xRotation = 0f;
        private KeyControl toggleCameraKey = Keyboard.current.f5Key;

        private const string PLAYER_MODEL_PATH = "MorphmanPlus/Body";
        private const string COSMETICS_PATH = "Cosmetics";
        private const string ITEM_HOLDPOINT_PATH = "MorphmanPlus/Hold Points/Item Hold Point";
        private const string HOLDPOINTS_PATH = "MorphmanPlus/Hold Points";


        protected override void UpdateData(ViewData data)
        {
            this.Data = data;

            if (data.Source != InputSourceIdentifier.Identifier)
                return;

            // Initializing Camera
            if (!data.IsInitialised)
            {
                // Sets initialise on the component.
                Callback.Invoke(new ResponseData
                {
                    IsInitialised = true,
                    IsActive = data.IsActive,
                    Source = data.Source,
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

                lookAction = new InputAction("look", InputActionType.Value);

                moveAction = new InputAction("move", InputActionType.Value);

                if (InputSourceIdentifier.DefaultInputSource.GetCurrentController(data.PlayerID) == ControllerType.Keyboard)
                {
                    moveAction.AddCompositeBinding("Dpad")
                        .With("Up", "<Keyboard>/w")
                        .With("Down", "<Keyboard>/s")
                        .With("Left", "<Keyboard>/a")
                        .With("Right", "<Keyboard>/d");
                    lookAction.AddBinding("<Mouse>/delta");
                }
                else
                {
                    moveAction.AddBinding("<Gamepad>/rightStick").WithProcessor("stickDeadzone(min=0.4,max=0.5)");
                    lookAction.AddBinding("<Gamepad>/rightStick")
                        .WithProcessor("stickDeadzone(min=0.125,max=0.925)")
                        .WithProcessor("scaleVector2(x=50,y=50)");
                }
            }

            // Anything below here requires the camera gameobject to not be null to be activated
            if (firstPersonCamera == null)
                return;


            
            if (data.IsActive)
            {
                firstPersonCamera.gameObject.SetActive(true);

                moveAction.Enable();
                lookAction.Enable();
                foreach (var action in movementAndLookActions)
                {
                    action.Disable();
                }

                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                //Vector3 desiredHoldPointPosition = new Vector3(0f, 0.65f, 0.55f);
                Vector3 desiredHoldPointLocalPosition = new Vector3(0f, 0f, 0f);
                transform.Find(ITEM_HOLDPOINT_PATH).localPosition = desiredHoldPointLocalPosition;
            }
            else
            {
                firstPersonCamera.gameObject.SetActive(false);

                moveAction.Disable();
                lookAction.Disable();
                foreach (var action in movementAndLookActions)
                {
                    action.Enable();
                }

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                Vector3 origLocalPos = new Vector3(0f, 1.158f, 0.336f);
                transform.Find(ITEM_HOLDPOINT_PATH).localPosition = origLocalPos;

                Quaternion origLocalRot = Quaternion.identity;
                transform.Find(ITEM_HOLDPOINT_PATH).localRotation = origLocalRot;

                //TODO: Fix. I believe it doesn't return to proper position because in Update you are updating the Hold Points pos and rot, while here it is the item holdpoint.
            }
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

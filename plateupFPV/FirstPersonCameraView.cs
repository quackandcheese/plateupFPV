using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KitchenFirstPersonView
{
    public struct CFirstPersonPlayer : IModComponent
    {
        public bool IsActive = false,
        public bool IsInitialised = false
    }
    
    public class FirstPersonPlayerView : UpdatableObjectView<FirstPersonPlayerView.MyViewData>
    {
        public class FirstPersonViewSystemBase : IncrementalViewSystemBase<MyViewData>, IModSystem
        {
            private EntityQuery _playerQuery;
            private InputAction moveAction;

            protected override void Initialise()
            {
                base.Initialise();
                _playerQuery = GetEntityQuery(new QueryHelper().All(typeof(CFirstPersonPlayer), typeof(CLinkedView)));
                
                moveAction = new InputAction("move", binding: "<Gamepad>/leftStick", processors: "stickDeadzone(min=0.125,max=0.925)");
                moveAction.AddCompositeBinding("Dpad")
                    .With("Up", "<Keyboard>/w")
                    .With("Down", "<Keyboard>/s")
                    .With("Left", "<Keyboard>/a")
                    .With("Right", "<Keyboard>/d");
                    
                private KeyControl toggleCameraKey = Keyboard.current.f1Key;
            }

            protected override void OnUpdate()
            {
                if (_myEntityQuery.IsEmpty) return;

                using NativeArray<CLinkedView> nativeArray = _myEntityQuery.ToComponentDataArray<CLinkedView>(Allocator.Temp);
                using var components = _playerQuery.ToComponentDataArray<CFirstPersonPlayer>(Allocator.Temp);

                //Add logic for toggling IsActive

                for (int i = 0; i < nativeArray.Length; i++)
                {
                    SendUpdate(nativeArray[i], new FirstPersonPlayerViewData { IsActive = components[0].IsActive, IsInitialised = components[0].IsInitialised, MovementVector = moveAction.ReadValue<Vector2>().normalized});
                }
            }
        }

        [MessagePackObject]
        public struct FirstPersonPlayerViewData : ISpecificViewData, IViewData, IViewResponseData, IViewData.ICheckForChanges<MyViewData>
        {
            [Key(0)] public bool IsActve;
            [Key(1)] public bool IsInitialised;
            [Key(2)] public Vector3 movementVector;

            public IUpdatableObject GetRelevantSubview(IObjectView view)
            {
                return view.GameObject.AddComponent<IndicatorLookAtPlayerView>();
                //if this view component is already on one of your prefabs from your asset bundle, you can just use the built in method
                // return view.GetSubView<MyView>();
            }

            public bool IsChangedFrom(MyViewData check)
            {
                return PlayerPosition.x != check.PlayerPosition.x || PlayerPosition.y != check.PlayerPosition.y || PlayerPosition.z != check.PlayerPosition.z;
            }
        }

        protected override void UpdateData(MyViewData data)
        {
            //this method lets you manipulate the gameobject 
            
            foreach (Transform child in transform)
            {
                //Quaternion originalRotation = child.rotation;


                //if (data.IsFirstPerson)
                //{
                //    originalRotation = child.rotation;
                    child.LookAt(data.PlayerPosition);
                child.Rotate(Vector3.right, -90);

                   
                //}
                //else
                //{
                //    child.rotation = originalRotation;
                //}
            }
        }
    }
}

using Kitchen;
using KitchenMods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace KitchenFirstPersonView
{
    public class ProgressIndicatorsLookAtPlayer : GenericSystemBase, IModSystem
    {
        private EntityQuery EntitiesToActOn;
        private EntityQuery PlayerStuff;

        protected override void Initialise()
        {
            base.Initialise();
            EntitiesToActOn = GetEntityQuery(typeof(CProgressIndicator), typeof(CPosition));

            PlayerStuff = GetEntityQuery(typeof(CPlayer), typeof(CPosition));
        }

        protected override void OnUpdate()
        {
            using var ents = EntitiesToActOn.ToEntityArray(Allocator.Temp);

            using var my_components2 = EntitiesToActOn.ToComponentDataArray<CPosition>(Allocator.Temp);

            for (var i = 0; i < ents.Length; i++)
            {
                var ent = ents[i];
                var my_component = my_components2[i];

                var playerPos = GetPlayerPosition();

                if ((playerPos != null || playerPos != default) && ent != default)
                {
                    my_component.Rotation = Quaternion.LookRotation(playerPos, Vector3.up);
                    Set(ent, my_component);
                }
            }
        }
        private Vector3 GetPlayerPosition()
        {
            using var playerEnts = PlayerStuff.ToEntityArray(Allocator.Temp);
            using var my_components = PlayerStuff.ToComponentDataArray<CPosition>(Allocator.Temp);

            for (var i = 0; i < playerEnts.Length; i++)
            {
                var ent = playerEnts[i];
                var my_component = my_components[i];

                if (ent != default)
                {
                    return my_component.Position;
                }
            }
            return default;
        }
    }
}

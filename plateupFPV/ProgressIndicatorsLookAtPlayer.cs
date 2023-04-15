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
    public struct CFirstPersonIndicator : IModComponent
    {
    }

    public class SetLookAtData : GenericSystemBase, IModSystem
    {
        private EntityQuery indicators;

        protected override void Initialise()
        {
            base.Initialise();
            indicators = GetEntityQuery(new QueryHelper()
                    .All(
                        typeof(CIndicator),
                        typeof(CPosition))
                    .None(
                        typeof(CFirstPersonIndicator)
                    ));
        }

        protected override void OnUpdate()
        {
            var indicators = this.indicators.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < indicators.Length; i++)
            {
                Set(indicators[i], new CFirstPersonIndicator()
                {
                });
            }
            indicators.Dispose();
        }
    }
}

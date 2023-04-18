using Kitchen;
using KitchenMods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using static Kitchen.AchievementTrackView;

namespace KitchenFirstPersonView
{
    public class ManagePlayerFirstPersonView : GenericSystemBase, IModSystem
    {
        private EntityQuery playerQuery;

        protected override void Initialise()
        {
            base.Initialise();

            playerQuery = GetEntityQuery(new QueryHelper()
                .All(
                    typeof(CPlayer),
                    typeof(CPosition))
                .None(
                    typeof(CFirstPersonPlayer)));
        }

        protected override void OnUpdate()
        {
            using var players = playerQuery.ToEntityArray(Unity.Collections.Allocator.TempJob);

            for (int i = 0; i < players.Length; i++)
            {
                Set(players[i], new CFirstPersonPlayer()
                {
                    IsActive = false,
                    IsInitialised = false
                });
            }
        }
    }
}

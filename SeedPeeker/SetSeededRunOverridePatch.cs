using HarmonyLib;
using Kitchen;
using KitchenData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;

namespace SeedPeeker
{
    [HarmonyPatch(typeof(SetSeededRunOverride))]
    public static class SetSeededRunOverridePatch
    {
        public class UnlockChain
        {
            public UnlockChain Chain1;
            public UnlockChain Chain2;

            public Unlock Unlock;
        }

        public static HashSet<int> CurrentUnlockIDs = new HashSet<int>();
        public static List<CProgressionRequest> Requests;

        [HarmonyPatch("CreateSeededRun")]
        [HarmonyPostfix]
        public static void CreateSeededRun_PostFix(Seed seed)
        {
            int category_seed = 848292;
            int instance = 3;

            int instance_seed = category_seed * 1231231 + instance;


            UnityEngine.Random.State cachedState = UnityEngine.Random.state;
            UnityEngine.Random.InitState(seed.IntValue + 124192293 * instance_seed);

            bool choose_bias_option = UnityEngine.Random.value > 0.5f;

            Requests = new List<CProgressionRequest>()
            {
                new CProgressionRequest() {Group = UnlockGroup.Generic},
                new CProgressionRequest() {Group = UnlockGroup.Dish},
            };

            foreach (CProgressionRequest request in Requests)
            {
                if (!AddUnlock(request.Group, choose_bias_option, 0))
                {
                    foreach (CProgressionRequest request2 in Requests)
                    {
                        if (request.Group != request2.Group)
                        {
                            AddUnlock(request2.Group, false, 0);
                        }
                    }
                }
            }

            UnityEngine.Random.state = cachedState;
        }

        public static UnlockChain Recurse(UnlockChain chain, int step)
        {
            // base case: 
        }

        public static bool AddUnlock(UnlockGroup group, bool choose_bias_option, int tier)
        {
            List<Unlock> unlocks = GetUnlocks(group, choose_bias_option, tier);
            foreach (Unlock unlock in unlocks)
            {
                bool flag = CurrentUnlockIDs.Contains(unlock.ID);
                if (!flag)
                {
                    bool flag2 = unlock.Requires.Any((Unlock r) => !CurrentUnlockIDs.Contains(r.ID));
                    if (!flag2)
                    {
                        bool flag3 = unlock.BlockedBy.Any((Unlock r) => CurrentUnlockIDs.Contains(r.ID));
                        if (!flag3)
                        {
                            CurrentUnlockIDs.Add(unlock.ID);
                            SeedPeekerMod.DebugLogger.LogInfo("Potential ID: " + unlock.ID);
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public static List<Unlock> GetUnlocks(UnlockGroup request_type, bool only_biased_dishes = false, int tier = 0)
        {
            double priority = 0.3;
            return (from u in GameData.Main.Get<Unlock>()
                    where u.IsUnlockable && u.UnlockGroup == request_type && !CurrentUnlockIDs.Contains(u.ID) && (u.IsSpecificFranchiseTier ? (u.MinimumFranchiseTier == tier) : (u.MinimumFranchiseTier <= tier))
                    orderby (double)UnityEngine.Random.value * priority * (double)(1f - u.SelectionBias)
                    select u).ToList<Unlock>();
        }
    }
}

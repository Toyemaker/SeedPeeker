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
using static SeedPeeker.SeedPeekerMod;

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
            public HashSet<int> CurrentUnlockIDs = new HashSet<int>();

            public int Step;
            public int Index;
        }

        public static HashSet<int> CurrentUnlockIDs = new HashSet<int>();
        public static List<CProgressionRequest> Requests;
        public static Seed Seed;
        public static UnlockChain Root;

        [HarmonyPatch("CreateSeededRun")]
        [HarmonyPostfix]
        public static void CreateSeededRun_PostFix(Seed seed)
        {
            Seed = seed;

            Recurse(null, 0, 0);
        }

        public static UnlockChain Recurse(UnlockChain chain, Unlock unlock, int step, int index)
        {
            UnlockChain chain = new UnlockChain()
            {
                Step = step,
                Index = index,
                Unlock = unlock,

            };

            if(step < 3)
            {
                Unlock[] unlocks = GetNextUnlocks();

                chain.Chain1 = Recurse(chain, unlocks[0], 1, index * 2);
                chain.Chain2 = Recurse(chain, unlocks[1], 1, index * 2 + 1);
            }
            else
            {
                return chain;
            }
        }

        public static Unlock[] GetNextUnlocks()
        {
            int category_seed = 848292;
            int instance = 3;

            int instance_seed = category_seed * 1231231 + instance;


            UnityEngine.Random.State cachedState = UnityEngine.Random.state;
            UnityEngine.Random.InitState(Seed.IntValue + 124192293 * instance_seed);

            bool choose_bias_option = UnityEngine.Random.value > 0.5f;

            Requests = new List<CProgressionRequest>()
            {
                new CProgressionRequest() {Group = UnlockGroup.Generic},
                new CProgressionRequest() {Group = UnlockGroup.Dish},
            };

            int i = 0;
            Unlock[] unlocks = new Unlock[2];
            foreach (CProgressionRequest request in Requests)
            {
                if (!AddUnlock(request.Group, choose_bias_option, 0, out unlocks[i++]))
                {
                    i--;
                    foreach (CProgressionRequest request2 in Requests)
                    {
                        if (request.Group != request2.Group)
                        {
                            DebugLogger.LogInfo("unlock2");
                            AddUnlock(request2.Group, false, 0, out unlocks[i++]);
                        }
                    }
                }
            }

            UnityEngine.Random.state = cachedState;

            return unlocks;
        }

        public static bool AddUnlock(UnlockGroup group, bool choose_bias_option, int tier, out Unlock returnUnlock)
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
                            returnUnlock = unlock;
                            SeedPeekerMod.DebugLogger.LogInfo("Potential ID: " + unlock.ID);
                            return true;
                        }
                    }
                }
            }
            returnUnlock = default;
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

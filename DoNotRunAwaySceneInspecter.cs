using Elements.Core;
using FrooxEngine;
using HarmonyLib;
using ResoniteModLoader;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace DoNotRunAwaySceneInspecter
{
    public class DoNotRunAwaySceneInspecter : ResoniteMod
    {
        public override string Name => "DoNotRunAwaySceneInspecter";
        public override string Author => "Sinduy";
        public override string Version => FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion;
        public override string Link => "https://github.com/sjsanjsrh/DoNotRunAwaySceneInspecter";
        public static readonly string DOMAIN_NAME = "com.Sinduy.DoNotRunAwaySceneInspecter";

        [AutoRegisterConfigKey]
        private static readonly ModConfigurationKey<bool> enabled =
          new ModConfigurationKey<bool>("enabled", "Should the mod be enabled", () => true);
        [AutoRegisterConfigKey]
        private static readonly ModConfigurationKey<float> distance =
          new ModConfigurationKey<float>("distance", "Distance to move the worker inspector slot if this value is 0 then set inspector position is in front of User", () => 0.1f);
        private static ModConfiguration Config;

        public static bool Enabled => Config.GetValue(enabled);
        public static float Distance => Config.GetValue(distance);

        public override void OnEngineInit()
        {
            Config = GetConfiguration();

            new Harmony(DOMAIN_NAME).PatchAll();
        }

        [HarmonyPatch(typeof(InspectorHelper), nameof(InspectorHelper.OpenInspectorForTarget), new Type[] { typeof(IWorldElement), typeof(Slot), typeof(bool) })]
        class InspectorHelper_OpenInspectorButton_Patch
        {
            public static bool Prefix(
                  IWorldElement target,
                  ref Slot source,
                  bool openWorkerOnly)
            {
                if (!Enabled) { return true; }

                if (Distance == 0.0) 
                {
                    source = null;
                    return true; 
                }

                if (target == null)
                    return false;
                Slot nearestParent1 = target.FindNearestParent<Slot>();
                User nearestParent2 = target.FindNearestParent<User>();
                Worker worker = openWorkerOnly ? target.FindNearestParent<Worker>() : null;
                if (worker == nearestParent1 || worker == nearestParent2)
                    worker = null;
                if (nearestParent1 == null && nearestParent2 == null && worker == null)
                {
                    UniLog.Warning("The target is neither on a Slot nor on an User.\n" + target.ParentHierarchyToString());
                }
                else
                {
                    Slot slot1 = target.World.LocalUserSpace.AddSlot("Inspector");
                    Slot slot2 = source?.GetComponentInParents<SceneInspector>()?.Slot ?? source?.GetComponentInParents<UserInspector>()?.Slot;
                    if (worker != null)
                        WorkerInspector.Create(slot1, worker);
                    else if (nearestParent1 != null)
                    {
                        SceneInspector sceneInspector = slot1.AttachComponent<SceneInspector>();
                        sceneInspector.Root.Target = nearestParent1;
                        sceneInspector.ComponentView.Target = nearestParent1;
                    }
                    else if (nearestParent2 != null)
                    {
                        UserInspector userInspector = slot1.AttachComponent<UserInspector>();
                        userInspector.ViewUser.Target = nearestParent2;
                        Stream nearestParent3 = target.FindNearestParent<Stream>();
                        if (nearestParent3 != null)
                        {
                            userInspector.ViewGroup.Value = UserInspector.View.Streams;
                            userInspector.ViewStreamGroup.Value = nearestParent3.GroupIndex;
                        }
                        else if (target.FindNearestParent<UserComponent>() != null)
                            userInspector.ViewGroup.Value = UserInspector.View.Components;
                        else
                            userInspector.ViewGroup.Value = UserInspector.View.User;
                    }
                    if (slot2 != null)
                    {
                        slot1.CopyTransform(slot2);
                        if (Distance > 0.0)
                        {
                            slot1.Position_Field.TweenTo(slot1.LocalPosition + (slot1.LocalRotation * float3.Backward * Distance), 0.2f);
                        }
                        else
                        {
                            slot2.Position_Field.TweenTo(slot2.LocalPosition + (slot2.LocalRotation * float3.Backward * Distance), 0.2f);
                        }
                    }
                    else
                        slot1.PositionInFrontOfUser(new float3?(float3.Backward));
                }

                return false;
            }
        }
    }
}

using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace KoiKoi
{
    [HarmonyPatch]
    public static class Patches
    {
        public static RenderTexture Texture;
        public static Camera CurrentCamera;
        public static Transform CurrentMonitor;
        
#if UNITY_EDITOR
        [HarmonyPatch(typeof(TestHarness))]
#else
        [HarmonyPatch(typeof(MouseControls))]
#endif
        [HarmonyPatch("Update")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> CastToCamera(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var getButtonMethod = AccessTools.Method(typeof(Input), 
#if UNITY_EDITOR
                nameof(Input.GetMouseButtonDown),
#else
                nameof(Input.GetKey),
#endif
                new[] { typeof(int) });
            var replaceCameraMethod = AccessTools.Method(typeof(Patches), nameof(ReplaceCamera));
            var jumpLabel = generator.DefineLabel();
            var last = new CodeInstruction(OpCodes.Nop);
#if UNITY_EDITOR
            sbyte raycastHitBoolVar = 16;
            sbyte raycastHitVar = 14;
#else
            sbyte raycastHitBoolVar = 7;
            sbyte raycastHitVar = 5;
#endif
            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldloc_S)
                {
                    var correct = false;
                    if (instruction.operand is LocalBuilder)
                        correct = ((LocalBuilder)instruction.operand).LocalIndex == raycastHitBoolVar;
                    else if (instruction.operand is sbyte)
                        correct = (sbyte)instruction.operand == raycastHitBoolVar;
                    if(correct)
                        instruction.labels.Add(jumpLabel);
                }

                if (
#if UNITY_EDITOR
                    last.opcode == OpCodes.Ldc_I4_0 &&
#endif
                    instruction.Calls(getButtonMethod)
                    )
                {
                    yield return new CodeInstruction(OpCodes.Pop);
                    yield return new CodeInstruction(OpCodes.Ldloca_S, raycastHitVar);
                    yield return new CodeInstruction(OpCodes.Call, replaceCameraMethod);
                    yield return new CodeInstruction(OpCodes.Dup);
                    yield return new CodeInstruction(OpCodes.Stloc_S, raycastHitBoolVar);
                    yield return new CodeInstruction(OpCodes.Brtrue, jumpLabel);
#if UNITY_EDITOR
                    yield return new CodeInstruction(OpCodes.Ldc_I4_0);
#else
                    yield return new CodeInstruction(OpCodes.Ldc_I4, (int)KeyCode.LeftControl);
#endif
                }
                last = instruction;
                yield return instruction;
            }
        }

        private static bool ReplaceCamera(ref RaycastHit hit)
        {
            if (Texture == null || CurrentCamera == null || CurrentMonitor == null || hit.collider == null || hit.collider.transform != CurrentMonitor)
                return false;
            var localPoint = -1 * (CurrentMonitor.InverseTransformPoint(hit.point) - new Vector3(0.5f, 0f, 0.5f));
            Ray ray = CurrentCamera.ScreenPointToRay(new Vector3(localPoint.x * Texture.width, localPoint.z * Texture.height, 0f));
            Debug.DrawRay(ray.origin, ray.direction);
            int layerMask = 1 << 11;
            bool rayCastHitSomething = Physics.Raycast(ray, out hit, 1000, layerMask);
            return rayCastHitSomething;
        }

        internal static void TPOnClaimChange(object __instance, string userNickName)
        {
            Forwards.TPBombComponent(__instance)?.GetComponent<KoiKoiModule>()?.SetTPClaim(userNickName);
        }

        internal static void TPOnUnclaim(object __instance)
        {
            TPOnClaimChange(__instance, "");
        }
    }
}
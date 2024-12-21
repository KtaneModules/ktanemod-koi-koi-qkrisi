using System.Collections;
using System.Runtime.CompilerServices;
using MemberForwarding;
using UnityEngine;

namespace KoiKoi
{
    public static class Forwards
    {
        [MemberForward("TwitchModule", "BombComponent", "TwitchPlaysAssembly")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static MonoBehaviour TPBombComponent(object __instance) => null;

        [MemberForward("TwitchModule", "Code", "TwitchPlaysAssembly")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string TPModuleID(object __instance) => null;

        [MemberForward("TwitchGame", "Modules", "TwitchPlaysAssembly")]
        [ObjectReference("TwitchGame", "Instance", "TwitchPlaysAssembly")]
        public static IEnumerable TPModules
        {
            [MethodImpl(MethodImplOptions.NoInlining)] get;
        }
    }
}
using UnityEngine;
using Steamworks;

namespace KoiKoi
{
    public class SteamAvatar
    {
        internal readonly uint Width;
        internal readonly uint Height;
        internal readonly byte[] RGBA;
        internal readonly Texture2D AvatarTexture;
        
        internal SteamAvatar(int handle)
        {
            SteamUtils.GetImageSize(handle, out Width, out Height);
            uint size = 4 * Height * Width;
            RGBA = new byte[size];
            SteamUtils.GetImageRGBA(handle, RGBA, (int)size);
            AvatarTexture = new Texture2D((int)Width, (int)Height, TextureFormat.RGBA32, false, true);
            AvatarTexture.LoadRawTextureData(RGBA);
            AvatarTexture.Apply();
        }
    }
}
using UnityEngine;

namespace KoiKoi
{
    public class TableSlot
    {
        public readonly Vector3 Position;
        
        public HanafudaCard CurrentCard;
        
        public bool IsFree => CurrentCard == null;

        public TableSlot(float x, float y, float z)
        {
            Position = new Vector3(x, y, z);
        }
    }
}
using System.Collections.Generic;
using System.Linq;

namespace KoiKoi
{
    public class KoiKoiHand : Dictionary<CardType, List<HanafudaCard>>
    {
    }
    
    public class YakuRule
    {
        public readonly CardType Type;
        public readonly int Count;

        public bool Check(KoiKoiHand hand) => hand[Type].Count >= Count;
        
        public YakuRule(CardType type, int count)
        {
            Type = type;
            Count = count;
        }
    }

    public class Yaku
    {
        public delegate int PointDelegate(KoiKoiHand hand);
        public delegate bool SpecialExceptionDelegate(KoiKoiHand hand);

        public readonly string Name;
        private readonly YakuRule[] RequiredRules;
        private readonly YakuRule[] ExceptionRules;
        public readonly PointDelegate GetPoints;
        private readonly SpecialExceptionDelegate SpecialException;

        public bool Check(KoiKoiHand hand) =>
            RequiredRules.All(rule => rule.Check(hand)) && !ExceptionRules.Any(rule => rule.Check(hand)) &&
            (SpecialException == null || !SpecialException(hand));

        public Yaku(string name, YakuRule[] requiredRules, YakuRule[] exceptionRules, PointDelegate getPoints, SpecialExceptionDelegate specialException = null)
        {
            Name = name;
            RequiredRules = requiredRules;
            ExceptionRules = exceptionRules ?? new YakuRule[0];
            GetPoints = getPoints;
            SpecialException = specialException;
        }
    }
}
using KoiKoi;

public partial class KoiKoiPlayroom
{
    private static readonly Yaku[] Yakus =
    {
        new Yaku("Five brights", new[]
        {
            new YakuRule(CardType.Bright, 5)
        }, null, hand => 15),
        new Yaku("Four brights", new[]
        {
            new YakuRule(CardType.Bright, 4)
        }, new[]
        {
            new YakuRule(CardType.Rain, 1)
        }, hand => 8),
        new Yaku("Four brights with rain", new[]
        {
            new YakuRule(CardType.Bright, 4),
            new YakuRule(CardType.Rain, 1)
        }, new[]
        {
            new YakuRule(CardType.Bright, 5)
        }, hand => 7),
        new Yaku("Three brights", new[]
        {
            new YakuRule(CardType.Bright, 3)
        }, new[]
        {
            new YakuRule(CardType.Bright, 4),
            new YakuRule(CardType.Rain, 1)
        }, hand => 6),
        new Yaku("Moon viewing", new[]
        {
            new YakuRule(CardType.Sake, 1),
            new YakuRule(CardType.Moon, 1)
        }, null, hand => 5),
        new Yaku("Cherry blossom viewing", new[]
        {
            new YakuRule(CardType.Sake, 1),
            new YakuRule(CardType.Curtain, 1)
        }, null, hand => 5),
        new Yaku("Boar-Deer-Butterflies", new[]
        {
            new YakuRule(CardType.Boar, 1),
            new YakuRule(CardType.Deer, 1),
            new YakuRule(CardType.Butterflies, 1)
        }, null, hand => 6),
        new Yaku("Animals", new[]
            {
                new YakuRule(CardType.Animal, 5)
            }, null, hand => hand[CardType.Animal].Count - 4),
        new Yaku("Ribbons", new[]
        {
            new YakuRule(CardType.Ribbon, 5)
        }, null, hand => hand[CardType.Ribbon].Count - 4),
        /*new Yaku("Blue ribbons and poetry slips", new[]
        {
            new YakuRule(CardType.BRibbon, 3),
            new YakuRule(CardType.Poetry, 3)
        }, null, hand => hand[CardType.Ribbon].Count + 4),*/
        new Yaku("Blue ribbons", new[]
        {
            new YakuRule(CardType.BRibbon, 3)
        }, null, hand => 6),
        new Yaku("Poetry ribbons", new[]
        {
            new YakuRule(CardType.Poetry, 3)
        }, null, hand => 6),
        new Yaku("Plains", new[]
        {
            new YakuRule(CardType.Plain, 10)
        }, null, hand => hand[CardType.Plain].Count - 9)
    };
}
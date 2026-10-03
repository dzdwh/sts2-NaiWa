using MegaCrit.Sts2.Core.MonsterMoves.Intents;

namespace NaiWa.monster;

public class LaughIntent : AbstractIntent
{
    public override IntentType IntentType => IntentType.Unknown;
    protected override string IntentPrefix => "LAUGH";
    protected override string SpritePath => "ui/intents/intent_laugh.png";
}

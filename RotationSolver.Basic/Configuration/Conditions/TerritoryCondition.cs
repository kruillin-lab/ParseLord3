namespace RotationSolver.Basic.Configuration.Conditions;

[Description("Territory Condition")]
internal class TerritoryCondition : DelayCondition
{
    public TerritoryConditionType TerritoryConditionType = TerritoryConditionType.TerritoryContentType;

    public int TerritoryId = 0;
    public string Name = "Not Chosen";

    protected override bool IsTrueInside(ICustomRotation rotation)
    {
        bool result = false;
        switch (TerritoryConditionType)
        {
            case TerritoryConditionType.TerritoryContentType:
                if (DataCenter.Territory != null)
                {
                    result = (int)DataCenter.Territory.Id == TerritoryId;
                }

                break;

            case TerritoryConditionType.DutyName:
                result = Name == DataCenter.Territory?.Name;
                break;

            case TerritoryConditionType.TerritoryName:
                result = Name == DataCenter.Territory?.Name;
                break;
        }
        return result;
    }
}

internal enum TerritoryConditionType : byte
{
    [Description("Territory Content Type")]
    TerritoryContentType,

    [Description("Territory Name")]
    TerritoryName,

    [Description("Duty Name")]
    DutyName,
}

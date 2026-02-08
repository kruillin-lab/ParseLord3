namespace RotationSolver.Basic.Configuration.Conditions;

[Description("Named Condition")]
internal class NamedCondition : DelayCondition
{
    public string ConditionName = "Not Chosen";
    protected override bool IsTrueInside(ICustomRotation rotation)
    {
        foreach ((string Name, ConditionSet Condition) in DataCenter.CurrentConditionValue.NamedConditions)
        {
            if (Name != ConditionName)
            {
                continue;
            }

            return Condition.IsTrue(rotation);
        }
        return false;
    }
}

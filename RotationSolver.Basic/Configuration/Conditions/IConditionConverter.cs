using Newtonsoft.Json.Linq;

namespace RotationSolver.Basic.Configuration.Conditions;

internal class IConditionConverter : JsonCreationConverter<ICondition>
{
    protected override ICondition? Create(JObject jObject)
    {
        if (FieldExists(nameof(ConditionSet.Conditions), jObject))
        {
            return new ConditionSet();
        }
        else if (FieldExists(nameof(ActionCondition.ActionConditionType), jObject))
        {
            return new ActionCondition();
        }
        else if (FieldExists(nameof(TargetCondition.TargetConditionType), jObject))
        {
            return new TargetCondition();
        }
        else if (FieldExists(nameof(RotationCondition.ComboConditionType), jObject))
        {
            return new RotationCondition();
        }
        else
        {
            return FieldExists(nameof(TraitCondition.TraitID), jObject)
                ? new TraitCondition()
                : FieldExists(nameof(NamedCondition.ConditionName), jObject)
                            ? new NamedCondition()
                            : FieldExists(nameof(TerritoryCondition.TerritoryConditionType), jObject) ? new TerritoryCondition() : (ICondition?)null;
        }
    }
}

internal abstract class JsonCreationConverter<T> : JsonConverter
{
    protected abstract T? Create(JObject jObject);

    public override bool CanConvert(Type objectType)
    {
        return typeof(T).IsAssignableFrom(objectType);
    }

    public override bool CanWrite => false;

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        // Load JObject from stream
        JObject jObject = JObject.Load(reader);

        // Create target object based on JObject
        T? target = Create(jObject);

        // Populate the object properties
        if (target != null)
        {
            serializer.Populate(jObject.CreateReader(), target);
        }

        return target;
    }

    protected static bool FieldExists(string fieldName, JObject jObject)
    {
        return jObject[fieldName] != null;
    }
}

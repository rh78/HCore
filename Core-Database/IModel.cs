namespace ReinhardHolzner.Core.Database
{
    public interface IModel<TJsonModel>
    {
        TJsonModel ConvertToJson();
    }
}

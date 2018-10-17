namespace HCore.Database
{
    public interface IModel<TJsonModel>
    {
        TJsonModel ConvertToJson();
    }
}

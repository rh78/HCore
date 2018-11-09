namespace HCore.Database.Models
{
    public interface IModel<TJsonModel>
    {
        TJsonModel ConvertToJson();
    }
}

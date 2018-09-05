using System;
using System.Collections.Generic;
using System.Text;

namespace ReinhardHolzner.HCore.ElasticSearch.Models
{
    public interface IModel<TJsonModel>
    {
        TJsonModel ConvertToJson();
    }
}

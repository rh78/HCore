using System;
using System.Collections.Generic;
using System.Text;

namespace ReinhardHolzner.HCore.Database
{
    public interface IModel<TJsonModel>
    {
        TJsonModel ConvertToJson();
    }
}

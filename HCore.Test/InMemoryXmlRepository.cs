using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.Repositories;

namespace HCore.Test
{
    internal class InMemoryXmlRepository : IXmlRepository
    {
        private readonly List<XElement> _elements = [];

        public IReadOnlyCollection<XElement> GetAllElements()
        {
            return _elements.ToList();
        }

        public void StoreElement(XElement element, string friendlyName)
        {
            _elements.Add(element);
        }
    }
}

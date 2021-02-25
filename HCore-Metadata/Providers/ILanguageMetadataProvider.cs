using System.Collections.Generic;

namespace HCore.Metadata
{
    public interface ILanguageMetadataProvider
    {
        public class LanguageCodeNameMapping
        {
            public string LanguageCode { get; internal set; }
            public string Name { get; internal set; }
        }

        public List<LanguageCodeNameMapping> GetLanguageList();

        public string GetLanguageName(string languageCode);
    }
}

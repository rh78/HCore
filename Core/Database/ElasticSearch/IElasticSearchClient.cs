using Nest;

namespace ReinhardHolzner.HCore.Database.ElasticSearch
{
    public interface IElasticSearchClient
    {
        ElasticClient ElasticClient { get; }

        void Initialize();

        IPromise<IIndexSettings> ConfigureNonConcatenateAndAutocompleteSettings(IndexSettingsDescriptor setting);
        IPromise<IIndexSettings> ConfigureConcatenateAndAutocompleteSettings(IndexSettingsDescriptor setting);
    }
}

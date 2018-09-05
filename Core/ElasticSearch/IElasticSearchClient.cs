using Nest;

namespace ReinhardHolzner.HCore.ElasticSearch
{
    public interface IElasticSearchClient
    {
        ElasticClient ElasticClient { get; }

        void Initialize();

        IPromise<IIndexSettings> ConfigureNonConcatenateAndAutocompleteSettings(IndexSettingsDescriptor setting);
        IPromise<IIndexSettings> ConfigureConcatenateAndAutocompleteSettings(IndexSettingsDescriptor setting);
    }
}

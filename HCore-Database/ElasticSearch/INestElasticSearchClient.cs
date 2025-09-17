using Nest;

namespace HCore.Database.ElasticSearch
{
    public interface INestElasticSearchClient
    {
        ElasticClient ElasticClient { get; }

        void Initialize();

        IPromise<IIndexSettings> ConfigureNonConcatenateAndAutocompleteSettings(IndexSettingsDescriptor setting);
        IPromise<IIndexSettings> ConfigureConcatenateAndAutocompleteSettings(IndexSettingsDescriptor setting);
    }
}

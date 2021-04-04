using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Spark.Engine.Core;
using Spark.Engine.Utility;
using Spark.Service;
using System;
using System.Collections.Generic;
using Task = System.Threading.Tasks.Task;

namespace Spark.Engine.Service
{
    [Obsolete("Use AsyncFhirService instead")]
    public class FhirService : IFhirService
    {
        private readonly IAsyncFhirService _asyncFhirService;

        public FhirService(IAsyncFhirService asyncFhirService)
        {
            _asyncFhirService = asyncFhirService ?? throw new ArgumentNullException(nameof(asyncFhirService));
        }

        public FhirResponse Read(IKey key, ConditionalHeaderParameters parameters = null)
        {
            return AsyncHelpers.RunSync(() => _asyncFhirService.ReadAsync(key, parameters));
        }

        public FhirResponse ReadMeta(IKey key)
        {
            return AsyncHelpers.RunSync(() => _asyncFhirService.ReadMetaAsync(key));
        }

        public FhirResponse AddMeta(IKey key, Parameters parameters)
        {
            return AsyncHelpers.RunSync(() => _asyncFhirService.AddMetaAsync(key, parameters));
        }

        public FhirResponse VersionRead(IKey key)
        {
            return AsyncHelpers.RunSync(() => _asyncFhirService.VersionReadAsync(key));
        }

        public FhirResponse Create(IKey key, Resource resource)
        {
            return AsyncHelpers.RunSync(() => _asyncFhirService.CreateAsync(key, resource));
        }

        public FhirResponse Put(Entry entry)
        {
            return AsyncHelpers.RunSync(() => _asyncFhirService.PutAsync(entry));
        }

        public FhirResponse Put(IKey key, Resource resource)
        {
            return AsyncHelpers.RunSync(() => _asyncFhirService.PutAsync(key, resource));
        }

        public FhirResponse ConditionalCreate(IKey key, Resource resource, IEnumerable<Tuple<string, string>> parameters)
        {
            return AsyncHelpers.RunSync(() => _asyncFhirService.ConditionalCreateAsync(key, resource, parameters));
        }

        public FhirResponse ConditionalCreate(IKey key, Resource resource, SearchParams parameters)
        {
            return AsyncHelpers.RunSync(() => _asyncFhirService.ConditionalCreateAsync(key, resource, parameters));
        }

        public FhirResponse Everything(IKey key)
        {
            return AsyncHelpers.RunSync(() => _asyncFhirService.EverythingAsync(key));
        }

        public FhirResponse Document(IKey key)
        {
            return AsyncHelpers.RunSync(() => _asyncFhirService.DocumentAsync(key));
        }

        public FhirResponse VersionSpecificUpdate(IKey versionedkey, Resource resource)
        {
            return AsyncHelpers.RunSync(() => _asyncFhirService.VersionSpecificUpdateAsync(versionedkey, resource));
        }

        public FhirResponse Update(IKey key, Resource resource)
        {
            return AsyncHelpers.RunSync(() => _asyncFhirService.UpdateAsync(key, resource));
        }

        public FhirResponse ConditionalUpdate(IKey key, Resource resource, SearchParams @params)
        {
            return AsyncHelpers.RunSync(() => _asyncFhirService.ConditionalUpdateAsync(key, resource, @params));
        }

        public FhirResponse Delete(IKey key)
        {
            return AsyncHelpers.RunSync(() => _asyncFhirService.DeleteAsync(key));
        }

        public FhirResponse Delete(Entry entry)
        {
            return AsyncHelpers.RunSync(() => _asyncFhirService.DeleteAsync(entry));
        }

        public FhirResponse ConditionalDelete(IKey key, IEnumerable<Tuple<string, string>> parameters)
        {
            return AsyncHelpers.RunSync(() => _asyncFhirService.ConditionalDeleteAsync(key, parameters));
        }

        public FhirResponse ValidateOperation(IKey key, Resource resource)
        {
            return AsyncHelpers.RunSync(() => _asyncFhirService.ValidateOperationAsync(key, resource));
        }

        public FhirResponse Search(string type, SearchParams searchCommand, int pageIndex = 0)
        {
            return AsyncHelpers.RunSync(() => _asyncFhirService.SearchAsync(type, searchCommand, pageIndex));
        }

        public FhirResponse Transaction(IList<Entry> interactions)
        {
            return AsyncHelpers.RunSync(() => _asyncFhirService.TransactionAsync(interactions));
        }

        public FhirResponse Transaction(Bundle bundle)
        {
            return AsyncHelpers.RunSync(() => _asyncFhirService.TransactionAsync(bundle));
        }

        public FhirResponse History(HistoryParameters parameters)
        {
            return AsyncHelpers.RunSync(() => _asyncFhirService.HistoryAsync(parameters));
        }

        public FhirResponse History(string type, HistoryParameters parameters)
        {
            return AsyncHelpers.RunSync(() => _asyncFhirService.HistoryAsync(type, parameters));
        }

        public FhirResponse History(IKey key, HistoryParameters parameters)
        {
            return AsyncHelpers.RunSync(() => _asyncFhirService.HistoryAsync(key, parameters));
        }

        public FhirResponse Mailbox(Bundle bundle, Binary body)
        {
            return AsyncHelpers.RunSync(() => _asyncFhirService.MailboxAsync(bundle, body));
        }

        public FhirResponse CapabilityStatement(string sparkVersion)
        {
            return AsyncHelpers.RunSync(() => _asyncFhirService.CapabilityStatementAsync(sparkVersion));
        }

        public FhirResponse GetPage(string snapshotkey, int index)
        {
            return AsyncHelpers.RunSync(() => _asyncFhirService.GetPageAsync(snapshotkey, index));
        }
    }
}

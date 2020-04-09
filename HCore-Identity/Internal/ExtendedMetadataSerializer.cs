﻿// from https://github.com/Sustainsys/Saml2/blob/72c4ca1a8b7fb2b7fb865ea5a53c9432a9f2a5bc/Sustainsys.Saml2/Metadata/ExtendedMetadataSerializer.cs

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Sustainsys.Saml2.Configuration;
using Sustainsys.Saml2.Tokens;
using Sustainsys.Saml2.Selectors;

namespace Sustainsys.Saml2.Metadata
{
	class ExtendedMetadataSerializer : MetadataSerializer
    {
        private ExtendedMetadataSerializer(SecurityTokenSerializer serializer)
            : base(serializer)
        { }

        private ExtendedMetadataSerializer() { }

        private static ExtendedMetadataSerializer readerInstance =
            new ExtendedMetadataSerializer();

        /// <summary>
        /// Use this instance for reading metadata. It uses custom extensions
        /// to increase feature support when reading metadata.
        /// </summary>
        public static ExtendedMetadataSerializer ReaderInstance
        {
            get
            {
                return readerInstance;
            }
        }

        private static ExtendedMetadataSerializer writerInstance =
            new ExtendedMetadataSerializer();

        public static ExtendedMetadataSerializer WriterInstance
        {
            get
            {
                return writerInstance;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "Method is only called by base class no validation needed.")]
        protected override void WriteCustomAttributes<T>(XmlWriter writer, T source)
        {
            if(typeof(T) == typeof(EntityDescriptor))
            {
                writer.WriteAttributeString("xmlns", "saml2", null, Saml2Namespaces.Saml2Name);
            }
        }


#if FALSE
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0")]
        protected override ServiceProviderSingleSignOnDescriptor ReadServiceProviderSingleSignOnDescriptor(XmlReader reader)
        {
            reader.Skip();
            return CreateServiceProviderSingleSignOnDescriptorInstance();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0")]
        protected override Organization ReadOrganization(XmlReader reader)
        {
            reader.Skip();
            return CreateOrganizationInstance();
        }
#endif
	}
}

//----------------------------------------------------------------------- 
// <copyright file="ServiceFabricJournalSpec.cs" company="Akka.NET Project"> 
//     Copyright (C) 2009-2016 Lightbend Inc. <http://www.lightbend.com> 
//     Copyright (C) 2013-2016 Akka.NET project <https://github.com/akkadotnet/akka.net> 
// </copyright> 
//----------------------------------------------------------------------- 
namespace Akka.Persistence.ServiceFabric.Tests
{
    using System.Configuration;
    using Akka.Configuration;
    using Akka.Persistence.TestKit.Journal;
    using Xunit;
    using Xunit.Abstractions;
    using System.Diagnostics;

    using Akka.Persistence.ServiceFabric.Journal;
    using Akka.Persistence.ServiceFabric.Tests.Mocks;

    using Microsoft.ServiceFabric.Services.Runtime;

    [Collection("ServiceFabricSpec")] 
    public class ServiceFabricJournalSpec : JournalSpec 
    { 
        private static readonly Config SpecConfig; 

        static ServiceFabricJournalSpec()
        { 
            SpecConfig = ConfigurationFactory.ParseString(@" 
                akka.test.single-expect-default = 3s 
                akka.persistence { 
                    publish-plugin-commands = on 
                    journal { 
                        plugin = ""akka.persistence.journal.servicefabricjournal""
                        servicefabricjournal { 
                            class = ""Akka.Persistence.ServiceFabric.Tests.Mocks.MockServiceFabricJournal, Akka.Persistence.ServiceFabric.Tests"" 
                            plugin-dispatcher = ""akka.actor.default-dispatcher"" 
                            key-prefix = ""akka:persistence:journal"" 
                        } 
                    } 
                }");
        } 


        public ServiceFabricJournalSpec(ITestOutputHelper output)
            : base(SpecConfig, typeof(ServiceFabricJournalSpec).Name, output) 
        {
            ServiceEventSource.Current.ServiceTypeRegistered(Process.GetCurrentProcess().Id, typeof(ServiceFabricJournalSpec).Name);

            Initialize(); 
        } 


        protected override bool SupportsRejectingNonSerializableObjects { get; } = false;
    } 
} 

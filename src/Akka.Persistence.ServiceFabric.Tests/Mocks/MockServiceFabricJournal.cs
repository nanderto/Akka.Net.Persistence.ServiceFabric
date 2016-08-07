using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Akka.Persistence.ServiceFabric.Tests.Mocks
{
    using Akka.Persistence.ServiceFabric.Journal;
    using Microsoft.ServiceFabric.Data;

    public class MockServiceFabricJournal : ServiceFabricJournal
    {
        public MockServiceFabricJournal()
        {
            this.StateManager = new MockReliableStateManager();
        }
    }
}

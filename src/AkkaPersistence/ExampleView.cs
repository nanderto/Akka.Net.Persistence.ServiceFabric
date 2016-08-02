//-----------------------------------------------------------------------
// <copyright file="ExampleView.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2016 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2016 Akka.NET project <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using Akka.Persistence;
using AkkaPersistence;

namespace PersistenceExample
{
    public class ExampleView : PersistentView
    {
        private int _numReplicated = 0;

        public override string PersistenceId { get { return "sample-id-4"; } }
        public override string ViewId { get { return "sample-view-id-4"; } }
        
        protected override bool Receive(object message)
        {
            if (message as string == "snap")
            {
                ServiceEventSource.Current.Message("View saving snapshot");
                SaveSnapshot(_numReplicated);
            }
            else if (message is SnapshotOffer)
            {
                var offer = (SnapshotOffer) message;
                _numReplicated = Convert.ToInt32(offer.Snapshot);
                ServiceEventSource.Current.Message("View received snapshot offer {0} (metadata = {1})", _numReplicated, offer.Metadata);
            }
            else if (IsPersistent)
            {
                _numReplicated++;
                ServiceEventSource.Current.Message("View replayed event {0} (num replicated = {1})", message, _numReplicated);
            }
            else if (message is SaveSnapshotSuccess)
            {
                var fail = (SaveSnapshotSuccess) message;
                ServiceEventSource.Current.Message("View snapshot success (metadata = {0})", fail.Metadata);
            }
            else if (message is SaveSnapshotFailure)
            {
                var fail = (SaveSnapshotFailure) message;
                ServiceEventSource.Current.Message("View snapshot failure (metadata = {0}), caused by {1}", fail.Metadata, fail.Cause);
            }
            else
            {
                ServiceEventSource.Current.Message("View received other message " + message);
            }

            return true;
        }
    }
}


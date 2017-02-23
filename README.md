# Akka.Persistence.ServiceFabric
ServiceFabic implementation of Akka.net Persistence

This implementation of Service Fabric persistence embeds the Service Fabric reliable collections in the actor (AsyncWriteJounrnal) responsible for persistence.

How to use: 

1) Reference the Akka.Persistence.ServiceFabric project / nuget (not yet available)

2) Create a stateful service in the standard service fabric way, using Visual Studio Wizard.

3) Find the class that inherits from the StatefulService and replace it with inheriting from AkkaStatefulService. The AkkaStatefulSertvice ensures that the Service Fabric StateManager is available to the actors to support Akka.Persistence, but it is concealed from the developer. 

4) Create your Actor System in the RunAsync method. Configuration can be done in-line or with config files as is standard in Akka.Net. You need to ensure that the configuration calls for using Akka.Persistence.ServiceFabric as the persistence mechanism. You are free to create and use Persistence Actors as you see fit.

Examples:

Load the solution and set the ServiceFabricHost as the startup project. Run the project and you should see the diagnostic events outputing a counter value. This is backed by Service Fabric. You can run the Service Fabric chaos tests and watch it as it is restored to different nodes, it does not loose events, although you will see the count fall behind as calls to increment the counter fail, as it is moved from node to node. 
There are other samples from Akka.net that have been reimplemented in this solution.



Appendix:

Remember to set the Test --> Test Settings --> Default Processor Architecture --> X64

Forgetting to check this setting leads to the unit tests not being discovered and its very hard to troubleshoot (this is a reminder to my self most of all).


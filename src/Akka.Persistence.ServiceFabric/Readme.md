This implementation of Service Fabric persistence embeds the Service Fabric reliable collections in the actor (AsyncWriteJounrnal) responsible for persistence.

How to use:
1) reference the Akka.Persistence.ServiceFabric project / nuget (not yet available)

2) Create a stateful service in the standard service fabric way.

3) Find the class that inherits from the StatefulServiuce and replace it with inheriting from AkkaStatefulService.

4) Create your Actor System in the RunAsync method. Configuration can be done in line or with config files as is standard in Akka.Net. You are free to create and use Persistence Actors as you see fit.

Examples:

Load the solution and set the ServiceFabricHost as the startup project. Run the project and you should see the diagnostic events outputing a counter value. This is backed by Service Fabric. You can run the Service Fabric chaos tests and watch it as it is restored to different nodes, it does not loose events, although you will see the count fall behind as calls to increment the counter fail, as it is moved from node to node. 

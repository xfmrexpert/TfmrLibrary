#:property OutputPath=./output
#:project ../src/TfmrLib/TfmrLib.csproj
using System.Runtime.CompilerServices;
using CircuitLib;
using TfmrLib;

var circ = new Circuit();
var node1 = circ.CreateNode("Node 1");
var node2 = circ.CreateNode("Node 2");
var node3 = circ.CreateNode("Node 3");
var node4 = circ.CreateNode("Node 4");
var branch1 = new Resistor(1, node1, node2, 10.0);
var branch2 = new Resistor(2, node3, node4, 20.0);
circ.Register(branch1);
circ.Register(branch2);

foreach (var node in circ.Nodes)
{
    var branches = circ.GetBranchesForNode(node);
    foreach (var branch in branches)
    {
        Console.WriteLine($"Node {node.Id} is connected to branch {branch.Id}");
    }
}

var tfmr = TestModels.TB904_ThreePhase();
tfmr.Initialize();
Console.WriteLine("Initialized");
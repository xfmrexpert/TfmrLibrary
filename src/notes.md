# How to handle connections and terminals
To specify the connections between winding segments, many of which are predefined and permanent, while some (related to DETC or LTC taps)
are variable.  We also need to be cognizant of winding direction.  

The present code envisions one internal "node" for each turn, with the connection points of parallel strands being nodes with parallel 
branches in between for each strand.  This is not strictly neccesary, though, and may change down the road if we introduce various levels 
of model granularity.

I'd like to define a higher level node (Terminal) to denote external nodes that may be connected in various ways depending on the exact 
calculation being down (e.g. grounding terminals, tieing some terminals together as for impulse tests, grounding through various impedances,
exciting certain terminals, leaving some open circuit, etc.)

It may or may not be advantageous to define a DETC and LTC class to easily change tap positions and reversing switch/change over selector 
operations.  This would basically encapsulate the tap nodes, change-over nodes, and a "terminal" or main node.

Connecting nodes remains an unknown. I'd prefer a solution that builds up or defines connectivity in a way that state is mutated as we go.
I'd rather not have to remember to call some init function to build up the internal connectivity state (though we could, I suppose).

We'll have to define the connectivity in some way to start.  Right now, the idea is to use a ConnectTo method to connect two nodes. This 
requires keeping a list of incident entities so that we can select one of the node objects and "repoint" the other enties to the selected 
node. An alternative would be to use a node key or ID, but that would still require some form of pruning pass to take all of the joined IDs
and assign them to one node ID.
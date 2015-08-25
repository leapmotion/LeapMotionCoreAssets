using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Callbacks;
#endif
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

/* The ExecuteBefore and ExecuteAfter attributes can be used to cause a behavior to be executed after or before
 * another behavior.  This is a more robust way to specify requirements in ordering than using Unity's built in
 * ScriptExecutionOrder tab since it cannot be changed or invalidated accidentally by a user.
 * 
 * These attributes can be stacked and combined as much as one desires.  The effects of the ordering attributes
 * are not inherited by an extending class.  
 * 
 * If one defines a cycle (Script A executed before B executes before C executes before A) the update of the 
 * script order will fail, and alert you to the cycle so that you can resolve it.  
 * 
 * The application of the execution order occurs after every script reload, so there is no need to manually 
 * trigger the proccess, but if you DO need to, you can go to Assets->Apply Execution Order Attributes to
 * trigger the proccess manually
 */

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ExecuteBeforeAttribute : Attribute {
  public readonly Type beforeType;

  public ExecuteBeforeAttribute(Type beforeType) {
    this.beforeType = beforeType;
  }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ExecuteAfterAttribute : Attribute {
  public readonly Type afterType;

  public ExecuteAfterAttribute(Type afterType) {
    this.afterType = afterType;
  }
}

#if UNITY_EDITOR
public class ExecutionOrderSolver {
  /* Every node represents a grouping of behaviors that all can the same execution index.  Grouping them
   * both helps algorithmic complexity, as well as ensuring that scripts with the same sorting index do
   * not become seperated */
  private class Node {
    /* A set of all the behavior types associated with this Node */
    public List<Type> types = new List<Type>(1);

    //Types that this node executes before
    public List<Type> beforeTypes = new List<Type>();

    //Types that this node executes after
    public List<Type> afterTypes = new List<Type>();

    /* Used during the topological sort.  Represents the number of edges that travel to this node in the graph*/
    public int incomingEdgeCount = 0;

    /* Represents the execution index of this node.  Is initialized to the exising execution index, and 
     * eventually is solved to satisfy the ordering attributes */
    public int executionIndex = 0;

    /* Is true if this node needs a new execution index calculated */
    public bool needsNewIndex = false;

    /* The ordering of anchored nodes is not allowed to change. Anchored nodes are behaviors that 
     * have no ordering attributes. */
    public bool isAnchored = false;

    public Node(Type type, int executionIndex) {
      this.types.Add(type);
      this.executionIndex = executionIndex;
    }

    /* Tries to combine another node into this one.  This method assumes that the other node is a direct
     * neighbor to this one in an ordering, as two nodes cannot be combined if they are not neighbors. */
    public bool tryCombineWith(Node other) {
      if (isEventSystem || other.isEventSystem) {
        return false;
      }

      /* If both nodes are anchored, but have difference execution indexes, we cannot combine them. */
      if (isAnchored && other.isAnchored && executionIndex != other.executionIndex) {
        return false;
      }

      /* If either node has an ordering conflict with the other, we cannot combine them. */
      if (other.doesHaveOrderingComparedTo(this)) {
        return false;
      }

      if (doesHaveOrderingComparedTo(other)) {
        return false;
      }

      /* This node and other node can be combined! */

      types.AddRange(other.types);
      beforeTypes.AddRange(other.beforeTypes);
      afterTypes.AddRange(other.afterTypes);

      executionIndex = isAnchored ? executionIndex : other.executionIndex;
      isAnchored |= other.isAnchored;
      needsNewIndex &= other.needsNewIndex;

      return true;
    }

    private bool doesHaveOrderingComparedTo(Node other) {
      foreach (Type t in other.types) {
        if (beforeTypes.Contains(t)) {
          return true;
        }
        if (afterTypes.Contains(t)) {
          return true;
        }
      }
      return false;
    }

    public bool isEventSystem {
      get {
        return types.Contains(typeof(UnityEngine.EventSystems.EventSystem));
      }
    }

    public bool hasOnlyUnityEngine {
      get {
        return types.All(t => t.Namespace != null && t.Namespace.StartsWith("UnityEngine"));
      }
    }

    public bool hasAnyUnityEngine {
      get {
        return types.Any(t => t.Namespace != null && t.Namespace.StartsWith("UnityEngine"));
      }
    }

    public override string ToString() {
      if (hasAnyUnityEngine && !isEventSystem) {
        return "[UnityEngine]";
      }

      string typeList = "";
      foreach (Type t in types) {
        if (typeList == "") {
          typeList = t.Name;
        } else {
          typeList += "+" + t.Name;
        }
      }
      return typeList;
    }
  }

  [MenuItem("Assets/Apply Execution Order Attributes")]
  [DidReloadScripts]
  public static void solveForExecutionOrders() {
    MonoScript[] monoscripts = MonoImporter.GetAllRuntimeMonoScripts();

    List<Node> nodes = new List<Node>();
    if (!tryConstructNodes(monoscripts, ref nodes)) {
      //tryConstructNodes returns false when no out of order node was found
      //If all nodes are already in order, we can just return!
      return;
    }

    unanchorReferencedNodes(ref nodes);

    collapseUnityEngineNodes(ref nodes);
    
    collapseAnchoredNodes(ref nodes);

    Dictionary<Node, List<Node>> edges = new Dictionary<Node, List<Node>>();

    constructRelativeEdges(nodes, ref edges);
    if (checkForCycles(edges)) return;

    constructAnchoredEdges(nodes, ref edges);
    if (checkForCycles(edges)) return;

    constructEventSystemEdges(nodes, ref edges);
    if (checkForCycles(edges)) return;

    solveTopologicalOrdering(ref nodes, ref edges);

    /* The only time this if statement will be satisfied is if we failed to find a cycle that
     * existed before, or if the solving algorithm failed to find an existing solution.  Both
     * cases are an error and we cannot procede.  This is mostly here for sanity checking, and
     * should never be hit if everything is working as it should.
     */

    if (edges.Values.Any(l => l.Count != 0)) {
      Debug.LogError("Topological sort failed for unknown reason!\nExecution order cannot be enforced!");
      return;
    }

    collapseNeighbors(ref nodes);

    if (!tryAssignExecutionIndexes(ref nodes)) {
      return;
    }

    applyExecutionIndexes(monoscripts, nodes);
  }

  /* Given all of the loaded monoscripts, construct a single node for each script.  This method returns true if
   * it found at least one node that was out of order.  
   * 
   * Behaviors that have no ordering attributes are considered 'anchored', as their ordering has been defined by
   * their current index in the ScriptExecutionOrder.  All anchored nodes should not be moved relative to each
   * other, as this could be an important ordering that has been defined by the user or plugins
   * 
   * Behaviors that do have ordering attributes are not considered anchored, even if they are current in order.
   * If the behavior is out of order relative to the requirements of its ordering attributes, it is marked
   * as needing a new index.  
   */
  private static bool tryConstructNodes(MonoScript[] monoscripts, ref List<Node> nodes) {
    Dictionary<Type, int> typeToIndex = new Dictionary<Type, int>();
    foreach (MonoScript script in monoscripts) {
      Type scriptType = script.GetClass();
      if (scriptType == null) {
        continue;
      }

      typeToIndex[scriptType] = MonoImporter.GetExecutionOrder(script);
    }

    bool didFindAnyOutOfOrder = false;

    foreach (MonoScript script in monoscripts) {
      Type scriptType = script.GetClass();
      if (scriptType == null) {
        continue;
      }

      Node newNode = new Node(scriptType, typeToIndex[scriptType]);
      nodes.Add(newNode);

      if (Attribute.IsDefined(scriptType, typeof(ExecuteAfterAttribute), false) || Attribute.IsDefined(scriptType, typeof(ExecuteBeforeAttribute), false)) {
        foreach (Attribute customAttribute in Attribute.GetCustomAttributes(scriptType, false)) {
          if (customAttribute is ExecuteAfterAttribute) {
            ExecuteAfterAttribute executeAfter = customAttribute as ExecuteAfterAttribute;

            if (!typeof(Behaviour).IsAssignableFrom(executeAfter.afterType)) {
              Debug.LogWarning(script.name + " cannot execute afer " + executeAfter.afterType + " because " + executeAfter.afterType + " is not a Behaviour");
              continue;
            }

            newNode.afterTypes.Add(executeAfter.afterType);

            if (newNode.executionIndex <= typeToIndex[executeAfter.afterType]) {
              newNode.needsNewIndex = true;
              didFindAnyOutOfOrder = true;
            }

          } else if (customAttribute is ExecuteBeforeAttribute) {
            ExecuteBeforeAttribute executeBefore = customAttribute as ExecuteBeforeAttribute;

            if (!typeof(Behaviour).IsAssignableFrom(executeBefore.beforeType)) {
              Debug.LogWarning(script.name + " cannot execute afer " + executeBefore.beforeType + " because " + executeBefore.beforeType + " is not a Behaviour");
              continue;
            }

            newNode.beforeTypes.Add(executeBefore.beforeType);

            if (newNode.executionIndex >= typeToIndex[executeBefore.beforeType]) {
              newNode.needsNewIndex = true;
              didFindAnyOutOfOrder = true;
            }
          }
        }
      } else {
        newNode.isAnchored = true;
      }
    }

    return didFindAnyOutOfOrder;
  }

  /* Any nodes that are referenced by other nodes in an ordering will be unanchored so that
   * they are not grouped, and so that they are completely free to move around to satisfy an
   * ordering.
   */
  private static void unanchorReferencedNodes(ref List<Node> nodes) {
    HashSet<Type> referencedTypes = new HashSet<Type>();
    foreach (Node node in nodes) {
      referencedTypes.UnionWith(node.beforeTypes);
      referencedTypes.UnionWith(node.afterTypes);
    }

    foreach (Node node in nodes) {
      if (node.hasOnlyUnityEngine || node.isEventSystem) {
        continue;
      }

      foreach (Type type in node.types) {
        if (referencedTypes.Contains(type)) {
          node.isAnchored = false;
          break;
        }
      }
    }
  }

  /* All Behaviors that are part of the UnityEngine cannot have their execution order changed,
   * so they are all collapsed into a single group to prevent them from being seperated out 
   * from each other.  Any ordering that tries to insert a script between two unity behaviors
   * will result in a cycle, since both unity behaviors will exist inside of the same Node.
   */
  private static void collapseUnityEngineNodes(ref List<Node> nodes) {
    List<Node> newNodeList = new List<Node>();

    Node unityEngineNode = null;
    foreach (Node node in nodes) {
      if (node.hasOnlyUnityEngine && !node.isEventSystem) {
        if (unityEngineNode == null) {
          unityEngineNode = node;
        } else {
          unityEngineNode.types.AddRange(node.types);
        }
      } else {
        newNodeList.Add(node);
      }
    }

    newNodeList.Add(unityEngineNode);

    nodes = newNodeList;
  }

  /* Anchored nodes never change their ordering relative to other anchored nodes.  Consequently we
   * can combine anchored nodes with the same index.  This helps remove valid orderings that move
   * a large body of scripts needlessly.  
   */
  private static void collapseAnchoredNodes(ref List<Node> nodes) {
    List<Node> newNodeList = new List<Node>();

    Dictionary<int, Node> _collapsedAnchoredNodes = new Dictionary<int, Node>();

    foreach (Node node in nodes) {
      //If the node is anchored, we can put it into the collapsed node
      //The EventSystem node should never be combined
      if (node.isAnchored && !node.isEventSystem) {
        Node anchorGroup;
        if (!_collapsedAnchoredNodes.TryGetValue(node.executionIndex, out anchorGroup)) {
          anchorGroup = node;
          _collapsedAnchoredNodes[anchorGroup.executionIndex] = anchorGroup;
          newNodeList.Add(anchorGroup);
        } else {
          anchorGroup.types.AddRange(node.types);
        }
      } else {
        newNodeList.Add(node);
      }
    }

    nodes = newNodeList;
  }

  private static void constructAnchoredEdges(List<Node> nodes, ref Dictionary<Node, List<Node>> edges) {
    //Create a sorted list of all the execution indexes of all of the anchored nodes
    List<int> anchoredNodeIndexes = nodes.Where(n => n.isAnchored).Select(n => n.executionIndex).Distinct().ToList();
    anchoredNodeIndexes.Sort();

    //Map each index to a list of all the anchored nodes with that index
    Dictionary<int, List<Node>> _indexToAnchoredNodes = new Dictionary<int, List<Node>>();
    foreach (Node anchoredNode in nodes.Where(n => n.isAnchored)) {
      List<Node> list;
      if (!_indexToAnchoredNodes.TryGetValue(anchoredNode.executionIndex, out list)) {
        list = new List<Node>();
        _indexToAnchoredNodes[anchoredNode.executionIndex] = list;
      }
      list.Add(anchoredNode);
    }

    /* Each anchored node has an edge connecting it to every other anchored node with the next lowest index
     * We do not need to connect every combination of nodes, because of the communicative property of comparison
     * if A > B > C, we don't need to specify that A > C explicitly, creating an edge for A > B and B > C is 
     * enough */
    foreach (Node anchoredNode in nodes.Where(n => n.isAnchored)) {
      int offset = anchoredNodeIndexes.IndexOf(anchoredNode.executionIndex);

      if (offset != 0) {
        List<Node> lowerNodes = _indexToAnchoredNodes[anchoredNodeIndexes[offset - 1]];
        foreach (Node lowerNode in lowerNodes) {
          addEdge(edges, lowerNode, anchoredNode);
        }
      }
    }
  }

  private static void constructRelativeEdges(List<Node> nodes, ref Dictionary<Node, List<Node>> edges) {
    Dictionary<Type, Node> typeToNode = new Dictionary<Type, Node>();
    foreach (Node node in nodes) {
      foreach (Type type in node.types) {
        typeToNode[type] = node;
      }
    }

    /* Build edges for non-anchored nodes.  This is simpler than the edges for the anchored nodes, since
     * there is exactly one edge for every ordering attribute */
    foreach (Node relativeNode in nodes.Where(n => !n.isAnchored)) {
      foreach (Type beforeType in relativeNode.beforeTypes) {
        Node beforeNode = typeToNode[beforeType];
        addEdge(edges, relativeNode, beforeNode);
      }

      foreach (Type afterType in relativeNode.afterTypes) {
        Node afterNode = typeToNode[afterType];
        addEdge(edges, afterNode, relativeNode);
      }
    }
  }

  private static void constructEventSystemEdges(List<Node> nodes, ref Dictionary<Node, List<Node>> edges) {
    Node eventSystemNode = nodes.First(n => n.isEventSystem);
    foreach (Node relativeNode in nodes.Where(n => !n.isAnchored)) {
      addEdge(edges, eventSystemNode, relativeNode);
    }
  }

  private static void addEdge(Dictionary<Node, List<Node>> edges, Node before, Node after) {
    List<Node> set;
    if (!edges.TryGetValue(before, out set)) {
      set = new List<Node>();
      edges[before] = set;
    }

    set.Add(after);
    after.incomingEdgeCount++;
  }

  /* Here we check to see if there are any cycles in the graph we have constructed.  Our
   * solving algorithm will tell us if there is no solution, but it doesn't give us any
   * useful information about why it is unsolvable.  We try to find a cycle here so that
   * we can output it so that the user can more easily find the cycle and correct it
   */
  private static bool checkForCycles(Dictionary<Node, List<Node>> edges) {
    Stack<Node> cycle = new Stack<Node>();

    foreach (var edge in edges) {
      if (findCycle(edges, edge.Key, cycle)) {

        

        string cycleString = cycle.Last().ToString();
        foreach (Node cycleNode in cycle) {
          cycleString = cycleNode.ToString() + " => " + cycleString;
        }

        EditorUtility.DisplayDialog("Execution Order Cycle!", "A cycle was found while trying to apply the Execution Order attributes!  Execution order cannot be applied until the cycle is removed\n\n" + cycleString, "Ok");
        return true;
      }
    }

    return false;
  }

  private static bool findCycle(Dictionary<Node, List<Node>> edges, Node visitingNode, Stack<Node> visitedNodes) {
    if (visitedNodes.Contains(visitingNode)) {
      return true;
    }

    List<Node> connections;
    if (edges.TryGetValue(visitingNode, out connections)) {

      visitedNodes.Push(visitingNode);

      foreach (Node nextNode in connections) {
        if (findCycle(edges, nextNode, visitedNodes)) {
          return true;
        }
      }

      visitedNodes.Pop();
    }

    return false;
  }

  /* Given a directed graph of nodes, returns an ordering of nodes such that a node
   * always falls before a node it points towards.  This modifies the graph in the proccess.
   * 
   * Direct implementation of https://en.wikipedia.org/wiki/Topological_sorting#Algorithms
   * 
   * This method destroys the graph during the solve.
   */
  private static void solveTopologicalOrdering(ref List<Node> nodes, ref Dictionary<Node, List<Node>> edges) {

    //Empty list to contain sorted nodes
    List<Node> L = new List<Node>(nodes.Count);

    //Set of all nodes with no incoming edges
    Stack<Node> S = new Stack<Node>(nodes.Where(s => s.incomingEdgeCount == 0));

    while (S.Count != 0) {
      //Remove a node n from S
      Node n = S.Pop();

      //Append n to L
      L.Add(n);

      List<Node> edgeList;
      if (edges.TryGetValue(n, out edgeList)) {

        //For every Node m where n -> m
        foreach (Node m in edgeList) {
          //Cut the edge from n to m
          m.incomingEdgeCount--;
          if (m.incomingEdgeCount == 0) {
            S.Push(m);
          }
        }

        //remove the edges from the graph
        edgeList.Clear();
      }
    }

    nodes = L;
  }

  /* It is often the case that neighboring nodes can be combined into a single node. 
   * This also ensures that there is exactly one default node.  A default node is a node
   * that us both anchored, and has an index of 0.  If there are multiple such nodes before
   * this method is called, they will always be neighbors, and will always be combined 
   * to one node after this method is complete. */
  private static void collapseNeighbors(ref List<Node> nodes) {
    List<Node> newNodeList = new List<Node>();

    Node current = nodes[0];
    newNodeList.Add(current);

    for (int i = 1; i < nodes.Count; i++) {
      Node node = nodes[i];
      if (!current.tryCombineWith(node)) {
        current = node;
        newNodeList.Add(current);
      }
    }

    nodes = newNodeList;
  }

  /* This method takes the Node ordering and assigns execution indexes to each of them.  The
   * anchored node at index 0 (the default node) is never moved, since that is the node that
   * contains all of the default scripts, and we would rather not change hundreds of meta
   * files to reorder around a single ordering request. This method does have the potential to
   * 'push' a Node that is already in order to a different index.  This only occurs if there is not
   * enough room between two Nodes to fit all the Nodes that need to be between.
   */
  private static bool tryAssignExecutionIndexes(ref List<Node> groupings) {
    /* We find where the default Node is in the ordering.  We want to keep this node at the same
     * execution index, so we need to shift all scripts away from this Node when making room*/

    var defaultNodes = groupings.Where(n => n.isAnchored && n.executionIndex == 0);
    if (defaultNodes.Count() != 1) {
      Debug.LogError("There must be exactly 1 default node, " + defaultNodes.Count() + " were found!");
      foreach (Node n in defaultNodes) {
        Debug.LogError(n);
      }
      return false;
    }

    int indexOfDefault = groupings.IndexOf(defaultNodes.First());

    /* Shift all nodes that come before the default node away from the default node */
    int minIndex = 0;
    for (int i = indexOfDefault - 1; i >= 0; i--) {
      minIndex--;

      if (!groupings[i].needsNewIndex) {
        minIndex = Mathf.Min(groupings[i].executionIndex, minIndex);
      }

      groupings[i].executionIndex = minIndex;
    }

    /* Shift all nodes that come after the default node away from the default node */
    int maxIndex = 0;
    for (int i = indexOfDefault + 1; i < groupings.Count; i++) {
      maxIndex++;

      if (!groupings[i].needsNewIndex) {
        maxIndex = Mathf.Max(groupings[i].executionIndex, maxIndex);
      }

      groupings[i].executionIndex = maxIndex;
    }

    return true;
  }

  /* Given the list of existing monoscripts and the Node ordering, apply the ordering to the
   * Unity ExecutionOrder using MonoImporter
   */
  private static void applyExecutionIndexes(MonoScript[] monoscripts, List<Node> nodes) {
    Dictionary<Type, MonoScript> typeToMonoScript = new Dictionary<Type, MonoScript>();
    foreach (MonoScript monoscript in monoscripts) {
      Type scriptType = monoscript.GetClass();
      if (scriptType == null) {
        continue;
      }

      typeToMonoScript[scriptType] = monoscript;
    }

    foreach (Node node in nodes) {
      foreach (Type type in node.types) {
        MonoScript monoscript = typeToMonoScript[type];
        if (MonoImporter.GetExecutionOrder(monoscript) != node.executionIndex) {
          MonoImporter.SetExecutionOrder(monoscript, node.executionIndex);
        }
      }
    }
  }
}
#endif

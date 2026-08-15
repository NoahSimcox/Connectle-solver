using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace Connectle_Solver;

public static class Runner
{
    private static int vertices = 0;
    private static List<Node> nodes = new List<Node>();
    private static List<(Node, Node)> mst = new List<(Node, Node)>();
    private static char startingChar = '@';
    static void Main()
    {
        string[] lines = File.ReadAllLines("vertices.txt");

        foreach (string line in lines)
        {
            string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            int x = int.Parse(parts[0]);
            int y = int.Parse(parts[1]);

            nodes.Add(new Node(x, y, ++startingChar));
            vertices++;
        }
        
        ConnectleBoard board = new ConnectleBoard(nodes);
        board.DrawBoard();
        
        mst = Prims(nodes);

        foreach ((Node, Node) edge in mst)
        {
            Console.Write($"{edge.Item1.nodeKey}{edge.Item2.nodeKey}  ");
        }
    }


    static List<(Node, Node)> Prims(List<Node> nodeList)
    {
        List<(Node, Node)> edgeList = new List<(Node, Node)>();
        List<Node> checkedNodes = new List<Node>();
        checkedNodes.Add(nodeList[0]);
        
        for (int i = 0; i < nodeList.Count - 1; i++)
        {
            edgeList.Add(FindShortestDistance(checkedNodes, nodeList));
            checkedNodes.Add(checkedNodes.Contains(edgeList[^1].Item1) ? edgeList[^1].Item2 : edgeList[^1].Item1);
        }
        
        return edgeList;
    }


    static (Node, Node) FindShortestDistance(List<Node> checkedNodes, List<Node> wholeList)
    {
        int distance = 100;
        List<Node> uncheckedNodes = new List<Node>();
        (Node, Node) edge = (null, null);
        
        uncheckedNodes = checkedNodes.Except(wholeList).Union(wholeList.Except(checkedNodes)).ToList();
        
        for (int i = 0; i < checkedNodes.Count; i++)
        {
            for (int j = 0; j < uncheckedNodes.Count; j++)
            {
                Node a = checkedNodes[i];
                Node b = uncheckedNodes[j];
                
                if (distance > checkedNodes[i].Distance(uncheckedNodes[j]))
                {
                    distance = checkedNodes[i].Distance(uncheckedNodes[j]);
                    
                    Node smaller = a.nodeKey < b.nodeKey ? a : b;
                    Node bigger = a.nodeKey > b.nodeKey ? a : b;
                    
                    edge = (smaller, bigger);
                }
                    
            }
        }

        return edge;
    }
}
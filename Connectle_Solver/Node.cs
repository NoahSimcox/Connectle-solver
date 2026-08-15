using System;

namespace Connectle_Solver;

public class Node
{
    public int x;
    public int y;
    public char nodeKey;

    public Node(int x, int y, char nodeKey)
    {
        this.x = x;
        this.y = y;
        this.nodeKey = nodeKey;
    }
    
    public int Distance(Node b)
    {
        return ((int)Math.Abs(x - b.x) + Math.Abs(y - b.y));
    }

    public static bool Contains(List<Node> nodes, int x, int y, out Node result)
    {
        result = new Node(-1, -1, '*');
        foreach (Node node in nodes)
        {
            if (node.x == x && node.y == y)
            {
                result = node;
                return true;
            } 
        }
        return false;
    }
    
}
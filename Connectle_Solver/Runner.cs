using System;
using System.Collections.Generic;
using System.Linq;

namespace Connectle_Solver;

public static class Runner
{
    const int WIDTH = 8;
    const int HEIGHT = 8;

    static readonly int INF = int.MaxValue / 4;

    // Represents a grid point
    public record struct Point(int X, int Y);

    // Neighbor directions
    static readonly (int dx, int dy)[] Directions =
    {
        (1, 0),
        (-1, 0),
        (0, 1),
        (0, -1)
    };

    // Convert (x,y) into a single vertex number
    static int Id(int x, int y)
    {
        return y * WIDTH + x;
    }

    // Convert vertex number back to (x,y)
    static Point GetPoint(int id)
    {
        return new Point(id % WIDTH, id / WIDTH);
    }

    static bool InBounds(int x, int y)
    {
        return x >= 0 && x < WIDTH &&
               y >= 0 && y < HEIGHT;
    }

    static void Main()
    {
        string filePath = "vertices.txt";

        if (!File.Exists(filePath))
        {
            Console.WriteLine($"Could not find {filePath}");
            return;
        }

        List<Point> terminals = new List<Point>();

        foreach (string line in File.ReadLines(filePath))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            string[] parts = line.Split(
                new[] { ' ', '\t', ',' },
                StringSplitOptions.RemoveEmptyEntries
            );

            if (parts.Length != 2)
            {
                Console.WriteLine(
                    $"Invalid line in file: {line}"
                );
                return;
            }

            if (!int.TryParse(parts[0], out int x) ||
                !int.TryParse(parts[1], out int y))
            {
                Console.WriteLine(
                    $"Invalid coordinates: {line}"
                );
                return;
            }

            if (!InBounds(x, y))
            {
                Console.WriteLine(
                    $"Coordinate ({x},{y}) is outside the 8x8 grid."
                );
                return;
            }

            terminals.Add(new Point(x, y));
        }

        if (terminals.Count == 0)
        {
            Console.WriteLine("No vertices were found in the file.");
            return;
        }

        Console.WriteLine($"Loaded {terminals.Count} terminals.");

        var result = Solve(terminals);

        Console.WriteLine();
        Console.WriteLine($"Minimum cost: {result.Cost}");

        Console.WriteLine("\nTree edges:");

        foreach (var edge in result.Edges)
        {
            Console.WriteLine(
                $"({edge.A.X},{edge.A.Y}) -> ({edge.B.X},{edge.B.Y})"
            );
        }

        Console.WriteLine("\nGrid:");

        PrintGrid(terminals, result.Edges);
    }

    public static (int Cost, List<(Point A, Point B)> Edges)
    Solve(List<Point> terminals)
{
    if (terminals == null || terminals.Count == 0)
        throw new ArgumentException("At least one terminal is required.");

    foreach (Point p in terminals)
    {
        if (!InBounds(p.X, p.Y))
        {
            throw new ArgumentException(
                $"Terminal ({p.X},{p.Y}) is outside the 8x8 grid."
            );
        }
    }

    int terminalCount = terminals.Count;
    int vertexCount = WIDTH * HEIGHT;
    int subsetCount = 1 << terminalCount;

    int[,] dp = new int[subsetCount, vertexCount];
    Parent[,] parent = new Parent[subsetCount, vertexCount];

    for (int s = 0; s < subsetCount; s++)
    {
        for (int v = 0; v < vertexCount; v++)
        {
            dp[s, v] = INF;
        }
    }

    // Base cases
    for (int i = 0; i < terminalCount; i++)
    {
        int subset = 1 << i;

        int vertex = Id(
            terminals[i].X,
            terminals[i].Y
        );

        dp[subset, vertex] = 0;
        parent[subset, vertex] = Parent.Terminal();
    }

    // ----------------------------------------------------
// DP over every subset
// ----------------------------------------------------

for (int subset = 1; subset < subsetCount; subset++)
{
    // ------------------------------------------------
    // If this subset contains multiple terminals,
    // combine smaller subsets.
    // ------------------------------------------------

    if ((subset & (subset - 1)) != 0)
    {
        for (int v = 0; v < vertexCount; v++)
        {
            int best = dp[subset, v];
            SplitInfo bestSplit = default;

            // Enumerate proper submasks
            for (int A = (subset - 1) & subset;
                 A > 0;
                 A = (A - 1) & subset)
            {
                int B = subset ^ A;

                if (B == 0)
                    continue;

                // Avoid doing A+B and B+A
                if (A > B)
                    continue;

                if (dp[A, v] == INF ||
                    dp[B, v] == INF)
                    continue;

                int candidate =
                    dp[A, v] + dp[B, v];

                if (candidate < best)
                {
                    best = candidate;
                    bestSplit = new SplitInfo(A, B);
                }
            }

            dp[subset, v] = best;

            if (bestSplit.A != 0)
            {
                parent[subset, v] =
                    Parent.Split(
                        bestSplit.A,
                        bestSplit.B
                    );
            }
        }
    }

    // ------------------------------------------------
    // Propagate this subset through the grid
    // ------------------------------------------------
    //
    // IMPORTANT:
    // We do this for SINGLETON subsets too.
    //

    Queue<int> queue = new Queue<int>();

    bool[] inQueue = new bool[vertexCount];

    for (int v = 0; v < vertexCount; v++)
    {
        if (dp[subset, v] < INF)
        {
            queue.Enqueue(v);
            inQueue[v] = true;
        }
    }

    while (queue.Count > 0)
    {
        int current = queue.Dequeue();
        inQueue[current] = false;

        Point p = GetPoint(current);

        foreach (var (dx, dy) in Directions)
        {
            int nx = p.X + dx;
            int ny = p.Y + dy;

            if (!InBounds(nx, ny))
                continue;

            int next = Id(nx, ny);

            int candidate =
                dp[subset, current] + 1;

            if (candidate < dp[subset, next])
            {
                dp[subset, next] = candidate;

                parent[subset, next] =
                    Parent.Move(current);

                if (!inQueue[next])
                {
                    queue.Enqueue(next);
                    inQueue[next] = true;
                }
            }
        }
    }
}

    int fullSet = subsetCount - 1;

    int bestVertex = -1;
    int bestCost = INF;

    for (int v = 0; v < vertexCount; v++)
    {
        if (dp[fullSet, v] < bestCost)
        {
            bestCost = dp[fullSet, v];
            bestVertex = v;
        }
    }

    // IMPORTANT: detect failure before reconstruction
    if (bestVertex == -1 || bestCost == INF)
    {
        throw new InvalidOperationException(
            "The Steiner tree could not be constructed."
        );
    }

    HashSet<(int, int)> edges =
        new HashSet<(int, int)>();

    Reconstruct(
        fullSet,
        bestVertex,
        parent,
        edges
    );

    List<(Point A, Point B)> resultEdges =
        new List<(Point A, Point B)>();

    foreach (var (a, b) in edges)
    {
        resultEdges.Add(
            (GetPoint(a), GetPoint(b))
        );
    }

    return (bestCost, resultEdges);
}

    // --------------------------------------------------------
    // Recursively reconstruct the tree
    // --------------------------------------------------------

    static void Reconstruct(
        int subset,
        int vertex,
        Parent[,] parent,
        HashSet<(int, int)> edges)
    {
        // Catch the actual problem instead of getting
        // IndexOutOfRangeException
        if (subset < 0 || subset >= parent.GetLength(0))
        {
            throw new Exception(
                $"Invalid subset during reconstruction: {subset}"
            );
        }

        if (vertex < 0 || vertex >= parent.GetLength(1))
        {
            throw new Exception(
                $"Invalid vertex during reconstruction: {vertex}"
            );
        }

        Parent p = parent[subset, vertex];

        if (p.Type == ParentType.Terminal)
        {
            return;
        }

        if (p.Type == ParentType.Move)
        {
            if (p.PreviousVertex < 0 ||
                p.PreviousVertex >= parent.GetLength(1))
            {
                throw new Exception(
                    $"Invalid previous vertex: {p.PreviousVertex}"
                );
            }

            AddEdge(
                edges,
                p.PreviousVertex,
                vertex
            );

            Reconstruct(
                subset,
                p.PreviousVertex,
                parent,
                edges
            );

            return;
        }

        if (p.Type == ParentType.Split)
        {
            if (p.SubsetA <= 0 ||
                p.SubsetA >= parent.GetLength(0))
            {
                throw new Exception(
                    $"Invalid subset A: {p.SubsetA}"
                );
            }

            if (p.SubsetB <= 0 ||
                p.SubsetB >= parent.GetLength(0))
            {
                throw new Exception(
                    $"Invalid subset B: {p.SubsetB}"
                );
            }

            Reconstruct(
                p.SubsetA,
                vertex,
                parent,
                edges
            );

            Reconstruct(
                p.SubsetB,
                vertex,
                parent,
                edges
            );

            return;
        }

        throw new Exception(
            $"No valid parent information for subset={subset}, vertex={vertex}"
        );
    }

    static void AddEdge(
        HashSet<(int, int)> edges,
        int a,
        int b)
    {
        if (a < b)
            edges.Add((a, b));
        else
            edges.Add((b, a));
    }

    // --------------------------------------------------------
    // Print grid
    // --------------------------------------------------------

    static void PrintGrid(
        List<Point> terminals,
        List<(Point A, Point B)> edges)
    {
        HashSet<Point> treePoints =
            new HashSet<Point>();

        foreach (var edge in edges)
        {
            treePoints.Add(edge.A);
            treePoints.Add(edge.B);
        }

        HashSet<Point> terminalSet =
            terminals.ToHashSet();

        for (int y = 0; y < HEIGHT; y++)
        {
            for (int x = 0; x < WIDTH; x++)
            {
                Point p = new Point(x, y);

                if (terminalSet.Contains(p))
                    Console.Write("T ");
                else if (treePoints.Contains(p))
                    Console.Write("* ");
                else
                    Console.Write(". ");
            }

            Console.WriteLine();
        }
    }

    // --------------------------------------------------------
    // Parent information
    // --------------------------------------------------------

    enum ParentType
    {
        None,
        Terminal,
        Split,
        Move
    }

    struct Parent
    {
        public ParentType Type;

        public int PreviousVertex;

        public int SubsetA;
        public int SubsetB;

        public static Parent Terminal()
        {
            return new Parent
            {
                Type = ParentType.Terminal
            };
        }

        public static Parent Split(
            int a,
            int b)
        {
            return new Parent
            {
                Type = ParentType.Split,
                SubsetA = a,
                SubsetB = b
            };
        }

        public static Parent Move(
            int previous)
        {
            return new Parent
            {
                Type = ParentType.Move,
                PreviousVertex = previous
            };
        }
    }

    struct SplitInfo
    {
        public int A;
        public int B;

        public SplitInfo(int a, int b)
        {
            A = a;
            B = b;
        }
    }
}

























// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.IO;
//
// namespace Connectle_Solver;
//
// public static class Runner
// {
//     private static int vertices = 0;
//     private static List<Node> nodes = new List<Node>();
//     private static List<(Node, Node)> mst = new List<(Node, Node)>();
//     private static char startingChar = '@';
//     static void Main()
//     {
//         string[] lines = File.ReadAllLines("vertices.txt");
//
//         foreach (string line in lines)
//         {
//             string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
//
//             int x = int.Parse(parts[0]);
//             int y = int.Parse(parts[1]);
//
//             nodes.Add(new Node(x, y, ++startingChar));
//             vertices++;
//         }
//         
//         ConnectleBoard board = new ConnectleBoard(nodes);
//         board.DrawBoard();
//         
//         mst = Prims(nodes);
//
//         foreach ((Node, Node) edge in mst)
//         {
//             Console.Write($"{edge.Item1.nodeKey}{edge.Item2.nodeKey}  ");
//         }
//     }
//
//
//     static List<(Node, Node)> Prims(List<Node> nodeList)
//     {
//         List<(Node, Node)> edgeList = new List<(Node, Node)>();
//         List<Node> checkedNodes = new List<Node>();
//         checkedNodes.Add(nodeList[0]);
//         
//         for (int i = 0; i < nodeList.Count - 1; i++)
//         {
//             edgeList.Add(FindShortestDistance(checkedNodes, nodeList));
//             checkedNodes.Add(checkedNodes.Contains(edgeList[^1].Item1) ? edgeList[^1].Item2 : edgeList[^1].Item1);
//         }
//         
//         return edgeList;
//     }
//
//
//     static (Node, Node) FindShortestDistance(List<Node> checkedNodes, List<Node> wholeList)
//     {
//         int distance = 100;
//         List<Node> uncheckedNodes = new List<Node>();
//         (Node, Node) edge = (null, null);
//         
//         uncheckedNodes = checkedNodes.Except(wholeList).Union(wholeList.Except(checkedNodes)).ToList();
//         
//         for (int i = 0; i < checkedNodes.Count; i++)
//         {
//             for (int j = 0; j < uncheckedNodes.Count; j++)
//             {
//                 Node a = checkedNodes[i];
//                 Node b = uncheckedNodes[j];
//                 
//                 if (distance > checkedNodes[i].Distance(uncheckedNodes[j]))
//                 {
//                     distance = checkedNodes[i].Distance(uncheckedNodes[j]);
//                     
//                     Node smaller = a.nodeKey < b.nodeKey ? a : b;
//                     Node bigger = a.nodeKey > b.nodeKey ? a : b;
//                     
//                     edge = (smaller, bigger);
//                 }
//                     
//             }
//         }
//
//         return edge;
//     }
// }
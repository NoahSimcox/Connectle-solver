using System.Text;

namespace Connectle_Solver;
public class ConnectleBoard(List<Node> selectedNodes)
{
    private readonly StringBuilder _board = new StringBuilder();
    public void DrawBoard()
    {
        for (int i = 0; i < 8; i++)
        {
            _board.Append("\n");
            for (int j = 0; j < 8; j++)
            {
                _board.Append(Node.Contains(selectedNodes, j, i, out Node currentNode) ? $"{currentNode.nodeKey}  " : "*  ");
            }
        }
        Console.WriteLine(_board.ToString());
    }
}

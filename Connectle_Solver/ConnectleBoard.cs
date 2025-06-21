namespace Connectle_Solver
{
    public class ConnectleBoard
    {
        private int boardSize;

        public ConnectleBoard(int boardSize)
        {
            this.boardSize = boardSize;
        }

        public void drawBoard()
        {
            for (int i = 0; i < boardSize; i++)
            {
                Console.Write("\n");
                for (int j = 0; j < boardSize; j++)
                {
                    Console.Write("*  ");
                }
            }
        }
    }
}
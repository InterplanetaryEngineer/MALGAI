using System;

namespace HumanInterface
{
    using Go;

    class Program
    {
        static GoShape boardShape;
        static Go game;
        static Player AI;

        static void Main(string[] args)
        {
            boardShape = new GoShape(9);
            game = new Go(boardShape);
            int mostRecent = AdaptiveGameAI.Program.GetMostRecent();

            AI = new Player(game, GameBase.Color.Black, AdaptiveGameAI.Program.savePath + mostRecent + ".dat");

            Tests();

            while (!game.Ended()) {
                GoPosition p = (GoPosition) AI.Play();
                Console.WriteLine(AdaptiveGameAI.Program.Output(GameBase.Color.Black, p, game, true));
                if (!game.Ended()) {
                    p = HumanPlay();
                    Console.WriteLine(AdaptiveGameAI.Program.Output(GameBase.Color.White, p, game, true));
                }
                    
            }
        }

        static void Tests()
        {
            Console.WriteLine("First Neuron:");
            foreach (var item in AI.net.layers[0]._matrix[0]) {
                Console.Write(item + ", ");
            }
            Console.WriteLine();
            Console.WriteLine("Raw results:");
            float[] result = AI.net.Compute(TestCompute(game));
            foreach (var item in result) {
                Console.Write(item + ", ");
            }
            /*
            Console.WriteLine();
            Console.WriteLine("Test Symmetry:");
            Console.WriteLine("0 Raw: " + game.Shape.Symmetries[0](game.Shape.At(0)).Index);
            Console.WriteLine("22 Raw: " + game.Shape.Symmetries[0](game.Shape.At(22)).Index);
            Console.WriteLine("Passed Raw: " + game.Shape.Symmetries[0](game.Shape.Passed()).Index);
            Console.WriteLine("0 Vertical: " + game.Shape.Symmetries[2](game.Shape.At(0)).Index);
            Console.WriteLine("22 Vertical: " + game.Shape.Symmetries[2](game.Shape.At(22)).Index);
            Console.WriteLine("Passed Vertical: " + game.Shape.Symmetries[2](game.Shape.Passed()).Index);
            Console.WriteLine();
            */
        }

        static GoPosition HumanPlay()
        {
            while(true) {
                try {
                    int x = int.Parse(GetLine()) - 1;
                    int y = 9 - int.Parse(GetLine());
                    if (boardShape.Valid(y, x) && game.IsLegal(boardShape.At(y, x), GameBase.Color.White)) {
                        game.Play(boardShape.At(y, x), GameBase.Color.White);
                        return boardShape.At(y, x);
                    }
                }
                catch { }

                Console.WriteLine("Invalid arguments. Try Again:");
            }
            
            string GetLine()
            {
                string input = null;
                while (input == null) {
                    input = Console.ReadLine();
                }
                return input;
            }
        }

        static float[] TestCompute(Go board)
        {
            float[] valueBoard = new float[board.State.Length];
            for (int i = 0; i < valueBoard.Length; i++) {
                if (board.State[i] == GameBase.Color.Empty) {
                    valueBoard[i] = 0;
                } else if (board.State[i] == GameBase.Color.Black) {
                    valueBoard[i] = 1;
                } else {
                    valueBoard[i] = -1;
                }
            }
            return valueBoard;
        }
    }
}

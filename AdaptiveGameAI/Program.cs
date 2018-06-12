#region About
/* Artificial Neural Network to learn board games
 * 
 * by Jonathan Hähne
 * 
 * License:
 * CC BY-NC-SA 4.0 : https://creativecommons.org/licenses/by-nc-sa/4.0/
*/
#endregion

#region To Do
/*
 * 
 */
#endregion


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AdaptiveGameAI
{
    using Go;
    using System.IO;
    using System.Text;
    using System.Threading;

    public class Program
    {
        static Go game;
        static Trainer trainer;
        public static string savePath = Environment.CurrentDirectory
            + $"/results/";


        static void Main(string[] args)
        {
            if (args.Length > 1)
                savePath = args[1];
            game = new Go(new GoShape(9));

            int mostRecent = GetMostRecent();
            if (mostRecent < 0) {
                trainer = new Trainer(game, 0, savePath);
            }
            else {
                trainer = new Trainer(game, mostRecent, savePath);
            }
            //trainer.Trainee.net.Save(savePath + "0.dat", new List<Type>() { typeof(GoShape) });


            while (true) {
                Console.WriteLine("Playing:");
                bool success = TestPlay();
                trainer.Evolve(success);
                if (!success && Console.KeyAvailable && Console.Read() == 'q')
                    return;
            }
        }

        static bool TestPlay()
        {
            int[] wins = new int[2];
            
            GoPosition p = Play(false);
            wins[0] = trainer.Judge(game.Result());
            Console.WriteLine(Output(GameBase.Color.Empty, p, game, false));
            
            p = Play(true);
            wins[1] = trainer.Judge(game.Result());
            Console.WriteLine(Output(GameBase.Color.Empty, p, game, false));

            return wins[0] == 0 && wins[1] == 1;

            GoPosition Play(bool reverse)
            {
                game.Reset();
                Player first, second;

                GameBase.Position played = game.Shape.Passed();
                if (reverse) {
                    first = trainer.Opponent;
                    second = trainer.Trainee;
                } else {
                    first = trainer.Trainee;
                    second = trainer.Opponent;
                }

                first.color = GameBase.Color.Black;
                second.color = GameBase.Color.White;

                while (!game.Ended()) {
                    int turn = game.History.Count;
                    played = first.Play();
                    if (!game.Ended())
                        played = second.Play();
                }

                return (GoPosition) played;
            }
        }

        public static string Output(GameBase.Color playercolor, GoPosition move, Go game, bool printBoard)
        {
            int[] result = game.Result();
            StringBuilder text = new StringBuilder("\n");
            text.Append("- Move ");
            text.Append(game.History.Count);
            text.Append(", after ");
            text.Append(playercolor.ToString());
            text.Append(" played at ");
            text.Append(move.Y + 1);
            text.Append(", ");
            text.Append(9 - move.X);
            if (printBoard) {
                text.Append(game.Print());
            }
            text.AppendLine();
            text.Append("Score: ");
            text.Append(result[0]);
            text.Append(" to ");
            text.Append(result[1]);
            return text.ToString();
        }

        public static int GetMostRecent()
        {
            string pos = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string[] files = Directory.GetFiles(savePath);//Path.GetDirectoryName(pos) + savePath);
            Console.WriteLine(files.Length);
            Thread.Sleep(1000);
            if (files.Length == 0)
                return -1;

            int[] numbers = new int[files.Length];
            for (int i = 0; i < files.Length; i++) {
                numbers[i] = int.Parse(Path.GetFileNameWithoutExtension(files[i]));
            }
            return numbers.Max();
        }
    }
}

namespace Go
{
    using GameBase;

    public class Go : Game
    {
        private List<Color[]> history;
        public List<Color[]> History { get { return history; } }
        public new GoShape Shape { get { return (GoShape) shape; } }
        bool passed = false;

        public Go(GoShape dimensions) : base(dimensions)
        {
            Reset();
        }
        public override void Reset()
        {
            base.Reset();
            history = new List<Color[]>();
            history.Add((Color[]) State.Clone());
        }
        public override string Print()
        {
            StringBuilder text = new StringBuilder("\n");
            for (int x = 0; x < Shape.Length; x++) {
                for (int y = 0; y < Shape.Length; y++) {
                    switch (State[x + y*Shape.Length]) {
                        case Color.Black:
                            text.Append("# ");
                            break;
                        case Color.White:
                            text.Append("O ");
                            break;
                        case Color.Empty:
                            text.Append("* ");
                            break;
                        default:
                            break;
                    }
                }
                text.Append("\n");
            }
            text.Append("Passed:" + State[State.Length - 1]);
            return text.ToString();
        }

        public override void Play(Position m, Color playercolor)
        {
            GoPosition move = (GoPosition) m;

            if (move.Passed) {
                if (board[Shape.Size - 1] != Color.Empty)
                    passed = true;
                else {
                    passed = false;
                    board[Shape.Size - 1] = playercolor;
                }
            } else {
                board[Shape.Size - 1] = Color.Empty;
                this[move] = playercolor;
                foreach (GoPosition pos in Neighbors(move)) {
                    if (this[pos] != playercolor) // if of opposite color
                        CheckKill(pos);
                }
                CheckKill(move);
            }

            history.Add((Color[]) State.Clone());
        }
        public override bool IsLegal(Position move, Color col)
        {
            if (this[move] != Color.Empty)
                return false;
            Go test = new Go((GoShape) shape) {
                board = (Color[]) State.Clone()
            };
            test.Play(move, col);
            foreach (Color[] past in History) {
                if (past.SequenceEqual(test.board))
                    return false;
            }
            return true;
        }
        public void Undo()
        {
            history.RemoveAt(history.Count - 1);
            board = (Color[]) history.Last().Clone();
        }

        public override bool Ended()
        {
            return passed || history.Count > 160;
        }
        public override int[] Result()
        {
            int white = 0;
            int black = 0;
            for (Position p = Shape.At(0); p.Index < Shape.Length * Shape.Length; p.Index++) {
                if (this[p] == Color.Black)
                    black++;
                else if (this[p] == Color.White)
                    white++;
                else {
                    Color[] adjacents = {
                        next(p,  1, 0),
                        next(p, -1, 0),
                        next(p, 0,  1),
                        next(p, 0, -1)
                    };
                    if (adjacents.Contains(Color.Black) && !adjacents.Contains(Color.White))
                        black++;
                    else if (adjacents.Contains(Color.White) && !adjacents.Contains(Color.Black))
                        white++;
                }
            }
            return (new int[] { black, white });

            Color next(Position start, int xmove, int ymove)
            {
                GoPosition marker = (GoPosition) Shape.At(start.Index);
                while (Shape.Valid(marker.X + xmove, marker.Y + ymove)) {
                    marker.X += xmove;
                    marker.Y += ymove;
                    if (this[marker] != Color.Empty)
                        return this[marker];
                }
                return Color.Empty;
            }
        }

        public GoPosition[] Neighbors(GoPosition c)
        {
            List<GoPosition> neighbors = new List<GoPosition>();
            if (Shape.Valid(c.X + 1, c.Y))
                neighbors.Add(Shape.At(c.X + 1, c.Y));
            if (Shape.Valid(c.X - 1, c.Y))
                neighbors.Add(Shape.At(c.X - 1, c.Y));
            if (Shape.Valid(c.X, c.Y + 1))
                neighbors.Add(Shape.At(c.X, c.Y + 1));
            if (Shape.Valid(c.X, c.Y - 1))
                neighbors.Add(Shape.At(c.X, c.Y - 1));
            return neighbors.ToArray();
        }
        private void CheckKill(GoPosition pos)

        {
            if (this[pos] == Color.Empty || pos.Index >= Shape.Length * Shape.Length)
                return;
            HashSet<GoPosition> visit = new HashSet<GoPosition>();
            List<GoPosition> visited = new List<GoPosition>();
            List<GoPosition> tocheck = new List<GoPosition>();
            Color c = this[pos];

            tocheck.Add(pos);

            while (tocheck.Count > 0) {
                GoPosition check = tocheck[0];

                if (!visited.Contains(check)) {

                    foreach (GoPosition position in Neighbors(check)) {
                        if (this[position] == Color.Empty) // freedom found
                            return;
                        if (this[position] == c)
                            tocheck.Add(position);
                    }
                    visited.Add(check);
                }
                tocheck.Remove(check);
            }

            // no freedom found
            foreach (GoPosition r in visited) {
                this[r] = Color.Empty;
            }
        }
    }

    public class Trainer
    {
        public Player Trainee => trainee;
        public Player Opponent => opponent;
        public int Generation => generation;

        Player trainee;
        Player opponent;
        Go game;
        float expansion = 0;
        NNet move;
        int generation;
        Random rng;
        string savePath;

        const float walkspeed = 0.1F;
        const float acceleration = 0.04F;
        const float Komi = 6.5f;
        
        public Trainer(Go game, int generation, string savePath)
        {
            this.game = game;
            this.generation = generation;
            this.savePath = savePath;
            rng = new Random();
            move = new NNet(game.Shape);

            if(generation == 0) {
                trainee = new Player(game, Color.White);
            } else {
                trainee = new Player(game, Color.White, savePath + generation + ".dat");
            }
            opponent = new Player(game, Color.Black);
            trainee.CloneTo(opponent);
        }

        public void Evolve(bool success)
        {
            if (success) {
                Console.WriteLine("==    Success    ==\n\n");
                Progress();
            } else {
                Console.WriteLine("==    Failure    ==\n\n");
                Retry();
            }
        }

        public void Progress()
        {
            trainee.CloneTo(opponent);

            expansion = 0;
            move.Magnitude = walkspeed;

            trainee.net.Save(savePath + generation + ".dat", new List<Type>() { typeof(GoShape) });
            trainee.net.Add(move);
            generation++;

        }
        public void Retry()
        {
            opponent.CloneTo(trainee);

            expansion += acceleration;

            //move.Reset((float) rng.NextDouble() * expansion);

            move.Reset();
            Layer layer = move.layers[rng.Next(0, move.layers.Length)];
            int n = rng.Next(0, layer._matrix.Length);
            int w = rng.Next(0, layer._matrix[0].Length);

            float[] weights = new float[layer._matrix[0].Length];
            weights.Initialize();
            weights[w] = (rng.Next(0, 2) * 2 - 1) * expansion;

            layer.SetNeuron(layer._shape.At(n), weights);


            Console.WriteLine(trainee.net.layers[0]._matrix[0][0]);
            trainee.net.Add(move);
            Console.WriteLine(trainee.net.layers[0]._matrix[0][0]);
            generation++;

        }

        public int Judge(int[] scores)
        {
            if (scores[0] > scores[1] + Komi)
                return 0;
            else
                return 1;
        }
    }

    public class Player : GameBase.Player
    {
        Go board;

        public Player(Go board, Color c) : base(c)
        {
            this.board = board;
            net = new NNet(board.Shape);
        }
        public Player(Go board, Color c, string filefrom) : base()
        {
            this.board = board;
            net = new NNet(filefrom);
            color = c;
        }

        public override Position Play()
        {
            float[] results = new float[board.Shape.Size];
            for (int i = 0; i < board.Shape.Size; i++) {
                Position test = board.Shape.At(i);
                if (board.IsLegal(test, color)) {
                    board.Play(test, color);
                    results[i] = net.Compute(DigitalizeBoard())[0];
                    board.Undo();
                }
            }

            GoPosition play = Evaluate(results);
            board.Play(play, color);
            return play;
        }

        private GoPosition Evaluate(float[] results)
        {
            GoPosition play = (GoPosition) board.Shape.At(0);
            float max = float.NegativeInfinity;

            for (int i = 0; i < results.Length; i++) {
                if (results[i] > max && board.IsLegal(board.Shape.At(i), color)) {
                    play.Index = i;
                    max = results[i];
                }
            }
            return play;
        }
        private float[] DigitalizeBoard()
        {
            float[] valueBoard = new float[board.State.Length];
            for (int i = 0; i < valueBoard.Length; i++) {
                if (board.State[i] == Color.Empty) {
                    valueBoard[i] = 0;
                } else if (board.State[i] == color) {
                    valueBoard[i] = 1;
                } else {
                    valueBoard[i] = -1;
                }
            }
            return valueBoard;
        }
    }

    public class NNet : GameBase.NNet
    {

        public GoShape boardshape;
        public int[] layerSizes = { 9, 9, 7, 5, 3, 1 };

        public NNet(GoShape boardshape) : base()
        {
            this.boardshape = boardshape;
            layers = new Layer[layerSizes.Length];
            for (int layer = 0; layer < layers.Length; layer++) {
                layers[layer] = new Layer(
                    layer == 0 ? boardshape : new GoShape(layerSizes[layer - 1]),
                    new GoShape(layerSizes[layer])
                    );
            }
        }

        public NNet(string filefrom) : base(filefrom)
        {

        }
    }
    
    [Serializable]
    public class GoShape : Shape
    {
        public override int Size
        {
            get {
                return dimensions[0] * dimensions[0] + 1;
            }
            set {
                throw new NotImplementedException();
            }
        }
        public int HalfLength
        {
            get {
                return (dimensions[0] + 1) / 2;
            }
        }
        public int Length {
            get {
                return dimensions[0];
            }
            set {
                dimensions[0] = value;
                Init(new Symmetry[] { Vertical, Horizontal, Diagonal });
            }
        }

        public GoShape() : base(new int[] { })
        {

        }
        public GoShape(int length) : base(new int[] { length })
        {
            Init(new Symmetry[] {
                Vertical, Horizontal, Diagonal
            });
        }

        public GoPosition At(int x, int y)
        {
            return new GoPosition(x, y, this);
        }
        public override Position At(int onedim)
        {
            return new GoPosition(onedim, this);
        }
        public GoPosition Passed()
        {
            return new GoPosition(Size - 1, this);
        }

        public bool Valid(int x, int y)
        {
            return x >= 0 && y >= 0 && x < Length && y < Length;
        }
        public override bool Valid(int index)
        {
            return index >= 0 && index < Size;
        }
        
        public override Func<Position, Position>[] Symmetries
        {
            get {
                return new Func<Position, Position>[] {
                    x => x,
                    x => Vertical(x),
                    x => Horizontal(x),
                    x => Diagonal(x),
                    x => Horizontal(Vertical(x)),
                    x => Horizontal(Diagonal(x)),
                    x => Vertical(Diagonal(x)),
                    x => Diagonal(Horizontal(Vertical(x)))
                };
            }
        }
        public Position Vertical(Position p)
        {
            GoPosition pos = (GoPosition) p;
            if (pos.Passed)
                return Passed();
            return At(Length - 1 - pos.X, pos.Y);
        }
        public Position Horizontal(Position p)
        {
            GoPosition pos = (GoPosition) p;
            if (pos.Passed)
                return Passed();
            return At(pos.X, Length - 1 - pos.Y);
        }
        public Position Diagonal(Position p)
        {
            GoPosition pos = (GoPosition) p;
            if (pos.Passed)
                return Passed();
            return At(pos.Y, pos.X);
        }
    }

    public class GoPosition : Position
    {
        private GoShape Shape => (GoShape) shape;
        public GoPosition(int onedim, GoShape shape) : base(onedim, shape)
        {
        }
        
        public GoPosition(int x, int y, GoShape shape) : base(shape)
        {
            X = x;
            Y = y;
        }

        public int X
        {
            get {
                if (!Passed)
                    return index % Shape.Length;
                else
                    return 0;
            }
            set {
                if (Shape.Valid(value, Y))
                    index = index - index % Shape.Length + value;
            }
        }
        public int Y
        {
            get {
                if (!Passed)
                    return index / Shape.Length;
                else
                    return 0;
            }
            set {
                if (Shape.Valid(X, value))
                    index = index % Shape.Length + value * Shape.Length;
            }
        }

        public bool Passed
        {
            get {
                return (index == PassedIndex());
            }
            set {
                index = PassedIndex();
            }
        }
        private int PassedIndex()
        {
            return Shape.Length * Shape.Length;
        }

        public override int Range
        {
            get {
                return Shape.Length * Shape.Length + 1;
            }
        }
    }
}


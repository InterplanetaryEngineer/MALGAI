using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;

namespace GameBase
{
    public abstract class Game
    {
        protected Shape shape;
        public virtual Shape Shape { get { return shape; } }
        protected Color[] board;
        public Color[] State { get { return board; } }

        protected Game(Shape dimensions)
        {
            shape = dimensions;
            board = new Color[shape.Size];
            Reset();
        }

        public virtual void Reset()
        {
            for (int i = 0; i < board.Length; i++) {
                board[i] = Color.Empty;
            }
        }
        public Color this[Position pos]
        {
            get {
                return board[pos.Index];
            }
            set {
                board[pos.Index] = value;
            }
        }
        public abstract string Print();

        public abstract void Play(Position move, Color col);
        public abstract bool IsLegal(Position move, Color col);
        public abstract bool Ended(); 

        public abstract int[] Result();
    }

    public abstract class Player
    {
        public NNet net;
        public Color color;

        public Player() { }

        public Player(Color c)
        {
            color = c;
        }
        public void CloneTo(Player other)
        {
            net.CloneTo(other.net);
        }

        public abstract Position Play();
    }

    public abstract class NNet
    {
        public Layer[] layers;

        public NNet()
        {

        }
        public NNet(string filefrom)
        {
            //XmlSerializer reader = new XmlSerializer(typeof(Layer[]));
            BinaryFormatter reader = new BinaryFormatter();

            using (FileStream fs = File.Open(filefrom, FileMode.Open)) {
                layers = (Layer[]) reader.Deserialize(fs);
            }
            Console.WriteLine(filefrom);
            Console.WriteLine(layers[0]._matrix[0][0]);
        }
        public void Reset()
        {
            Parallel.ForEach(layers, 
                (layer) => { layer.Reset(); });
        }
        public void Reset(float magnitude)
        {
            Parallel.ForEach(layers,
                (layer) => { layer.Reset(magnitude); });
        }
        public void CloneTo(NNet destination)
        {
            for (int l = 0; l < layers.Length; l++) {
                layers[l].CloneTo(destination.layers[l]);
            }
        }
        public virtual void Save(string fileto, List<Type> extratypes)
        {
            //extratypes.AddRange(new Type[] { typeof(Shape) });
            //XmlSerializer writer = new XmlSerializer(typeof(Layer[]), extratypes.ToArray());
            BinaryFormatter writer = new BinaryFormatter();

            using (FileStream fs = File.Create(fileto)) {
                writer.Serialize(fs, layers);
            }
        }

        public virtual float[] Compute(float[] data)
        {
            for (int i = 0; i < layers.Length; i++) {
                data = layers[i].Compute(data);
            }
            return data;
        }
        
        public void Add(NNet summand)
        {
            for (int l = 0; l < summand.layers.Length; l++) {
                layers[l].Add(summand.layers[l]);
            }
        }
        public void Mult(float factor)
        {
            for (int l = 0; l < layers.Length; l++) {
                layers[l].Mult(factor);
            }
        }
        public float Magnitude
        {
            get {
                float sum = 0;
                for (int l = 0; l < layers.Length; l++) {
                    sum += layers[l].SquareSize;
                }
                return (float) Math.Sqrt(sum);
            }
            set {
                Mult((1 / Magnitude) * value);
            }
        }
    }
    
    
    [Serializable]
    public class Layer
    {
        public Shape _input;
        public Shape _shape;
        public float[][] _matrix;
        protected Random random = new Random();

        public Layer()
        {

        }
        public Layer(Shape input, Shape shape)
        {
            _input = input;
            _shape = shape;
            _matrix = new float[shape.Size][];

            for(int i = 0; i < _matrix.Length; i++) {
                _matrix[i] = new float[_input.Size + 1];
            }
            Reset(1);
        }
        public void Reset()
        {
            float[] weights = new float[_input.Size + 1];
            weights.Initialize();

            for (int i = 0; i < _shape.Needed.Length; i++) {
                Position n = _shape.At(_shape.Needed[i]);
                SetNeuron(n, weights);
            }
        }
        public void Reset(float magnitude)
        {
            for (int i = 0; i < _shape.Needed.Length; i++) {
                Position n = _shape.At(_shape.Needed[i]);
                float[] weights = new float[_input.Size + 1];

                for (int w = 0; w < weights.Length; w++) {
                    weights[w] = magnitude;//(float) random.NextDouble() * magnitude * 2 - magnitude;
                }

                SetNeuron(n, weights);
            }
        }
        public void SetNeuron(Position n, float[] weights)
        {
            for (int transform = 0; transform < _shape.Symmetries.Length; transform++) {
                Position neuron = _shape.Symmetries[transform](n);

                for (int w = 0; w < weights.Length; w++) {
                    Position tmp = _input.At(w);
                    Position weight = _input.Symmetries[transform](tmp);
                    _matrix[neuron.Index][weight.Index] = weights[w];
                }
            }
        }
        public void CloneTo(Layer destination)
        {
            for (int i = 0; i < _matrix.Length; i++) {
                destination._matrix[i] = (float[])_matrix[i].Clone();
            }
        }
        
        public float[] Compute(float[] data)
        {
            float[] result = new float[_shape.Size];

            Parallel.For(0, result.Length,
                (n) => { result[n] = CompNeuron(n, data); });
            return result;
        }
        private float CompNeuron(int n, float[] data)
        {
            float sum = _matrix[n][_input.Size]; //bias
            for (int w = 0; w < _input.Size; w++) {
                sum += data[w] * _matrix[n][w];
            }
            return Activation(sum);
        }
        private float Activation(float x)
        {
            if (x > 0)
                return (float) Math.Sqrt(x);
            return (float) -Math.Sqrt(-x);
            /*
            return Math.Tanh(x);

            if (x > 0)
                return 2 - 1 / (Math.Abs(x) + 1);
            return 1 / (Math.Abs(x) + 1);

            if (x > 0)
                return (float) Math.Sqrt(x);
            return (float) -Math.Sqrt(-x);
            */
        }

        public void Add(Layer summand)
        {
            for (int n = 0; n < _matrix.Length ; n++) {
                for (int w = 0; w < _matrix[n].Length; w++) {
                    _matrix[n][w] += summand._matrix[n][w];
                }
            }
        }
        public void Mult(float factor)
        {
            for (int n = 0; n < _matrix.Length; n++) {
                for (int w = 0; w < _matrix[n].Length; w++) {
                    _matrix[n][w] *= factor;
                }
            }
        }
        public float SquareSize
        {
            get {
                float sum = 0;
                foreach (float[] neuron in _matrix) {
                    foreach (float weight in neuron) {
                        sum += weight * weight;
                    }
                }
                return sum;
            }
        }
    }
    
    [Serializable]
    public abstract class Shape
    {
        public abstract int Size
        {
            get;
            set;
        }
        public int[] Dimensions { get => dimensions; }
        public int[] Needed;
        protected int[] dimensions;
        
        public delegate Position Symmetry(Position pos);
        
        public Shape(int[] lengths)
        {
            dimensions = lengths;
        }
        public void Init(Symmetry[] symmetries)
        {
            bool[] visited = new bool[Size];
            HashSet<int> runningTotal = new HashSet<int>();

            for (int i = 0; i < Size; i++) {
                foreach(var symmetry in Symmetries) {
                    int p = symmetry(At(i)).Index;

                    if (!visited[p]) {
                        visited[p] = true;
                        runningTotal.Add(i);
                    }
                }
            }
            Needed = new int[runningTotal.Count];
            runningTotal.CopyTo(Needed);
        }
        public abstract Func<Position, Position>[] Symmetries { get; }

        public abstract Position At(int onedim);
        public abstract bool Valid(int index);
    }

    public abstract class Position
    {
        protected int index;
        protected Shape shape;
        
        public Position(Shape shape)
        {
            this.shape = shape;
        }
        
        public Position(int index, Shape shape)
        {
            this.shape = shape;
            this.index = index;
        }

        public override bool Equals(object obj)
        {
            Position other = obj as Position;
            return other != null && other.index == index;
        }

        public bool Valid()
        {
            return Index >= 0 && Index < Range;
        }

        public int Index
        {
            get {
                return index;
            }
            set {
                if (value >= 0 && value < Range)
                    index = value;
            }
        }
        public abstract int Range { get; }
    }
    
    public enum Color
    {
        Black, White, Empty
    }
}


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MDPyramid1
{
    public class Program
    {
        private static readonly string[] COLUMN_SEPARATORS = new[] { " ", "\t" }; // column separating chars in input
        private const string INPUT_FILE = @"
215
192 124
117 269 442
218 836 347 235
320 805 522 417 345
229 601 728 835 133 124
248 202 277 433 207 263 257
359 464 504 528 516 716 871 182
461 441 426 656 863 560 380 171 923
381 348 573 533 448 632 387 176 975 449
223 711 445 645 245 543 931 532 937 541 444
330 131 333 928 376 733 017 778 839 168 197 197
131 171 522 137 217 224 291 413 528 520 227 229 928
223 626 034 683 839 052 627 310 713 999 629 817 410 121
924 622 911 233 325 139 721 218 253 223 107 233 230 124 233
";
        /// <summary>
        /// Used to mark best (or impossible) choice from every node
        /// </summary>
        public enum ChoiceType : byte
        {
            NotCheckedYet = 0,
            Below,
            DiagonalRight,
            Impossible
        }

        public class Node
        {
            public int TheValue { get; set; }
            public Node NodeBelow { get; set; }
            public Node NodeDiagonalToTheRight { get; set; }
            public ChoiceType BestChoice { get; set; }
            public long? BestSum { get; set; }

            /// <summary>
            /// Creates new node without childen with BestSum = null, BestChoice = NotCheckedYet
            /// </summary>
            /// <param name="theValue">Integer value of the node itself</param>
            public Node(int theValue) { TheValue = theValue; }
        }

        public class IncorrectInputStringException : Exception
        {
            //public IncorrectInputStringException() { }
            public IncorrectInputStringException(string message) : base(message) { }
            public IncorrectInputStringException(string message, Exception inner) : base(message, inner) { }
            protected IncorrectInputStringException(
              System.Runtime.Serialization.SerializationInfo info,
              System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
        }

        /// <summary>
        /// Used to skip empty lines in the TextReader (you can use StringReader to read from string or StreamReader to read from file)
        /// </summary>
        /// <returns>Next non-empty line</returns>
        public static string NextNonEmptyLine(TextReader r)
        {
            var result = r.ReadLine();
            return (null == result || !string.IsNullOrWhiteSpace(result)) ? result : NextNonEmptyLine(r);
        }

        /// <summary>
        /// Given an input string, splits it by separators (ignores several separators in a row) and then parses integers
        /// and creates Node(int) from those integers. Also checks if the count of those integers is as expected.
        /// </summary>
        /// <param name="row">input string</param>
        /// <param name="columnSeparators">what characters are used to separate columns</param>
        /// <param name="expectedCount">how many columns should there be (throws IncorrectInputStringException on mismatch)</param>
        public static Node[] ParseRowOfNodes(string row, string[] columnSeparators, int expectedCount)
        {
            string[] parts = row.Split(columnSeparators, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != expectedCount)
                throw new IncorrectInputStringException($"Expected {expectedCount} numbers in row '{row}', but got {parts.Length}");
            var result = new Node[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                int num;
                if (!int.TryParse(parts[i], out num))
                    throw new IncorrectInputStringException($"Couldn't parse number {parts[i]} in row '{row}'");
                result[i] = new Node(num);
            }
            return result;
        }

        /// <summary>
        /// Parses the input and returns root Node. Can throw IncorrectInputStringException if the string is empty or in unexpected format.
        /// Expects one more integer in the next row than the previous, starting with one and skipping empty rows.
        /// So every Node has one NodeBelow and one NodeDiagonalToTheRight and next Node's NodeBelow is the same object as current Node's
        /// NodeDiagonalToTheRight.
        /// </summary>
        /// <param name="r">TextReader to read from (can be StringReader to read from string or StreamReader to read from file)</param>
        /// <returns>root Node</returns>
        public static Node ParseInput(TextReader r)
        {
            int expectedColumnCount = 1;
            string str = NextNonEmptyLine(r);
            if (null == str)
                throw new IncorrectInputStringException("The input is empty!");
            Node[] lastRowNodes = ParseRowOfNodes(str, COLUMN_SEPARATORS, expectedColumnCount);
            var root = lastRowNodes[0];

            while (null != (str = NextNonEmptyLine(r)))
            {
                var newRowNodes = ParseRowOfNodes(str, COLUMN_SEPARATORS, ++expectedColumnCount);
                for (int j = 0; j < lastRowNodes.Length; j++)
                {
                    lastRowNodes[j].NodeBelow = newRowNodes[j];
                    lastRowNodes[j].NodeDiagonalToTheRight = newRowNodes[j + 1];
                }
                lastRowNodes = newRowNodes;
            }

            return root;
        }

        /// <summary>
        /// The main recursive method, that updates the tree from current Node with BestChoice and BestSum
        /// </summary>
        /// <param name="n">Node to update from (pass root to update best route from root</param>
        /// <param name="prevValueToValidate">if supplied, will be checked against current Node's integer value
        /// (so there is odd->even->odd->even... sequence)</param>
        /// <returns>best sum from current Node (null, if can't pass odd/even validation)
        /// </returns>
        public static long? CheckChoice(Node n, int? prevValueToValidate = null)
        {
            if (null == n)
                return 0;

            if (prevValueToValidate.HasValue && (0 == ((prevValueToValidate.Value ^ n.TheValue) & 1)))
                return null;

            switch (n.BestChoice)
            {
                case ChoiceType.NotCheckedYet: // let's try both ways and calculate which is best
                    var sumBelow = CheckChoice(n.NodeBelow, n.TheValue);
                    var sumDiag = CheckChoice(n.NodeDiagonalToTheRight, n.TheValue);

                    if (!sumBelow.HasValue && !sumDiag.HasValue)
                    {
                        n.BestChoice = ChoiceType.Impossible;
                        return n.BestSum = null;
                    }
                    else if (!sumDiag.HasValue || sumBelow >= sumDiag)
                    {
                        n.BestChoice = ChoiceType.Below;
                        return n.BestSum = sumBelow + n.TheValue;
                    }
                    else
                    {
                        n.BestChoice = ChoiceType.DiagonalRight;
                        return n.BestSum = sumDiag + n.TheValue;
                    }
                case ChoiceType.Below:
                case ChoiceType.DiagonalRight:
                    return n.BestSum; // we already know where to go and best sum, so just return it
                case ChoiceType.Impossible:
                    return null;
                default:
                    throw new ApplicationException($"Uknown node's status: {n.BestChoice}");
            }
        }

        /// <summary>
        /// Recursively write to console values of Nodes by traveling through BestChoice indications
        /// </summary>
        /// <param name="n">Node to start (root)</param>
        /// <param name="first">If false, writes ', ' before the value</param>
        public static void ConsoleWriteChoice(Node n, bool first = true)
        {
            if (null == n)
                return;
            if (!first)
                Console.Write(", ");
            Console.Write(n.TheValue);
            switch (n.BestChoice)
            {
                case ChoiceType.Below:
                    ConsoleWriteChoice(n.NodeBelow, false);
                    break;
                case ChoiceType.DiagonalRight:
                    ConsoleWriteChoice(n.NodeDiagonalToTheRight, false);
                    break;
                case ChoiceType.NotCheckedYet:
                case ChoiceType.Impossible:
                default:
                    throw new ApplicationException($"You can't output choice, when some node's status is {n.BestChoice}");
            }
        }

        /// <summary>
        /// Parses input from const string INPUT_FILE, then runs CheckChoice for root, which searches best route and update Nodes accordingly.
        /// Then, if successful in finding best route, ouputs the sum and prints the path recursively.
        /// 
        /// Possible improvements:
        /// 1) Make sure the data types are correct (maybe 'int' is not enough for numbers, maybe it should be 'double', 'decimal' or something else.
        /// And is 'long' for sum enough.
        /// 2) If better speed is required and there are a lot of rows in the input, we can use more cores: if we check null and odd->even->odd->even
        /// sequence before calling CheckChoice() recusively and make sure both paths look promising, then we can schedule on of the paths into some
        /// job collection (i.e. BlockingCollection), and enter another path ourselves and, after we finish it, check for results of the other one or
        /// take some job ourselves. And there should be a few (i.e. depending on number of cores) threads waiting for those jobs and using CheckChoice.
        /// 3) If speed or RAM are still an issue, we could reduce the code readibility and rewrite recursion with "while"s and maybe even change Node
        /// objects to corresponding arrays.
        /// 4) On the other hand, if we need not speed, but flexibility, we could have arbitrary number of children (instead of two). We could also let
        /// user supply validation rules in some form or language (in this case odd->even->odd->even) etc.
        /// 
        /// P.S.: I would ask those questions before solving this problem, but it's weekend, so I just mentioned those things here and developed
        /// some verion of it :)
        /// </summary>
        static void Main(string[] args)
        {
            Node root;
            try
            {
                if (1 == args.Length && File.Exists(args[0]))
                {
                    Console.WriteLine($"Using input from file {args[0]}");
                    using (var fr = new StreamReader(args[0]))
                        root = ParseInput(fr);
                }
                else
                {
                    Console.WriteLine($"Using input from constant string. If you want to use file, then enter it's name as the first argument. "
                        + $"Constant value:\n{INPUT_FILE}");
                    using (var sr = new StringReader(INPUT_FILE))
                        root = ParseInput(sr);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Couldn't parse input: {ex}");
                Console.ReadKey();
                return;
            }

            var sum = CheckChoice(root);
            if (!sum.HasValue)
            {
                Console.WriteLine("It's impossible to reach a goal with these numbers!");
            }
            else
            {
                Console.WriteLine($"Max sum: {sum.Value}");
                Console.Write("Path: ");
                ConsoleWriteChoice(root);
                Console.WriteLine();
            }

            Console.WriteLine("Press any key to quit...");
            Console.ReadKey();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace LexicalAnalyzer
{
    public class NFA : FA
    {
        public NFA(IEnumerable<int> S, IEnumerable<char> Sigma, IEnumerable<(int s1, char c, int s2)> MappingTable, IEnumerable<int> S_0, IEnumerable<int> Z)
            : base(S, Sigma, MappingTable, Z)
        {
            _S_0 = S_0.ToHashSet();
        }

        public NFA(FA fa, IEnumerable<int> S_0) : this(fa.S, fa.Sigma, fa.MappingTable, S_0, fa.Z) { }

        protected HashSet<int> _S_0;

        public IEnumerable<int> S_0 { get => _S_0.AsEnumerable(); }

        public void Save(BinaryWriter bw)
        {
            var maxState = S.Max();
            bw.Write(maxState);
            var stateWriter = new Writer(bw, maxState);

            var s0_size = _S_0.Count();
            bw.Write(s0_size);
            for (int j = 0; j < s0_size; j++)
                stateWriter.Write(_S_0.ElementAt(j));
            var z_size = _Z.Count();
            bw.Write(z_size);
            for (int j = 0; j < z_size; j++)
                stateWriter.Write(_Z.ElementAt(j));
            var table_size = _MappingTable.Count();
            bw.Write(table_size);
            for (int j = 0; j < table_size; j++)
            {
                var item = _MappingTable.ElementAt(j);
                stateWriter.Write(item.s1);
                bw.Write(item.c);
                stateWriter.Write(item.s2);
            }
        }

        public static NFA Load(BinaryReader br)
        {
            var maxState = br.ReadInt32();
            var stateReader = new Reader(br, maxState);

            var s0_size = br.ReadInt32();
            var s0 = new int[s0_size];
            for (int i = 0; i < s0_size; i++)
                s0[i] = stateReader.Read();
            var z_size = br.ReadInt32();
            var z = new int[z_size];
            for (int i = 0; i < z_size; i++)
                z[i] = stateReader.Read();
            var table_size = br.ReadInt32();
            var table = new (int s1, char c, int s2)[table_size];
            for (int i = 0; i < table_size; i++)
            {
                table[i] = (stateReader.Read(), br.ReadChar(), stateReader.Read());
            }
            return new(table.Select(i => i.s1).Union(table.Select(i => i.s2)),
                table.Select(i => i.c).Where(c => c != FA.CHAR_Epsilon),
                table, s0, z);
        }

        //{t}
        public static NFA CreateFrom(char c)
        {
            //S
            var S = new HashSet<int>();
            int id = 0;
            int head = id;
            S.Add(head);
            id = S.Count;
            int tail = id;
            S.Add(tail);

            //Sigma
            var Sigma = new HashSet<char>();
            Sigma.Add(c);

            //Mapping
            var MappingTable = new HashSet<(int s1, char c, int s2)>();
            MappingTable.Add((head, c, tail));

            //Z
            var Z = new HashSet<int>();
            Z.Add(tail);

            //S_0
            var S_0 = new HashSet<int>();
            S_0.Add(head);

            return new(S, Sigma, MappingTable, S_0, Z);
        }

        public static NFA CreateFrom(params char[] chars)
        {
            NFA dig = CreateEpsilon();
            foreach (var c in chars)
            {
                dig = dig.Or(CreateFrom(c));
            }
            return dig;
        }
        public static NFA[] CreateArrayFrom(params char[] chars)
        {
            NFA[] arrNFA = new NFA[chars.Length];
            for(int i = 0; i < arrNFA.Length; i++)
            {
                arrNFA[i] = NFA.CreateFrom(chars[i]);
            }
            return arrNFA;
        }

        public static NFA CreateEpsilon()
        {
            //S
            var S = new HashSet<int>();
            int id = 0;
            int single = id;
            S.Add(single);

            //Sigma
            var Sigma = new HashSet<char>();

            //Mapping
            var MappingTable = new HashSet<(int s1, char c, int s2)>();

            //Z
            var Z = new HashSet<int>();
            Z.Add(single);

            //S_0
            var S_0 = new HashSet<int>();
            S_0.Add(single);

            return new(S, Sigma, MappingTable, S_0, Z);
        }

        protected override void S0ToString(StringBuilder builder)
        {
            builder.Append(PRE).Append($"S0 : {{");
            foreach (var s in S_0)
            {
                builder.Append($" {s},");
            }
            if (S_0.Any())
                builder.Length -= 1;
            builder.Append(" }").AppendLine();
        }

        //[{from}-{to}]
        public static NFA CreateRange(char from, char to)
        {
            Debug.Assert(from <= to);
            NFA dig = CreateFrom(from);
            for (char c = (char)(from + 1); c <= to; c++)
            {
                var newDig = CreateFrom(c);
                dig = dig.Or(newDig);
            }
            return dig;
        }

        public static NFA CreateFromString(string str)
        {
            NFA dig = CreateEpsilon();
            foreach (var c in str)
            {
                dig = dig.Join(CreateFrom(c));
            }
            return dig;
        }

        public static NFA CreateFromString(params string[] arr)
        {
            NFA dig = CreateEpsilon();
            foreach (var str in arr)
            {
                dig = dig.Or(CreateFromString(str));
            }
            return dig;
        }

        class Writer
        {
            private BinaryWriter bw;
            private int maxValue;
            public Writer(BinaryWriter bw, int maxValue)
            {
                this.bw = bw;
                this.maxValue = maxValue;
            }

            public void Write(int value)
            {
                switch (maxValue)
                {
                    case int x when x <= byte.MaxValue:
                        bw.Write((byte)value);
                        break;
                    case int x when x <= UInt16.MaxValue:
                        bw.Write((UInt16)value);
                        break;
                    default:
                        bw.Write(value);
                        break;
                }
            }
        }

        class Reader
        {
            private BinaryReader br;
            private int maxValue;
            public Reader(BinaryReader br, int maxValue)
            {
                this.br = br;
                this.maxValue = maxValue;
            }

            public int Read()
            {
                switch (maxValue)
                {
                    case int x when x <= byte.MaxValue:
                        return br.ReadByte();
                    case int x when x <= UInt16.MaxValue:
                        return br.ReadUInt16();
                    default:
                        return br.Read();
                }
            }
        }
    }

    public static class NFAHelper
    {
        //{fst}{snd}
        public static NFA Join(this NFA fst, NFA snd)
        {
            //S
            var S = new HashSet<int>();
            S.UnionWith(fst.S);
            var mid = S.Count;
            S.Add(mid);
            int snd_base_id = S.Count;
            S.UnionWith(snd.S.Select(s => snd_base_id + s));

            //Sigma
            var Sigma = new HashSet<char>();
            Sigma.UnionWith(fst.Sigma);
            Sigma.UnionWith(snd.Sigma);

            //Mapping
            var MappingTable = new HashSet<(int s1, char c, int s2)>();
            MappingTable.UnionWith(fst.MappingTable);
            MappingTable.UnionWith(snd.MappingTable.Select(i => (snd_base_id + i.s1, i.c, snd_base_id + i.s2)));

            //Z
            var Z = new HashSet<int>();
            Z.UnionWith(snd.Z.Select(z => snd_base_id + z));

            //S_0
            var S_0 = new HashSet<int>();
            S_0.UnionWith(fst.S_0);

            //Join
            MappingTable.UnionWith(fst.Z.Select(z => (z, FA.CHAR_Epsilon, mid))); // fst.Z --eps-> mid
            MappingTable.UnionWith(snd.S_0.Select(s => (mid, FA.CHAR_Epsilon, snd_base_id + s))); // mid --eps-> snd.S_0

            return new(S, Sigma, MappingTable, S_0, Z);
        }

        //{fst}|{snd}
        public static NFA Or(this NFA fst, NFA snd)
        {
            //S
            var S = new HashSet<int>();
            //int head = 0;
            //S.Add(head);
            int fst_base_id = S.Count;
            S.UnionWith(fst.S.Select(s => fst_base_id + s));
            int snd_base_id = S.Count;
            S.UnionWith(snd.S.Select(s => snd_base_id + s));
            //int tail = S.Count;
            //S.Add(tail);

            //Sigma
            var Sigma = new HashSet<char>();
            Sigma.UnionWith(fst.Sigma);
            Sigma.UnionWith(snd.Sigma);

            //Mapping
            var MappingTable = new HashSet<(int s1, char c, int s2)>();
            MappingTable.UnionWith(fst.MappingTable.Select(i => (fst_base_id + i.s1, i.c, fst_base_id + i.s2)));
            MappingTable.UnionWith(snd.MappingTable.Select(i => (snd_base_id + i.s1, i.c, snd_base_id + i.s2)));

            //Z
            var Z = new HashSet<int>();
            //Z.Add(tail);
            Z.UnionWith(fst.Z.Select(z => fst_base_id + z));
            Z.UnionWith(snd.Z.Select(z => snd_base_id + z));

            //S_0
            var S_0 = new HashSet<int>();
            //S_0.Add(head);
            S_0.UnionWith(fst.S_0.Select(s => fst_base_id + s));
            S_0.UnionWith(snd.S_0.Select(s => snd_base_id + s));

            //Or
            //MappingTable.UnionWith(fst.S_0.Select(s => (head, FA.CHAR_Epsilon, fst_base_id + s)));
            //MappingTable.UnionWith(snd.S_0.Select(s => (head, FA.CHAR_Epsilon, snd_base_id + s)));
            //MappingTable.UnionWith(fst.Z.Select(z => (fst_base_id + z, FA.CHAR_Epsilon, tail)));
            //MappingTable.UnionWith(snd.Z.Select(z => (snd_base_id + z, FA.CHAR_Epsilon, tail)));

            return new(S, Sigma, MappingTable, S_0, Z);
        }

        //{dig}*
        public static NFA Closure(this NFA dig)
        {
            //S
            var S = new HashSet<int>();
            int head = 0;
            S.Add(head);
            int dig_base_id = S.Count;
            S.UnionWith(dig.S.Select(s => dig_base_id + s));
            int tail = S.Count;
            S.Add(tail);

            //Sigma
            var Sigma = new HashSet<char>();
            Sigma.UnionWith(dig.Sigma);

            //Mapping
            var MappingTable = new HashSet<(int s1, char c, int s2)>();
            MappingTable.UnionWith(dig.MappingTable.Select(i => (dig_base_id + i.s1, i.c, dig_base_id + i.s2)));

            //Z
            var Z = new HashSet<int>();
            Z.Add(tail);

            //S_0
            var S_0 = new HashSet<int>();
            S_0.Add(head);

            //Union
            MappingTable.Add((head, FA.CHAR_Epsilon, tail));
            MappingTable.UnionWith(dig.S_0.Select(s => (head, FA.CHAR_Epsilon, dig_base_id + s))); // head --eps-> dig.S_0
            MappingTable.UnionWith(dig.Z.Select(z => (dig_base_id + z, FA.CHAR_Epsilon, tail))); // dig.Z --eps-> tail
            MappingTable.UnionWith(dig.Z.Select(z => (tail, FA.CHAR_Epsilon, head))); // tail --eps-> head
            //MappingTable.UnionWith(dig.Z.SelectMany(z => dig.S_0.Select(s => (dig_base_id + z, FA.CHAR_Epsilon, dig_base_id + s)))); // dig.Z --eps-> dig.S_0

            return new(S, Sigma, MappingTable, S_0, Z);
        }
    }
}

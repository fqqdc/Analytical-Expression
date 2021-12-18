using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analytical_Expression
{
    public abstract class PDA
    {
        public record Mapping(int q1, Symbol z, Terminal a,  int q2, IEnumerable<Symbol> gamma);
        class MappingComparer : IEqualityComparer<Mapping>
        {
            public static IEqualityComparer<Mapping> Default { get; private set; }
            static MappingComparer()
            {
                Default = new MappingComparer();
            }

            bool IEqualityComparer<Mapping>.Equals(Mapping? x, Mapping? y)
            {
                if (x.q1 != y.q1) return false;
                if (x.a != y.a) return false;
                if (x.z != y.z) return false;
                if (x.q2 != y.q2) return false;
                var x_gamma = x.gamma.ToArray();
                var y_gamma = y.gamma.ToArray();
                if (x_gamma.Length != y_gamma.Length) return false;
                for (int i = 0; i < x_gamma.Length; i++)
                {
                    if (x_gamma[i] != y_gamma[i]) return false;
                }
                return true;
            }

            int IEqualityComparer<Mapping>.GetHashCode(Mapping obj)
            {
                return 0;
            }
        }

        public PDA(IEnumerable<int> Q, IEnumerable<Terminal> Sigma, IEnumerable<Mapping> Delta, int q_0, Symbol z_0, IEnumerable<int> F)
        {
            this._Q.UnionWith(Q);
            this._Sigma.UnionWith(Sigma);
            this._Delta.UnionWith(Delta);
            this.Q_0 = q_0;
            this.Z_0 = z_0;
            this._F.UnionWith(F);
        }

        protected HashSet<int> _Q = new();
        protected HashSet<Terminal> _Sigma = new();
        protected HashSet<Mapping> _Delta = new(MappingComparer.Default);
        protected HashSet<int> _F = new();


        /// <summary>
        /// 控制器的有限状态集
        /// </summary>
        public IEnumerable<int> Q { get => _Q.AsEnumerable(); }
        /// <summary>
        /// 下推栈内字母表
        /// </summary>
        public IEnumerable<Terminal> Sigma { get => _Sigma.AsEnumerable(); }
        /// <summary>
        /// 有限子集映射
        /// </summary>
        public IEnumerable<Mapping> Delta { get => _Delta.AsEnumerable(); }
        /// <summary>
        /// 控制器的初始状态
        /// </summary>
        public int Q_0 { get; protected set; }
        /// <summary>
        /// 下推栈的栈初始符
        /// </summary>
        public Symbol Z_0 { get; protected set; }
        /// <summary>
        /// 控制器的终态集
        /// </summary>
        public IEnumerable<int> F { get => _F.AsEnumerable(); }

        #region ToString
        protected const string PRE = "    ";
        public override string ToString()
        {
            StringBuilder builder = new();
            NameToString(builder);
            builder.Append("{").AppendLine();
            QToString(builder);
            SigmaToString(builder);
            MappingToString(builder);
            Q0ToString(builder);
            Z0ToString(builder);
            FToString(builder);
            builder.Append("}").AppendLine();
            return builder.ToString();
        }
        protected virtual void NameToString(StringBuilder builder)
        {
            builder.Append(this.GetType().Name).AppendLine();
        }
        protected virtual void QToString(StringBuilder builder)
        {
            builder.Append(PRE).Append($"Q : {{");
            foreach (var s in Q)
            {
                builder.Append($" {s},");
            }
            builder.Length -= 1;
            builder.Append(" }").AppendLine();
        }
        protected virtual void SigmaToString(StringBuilder builder)
        {
            builder.Append(PRE).Append($"Sigma : {{");
            foreach (var t in Sigma)
            {
                builder.Append($" {t},");
            }
            builder.Length -= 1;
            builder.Append(" }").AppendLine();
        }
        protected virtual void MappingToString(StringBuilder builder)
        {
            builder.Append(PRE).Append("Mapping :").AppendLine();
            builder.Append(PRE).Append("{").AppendLine();
            foreach (var pGroup in Delta.GroupBy(i => (i.q1)).OrderBy(g => g.Key))
            {
                foreach (var p in pGroup.OrderBy(p => p.z.Name).ThenBy(p => p.a.Name).ThenBy(p => p.q2))
                {
                    builder.Append(PRE).Append(PRE)
                        .AppendLine($"( {p.q1}, {p.z}, {p.a} ) = ( {p.q2}, {string.Concat(p.gamma)} )");
                }
            }
            builder.Append(PRE).Append("}").AppendLine();
        }
        protected virtual void Q0ToString(StringBuilder builder)
        {
            builder.Append(PRE).Append($"Q_0 : {Q_0}").AppendLine();
        }
        protected virtual void Z0ToString(StringBuilder builder)
        {
            builder.Append(PRE).Append($"Z_0 : {Z_0}").AppendLine();
        }
        protected virtual void FToString(StringBuilder builder)
        {
            builder.Append(PRE).Append($"F : {{");
            foreach (var s in F)
            {
                builder.Append($" {s},");
            }
            if (F.Count() > 0)
                builder.Length -= 1;
            builder.Append(" }").AppendLine();
        }
        #endregion ToString
    }
}

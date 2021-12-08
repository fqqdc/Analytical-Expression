using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analytical_Expression
{
    public abstract class PDA
    {
        public record PDAMapping(int q1, Terminal a, Terminal z, int q2, IEnumerable<Symbol> gamma);

        public static Terminal EPSILON = new Terminal("ε");

        public PDA()
        {
            throw new NotImplementedException();
        }

        protected HashSet<int> _Q = new();
        protected HashSet<Terminal> _Sigma = new();
        protected HashSet<PDAMapping> _MappingTable = new();
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
        public IEnumerable<PDAMapping> MappingTable { get => _MappingTable.AsEnumerable(); }
        /// <summary>
        /// 控制器的初始状态
        /// </summary>
        public int Q_0 { get; protected set; }
        /// <summary>
        /// 下推栈的栈初始符
        /// </summary>
        public Terminal Z_0 { get; protected set; }
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
            foreach (var pGroup in MappingTable.GroupBy(i => (i.q1)).OrderBy(g => g.Key))
            {
                builder.Append(PRE).Append(PRE);
                foreach (var p in pGroup.OrderBy(p => p.a.Name).ThenBy(p => p.z.Name).ThenBy(p => p.q2))
                {
                    builder.Append($"({p.q1}, {p.a}, {p.z}) = ({p.q2}, {string.Concat(p.gamma)}), ");
                }
                builder.Length -= 2;
                builder.AppendLine();
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

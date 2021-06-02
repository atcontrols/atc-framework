namespace ATC.Framework
{
    public enum JoinType
    {
        Digital,
        Analog,
        Serial,
    }

    abstract public class Join<T>
    {
        public uint Number { get; private set; }
        public JoinType JoinType { get; private set; }
        internal uint Offset { get; set; }

        /// <summary>
        /// The current value of the join (read only)
        /// </summary>
        public T Value { get; internal set; }

        protected Join(uint number, JoinType joinType)
        {
            Number = number;
            JoinType = joinType;
        }

        public override string ToString()
        {
            return string.Format("{0} (Number: {1}, Offset: {2}, Value: {3})", this.GetType().Name, Number, Offset, Value);
        }
    }
}

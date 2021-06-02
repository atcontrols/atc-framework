namespace ATC.Framework.UserInterface.Standard
{
    public class Page
    {
        #region Static members
        public static Page Start = new Page("Start", StandardJoin.PageStart);
        public static Page Wait = new Page("Wait", StandardJoin.PageWait);
        public static Page Main = new Page("Main", StandardJoin.PageMain);
        public static Page Settings = new Page("Settings", StandardJoin.PageSettings);
        #endregion

        #region Properties
        public string Name { get; private set; }
        public DigitalJoin Join { get; private set; }
        #endregion

        #region Constructor
        public Page(string name, uint joinNumber)
            : this(name, new DigitalJoin(joinNumber))
        {
        }

        public Page(string name, DigitalJoin join)
        {
            Name = name;
            Join = join;
        }
        #endregion

        public override string ToString()
        {
            return Name;
        }
    }
}

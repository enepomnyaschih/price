namespace Price
{
    public class ConvertConfig
    {
        public class GroupConfig
        {
            public string input;
            public string group;
            public string text;
            public string merge;

            public Expression inputExpression;
            public Expression groupExpression;
            public Expression textExpression;
        }

        public GroupConfig[] groups;
    }
}

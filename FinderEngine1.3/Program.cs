    public static class Program
    {
        public static void Main(string[] args)
        {
            using(FinderEngine.Application application = new FinderEngine.Application(800, 600, "demo"))
            {
                application.Start(args);
            }
        }
    }

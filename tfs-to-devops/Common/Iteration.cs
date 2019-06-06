using System;

namespace Common
{
    public class Iteration
    {
        public Iteration(int iterationId, string iterationPath)
        {
            IterationId = iterationId;
            IterationPath = iterationPath;
        }

        public int IterationId { get; }
        public string IterationPath { get; }
}
}

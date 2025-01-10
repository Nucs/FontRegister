using NUnit.Framework;

#if DEBUG //we only use this locally
[assembly: Parallelizable(ParallelScope.Fixtures)]
#endif

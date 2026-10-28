using System;
using System.Collections.Generic;

namespace Uptime.Connections
{
    public enum CleanStartResult { Started, UnknownNode, AlreadyActive }
    public enum CleanSelectionResult { InvalidPid, Success, InnocentProcess, NoSession }

    public sealed class CleanProcess
    {
        internal CleanProcess(int pid, int cpuPercent, string name)
        { Pid = pid; CpuPercent = cpuPercent; Name = name; }
        public int Pid { get; }
        public int CpuPercent { get; }
        public string Name { get; }
    }

    public sealed class CleanSession
    {
        internal CleanSession(int id, NodeResources target, NodeInfection[] startingInfections,
            CleanProcess[] processes, int maliciousPid, int actionSequence)
        {
            Id = id;
            Target = target;
            StartingInfections = startingInfections;
            Processes = Array.AsReadOnly(processes);
            MaliciousPid = maliciousPid;
            ActionSequence = actionSequence;
        }
        public int Id { get; }
        public string NodeId => Target.View.NodeId;
        public IReadOnlyList<CleanProcess> Processes { get; }
        public bool WasHealthy => StartingInfections.Length == 0;
        internal NodeResources Target { get; }
        internal NodeInfection[] StartingInfections { get; }
        internal int MaliciousPid { get; }
        internal int ActionSequence { get; }
    }
}

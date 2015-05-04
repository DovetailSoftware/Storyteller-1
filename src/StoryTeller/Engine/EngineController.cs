﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FubuCore;
using StoryTeller.Engine.UserInterface;
using StoryTeller.Messages;
using StoryTeller.Model;
using StoryTeller.Model.Persistence;
using StoryTeller.Remotes.Messaging;

namespace StoryTeller.Engine
{
    public class EngineController : IResultObserver,
        IListener<RunSpec>,
        IListener<RunSpecs>,
        IListener<CancelSpec>,
        IListener<CancelAllSpecs>
    {
        private readonly ISpecificationEngine _engine;
        private readonly IUserInterfaceObserver _observer;
        private readonly ISpecRunner _runner;

        private readonly IList<SpecExecutionRequest> _outstanding = new List<SpecExecutionRequest>();


        public EngineController(ISpecificationEngine engine, IUserInterfaceObserver observer, ISpecRunner runner)
        {
            _engine = engine;
            _observer = observer;
            _runner = runner;
        }

        public IEnumerable<SpecExecutionRequest> OutstandingRequests()
        {
            return _outstanding.ToArray();
        } 

        public void Receive(RunSpec message)
        {
            RunSpec(message.id, message.spec);
            SendQueueState();
        }

        public virtual void RunSpec(string id, Specification specification)
        {
            if (OutstandingRequests().Any(x => x.Node.id == id)) return;

            var spec = specification.ToNode();
            if (spec == null) return;

            var request = new SpecExecutionRequest(spec, this, specification);
            _outstanding.Add(request);

            _engine.Enqueue(request);
        }

        void IResultObserver.Handle<T>(T message)
        {
            _observer.SendToClient(message);
        }

        public void SpecExecutionFinished(SpecNode node, SpecResults results)
        {
            var outstanding = _outstanding.FirstOrDefault(x => Equals(x.Node, node));
            if (outstanding == null) return;

            _outstanding.Remove(outstanding);

            _observer.SendToClient(new SpecExecutionCompleted(node.id, results));

            SendQueueState();
        }

        public void Receive(CancelSpec message)
        {
            CancelSpec(message.id);
            SendQueueState();
        }

        public virtual void CancelSpec(string id)
        {
            _outstanding.Where(x => x.Node.id == id).Each(x => x.Cancel());
            _outstanding.RemoveAll(x => x.Node.id == id);

            _engine.CancelRunningSpec(id);

        }

        public void Receive(CancelAllSpecs message)
        {
            _engine.CancelRunningSpec(null);

            OutstandingRequests().Each(x => x.Cancel());

            _outstanding.Clear();
            SendQueueState();
        }

        public void Receive(RunSpecs message)
        {
            message.specs.Each(x => RunSpec(x.id, x));
            SendQueueState();
        }

        public QueueState QueueState()
        {
            var running = _runner.RunningSpecId();
            return new QueueState
            {
                queued = _outstanding.Where(x => x.Node.id != running).Select(x => x.Node.id).ToArray(),
                running = running
            };
        }

        public void SendQueueState()
        {
            _observer.SendToClient(QueueState());
        }
    }
}
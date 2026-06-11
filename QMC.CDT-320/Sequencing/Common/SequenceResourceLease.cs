using System;

namespace QMC.CDT320.Sequencing
{
    public sealed class SequenceResourceLease : IDisposable
    {
        private readonly SequenceResourceManager _owner;
        private readonly SequenceResourceKind _resource;
        private bool _disposed;

        internal SequenceResourceLease(SequenceResourceManager owner, SequenceResourceKind resource, string holder)
        {
            _owner = owner;
            _resource = resource;
            Holder = holder ?? "";
        }

        public SequenceResourceKind Resource
        {
            get { return _resource; }
        }

        public string Holder { get; private set; }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            if (_owner != null)
                _owner.Release(_resource, Holder);
        }
    }
}
